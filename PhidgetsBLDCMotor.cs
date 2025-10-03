using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Timers;

namespace Phidgets2Prosim
{

	public class MotorTuningOptions
	{
		public double? MaxVelocity { get; set; }
		public double? MinVelocity { get; set; }
		public double? VelocityBand { get; set; }
		public double? CurveGamma { get; set; }
		public double? DeadbandEnter { get; set; }
		public double? DeadbandExit { get; set; }
		public double? MaxVelStepPerTick { get; set; }
		public double? Kp { get; set; }
		public double? Ki { get; set; }
		public double? Kd { get; set; }
		public double? IntegralLimit { get; set; }
		public double? PositionFilterAlpha { get; set; }
		public int? TickMs { get; set; }
	}

	internal class PhidgetsBLDCMotor : PhidgetDevice, IDisposable
	{
		///If true, reverses the motor direction logic (inverts sign of the error)
		public bool Reversed { get; set; } = false;

		///Offset applied to the target position before computing the error
		public int Offset { get; set; } = 0;

		///Maximum allowed motor velocity (0..1). Used when the error is large
		public double MaxVelocity { get; set; } = 0.70;

		///Minimum velocity to overcome static friction when error is small
		public double MinVelocity { get; set; } = 0.03;

		///Error distance (in position units) at which the motor reaches MaxVelocity
		public double VelocityBand { get; set; } = 250.0;

		///Curve shaping factor for error-to-velocity mapping (0.5–1.0 = softer near zero)
		public double CurveGamma { get; set; } = 0.6;

		///Distance threshold to enter the settled (stopped) zone.
		public double DeadbandEnter { get; set; } = 2.0;

		///Distance threshold to exit the settled (stopped) zone (should be > DeadbandEnter)
		public double DeadbandExit { get; set; } = 4.0;

		///Maximum allowed change in commanded velocity per control loop tick (slew limiter)
		public double MaxVelStepPerTick { get; set; } = 0.008;

		///Proportional gain (optional) to reduce steady-state error
		public double Kp { get; set; } = 0.0;

		///Integral gain (optional) to remove small bias error (start at 0.0)
		public double Ki { get; set; } = 0.0;

		///Derivative gain (damping) on error rate to suppress oscillations
		public double Kd { get; set; } = 0.0;

		/// Only integrate when |error| ≤ this band (prevents wind-up and hunting).
		/// Tune ~6–12 in your position units.
		public double IOnBand { get; set; } = 8.0;

		///Maximum absolute integral term value to prevent wind-up
		public double IntegralLimit { get; set; } = 0.3;

		///Smoothing factor for low-pass filtering of position feedback (0..1, higher = less filtering)
		public double PositionFilterAlpha { get; set; } = 0.30;

		///Interval (in milliseconds) for the control loop tick. Lower = faster updates
		public int TickMs { get; set; } = 20;

		// --- Internals ---
		private readonly BLDCMotor _motor = new BLDCMotor();
		private readonly object _lock = new object();
		private readonly Timer _loop;

		private volatile bool _motorOn = false;
		private double _targetPos = 0.0;
		private double _currentPos = 0.0;

		private double _currentPosFiltered = 0.0;
		private double _velCmd = 0.0;
		private double _integral = 0.0;
		private double _lastError = 0.0;
		private bool _isSettled = true;
		private bool _disposed;

		public PhidgetsBLDCMotor(
			int deviceSerialNumber,
			int hubPort,
			ProSimConnect connection,
			bool reversed,
			int offset,
			string refTurnOn,
			string refCurrentPos,
			string refTargetPos,
			double acceleration,
			MotorTuningOptions options = null
		)
		{
			try
			{
				// Apply optional overrides if provided

				if (options != null)
				{
					if (options.MaxVelocity.HasValue) MaxVelocity = options.MaxVelocity.Value;
					if (options.MinVelocity.HasValue) MinVelocity = options.MinVelocity.Value;
					if (options.VelocityBand.HasValue) VelocityBand = options.VelocityBand.Value;
					if (options.CurveGamma.HasValue) CurveGamma = options.CurveGamma.Value;
					if (options.DeadbandEnter.HasValue) DeadbandEnter = options.DeadbandEnter.Value;
					if (options.DeadbandExit.HasValue) DeadbandExit = options.DeadbandExit.Value;
					if (options.MaxVelStepPerTick.HasValue) MaxVelStepPerTick = options.MaxVelStepPerTick.Value;
					if (options.Kp.HasValue) Kp = options.Kp.Value;
					if (options.Ki.HasValue) Ki = options.Ki.Value;
					if (options.IntegralLimit.HasValue) IntegralLimit = options.IntegralLimit.Value;
					if (options.PositionFilterAlpha.HasValue) PositionFilterAlpha = options.PositionFilterAlpha.Value;
					if (options.TickMs.HasValue) TickMs = options.TickMs.Value;
				}

				Reversed = reversed;
				Offset = offset;

				if (hubPort >= 0)
				{
					_motor.HubPort = hubPort;
					_motor.IsRemote = true;
				}

				_motor.DeviceSerialNumber = deviceSerialNumber;
				_motor.Open(5000);
				_motor.Acceleration = acceleration; // keep Phidgets HW acceleration too
				_motor.TargetBrakingStrength = 1.0;

				var drCurrent = new DataRef(refCurrentPos, 100, connection);
				var drTurnOn = new DataRef(refTurnOn, 100, connection);
				var drTarget = new DataRef(refTargetPos, 100, connection);

				drCurrent.onDataChange += DataRef_onCurrentPosChanged;
				drTurnOn.onDataChange += DataRef_onTurnOnChanged;
				drTarget.onDataChange += DataRef_onTargetChanged;

				_loop = new Timer(TickMs) { AutoReset = true, Enabled = true };
				_loop.Elapsed += ControlTick;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}

		private void DataRef_onCurrentPosChanged(DataRef dataRef)
		{
			if (!_motorOn) return;
			double v = SafeToDouble(dataRef.value);
			lock (_lock) { _currentPos = v; }
		}

		private void DataRef_onTurnOnChanged(DataRef dataRef)
		{
			var newOn = SafeToBool(dataRef.value);
			if (_motorOn && !newOn) StopMotor();
			_motorOn = newOn;
		}

		private void DataRef_onTargetChanged(DataRef dataRef)
		{
			if (!_motorOn) return;
			double t = SafeToDouble(dataRef.value) - Offset;
			lock (_lock) { _targetPos = t; }
		}

		private void ControlTick(object sender, ElapsedEventArgs e)
		{
			if (!_motorOn) return;

			double cur, tgt;
			lock (_lock)
			{
				cur = _currentPos;
				tgt = _targetPos;
			}

			// filter current position
			_currentPosFiltered = PositionFilterAlpha * cur + (1.0 - PositionFilterAlpha) * _currentPosFiltered;

			// signed error (apply reverse if needed)
			double error = tgt - _currentPosFiltered;
			if (Reversed) error = -error;

			// hysteretic settle detection
			if (_isSettled)
			{
				if (Math.Abs(error) > DeadbandExit) _isSettled = false;
			}
			else
			{
				if (Math.Abs(error) < DeadbandEnter) _isSettled = true;
			}

			double desiredVel = 0.0;
			double dt = TickMs / 1000.0;

			if (!_isSettled)
			{
				// base non-linear mapping (gentle near zero)
				double norm = Math.Min(1.0, Math.Abs(error) / VelocityBand);
				double shaped = Math.Pow(norm, CurveGamma);
				desiredVel = Math.Sign(error) * (MinVelocity + (MaxVelocity - MinVelocity) * shaped);

				// simple PID-like terms (mostly PD by default)
				if (Kp != 0.0 || Ki != 0.0 || Kd != 0.0)
				{
					double p = Kp * error;

					// integrate only when close to the target; otherwise reset
					if (Math.Abs(error) <= IOnBand)
					{
						_integral += Ki * error * dt;
						_integral = Clamp(_integral, -IntegralLimit, +IntegralLimit);
					}
					else
					{
						_integral = 0.0;
					}

					// optional derivative (usually keep Kd = 0 for now)
					if (Kd != 0.0)
					{
						double errorRate = (error - _lastError) / Math.Max(1e-6, dt);
						desiredVel += -Kd * errorRate; // negative for damping
					}

					desiredVel += p + _integral;
					_lastError = error;
				}

				desiredVel = Clamp(desiredVel, -1.0, 1.0);
			}
			else
			{
				_integral = 0.0;
				_lastError = 0.0;
			}

			// software slew limiting on the command
			double step = desiredVel - _velCmd;
			step = Clamp(step, -MaxVelStepPerTick, +MaxVelStepPerTick);
			_velCmd += step;

			// fully stop when settled and nearly zero
			if (_isSettled && Math.Abs(_velCmd) < MinVelocity * 0.5)
				_velCmd = 0.0;

			try { _motor.TargetVelocity = _velCmd; } catch { /* ignore transient Phidgets errors */ }
		}

		private void StopMotor()
		{
			_velCmd = 0;
			_integral = 0;
			_lastError = 0;
			_isSettled = true;
			try { _motor.TargetVelocity = 0; } catch { }
		}

		private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
		private static double SafeToDouble(object o) { try { return Convert.ToDouble(o); } catch { return 0; } }
		private static bool SafeToBool(object o) { try { return Convert.ToBoolean(o); } catch { return false; } }

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_loop?.Stop();
			_loop?.Dispose();
			try { _motor.TargetVelocity = 0; _motor.Close(); _motor.Dispose(); } catch { }
		}

		///Pause or resume the motor. If paused, sets velocity to 0
		public void Pause(bool isPaused)
		{
			_motorOn = !isPaused;
			if (isPaused)
			{
				try { _motor.TargetVelocity = 0; } catch { }
			}
		}
	}
}