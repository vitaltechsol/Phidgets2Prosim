using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Timers;

namespace Phidgets2Prosim
{
    internal class PhidgetsBLDCMotor : MotorBase, IDisposable
    {
        // ===== Public knobs preserved from your file =====
        public bool Reversed { get; set; } = false;
        public int Offset { get; set; } = 0;

        // ===== Internals =====
        private readonly BLDCMotor _motor = new BLDCMotor();
        private readonly object _lock = new object();
        private readonly Timer _loop;

        private volatile bool _motorOn = false;
        private double _targetPos = 0.0;
        private double _currentPos = 0.0;

        private double _currentPosFiltered = 0.0;
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
        ) : base(options)
        {
            try
            {
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

                // ProSim bindings (unchanged)
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

            // Filter feedback (use base LPF)
            _currentPosFiltered = LowPass(_currentPosFiltered, cur);

            // Signed error (apply reverse if needed)
            double error = tgt - _currentPosFiltered;
            if (Reversed) error = -error;

            // Hysteresis + velocity calc via shared helpers
            UpdateHysteresis(Math.Abs(error));
            double desiredVel = VelocityFromError(error, TickMs / 1000.0);
            _velCmd = SlewTowards(_velCmd, desiredVel);

            // Snap to 0 when truly settled
            if (_isSettled && Math.Abs(_velCmd) < MinVelocity * 0.5) _velCmd = 0.0;

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

        protected override void ApplyVelocity(double velocity)
        {
            // Used by base when you decide to leverage RunVoltageChaseLoop in the future.
            try { _motor.TargetVelocity = velocity; } catch { }
        }

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
