using Phidget22;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal abstract class MotorBase : PhidgetDevice
    {
        // ===== Feedback (VoltageRatio 0..1 in your current code) =====
        public int TargetVoltageInputHub { get; set; } = -1;
        public int TargetVoltageInputPort { get; set; } = -1;
        public int TargetVoltageInputChannel { get; set; } = 0;

        private VoltageRatioInput _vin = new VoltageRatioInput();
        protected double _feedbackVoltage = 0.0;
        protected double _filteredVoltage = 0.0;

        // ===== Tuning =====
        protected double MaxVelocity = 0.55;
        protected double MinVelocity = 0.085;
        protected double VelocityBand = 0.50;
        protected double CurveGamma = 0.65;
        protected double DeadbandEnter = 0.01;
        protected double DeadbandExit = 0.02;
        protected double MaxVelStepPerTick = 0.01;      // slew on velocity command
        protected double Kp = 0.0, Ki = 0.0, Kd = 0.0;
        protected double IOnBand = 1.0;
        protected double IntegralLimit = 0.25;
        protected double PositionFilterAlpha = 0.25;
        protected int TickMs = 20;

        // NEW: setpoint slew so targetV evolves smoothly when the request changes
        protected double SetpointSlewPerTick = 0.02;     // in *feedback units* per tick (0..1 domain for VoltageRatio)
        protected double TargetEpsilon = 1e-6;

        // ===== Control state =====
        protected double _velCmd = 0.0;
        protected double _integral = 0.0;
        protected double _lastError = 0.0;
        protected bool _isSettled = true;

        // ===== Loop lifetime =====
        private CancellationTokenSource _cts;            // used only for real stop/shutdown
        private Task _currentLoop;
        private volatile bool _loopRunning = false;

        // ===== Retargeting (no cancel): latest requested setpoint =====
        private double _requestedTarget = double.NaN;

        private readonly object _startLock = new object();

        // (kept for compatibility if you’re passing tuning in)
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

            // Optional: let tuning also set setpoint slew
            if (options is MotorTuningOptionsEx ex)
            {
                if (ex.SetpointSlewPerTick.HasValue) SetpointSlewPerTick = ex.SetpointSlewPerTick.Value;
                if (ex.TargetEpsilon.HasValue) TargetEpsilon = ex.TargetEpsilon.Value;
            }
        }

        // ---- Sensor hookup
        public async void AttachTargetVoltageInput()
        {
            _vin?.Close();
            _vin.HubPort = TargetVoltageInputPort;
            _vin.IsRemote = true;
            _vin.IsHubPortDevice = TargetVoltageInputChannel == -1;
            _vin.DeviceSerialNumber = TargetVoltageInputHub;
            _vin.Channel = TargetVoltageInputChannel;
            _vin.VoltageRatioChange += (s, e) =>
            {
                _feedbackVoltage = e.VoltageRatio;
                if (_filteredVoltage == 0.0) _filteredVoltage = _feedbackVoltage;
            };

            await Task.Run(() => _vin.Open(5000));
            Debug.WriteLine("AttachTargetVoltageInput");
            _vin.VoltageRatioChangeTrigger = 0.02;
            _vin.DataInterval = 50;
        }

        // ---- Helpers
        protected double LowPass(double prevFiltered, double sample)
            => PositionFilterAlpha * sample + (1.0 - PositionFilterAlpha) * prevFiltered;

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

        protected double VelocityFromError(double error, double dt)
        {
            if (_isSettled) return 0.0;

            double norm = Math.Min(1.0, Math.Abs(error) / Math.Max(1e-6, VelocityBand));
            double shaped = Math.Pow(norm, CurveGamma);
            double vel = Math.Sign(error) * (MinVelocity + (MaxVelocity - MinVelocity) * shaped);

            if (Kp != 0.0 || Ki != 0.0 || Kd != 0.0)
            {
                double p = Kp * error;

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

        protected static double Clamp(double v, double lo, double hi)
            => v < lo ? lo : (v > hi ? hi : v);

        // === ABSTRACT: subclasses actually send to hardware ===
        protected abstract void ApplyVelocity(double velocity);

        // === PUBLIC: retarget without cancelling the loop ===
        public Task OnTargetMoving(double movingTo, double[] targetMap, double[] scaleMap)
        {
            if (targetMap == null || scaleMap == null)
                throw new ArgumentNullException("targetMap/scaleMap cannot be null.");
            if (targetMap.Length != scaleMap.Length || targetMap.Length < 2)
                throw new ArgumentException("targetMap/scaleMap must be same length >= 2.");

            double mapped = MapOnce(movingTo, targetMap, scaleMap);  // ratio setpoint (0..1 for VoltageRatio)
            //_requestedTarget = Clamp(mapped, 0.0, 1.0);
            Volatile.Write(ref _requestedTarget, Clamp(mapped, 0.0, 1.0));

            // Start loop lazily if not running
            if (!_loopRunning)
            {
                lock (_startLock)
                {
                    if (!_loopRunning)
                    {
                        _cts?.Cancel();
                        _cts?.Dispose();
                        _cts = new CancellationTokenSource();
                        _currentLoop = RunVoltageChaseLoop(_cts.Token);
                    }
                }
            }

            // No await here: we want motor to respond while updates keep coming
            return Task.CompletedTask;
        }

        private static double MapOnce(double movingTo, double[] targetMap, double[] scaleMap)
        {
            int n = targetMap.Length;
            if (movingTo <= targetMap[0])
            {
                Debug.WriteLine($"OnTargetMoving({movingTo}) -> below [{targetMap[0]}] -> voltage {scaleMap[0]:F3}");
                return scaleMap[0];
            }
            if (movingTo >= targetMap[n - 1])
            {
                Debug.WriteLine($"OnTargetMoving({movingTo}) -> above [{targetMap[n - 1]}] -> voltage {scaleMap[n - 1]:F3}");
                return scaleMap[n - 1];
            }

            int i = 0;
            for (; i < n - 1; i++)
            {
                if (targetMap[i] <= movingTo && movingTo <= targetMap[i + 1]) break;
            }

            double x0 = targetMap[i], x1 = targetMap[i + 1];
            double y0 = scaleMap[i], y1 = scaleMap[i + 1];

            if (Math.Abs(x1 - x0) < 1e-12) return y0;

            double t = (movingTo - x0) / (x1 - x0);
            double y = y0 + t * (y1 - y0);
            Debug.WriteLine($"OnTargetMoving({movingTo}) -> segment {i} [{x0},{x1}] -> {t:F2} -> voltage {y:F3}");
            return y;
        }

        /// <summary>
        /// Single, long-lived loop. It tracks a mutable `_requestedTarget` and slews a local
        /// `tgt` toward it. No cancellations on retarget; cancellation only on shutdown.
        /// </summary>
        private async Task RunVoltageChaseLoop(CancellationToken ct)
        {
            _loopRunning = true;
            var loopId = Guid.NewGuid().ToString("N").Substring(0, 6);

            // Initialize chase target from the current request (or current feedback if NaN)
            double tgt = !double.IsNaN(_requestedTarget) ? _requestedTarget : _filteredVoltage;

            Debug.WriteLine($"[loop {loopId}] start tgt={tgt:F6}");

            _isSettled = false;
            _velCmd = 0;

            try
            {
                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Debug.WriteLine($"[loop {loopId}] cancelled");
                        ApplyVelocity(0.0);
                        break;
                    }

                    // Slew the chase target toward the latest requested target
                    //   double req = Volatile.Read(ref _requestedTarget);

                    double req = !double.IsNaN(Volatile.Read(ref _requestedTarget))
                          ? Volatile.Read(ref _requestedTarget)
                          : tgt;

                    if (Math.Abs(req - tgt) > TargetEpsilon)
                    {
                        double delta = req - tgt;
                        double step = Clamp(delta, -SetpointSlewPerTick, +SetpointSlewPerTick);
                        tgt += step;
                    }

                    // Feedback & control
                    _filteredVoltage = LowPass(_filteredVoltage, _feedbackVoltage);
                    double err = tgt - _filteredVoltage;
                    UpdateHysteresis(Math.Abs(err));

                    double dt = TickMs / 1000.0;
                    double desired = VelocityFromError(err, dt);
                    _velCmd = SlewTowards(_velCmd, desired);

                    // Stiction kick (optional)
                    if (!_isSettled && Math.Abs(_velCmd) < MinVelocity && Math.Abs(err) > DeadbandEnter)
                    {
                        _velCmd = Math.Sign(desired == 0 ? err : desired) * MinVelocity;
                    }

                    Debug.WriteLine($"[loop {loopId}] req {req:F6} | tgt {tgt:F6} | fb {_filteredVoltage:F6} | " +
                                    $"err {err:F3} desired {desired:F3} cmd {_velCmd:F3} settled {_isSettled}");

                    ApplyVelocity(_velCmd);

                    // Stop if truly settled and no retarget pending
                    if (_isSettled &&
                        Math.Abs(_velCmd) < MinVelocity * 0.5 &&
                        (double.IsNaN(_requestedTarget) || Math.Abs(_requestedTarget - tgt) <= TargetEpsilon))
                    {
                        ApplyVelocity(0.0);
                        Debug.WriteLine($"[loop {loopId}] stop");
                        break;
                    }

                    await Task.Delay(TickMs, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                _loopRunning = false;
            }
        }
    }

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
        public double? IOnBand { get; set; }
        public double? IntegralLimit { get; set; }
        public double? PositionFilterAlpha { get; set; }
        public int? TickMs { get; set; }
    }

    // Optional extension to tune setpoint behavior
    public class MotorTuningOptionsEx : MotorTuningOptions
    {
        public double? SetpointSlewPerTick { get; set; }   // 0..1 domain per tick
        public double? TargetEpsilon { get; set; }         // small epsilon to consider setpoint equal
    }
}
