using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Phidgets2Prosim
{
    internal class PhidgetsBLDCMotor : MotorBase, IDisposable
    {
        // ===== Public knobs preserved =====
        public bool Reversed { get; set; } = false;
        public int Offset { get; set; } = 0;

        // (Optional) for logs/telemetry
        public int HubPort { get; private set; } = -1;
        public int Serial { get; private set; } = 0;

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
            int serial,
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
                // Wire user knobs/tuning into base
                Reversed = reversed;
                Offset = offset;

                HubPort = hubPort;
                Serial = serial;

                if (hubPort >= 0)
                {
                    _motor.HubPort = hubPort;
                    _motor.IsRemote = true;
                }
                _motor.DeviceSerialNumber = serial;
                Open();

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

            // Filter feedback
            _currentPosFiltered = LowPass(_currentPosFiltered, cur);

            // Signed error (apply reverse if needed)
            double error = tgt - _currentPosFiltered;
            if (Reversed) error = -error;

            // Hysteresis + velocity calc via base
            UpdateHysteresis(Math.Abs(error));
            double dt = Math.Max(1e-6, TickMs / 1000.0);
            double desiredVel = VelocityFromError(error, dt);
            VelCmd = SlewTowards(_velCmd, desiredVel);

            // Snap to 0 when truly settled
            double vMin = Math.Max(0.0, MinVelocity);
            if (_isSettled && Math.Abs(_velCmd) < vMin * 0.5) _velCmd = 0.0;

            // Apply to hardware
            ApplyVelocity(_velCmd);
        }

        private void StopMotor()
        {
            _velCmd = 0;
            _integralState = 0;
            _lastError = 0;
            _isSettled = true;
            try { _motor.TargetVelocity = 0; } catch { }
        }

        private static double SafeToDouble(object o) { try { return Convert.ToDouble(o); } catch { return 0; } }
        private static bool SafeToBool(object o) { try { return Convert.ToBoolean(o); } catch { return false; } }

        protected override void ApplyVelocity(double velocity)
        {
            try { _motor.TargetVelocity = velocity; } catch { /* ignore transient phidgets errors */ }
        }

        public async void Open()
        {
            try
            {
                await Task.Run(() => _motor.Open(500));
                _motor.Acceleration = Acceleration;
                _motor.TargetBrakingStrength = 1.0;
                SendInfoLog($"BLDC Motor Connected {Serial}: {HubPort}");
            }
            catch (Exception ex)
            {
                SendErrorLog($"BLOpen Fail for DC Motor {Serial}: {HubPort}");
                SendErrorLog(ex.ToString());
            }
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

        // -------- Optional: shared logging hooks (no-ops here unless you wire them) --------
        private void SendInfoLog(string s) { System.Diagnostics.Debug.WriteLine(s); }
        private void SendErrorLog(string s) { System.Diagnostics.Debug.WriteLine(s); }
    }
}
