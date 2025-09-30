using Phidget22;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Phidgets2Prosim.ControlMath;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Shared tuning/state + helpers for Phidgets motors (BLDC/DC).
    /// - Holds MotorTuningOptions (applied in ctor).
    /// - Reusable PID-ish mapping (error -> velocity).
    /// - Low-pass filter, hysteresis, slew-rate limiting.
    /// - Optional VoltageInput hookup for DC-style "chase a voltage".
    /// Subclasses only need to call the helpers and implement how to apply velocity.
    /// </summary>
    internal abstract class MotorBase : PhidgetDevice
    {
        // ===== Optional VoltageInput feedback (used by DC; harmless for BLDC) =====
        public int TargetVoltageInputHub { get; set; } = -1;
        public int TargetVoltageInputPort { get; set; } = -1;
        public int TargetVoltageInputChannel { get; set; } = 0;
        protected VoltageInput _vin;
        protected double _feedbackVoltage = 0.0;
        protected double _filteredVoltage = 0.0;

        // ===== Tuning (defaults match your prior working values; override via options) =====
        protected double MaxVelocity = 0.25;
        protected double MinVelocity = 0.035;
        protected double VelocityBand = 0.50;        // "full-speed" band (units = feedback units)
        protected double CurveGamma = 0.65;          // error shaping
        protected double DeadbandEnter = 0.01;       // settle threshold
        protected double DeadbandExit = 0.02;       // re-activate threshold
        protected double MaxVelStepPerTick = 0.01;   // slew-per-tick
        protected double Kp = 0.0, Ki = 0.0, Kd = 0.0;
        protected double IOnBand = 1;
        protected double IntegralLimit = 0.25;
        protected double PositionFilterAlpha = 0.25; // 0..1 (more = heavier weighting of newest sample)
        protected int TickMs = 20;

        // ===== Control state shared by loops =====
        protected double _velCmd = 0.0;
        protected double _integral = 0.0;
        protected double _lastError = 0.0;
        protected bool _isSettled = true;

        protected MotorBase(MotorTuningOptions options = null)
        {
            ApplyTuning(options);
        }

        protected void ApplyTuning(MotorTuningOptions options)
        {
            if (options == null) return;
            if (options.MaxVelocity.HasValue) MaxVelocity = options.MaxVelocity.Value;
            if (options.MinVelocity.HasValue) MinVelocity = options.MinVelocity.Value;
            if (options.VelocityBand.HasValue) VelocityBand = options.VelocityBand.Value;
            if (options.CurveGamma.HasValue) CurveGamma = options.CurveGamma.Value;
            if (options.DeadbandEnter.HasValue) DeadbandEnter = options.DeadbandEnter.Value;
            if (options.DeadbandExit.HasValue) DeadbandExit = options.DeadbandExit.Value;
            if (options.MaxVelStepPerTick.HasValue) MaxVelStepPerTick = options.MaxVelStepPerTick.Value;
            if (options.Kp.HasValue) Kp = options.Kp.Value;
            if (options.Ki.HasValue) Ki = options.Ki.Value;
            if (options.Kd.HasValue) Kd = options.Kd.Value;
			if (options.IOnBand.HasValue) IOnBand = options.IOnBand.Value;
			if (options.IntegralLimit.HasValue) IntegralLimit = options.IntegralLimit.Value;
            if (options.PositionFilterAlpha.HasValue) PositionFilterAlpha = options.PositionFilterAlpha.Value;
            if (options.TickMs.HasValue) TickMs = options.TickMs.Value;
        }

		private IScalarSource _ExternalTarget;                  //#######

		public void UseExternalTarget(IScalarSource source)
		{
			if (_ExternalTarget != null)
				_ExternalTarget.ValueChanged -= OnExternalTargetChanged;

			_ExternalTarget = source;

			if (_ExternalTarget != null)
			{
				_ExternalTarget.ValueChanged += OnExternalTargetChanged;

				// Seed values
				var v = _ExternalTarget.CurrentValue;
				_feedbackVoltage = v;
				if (_filteredVoltage == 0.0) _filteredVoltage = v;
			}
		}

		private void OnExternalTargetChanged(double v)
		{
			_feedbackVoltage = v;
			if (_filteredVoltage == 0.0) _filteredVoltage = v;
		}                                                       //######
        // You may keep your existing AttachTargetVoltageInput() during transition;
		// once everything binds via UseExternalTarget(), you can remove it.


		// ---- Sensor helpers
		protected double LowPass(double prevFiltered, double sample)
        {
            return PositionFilterAlpha * sample + (1.0 - PositionFilterAlpha) * prevFiltered;
        }

        protected void UpdateHysteresis(double absError)
        {
            if (_isSettled)
            {
                if (absError > DeadbandExit) _isSettled = false;
            }
            else
            {
                if (absError < DeadbandEnter) _isSettled = true;
            }
        }

        // ---- Control mapping: error (units of feedback) -> velocity [-1..1]
        protected double VelocityFromError(double error, double dt)
        {
            if (_isSettled) return 0.0;

            // base non-linear mapping
            double norm = Math.Min(1.0, Math.Abs(error) / Math.Max(1e-6, VelocityBand));
            double shaped = Math.Pow(norm, CurveGamma);
            double vel = Math.Sign(error) * (MinVelocity + (MaxVelocity - MinVelocity) * shaped);

            // PID embellishments (optional)
            if (Kp != 0.0 || Ki != 0.0 || Kd != 0.0)
            {
                double p = Kp * error;

				// integrate near target to avoid windup
				//if (Math.Abs(error) <= (VelocityBand * 0.5))
				if (Math.Abs(error) <= IOnBand)
				{
                    _integral += Ki * error * dt;
                    _integral = Clamp(_integral, -IntegralLimit, +IntegralLimit);
                }
                else
                {
                    _integral = 0.0;
                }

                double d = 0.0;
                if (Kd != 0.0)
                {
                    d = Kd * ((error - _lastError) / Math.Max(1e-6, dt));
                }

                vel += p + _integral - d;
                _lastError = error;
            }

            return Clamp(vel, -1.0, 1.0);
        }

        protected double SlewTowards(double current, double desired)
        {
            double step = desired - current;
            step = Clamp(step, -MaxVelStepPerTick, +MaxVelStepPerTick);
            return current + step;
        }

 /*       // ---- VoltageInput wiring (used by DC)  
        public void AttachTargetVoltageInput()
        {
            _vin?.Close();
            _vin = new VoltageInput
            {
                IsHubPortDevice = true,
                DeviceSerialNumber = TargetVoltageInputHub,
                HubPort = TargetVoltageInputPort,
                Channel = TargetVoltageInputChannel,
                IsRemote = true
            };
            _vin.Open(5000);
            _vin.VoltageChange += (s, e) =>
            {
                _feedbackVoltage = e.Voltage;
                if (_filteredVoltage == 0.0) _filteredVoltage = _feedbackVoltage;
            };
        }
*/
        /// <summary>Subclass must send the velocity to the actual Phidgets motor.</summary>
        protected abstract void ApplyVelocity(double velocity);

        /// <summary>Convenience loop to chase a target voltage using shared tuning.</summary>
        protected async Task RunVoltageChaseLoop(double targetVoltage)
        {
            double tgt = Clamp(targetVoltage, 0.0, 5.0);
            while (true)
            {
                _filteredVoltage = LowPass(_filteredVoltage, _feedbackVoltage);
                double err = tgt - _filteredVoltage;
                UpdateHysteresis(Math.Abs(err));

                double dt = TickMs / 1000.0;
                double desired = VelocityFromError(err, dt);
                _velCmd = SlewTowards(_velCmd, desired);
                Debug.WriteLine($"tgt {tgt:F2} fbk {_filteredVoltage:F2} err {err:F2} des {desired:F3} cmd {_velCmd:F3} settled {_isSettled}");

                ApplyVelocity(_velCmd);

                if (_isSettled && Math.Abs(_velCmd) < MinVelocity * 0.5)
                {
                    ApplyVelocity(0.0);
                    break;
                }

                await Task.Delay(TickMs);
            }
        }


		/// <summary>
		/// Map a "movingTo" value through a piecewise-linear mapping defined by targetMap -> scaleMap,
		/// then chase the resulting value as a voltage using RunVoltageChaseLoop().
		///
		/// Example:
		///   movingTo = 125.0
		///   targetMap = [0, 250]
		///   scaleMap  = [2, 4]
		///   -> movingTo is 50% of the targetMap span, so voltage = 3.0
		///
		/// Supports multiple segments, e.g. targetMap=[0,100,400], scaleMap=[0,1,3].
		/// Values outside the domain are clamped to the nearest endpoint.
		/// </summary>
		public Task OnTargetMoving(double movingTo, double[] targetMap, double[] scaleMap)
		{
			if (targetMap == null || scaleMap == null)
				throw new ArgumentNullException("targetMap/scaleMap cannot be null.");
			if (targetMap.Length != scaleMap.Length)
				throw new ArgumentException("targetMap and scaleMap must have the same length.");
			if (targetMap.Length < 2)
				throw new ArgumentException("targetMap/scaleMap must contain at least two points.");

			// Find segment containing movingTo; assume targetMap is non-decreasing.
			int n = targetMap.Length;

			// Clamp outside domain
			if (movingTo <= targetMap[0])
				return RunVoltageChaseLoop(scaleMap[0]);
			if (movingTo >= targetMap[n - 1])
				return RunVoltageChaseLoop(scaleMap[n - 1]);

			// Find i such that targetMap[i] <= movingTo <= targetMap[i+1]
			int i = 0;
			for (; i < n - 1; i++)
			{
				double a = targetMap[i];
				double b = targetMap[i + 1];
				if (a <= movingTo && movingTo <= b)
					break;
			}

			// Linear interpolate within segment i..i+1
			double x0 = targetMap[i], x1 = targetMap[i + 1];
			double y0 = scaleMap[i], y1 = scaleMap[i + 1];

			// Protect against degenerate segment
			if (Math.Abs(x1 - x0) < 1e-12)
				return RunVoltageChaseLoop(y0);

			double t = (movingTo - x0) / (x1 - x0);        // 0..1
			double y = y0 + t * (y1 - y0);                 // mapped value

			return RunVoltageChaseLoop(y);
		}
    }

    /// <summary>
    /// All tuning knobs used by both BLDC and DC controllers.
    /// Keep values conservative by default; override per-motor/axis as needed.
    /// </summary>
    public class MotorTuningOptions
    {
        public double? MaxVelocity { get; set; }            // 0..1 cap when error is large
        public double? MinVelocity { get; set; }            // small bias to overcome stiction
        public double? VelocityBand { get; set; }           // error distance where MaxVelocity is reached (units of feedback)
        public double? CurveGamma { get; set; }             // non-linear shaping of error→velocity
        public double? DeadbandEnter { get; set; }          // settle (stop) threshold (units of feedback)
        public double? DeadbandExit { get; set; }           // re-activate threshold (units of feedback)
        public double? MaxVelStepPerTick { get; set; }      // slew rate limit per tick
        public double? Kp { get; set; }                     // proportional gain
        public double? Ki { get; set; }                     // integral gain
        public double? Kd { get; set; }                     // derivative gain
        public double? IOnBand { get; set; }                // Integrates when Error < this to prevent wind-up
        public double? IntegralLimit { get;  set; }          // anti-windup clamp (velocity units)
        public double? PositionFilterAlpha { get; set; }    // low-pass on feedback (0..1)
        public int? TickMs { get; set; }                    // control loop period (ms)
    }

    internal static class ControlMath
    {
        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
