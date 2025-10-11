using ProSimSDK;
using System;

namespace Phidgets2Prosim
{
    internal class Custom_ParkingBrake : PhidgetDevice
    {
        private readonly ProSimConnect _connection;

        // Hard-coded ProSim DataRefs
        private const string REF_TOE_LEFT = "system.analog.A_FC_TOEBRAKE_LEFT_CAPT";
        private const string REF_TOE_RIGHT = "system.analog.A_FC_TOEBRAKE_RIGHT_CAPT";
        private const string REF_S_MIP = "system.switches.S_MIP_PARKING_BRAKE";

        // Config
        private readonly string _switchVariable;
        private readonly string _relayVariable;
        private readonly int _toeBrakeThreshold;

        // Live state
        private volatile int _toeLeft;
        private volatile int _toeRight;
        private volatile int _sMip;

        // Last values (to suppress duplicates)
        private int _lastToeLeft = int.MinValue;
        private int _lastToeRight = int.MinValue;
        private int _lastSw = int.MinValue;
        private int _lastSMip = int.MinValue;
        private int _lastRelay = int.MinValue;
        private bool? _lastDecision = null;

        private const int ToeLogDelta = 25;

        private DataRef _drToeL;
        private DataRef _drToeR;
        private DataRef _drS;

        private IDisposable _switchSubscription;

        public Custom_ParkingBrake(
            ProSimConnect connection,
            string switchVariable,
            string relayVariable,
            int toeBrakeThreshold = 1000)
        {
            _connection = connection;
            _switchVariable = switchVariable;
            _relayVariable = relayVariable;
            _toeBrakeThreshold = toeBrakeThreshold;

            // ProSim DataRefs (50 ms)
            _drToeL = new DataRef(REF_TOE_LEFT, 50, _connection);
            _drToeR = new DataRef(REF_TOE_RIGHT, 50, _connection);
            _drS = new DataRef(REF_S_MIP, 50, _connection);

            _drToeL.onDataChange += d =>
            {
                var newVal = SafeInt(d.value);
                if (Math.Abs(newVal - _toeLeft) >= ToeLogDelta)
                {
                    _toeLeft = newVal;
                    Evaluate();
                    SendSnapshotIfChanged();
                }
            };

            _drToeR.onDataChange += d =>
            {
                var newVal = SafeInt(d.value);
                if (Math.Abs(newVal - _toeRight) >= ToeLogDelta)
                {
                    _toeRight = newVal;
                    Evaluate();
                    SendSnapshotIfChanged();
                }
            };

            _drS.onDataChange += d =>
            {
                var newVal = SafeInt(d.value);
                if (newVal != _sMip)
                {
                    _sMip = newVal;
                    Evaluate();
                    SendSnapshotIfChanged();
                }
            };

            // Switch variable
            _switchSubscription = VariableManager.Subscribe(_switchVariable, (_, val) =>
            {
                if (_lastSw != val)
                {
                    _lastSw = val;
                    Evaluate();
                    SendSnapshotIfChanged();
                }
            });

            SendInfoLog("[PB] Custom_ParkingBrake initialized (change-only logging).");

            Evaluate();
            SendSnapshotIfChanged(force: true); // show initial snapshot
        }

        public void Close()
        {
            try { _switchSubscription?.Dispose(); } catch { }
        }

        private void Evaluate()
        {
            int sw = VariableManager.Get(_switchVariable);

            bool cToeL = _toeLeft > _toeBrakeThreshold;
            bool cToeR = _toeRight > _toeBrakeThreshold;
            bool cSw = (sw == 1);
            bool cMip = (_sMip == 0);

            bool cond = cToeL && cToeR && cSw && cMip;

            // Detect relay change around the Set
            int relayBefore = VariableManager.Get(_relayVariable);
            VariableManager.Set(_relayVariable, cond ? 1 : 0);
            int relayAfter = VariableManager.Get(_relayVariable);

            if (_lastDecision == null || _lastDecision.Value != cond)
            {
                _lastDecision = cond;
                SendInfoLog(
                    $@"[PB] DECISION: L({_toeLeft}{(cToeL ? ">" : "<=")}{_toeBrakeThreshold}) & " +
                    $@"R({_toeRight}{(cToeR ? ">" : "<=")}{_toeBrakeThreshold}) & " +
                    $@"Sw({sw}) & S_MIP({_sMip}) => {(cond ? "ON" : "OFF")}"
                );
            }

            // If relay flipped, print a MON now (change-only filter inside will still apply)
            if (relayAfter != relayBefore)
                SendSnapshotIfChanged();
        }


        private void SendSnapshotIfChanged(bool force = false)
        {
            int sw = VariableManager.Get(_switchVariable);
            int rv = VariableManager.Get(_relayVariable);

            bool changed =
                force ||
                _toeLeft != _lastToeLeft ||
                _toeRight != _lastToeRight ||
                sw != _lastSw ||
                _sMip != _lastSMip ||
                rv != _lastRelay;

            if (!changed) return;

            var line = $"[PB] MON L={_toeLeft} R={_toeRight} Sw={sw} S_MIP={_sMip} RelayVar={rv} Th={_toeBrakeThreshold}";
            SendInfoLog(line);                         // goes to your UI (if wired)
            System.Diagnostics.Debug.WriteLine(line);  // always appears in VS Output

            _lastToeLeft = _toeLeft;
            _lastToeRight = _toeRight;
            _lastSw = sw;
            _lastSMip = _sMip;
            _lastRelay = rv;
        }


        private static int SafeInt(object v)
        {
            try { return Convert.ToInt32(v); } catch { return 0; }
        }
    }
}
