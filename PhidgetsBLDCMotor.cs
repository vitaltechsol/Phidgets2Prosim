// PhidgetsBLDCMotor.cs
using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Timers;
using static Phidgets2Prosim.ControlMath;

namespace Phidgets2Prosim
{
    internal class PhidgetsBLDCMotor : PhidgetDevice, IDisposable
    {
        // ===== Public knobs =====
        public bool Reversed { get; set; } = false;
        public int Offset { get; set; } = 0;

        public double MaxVelocity { get; set; } = 0.20;
        public double MinVelocity { get; set; } = 0.03;
        public double VelocityBand { get; set; } = 250.0;
        public double CurveGamma { get; set; } = 0.6;
        public double DeadbandEnter { get; set; } = 2.0;
        public double DeadbandExit { get; set; } = 4.0;
        public double MaxVelStepPerTick { get; set; } = 0.008;
        public double Kp { get; set; } = 0.0;
        public double Ki { get; set; } = 0.0;
        public double Kd { get; set; } = 0.0;
        public double IntegralLimit { get; set; } = 0.3;
        public double PositionFilterAlpha { get; set; } = 0.30;
        public int TickMs { get; set; } = 20;

        // ===== Internals =====
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
                    if (options.Kd.HasValue) Kd = options.Kd.Value;
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
                _motor.Acceleration = acceleration;
                _motor.TargetBrakingStrength = 1.0;

                // ProSim bindings
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

            // Filter feedback
            _currentPosFiltered = PositionFilterAlpha * cur + (1.0 - PositionFilterAlpha) * _currentPosFiltered;

            // Signed error (apply reverse if needed)
            double error = tgt - _currentPosFiltered;
            if (Reversed) error = -error;

            // Hysteretic settled state
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
                // Non-linear base mapping
                double norm = Math.Min(1.0, Math.Abs(error) / VelocityBand);
                double shaped = Math.Pow(norm, CurveGamma);
                desiredVel = Math.Sign(error) * (MinVelocity + (MaxVelocity - MinVelocity) * shaped);

                // Optional PID-like enhancements
                if (Kp != 0.0 || Ki != 0.0 || Kd != 0.0)
                {
                    double p = Kp * error;

                    if (Math.Abs(error) <= 8.0)
                    {
                        _integral += Ki * error * dt;
                        _integral = Clamp(_integral, -IntegralLimit, +IntegralLimit);
                    }
                    else
                    {
                        _integral = 0.0;
                    }

                    if (Kd != 0.0)
                    {
                        double errorRate = (error - _lastError) / Math.Max(1e-6, dt);
                        desiredVel += -Kd * errorRate;
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

            // Slew-limit
            double step = desiredVel - _velCmd;
            step = Clamp(step, -MaxVelStepPerTick, +MaxVelStepPerTick);
            _velCmd += step;

            // Snap to 0 when truly settled
            if (_isSettled && Math.Abs(_velCmd) < MinVelocity * 0.5)
                _velCmd = 0.0;

            try { _motor.TargetVelocity = _velCmd; } catch { /* ignore transient phidgets errors */ }
        }

        private void StopMotor()
        {
            _velCmd = 0;
            _integral = 0;
            _lastError = 0;
            _isSettled = true;
            try { _motor.TargetVelocity = 0; } catch { }
        }

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
