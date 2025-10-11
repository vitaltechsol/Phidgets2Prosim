using Phidget22;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    /// <summary>
    /// MotorBase: soft-retargeting control loop for Phidgets DC/BLDC.
    /// - One long-lived loop tracks a mutable requested setpoint (no cancel on each retarget).
    /// - Hysteresis, non-linear error→velocity, optional PID, slew limiting.
    /// - Input-activity awareness prevents premature settling & braking while the reference is moving.
    /// - All logs are timestamped.
    /// Subclasses implement ApplyVelocity() to talk to hardware (and may manage braking).
    /// </summary>
    internal abstract class MotorBase : PhidgetDevice
    {
        // ===== Optional VoltageRatioInput feedback (0..1 domain) =====
        public int TargetVoltageInputHub { get; set; } = -1;
        public int TargetVoltageInputPort { get; set; } = -1;
        public int TargetVoltageInputChannel { get; set; } = 0;
        public double Acceleration { get; set; } = 100;
        
        private VoltageRatioInput _vin = new VoltageRatioInput();
        protected double _feedbackVoltage = 0.0;
        protected double _filteredVoltage = 0.0;

        // ===== Tuning (defaults; override via MotorTuningOptions / MotorTuningOptionsEx) =====
        protected double MaxVelocity = 0.55;
        protected double MinVelocity = 0.085;
        protected double VelocityBand = 0.50;        // feedback units where MaxVelocity is reached
        protected double CurveGamma = 0.65;          // non-linear shaping exponent
        protected double DeadbandEnter = 0.01;       // settle threshold
        protected double DeadbandExit = 0.02;        // wake-up threshold
        protected double MaxVelStepPerTick = 0.01;   // velocity slew per tick
        protected double Kp = 0.0, Ki = 0.0, Kd = 0.0;
        protected double IOnBand = 1.0;
        protected double IntegralLimit = 0.25;
        protected double PositionFilterAlpha = 0.25; // 0..1 (higher = more weight on newest sample)
        protected int TickMs = 20;

        // === Setpoint behavior (0..1 domain for VoltageRatio) ===
        protected double SetpointSlewPerTick = 0.02; // change in target per tick
        protected double TargetEpsilon = 1e-6;

        // ===== Control state =====
        protected double _velCmd = 0.0;
        protected double _integral = 0.0;
        protected double _lastError = 0.0;
        protected bool _isSettled = true;

        // ===== Loop lifetime =====
        private CancellationTokenSource _cts;
        private Task _currentLoop;
        private volatile bool _loopRunning = false;
        private readonly object _startLock = new object();

        // ===== Retargeting (no cancel); thread-safe with Volatile =====
        private double _requestedTarget = double.NaN; // 0..1 desired feedback target
        private DateTime _lastReqUtc = DateTime.MinValue;

        // Activity windows
        private static readonly TimeSpan InputActiveWindow = TimeSpan.FromMilliseconds(600);   // keep awake/brake-off while ref is changing
        private static readonly TimeSpan IdleStopWindow = TimeSpan.FromMilliseconds(1500);  // stop loop only after quiet period

        protected MotorBase(MotorTuningOptions options = null)
        {
            ApplyTuning(options);
        }

        // ===== Timestamped logging helpers =====
        protected static string TS() => DateTime.UtcNow.ToString("HH:mm:ss.fff");
        protected static void Log(string msg) => Debug.WriteLine($"[{TS()}] {msg}");

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

            if (options is MotorTuningOptionsEx ex)
            {
                if (ex.SetpointSlewPerTick.HasValue) SetpointSlewPerTick = ex.SetpointSlewPerTick.Value;
                if (ex.TargetEpsilon.HasValue) TargetEpsilon = ex.TargetEpsilon.Value;
            }
        }

        // ===== Sensor hookup =====
        public async void AttachTargetVoltageInput()
        {
            try
            {
                _vin?.Close();
            }
            catch { /* ignore */ }

            _vin = new VoltageRatioInput
            {
                HubPort = TargetVoltageInputPort,
                IsRemote = true,
                IsHubPortDevice = TargetVoltageInputChannel == -1,
                DeviceSerialNumber = TargetVoltageInputHub,
                Channel = TargetVoltageInputChannel
            };

            _vin.VoltageRatioChange += (s, e) =>
            {
                _feedbackVoltage = e.VoltageRatio;
                if (_filteredVoltage == 0.0) _filteredVoltage = _feedbackVoltage;
                //Log($"[Vin] voltage ratio {_feedbackVoltage:F4}");
            };

            await Task.Run(() => _vin.Open(5000));
            Log("AttachTargetVoltageInput: opened");
            _vin.VoltageRatioChangeTrigger = 0.0002;
            _vin.DataInterval = 50;
        }

        // ===== Core helpers =====
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

            // Non-linear mapping with min bias for stiction
            double norm = Math.Min(1.0, Math.Abs(error) / Math.Max(1e-6, VelocityBand));
            double shaped = Math.Pow(norm, CurveGamma);
            double vel = Math.Sign(error) * (MinVelocity + (MaxVelocity - MinVelocity) * shaped);

            // Optional PID
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

        // ===== ABSTRACT: implement in subclass to drive hardware =====
        /// <summary>
        /// Send velocity to the device. If you manage braking, consider leaving brake OFF while input is active
        /// (i.e., (UtcNow - _lastReqUtc) &lt; InputActiveWindow) even when velocity == 0, so motion starts instantly.
        /// </summary>
        protected abstract void ApplyVelocity(double velocity);

        // ===== Public API: soft retarget (NO cancel) =====
        public Task OnTargetMoving(double movingTo, double[] targetMap, double[] scaleMap)
        {
            if (targetMap == null || scaleMap == null)
                throw new ArgumentNullException("targetMap/scaleMap cannot be null.");
            if (targetMap.Length != scaleMap.Length || targetMap.Length < 2)
                throw new ArgumentException("targetMap/scaleMap must be same length >= 2.");

            double mapped = MapOnce(movingTo, targetMap, scaleMap);  // 0..1 ratio
            mapped = Clamp(mapped, 0.0, 1.0);

            // Log mapping
            // (MapOnce already logs segment details.)
            Volatile.Write(ref _requestedTarget, mapped);
            _lastReqUtc = DateTime.UtcNow;

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

            return Task.CompletedTask;
        }

        public async Task StopChaseAsync()
        {
            if (_cts == null) return;
            try
            {
                _cts.Cancel();
                if (_currentLoop != null) await _currentLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _currentLoop = null;
            }
        }

        private static double MapOnce(double movingTo, double[] targetMap, double[] scaleMap)
        {
            int n = targetMap.Length;

            if (movingTo <= targetMap[0])
            {
                Log($"OnTargetMoving({movingTo}) -> below [{targetMap[0]}] -> voltage {scaleMap[0]:F3}");
                return scaleMap[0];
            }
            if (movingTo >= targetMap[n - 1])
            {
                Log($"OnTargetMoving({movingTo}) -> above [{targetMap[n - 1]}] -> voltage {scaleMap[n - 1]:F3}");
                return scaleMap[n - 1];
            }

            int i = 0;
            for (; i < n - 1; i++)
            {
                if (targetMap[i] <= movingTo && movingTo <= targetMap[i + 1]) break;
            }

            double x0 = targetMap[i], x1 = targetMap[i + 1];
            double y0 = scaleMap[i], y1 = scaleMap[i + 1];

            if (Math.Abs(x1 - x0) < 1e-12)
            {
                Log($"OnTargetMoving({movingTo}) -> degenerate seg [{x0},{x1}] -> voltage {y0:F3}");
                return y0;
            }

            double t = (movingTo - x0) / (x1 - x0); // 0..1
            double y = y0 + t * (y1 - y0);
            Log($"OnTargetMoving({movingTo}) -> segment {i} [{x0},{x1}] -> {t:F2} -> voltage {y:F3}");
            return y;
        }

        /// <summary>
        /// One long-lived loop that tracks a mutable requested target.
        /// Avoids settling/braking while input is active; stops only after the input is idle.
        /// All logs are timestamped.
        /// </summary>
        private async Task RunVoltageChaseLoop(CancellationToken ct)
        {
            _loopRunning = true;
            var loopId = Guid.NewGuid().ToString("N").Substring(0, 6);

            // Initial chase target
            double initReq = Volatile.Read(ref _requestedTarget);
            double tgt = !double.IsNaN(initReq) ? initReq : _filteredVoltage;
            Log($"[loop {loopId}] start tgt={tgt:F6}");

            _isSettled = false;
            _velCmd = 0;

            try
            {
                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Log($"[loop {loopId}] cancelled");
                        ApplyVelocity(0.0);
                        break;
                    }

                    // Slew chase target toward the latest request
                    double latest = Volatile.Read(ref _requestedTarget);
                    double req = !double.IsNaN(latest) ? latest : tgt;

                    if (Math.Abs(req - tgt) > TargetEpsilon)
                    {
                        double delta = req - tgt;
                        double step = Clamp(delta, -SetpointSlewPerTick, +SetpointSlewPerTick);
                        tgt += step;
                    }

                    // Determine whether upstream input is "active"
                    bool inputActive = (DateTime.UtcNow - _lastReqUtc) < InputActiveWindow;

                    // Feedback & control
                    _filteredVoltage = LowPass(_filteredVoltage, _feedbackVoltage);
                    double err = tgt - _filteredVoltage;

                    // Hysteresis with input-awareness
                    UpdateHysteresis(Math.Abs(err));
                    if (inputActive) _isSettled = false; // keep awake while target is moving

                    double dt = TickMs / 1000.0;
                    double desired = VelocityFromError(err, dt);
                    _velCmd = SlewTowards(_velCmd, desired);

                    // Stiction kick: ensure we don't send sub-MinVelocity while we're trying to move
                    if (!_isSettled && Math.Abs(_velCmd) < MinVelocity && Math.Abs(err) > DeadbandEnter)
                    {
                        _velCmd = Math.Sign(desired == 0 ? err : desired) * MinVelocity;
                    }

                    Log($"[loop {loopId}] req {req:F6} | tgt {tgt:F6} | fb {_filteredVoltage:F6} | " +
                        $"err {err:F3} desired {desired:F3} cmd {_velCmd:F3} settled {_isSettled} active {inputActive}");

                    ApplyVelocity(_velCmd);

                    // Only stop after idle + actually settled
                    bool recentlyIdle = (DateTime.UtcNow - _lastReqUtc) >= IdleStopWindow;
                    double latestAgain = Volatile.Read(ref _requestedTarget);

                    if (recentlyIdle &&
                        _isSettled &&
                        Math.Abs(_velCmd) < MinVelocity * 0.5 &&
                        (double.IsNaN(latestAgain) || Math.Abs(latestAgain - tgt) <= TargetEpsilon))
                    {
                        ApplyVelocity(0.0);
                        Log($"[loop {loopId}] stop");
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

    /// <summary>
    /// Tuning knobs for the controller.
    /// </summary>
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

    /// <summary>
    /// Optional extras for setpoint behavior.
    /// </summary>
    public class MotorTuningOptionsEx : MotorTuningOptions
    {
        public double? SetpointSlewPerTick { get; set; }   // 0..1 per TickMs
        public double? TargetEpsilon { get; set; }         // equality threshold for setpoint
    }
}
