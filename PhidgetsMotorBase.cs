using Phidget22;
using Phidget22.Events;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Phidgets2Prosim
{
    internal abstract class MotorBase : PhidgetDevice, IDisposable
    {
        // ---- Phidgets Voltage Input (target feedback) ----
        public int TargetVoltageInputHub { get; set; } = -1;
        public int TargetVoltageInputPort { get; set; } = -1;
        public int TargetVoltageInputChannel { get; set; } = -1;

        /// <summary>Phidgets VoltageRatioInput.VoltageRatioChangeTrigger (ratio units). Typical: 0.0005 ~ 0.002</summary>
        public double VoltageRatioChangeTrigger { get; set; } = 0.001;

        /// <summary>If true, the incoming VoltageRatio will be transformed as 1 - ratio.</summary>
        public bool InvertInput { get; set; } = false;

        /// <summary>Milliseconds between safety ticks that keep the loop responsive even if no input events fire.</summary>
        public int TickMsSafety { get; set; } = 25;

        /// <summary>Optional tuning knobs.</summary>
        public MotorTuningOptions Tuning { get; set; } = new MotorTuningOptions
        {
            MaxVelocity = 0.7,
            MinVelocity = 0.4,          // helps overcome static friction near target
            VelocityBand = 0.5,         // shrink velocity as we get close
            CurveGamma = 1.0,           // 1 = linear
            DeadbandEnter = 0.01,       // consider "on target" when |error| <= 1%
            DeadbandExit = 0.015,       // exit deadband when |error| > 1.5%
            MaxVelStepPerTick = 0.20,   // slew limit per tick
            Kp = 0.05,
            Ki = 0.0,
            Kd = 0.0,
            IOnBand = 0.10,
            IntegralLimit = 0.5,
            TickMs = 20,
            PositionFilterAlpha = 0.15,
            SetpointSlewPerTick = 0.10  // rate-limiter for the target value that the motor controller is chasing. Only move the target this much per control tick
        };

        /// <summary>Acceleration to apply to the concrete motor (if supported).</summary>
        public double Acceleration { get; set; } = 100;

        /// <summary>Mapping from real-world target values to a normalized 0..1 scale to chase.</summary>
        public double[] TargetPosMap { get; set; } = Array.Empty<double>();
        public double[] TargetPosScaleMap { get; set; } = Array.Empty<double>();

        public PhidgetsVoltageInput VoltageInput;

        // Loop state
        private readonly object _lock = new object();
        private double _latestMappedTarget = 0.0;   // 0..1
        private double _smoothedRatio = 0.0;        // 0..1 measured (smoothed)
        private double _rawRatio = 0.0;             // 0..1 measured (raw)
        private bool _loopRunning = false;
        private CancellationTokenSource _cts;
        private double _integral = 0.0;
        private double _prevErr = 0.0;
        private double _currentVelCmd = 0.0;        // last velocity sent, [-1..1]
        private bool _inDeadband = false;
        private double _tgtSlewed = 0.0;
        private bool _tgtInit = false;

        // ----- Exposed read-only state for subclasses / diagnostics -----
        protected double CurrentSmoothed => _smoothedRatio;
        protected double CurrentRaw => _rawRatio;
        protected double CurrentVelCmd => _currentVelCmd;
        protected bool InDeadband => _inDeadband;

        protected double VelCmd
        {
            get => _currentVelCmd;
            set
            {
                double vMax = Clamp01(Tuning.MaxVelocity ?? 1.0);
                _currentVelCmd = Clamp(value, -vMax, +vMax);
            }
        }

        protected int TickMs => Tuning.TickMs ?? 20;

        // Expose MinVelocity as a protected convenience property in [0..1]
        protected double MinVelocity => Clamp01(Tuning.MinVelocity ?? 0.0);

        // Expose a proxy for the private velocity command so subclasses can reuse _velCmd
        protected double _velCmd
        {
            get => _currentVelCmd;
            set
            {
                // Clamp to [-MaxVelocity .. +MaxVelocity]
                double vMax = Clamp01(Tuning.MaxVelocity ?? 1.0);
                _currentVelCmd = Clamp(value, -vMax, +vMax);
            }
        }
        protected bool _isSettled
        {
            get => _inDeadband;
            set => _inDeadband = value;
        }

        protected double _integralState
        {
            get => _integral;
            set => _integral = value;
        }

        protected double _lastError
        {
            get => _prevErr;
            set => _prevErr = value;
        }
        protected MotorBase(MotorTuningOptions options = null)
        {
            ApplyTuning(options);
        }

        protected void ApplyTuning(MotorTuningOptions options)
        {
            if (options == null) return;
            if (options.MaxVelocity.HasValue) Tuning.MaxVelocity = options.MaxVelocity.Value;
            if (options.MinVelocity.HasValue) Tuning.MinVelocity = options.MinVelocity.Value;
            if (options.VelocityBand.HasValue) Tuning.VelocityBand = options.VelocityBand.Value;
            if (options.CurveGamma.HasValue) Tuning.CurveGamma = options.CurveGamma.Value;
            if (options.DeadbandEnter.HasValue) Tuning.DeadbandEnter = options.DeadbandEnter.Value;
            if (options.DeadbandExit.HasValue) Tuning.DeadbandExit = options.DeadbandExit.Value;
            if (options.MaxVelStepPerTick.HasValue) Tuning.MaxVelStepPerTick = options.MaxVelStepPerTick.Value;
            if (options.Kp.HasValue) Tuning.Kp = options.Kp.Value;
            if (options.Ki.HasValue) Tuning.Ki = options.Ki.Value;
            if (options.Kd.HasValue) Tuning.Kd = options.Kd.Value;
            if (options.IOnBand.HasValue) Tuning.IOnBand = options.IOnBand.Value;
            if (options.IntegralLimit.HasValue) Tuning.IntegralLimit = options.IntegralLimit.Value;
            if (options.PositionFilterAlpha.HasValue) Tuning.PositionFilterAlpha = options.PositionFilterAlpha.Value;
            if (options.TickMs.HasValue) Tuning.TickMs = options.TickMs.Value;
            if (options.SetpointSlewPerTick.HasValue) Tuning.SetpointSlewPerTick = options.SetpointSlewPerTick.Value;

        }

        // ---- Lifecycle ------------------------------------------------------

        /// <summary>Open the VoltageRatioInput and wire events. Call this before commanding motion.</summary>
        public virtual async Task InitializeAsync()
        {
            if (VoltageInput != null)
            {
                SendInfoLog("DC Motor paired to Voltage Input");
                VoltageInput.onScaledValueChanged += PhidgetsVoltageInput_onScaledValueChanged;
            }
            await Task.CompletedTask;
        }

        private void PhidgetsVoltageInput_onScaledValueChanged(double obj)
        {
            Debug.WriteLine($"[VIN] ScaledValueChanged: {obj}");
            // Treat 'obj' as engineering units (e.g., 0..17) coming from your mapping
            var alpha = Tuning.PositionFilterAlpha ?? 0.15;
            _rawRatio = obj;
            _smoothedRatio = _smoothedRatio + alpha * (obj - _smoothedRatio);
            Debug.WriteLine($"[VIN] scaled={obj:F4} smoothed={_smoothedRatio:F4}");
        }

        public virtual void Dispose()
        {
            if (VoltageInput != null)
            {
                VoltageInput.Close();
            }
            _cts?.Cancel();
            _cts?.Dispose();
        }

        // ---- Target API -----------------------------------------------------

        /// <summary>
        /// Main entry point: caller provides a normalized/engineered target (already in 0..1 or scaled).
        /// Safe to call frequently (e.g., from your DataRef change handler).
        /// </summary>
        public Task OnTargetMoving(double movingTo)
        {
            lock (_lock)
            {
                _latestMappedTarget = movingTo;
                Debug.WriteLine($"[Target] movingTo={movingTo:F3}");
                // If a new target arrives while in deadband, force “wake up” by marking out-of-band
                if (_inDeadband && Math.Abs(_latestMappedTarget - _smoothedRatio) > (Tuning.DeadbandExit ?? 0.015))
                    _inDeadband = false;

                if (!_loopRunning)
                {
                    _loopRunning = true;
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();
                    _ = Task.Run(() => RunChaseLoopAsync(_cts.Token));
                }
            }

            return Task.CompletedTask;
        }

        // ---- Control loop ---------------------------------------------------

        private async Task RunChaseLoopAsync(CancellationToken ct)
        {
            var tickMs = Tuning.TickMs ?? 20;
            var kp = Tuning.Kp ?? 0.0;
            var ki = Tuning.Ki ?? 0.0;
            var kd = Tuning.Kd ?? 0.0;
            var velBand = Tuning.VelocityBand ?? 0.3;
            var dbEnter = Tuning.DeadbandEnter ?? 0.01;
            var dbExit = Tuning.DeadbandExit ?? 0.015;
            var maxStep = Tuning.MaxVelStepPerTick ?? 0.2;
            var vMax = Clamp01(Tuning.MaxVelocity ?? 1.0);
            var vMin = Clamp01(Tuning.MinVelocity ?? 0.0);
            var gamma = Math.Max(0.1, Tuning.CurveGamma ?? 1.0);
            var iLimit = Tuning.IntegralLimit ?? 0.5;
            var iOnBand = Tuning.IOnBand ?? 0.15;

            _integral = 0.0;
            _prevErr = 0.0;

            var sw = Stopwatch.StartNew();
            long lastTs = sw.ElapsedMilliseconds;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    long ts = sw.ElapsedMilliseconds;
                    double dt = Math.Max(1e-3, (ts - lastTs) / 1000.0); // seconds
                    lastTs = ts;

                    double target, pos;
                    lock (_lock)
                    {
                        target = _latestMappedTarget;
                        pos = _smoothedRatio;
                    }

                    // Optional setpoint slew (smooth target steps)
                    double __slewStep = 0.0;
                    if (Tuning.SetpointSlewPerTick.HasValue && Tuning.SetpointSlewPerTick.Value > 0.0)
                        __slewStep = Tuning.SetpointSlewPerTick.Value;
                    if (!_tgtInit) { _tgtSlewed = target; _tgtInit = true; }
                    if (__slewStep > 0.0)
                    {
                        double __dSlew = target - _tgtSlewed;
                        if (Math.Abs(__dSlew) > __slewStep)
                            _tgtSlewed += Math.Sign(__dSlew) * __slewStep;
                        else
                            _tgtSlewed = target;
                        target = _tgtSlewed;
                    }

                    double err = target - pos;
                    double aerr = Math.Abs(err);

                    // Deadband logic (enter/exit with hysteresis)
                    if (_inDeadband)
                    {
                        if (aerr > dbExit) _inDeadband = false;
                    }
                    else
                    {
                        if (aerr <= dbEnter) _inDeadband = true;
                    }

                    if (_inDeadband)
                    {
                        if (Math.Abs(_currentVelCmd) > 1e-6)
                        {
                            _currentVelCmd = 0;
                            ApplyVelocity(0);
                        }

                        _integral = 0.0; // reset windup when stable
                        // Stay in the loop; respond instantly if new target moves us out of deadband
                        await Task.Delay(Math.Max(1, Math.Min(tickMs, TickMsSafety)), ct);
                        // Debug.WriteLine($"[Loop] In deadband at pos={pos:F3} target={target:F3}");
                        continue;
                    }
                    else
                    {
                        // PID
                        if (aerr < iOnBand) _integral += err * dt;
                        _integral = Clamp(_integral, -iLimit, iLimit);
                        double deriv = (err - _prevErr) / dt;
                        _prevErr = err;

                        double pid = kp * err + ki * _integral + kd * deriv;

                        // --- Velocity taper near target (shapes how fast we go when close) ---
                        double taper = (aerr < velBand)
                            ? Math.Pow(aerr / Math.Max(1e-6, velBand), gamma)
                            : 1.0;

                        // --- Direction: if PID is tiny (e.g., kp=0), keep sign from the error ---
                        double dirSource = (Math.Abs(pid) > 1e-6) ? pid : err;

                        // --- Magnitude: taper PID magnitude
                        double pidMag = Math.Min(vMax, Math.Abs(pid)) * taper;

                        // ---- HARD FLOOR: never below MinVelocity when moving (outside deadband)
                        double floor = _inDeadband ? 0.0 : vMin;

                        double cmdMag = Math.Max(floor, pidMag);
                        cmdMag = Math.Min(cmdMag, vMax);

                        double cmd = (dirSource >= 0) ? cmdMag : -cmdMag;

                        // Ensure command respects the floor even if pidMag goes tiny
                        if (!_inDeadband && Math.Abs(cmd) > 1e-12 && Math.Abs(cmd) < floor)
                            cmd = Math.Sign(cmd) * floor;

                        // If we were stopped or below the floor and new command wants to move, snap to floor
                        if (!_inDeadband && Math.Abs(_currentVelCmd) < (floor - 1e-6) && Math.Abs(cmd) >= floor)
                        {
                            _currentVelCmd = (cmd >= 0 ? 1 : -1) * floor;
                            ApplyVelocity(_currentVelCmd);
                        }
                        else
                        {
                            // Slew-limit normally
                            var slewed = Slew(_currentVelCmd, cmd, maxStep);
                            if (Math.Abs(slewed - _currentVelCmd) > 1e-6)
                            {
                                _currentVelCmd = slewed;
                                ApplyVelocity(slewed);
                            }
                        }
                    }

                    // Safety tick — loop still runs even if no VIN events
                    await Task.Delay(Math.Max(1, Math.Min(tickMs, TickMsSafety)), ct);
                }
                SendInfoLog("DC Motor stopped");
            }
            catch (TaskCanceledException) { /* normal */ }
            catch (Exception ex)
            {
                SendErrorLog($"Chase loop error: {ex}");
            }
            finally
            {
                // stop motor on exit
                try { ApplyVelocity(0); } catch { /* ignore */ }
                lock (_lock) { _loopRunning = false; }
            }
        }

        // ---- Abstract for concrete motors ----------------------------------

        /// <summary>Send a velocity command in [-1..1] to the hardware.</summary>
        public abstract void ApplyVelocity(double velocity);

        // ---- Mapping / Math helpers ----------------------------------------

        protected static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
        protected static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? v = hi : v);
        protected static double Slew(double from, double to, double maxStep)
        {
            double d = to - from;
            if (Math.Abs(d) <= maxStep) return to;
            return from + Math.Sign(d) * maxStep;
        }
        protected static double SignKeep(double signSource, double mag) => (signSource >= 0) ? mag : -mag;

        /// <summary>Shared low-pass filter helper (alpha in [0..1]).</summary>
        protected double LowPass(double prev, double latest)
        {
            double alpha = Math.Max(0.0, Math.Min(1.0, Tuning.PositionFilterAlpha ?? 0.15));
            return (alpha * latest) + ((1.0 - alpha) * prev);
        }

        /// <summary>Update _inDeadband using enter/exit hysteresis.</summary>
        protected void UpdateHysteresis(double absError)
        {
            double enter = Math.Max(0.0, Tuning.DeadbandEnter ?? 0.01);
            double exit = Math.Max(enter + 1e-6, Tuning.DeadbandExit ?? 0.015);

            if (_inDeadband)
            {
                if (absError > exit) _inDeadband = false;
            }
            else
            {
                if (absError <= enter) _inDeadband = true;
            }
        }

        /// <summary>
        /// Nonlinear error→velocity with PID correction (sign from error).
        /// Returns a velocity in [-MaxVelocity..+MaxVelocity].
        /// </summary>
        protected double VelocityFromError(double error, double dtSeconds)
        {
            double vMax = Clamp01(Tuning.MaxVelocity ?? 1.0);
            double vMin = Clamp01(Tuning.MinVelocity ?? 0.0);
            double vb = Math.Max(1e-6, Tuning.VelocityBand ?? 0.3);
            double gamma = Math.Max(0.01, Tuning.CurveGamma ?? 1.0);

            double sign = Math.Sign(error);
            double absErr = Math.Abs(error);

            // Base curve
            double norm = Math.Min(1.0, absErr / vb);
            double shaped = Math.Pow(norm, gamma);
            double baseVel = _inDeadband ? 0.0 : shaped * vMax;
            if (!_inDeadband && baseVel < vMin) baseVel = vMin;

            // PID
            double kp = Tuning.Kp ?? 0.0;
            double ki = Tuning.Ki ?? 0.0;
            double kd = Tuning.Kd ?? 0.0;
            double iBand = Math.Max(0.0, Tuning.IOnBand ?? 0.15);
            double iLim = Math.Max(0.0, Tuning.IntegralLimit ?? 0.5);

            if (absErr <= iBand) _integral += error * dtSeconds;
            else _integral *= 0.9;
            _integral = Clamp(_integral, -iLim, iLim);

            double deriv = dtSeconds > 0 ? (error - _prevErr) / dtSeconds : 0.0;
            _prevErr = error;

            double pid = (kp * error) + (ki * _integral) + (kd * deriv);

            double vel = (baseVel + Math.Abs(pid)) * sign;
            return Math.Max(-vMax, Math.Min(vMax, vel));
        }

        /// <summary>Slew limiter that uses Tuning.MaxVelStepPerTick.</summary>
        protected double SlewTowards(double current, double desired)
        {
            double step = Math.Max(0.0, Tuning.MaxVelStepPerTick ?? 0.2);
            return Slew(current, desired, step);
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

        public double? SetpointSlewPerTick { get; set; }
    }
}
