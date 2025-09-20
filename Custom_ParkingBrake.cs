using ProSimSDK;
using System;
using System.Timers;
using System.Diagnostics;

namespace Phidgets2Prosim
{
	internal class Custom_ParkingBrake : PhidgetDevice
	{
		private readonly ProSimConnect _connection;

		// Config
		private readonly string _switchVariable;
		private readonly string _relayVariable;
		private readonly string _refToeLeft;
		private readonly string _refToeRight;
		private readonly string _refS_MipParkingBrake;
		private readonly int _toeBrakeThreshold;

		// State caches
		private volatile int _toeLeft;
		private volatile int _toeRight;
		private volatile int _sMip;

		// Subscriptions / helpers
		private IDisposable _switchSubscription;
		private Timer _diagTimer;

		// Keep DataRef instances to avoid GC
		private DataRef _drToeL;
		private DataRef _drToeR;
		private DataRef _drS;

		public Custom_ParkingBrake(
			ProSimConnect connection,
			string switchVariable,
			string relayVariable,
			string refToeLeft = "system.analog.A_FC_TOEBRAKE_LEFT_CAPT",
			string refToeRight = "system.analog.A_CP_TOEBRAKE_RIGHT_CAPT",
			string refS_MipParkingBrake = "system.switches.S_MIP_PARKING_BRAKE",
			int toeBrakeThreshold = 1000)
		{
			_connection = connection;
			_switchVariable = switchVariable;
			_relayVariable = relayVariable;
			_refToeLeft = refToeLeft;
			_refToeRight = refToeRight;
			_refS_MipParkingBrake = refS_MipParkingBrake;
			_toeBrakeThreshold = toeBrakeThreshold;

			// DataRefs
			_drToeL = new DataRef(_refToeLeft, 50, _connection);
			_drToeR = new DataRef(_refToeRight, 50, _connection);
			_drS = new DataRef(_refS_MipParkingBrake, 50, _connection);

			_drToeL.onDataChange += d =>
			{
				int prev = _toeLeft;
				_toeLeft = SafeInt(d.value);
				LogInfo($"ToeL {prev} -> {_toeLeft}");
				Evaluate();
			};

			_drToeR.onDataChange += d =>
			{
				int prev = _toeRight;
				_toeRight = SafeInt(d.value);
				LogInfo($"ToeR {prev} -> {_toeRight}");
				Evaluate();
			};

			_drS.onDataChange += d =>
			{
				int prev = _sMip;
				_sMip = SafeInt(d.value);
				LogInfo($"S_MIP {prev} -> {_sMip}");
				Evaluate();
			};

			// Switch variable subscription
			_switchSubscription = VariableManager.Subscribe(_switchVariable, (name, val) =>
			{
				LogInfo($"SwitchVar {name} = {val}");
				Evaluate();
			});

			// Diagnostics timer (300 ms)
			_diagTimer = new Timer(300);
			_diagTimer.AutoReset = true;
			_diagTimer.Elapsed += (_, __) =>
			{
				int sw = VariableManager.Get(_switchVariable);
				int rv = VariableManager.Get(_relayVariable);
				LogInfo($"MON L={_toeLeft} R={_toeRight} Sw={sw} S_MIP={_sMip} RelayVar={rv} Th={_toeBrakeThreshold}");
			};
			_diagTimer.Start();

			LogInfo("Custom_ParkingBrake initialized (Variable-driven; diagnostics ON).");
			Evaluate();
		}

		private static int SafeInt(object v)
		{
			try { return Convert.ToInt32(v); } catch { return 0; }
		}

		private void Evaluate()
		{
			int sw = VariableManager.Get(_switchVariable);

			bool cToeL = _toeLeft > _toeBrakeThreshold;
			bool cToeR = _toeRight > _toeBrakeThreshold;
			bool cSw = (sw == 1);
			bool cMip = (_sMip == 0);

			bool cond = cToeL && cToeR && cSw && cMip;

			VariableManager.Set(_relayVariable, cond ? 1 : 0);

			LogInfo(
				$"DECISION: L({_toeLeft}{(cToeL ? ">" : "<=")}{_toeBrakeThreshold}) & " +
				$"R({_toeRight}{(cToeR ? ">" : "<=")}{_toeBrakeThreshold}) & " +
				$"Sw({sw}) & S_MIP({_sMip}) => {(cond ? "ON" : "OFF")}"
			);
		}

		public void Close()
		{
			try { if (_diagTimer != null) { _diagTimer.Stop(); _diagTimer.Dispose(); } } catch { }
			try { _switchSubscription?.Dispose(); } catch { }
		}

		// Unified logging helper: goes to your app log (SendInfoLog) and VS Output
		private void LogInfo(string message)
		{
			SendInfoLog("[PB] " + message);
			Debug.WriteLine("[PB] " + message);
		}
	}
}
