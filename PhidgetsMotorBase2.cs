using Phidget22;
using Phidget22.Events;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Phidgets2Prosim
{
    internal abstract class MotorBase2 : PhidgetDevice, IDisposable
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

        ///// <summary>Smoothing factor for measured position [0..1]. 0=no smoothing, 1=heavy smoothing.</summary>
        //public double PositionFilterAlpha { get; set; } = 0.15;

        /// <summary>Optional tuning knobs.</summary>
        public MotorTuningOptions Tuning { get; set; } = new MotorTuningOptions
        {
            MaxVelocity = 0.7,
            MinVelocity = 0.4,         // helps overcome static friction near target
            VelocityBand = 0.5, // shrink velocity as we get close
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
            PositionFilterAlpha = 0.15
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

        protected MotorBase2(MotorTuningOptions options = null)
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
        /// Main entry point: caller provides a real-world target (e.g., degrees), we map it to 0..1 and start/refresh the loop.
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

                    double cmd;
                    if (_inDeadband)
                    {
                        //cmd = 0.0;
                        if (Math.Abs(_currentVelCmd) > 1e-6)
                        {
                            _currentVelCmd = 0;
                            ApplyVelocity(0);
                        }

                        _integral = 0.0; // reset windup when stable
                                         // Stay in the loop; respond instantly if new target moves us out of deadband
                        await Task.Delay(Math.Max(1, Math.Min(tickMs, TickMsSafety)), ct);
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

                        // --- Magnitude: taper PID magnitude, but DO NOT taper the stiction floor ---
                        double pidMag = Math.Min(vMax, Math.Abs(pid)) * taper;

                        // Stiction floor applies whenever we're outside the deadband (do not scale it down)
                        double floor = (aerr > dbEnter) ? vMin : 0.0;

                        double cmdMag = Math.Max(floor, pidMag);
                        cmdMag = Math.Min(cmdMag, vMax);

                        // Apply sign and clamp
                        cmd = (dirSource >= 0) ? cmdMag : -cmdMag;
                    }

                    //// Slew-limit velocity
                    //cmd = Slew(_currentVelCmd, cmd, maxStep);
                    //_currentVelCmd = cmd;

                    //ApplyVelocity(cmd);


                    // If we were stopped or below the stiction floor, and the new command
                    // wants to move with at least the floor, snap straight to the floor.
                    if (Math.Abs(_currentVelCmd) < (vMin - 1e-6) && Math.Abs(cmd) >= vMin)
                    {
                        _currentVelCmd = (cmd >= 0 ? 1 : -1) * vMin;
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

                    // Safety tick — loop still runs even if no VIN events
                    await Task.Delay(Math.Max(1, Math.Min(tickMs, TickMsSafety)), ct);
                }
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
        protected abstract void ApplyVelocity(double velocity);

        // ---- Mapping --------------------------------------------------------


        protected static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
        protected static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
        protected static double Slew(double from, double to, double maxStep)
        {
            double d = to - from;
            if (Math.Abs(d) <= maxStep) return to;
            return from + Math.Sign(d) * maxStep;
        }
        protected static double SignKeep(double signSource, double mag) => (signSource >= 0) ? mag : -mag;
    }


}
