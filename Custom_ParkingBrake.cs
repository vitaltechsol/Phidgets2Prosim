using ProSimSDK;
using System;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
	internal class Custom_ParkingBrake : PhidgetDevice
	{
		private readonly ProSimConnect _connection;

		// Hard-coded ProSim DataRefs
		private const string REF_TOE_LEFT = "system.analog.A_FC_TOEBRAKE_LEFT_CAPT";
		private const string REF_TOE_RIGHT = "system.analog.A_FC_TOEBRAKE_RIGHT_CAPT";
		private const string REF_S_MIP = "system.switches.S_MIP_PARKING_BRAKE";
		private const string REF_I_MIP = "system.indicators.I_MIP_PARKING_BRAKE";
		// ProSim's own, authoritative release decision. This fires on ProSim's
		// schedule based on its internal systems logic - NOT something we get
		// to gate or refuse. Our hardware must always follow it unconditionally,
		// or the physical handle and the simulated aircraft state can diverge
		// permanently (the "stuck handle" failure mode).
		private const string REF_RELEASE = "system.gates.B_PARKING_BRAKE_RELEASE";

		// Config - engage
		private readonly string _switchVariable;
		private readonly string _relayVariable;
		private readonly int _toeBrakeThreshold;

		// Config - release
		// NOTE: ReleaseToeBrakeThreshold is currently unused by this class.
		// We no longer compute our own release condition - we just mirror
		// ProSim's B_PARKING_BRAKE_RELEASE dataref unconditionally. Kept in
		// config for now in case it's needed again once the S_MIP delay
		// (separate piece of work) is added.
		private readonly int _releaseToeBrakeThreshold;
		private readonly string _releaseVariable;
		private readonly int _releaseDelayOnMs;
		private readonly int _releaseMaxTimeOnMs;

		// Live state - engage
		private volatile int _toeLeft;
		private volatile int _toeRight;
		private volatile int _sMip;

		// Live state - release (monitoring/logging only, does not gate anything)
		private volatile int _iMip;
		private bool _lastReleaseSignal = false;
		private bool _releaseInProgress = false;
		// True once I_MIP has been genuinely ON at some point during the current
		// engagement. CheckMismatch() only fires if I_MIP dropped to 0 AFTER being
		// on - never on the transient I_MIP=0 that's expected right after S_MIP
		// sets, before ProSim has caught up to reflect the engage.
		private bool _iMipWasOnThisCycle = false;

		// Last values (to suppress duplicates)
		private int _lastToeLeft = int.MinValue;
		private int _lastToeRight = int.MinValue;
		private int _lastSw = int.MinValue;
		private int _lastSMip = int.MinValue;
		private int _lastIMipLogged = int.MinValue;
		private int _lastRelay = int.MinValue;
		private bool? _lastDecision = null;

		private const int ToeLogDelta = 25;

		private DataRef _drToeL;
		private DataRef _drToeR;
		private DataRef _drS;
		private DataRef _drIMip;
		private DataRef _drRelease;

		private IDisposable _switchSubscription;

		public Custom_ParkingBrake(
			ProSimConnect connection,
			string switchVariable,
			string relayVariable,
			int toeBrakeThreshold = 1000,
			int releaseToeBrakeThreshold = 900,
			string releaseVariable = null,
			int releaseDelayOnMs = 50,
			int releaseMaxTimeOnMs = 1000)
		{
			_connection = connection;
			_switchVariable = switchVariable;
			_relayVariable = relayVariable;
			_toeBrakeThreshold = toeBrakeThreshold;

			_releaseToeBrakeThreshold = releaseToeBrakeThreshold;
			_releaseVariable = releaseVariable;
			_releaseDelayOnMs = releaseDelayOnMs;
			_releaseMaxTimeOnMs = releaseMaxTimeOnMs;

			// ProSim DataRefs (50 ms)
			_drToeL = new DataRef(REF_TOE_LEFT, 50, _connection);
			_drToeR = new DataRef(REF_TOE_RIGHT, 50, _connection);
			_drS = new DataRef(REF_S_MIP, 50, _connection);
			_drIMip = new DataRef(REF_I_MIP, 50, _connection);
			_drRelease = new DataRef(REF_RELEASE, 50, _connection);

			_drToeL.onDataChange += d =>
			{
				var newVal = SafeInt(d.value);
				if (Math.Abs(newVal - _toeLeft) >= ToeLogDelta)
				{
					_toeLeft = newVal;
					Evaluate();
					CheckMismatch();
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
					CheckMismatch();
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
					CheckMismatch();
					SendSnapshotIfChanged();
				}
			};

			_drIMip.onDataChange += d =>
			{
				var newVal = SafeInt(d.value);
				if (newVal != _iMip)
				{
					_iMip = newVal;
					if (_iMip != 0) _iMipWasOnThisCycle = true;
					CheckMismatch();
					SendSnapshotIfChanged();
				}
			};

			// Unconditional mirror of ProSim's own release decision. Fires the
			// physical release the instant ProSim signals it - no suppression,
			// no arming, no exceptions. This is what keeps the physical handle
			// in sync with the simulated aircraft state.
			_drRelease.onDataChange += d =>
			{
				bool nowTrue = SafeInt(d.value) != 0;
				if (nowTrue && !_lastReleaseSignal)
				{
					SendInfoLog("[PB] ProSim signaled release (B_PARKING_BRAKE_RELEASE) - firing hardware.");
					_ = FireReleaseAsync();
				}
				_lastReleaseSignal = nowTrue;
				CheckMismatch();
				SendSnapshotIfChanged();
			};

			// Switch variable
			_switchSubscription = VariableManager.Subscribe(_switchVariable, (_, val) =>
			{
				if (_lastSw != val)
				{
					_lastSw = val;
					Evaluate();
					CheckMismatch();
					SendSnapshotIfChanged();
				}
			});

			if (string.IsNullOrWhiteSpace(_releaseVariable))
			{
				SendInfoLog("[PB] No ReleaseVariable configured - release action disabled.");
			}
			else
			{
				// Make sure the variable exists at a known (off) state before
				// any hardware output binds to it via UserVariable.
				VariableManager.Set(_releaseVariable, 0);
			}

			SendInfoLog("[PB] Custom_ParkingBrake initialized (change-only logging).");

			Evaluate();
			CheckMismatch();
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

		private void CheckMismatch()
		{
			if (string.IsNullOrWhiteSpace(_releaseVariable))
				return;

			if (_iMip == 0 && _sMip != 0 && _iMipWasOnThisCycle)
			{
				SendInfoLog("[PB] MISMATCH: I_MIP dropped to 0 but S_MIP still set - forcing release.");
				_ = FireReleaseAsync();
			}

			if (_sMip == 0)
			{
				// Handle confirmed released (or never engaged) - ready for next cycle.
				_iMipWasOnThisCycle = false;
			}
		}

		private async Task FireReleaseAsync()
		{
			if (string.IsNullOrWhiteSpace(_releaseVariable))
				return;

			// Guard against overlapping pulses if ProSim signals release again
			// while a pulse is already in flight - does not suppress the
			// signal, just avoids stacking timers on the same output.
			if (_releaseInProgress)
				return;

			_releaseInProgress = true;
			try
			{
				if (_releaseDelayOnMs > 0)
					await Task.Delay(_releaseDelayOnMs);

				VariableManager.Set(_releaseVariable, 1);
				SendInfoLog($"[PB] {_releaseVariable} = 1 (release ON)");

				if (_releaseMaxTimeOnMs > 0)
				{
					await Task.Delay(_releaseMaxTimeOnMs);

					VariableManager.Set(_releaseVariable, 0);
					SendInfoLog($"[PB] {_releaseVariable} = 0 (release OFF)");
				}
			}
			catch (Exception ex)
			{
				SendErrorLog("[PB] Release pulse failed");
				SendErrorLog(ex.ToString());
			}
			finally
			{
				_releaseInProgress = false;
			}
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
				_iMip != _lastIMipLogged ||
				rv != _lastRelay;

			if (!changed) return;

			var line = $"[PB] MON L={_toeLeft} R={_toeRight} Sw={sw} S_MIP={_sMip} I_MIP={_iMip} " +
					   $"RelayVar={rv} ReleaseSignal={_lastReleaseSignal} EngageTh={_toeBrakeThreshold}";
			SendInfoLog(line);                         // goes to your UI (if wired)
			System.Diagnostics.Debug.WriteLine(line);  // always appears in VS Output

			_lastToeLeft = _toeLeft;
			_lastToeRight = _toeRight;
			_lastSw = sw;
			_lastSMip = _sMip;
			_lastIMipLogged = _iMip;
			_lastRelay = rv;
		}


		private static int SafeInt(object v)
		{
			try { return Convert.ToInt32(v); } catch { return 0; }
		}
	}
}