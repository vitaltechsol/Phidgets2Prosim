using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;

namespace Phidgets2Prosim
{
	internal class Custom_ParkingBrake : PhidgetDevice
	{
		private readonly ProSimConnect connection;
		private readonly PhidgetsOutput brakeOutput;

		private double leftCapt = 0, rightCapt = 0, leftFO = 0, rightFO = 0;
		private int inputValue = 0;
		private int parkingBrake = 0;
		private readonly double threshold;

		public Custom_ParkingBrake(CustomParkingBrakeInst inst, ProSimConnect connection)
		{
			this.connection = connection;
			this.threshold = inst.ToeBrakeThreshold;

			// CAPT pair
			var drLCapt = new DataRef("system.analog." + inst.ToeBrakeLeftCaptRef, 100, connection);
			var drRCapt = new DataRef("system.analog." + inst.ToeBrakeRightCaptRef, 100, connection);
			// FO pair
			var drLFO = new DataRef("system.analog." + inst.ToeBrakeLeftFORef, 100, connection);
			var drRFO = new DataRef("system.analog." + inst.ToeBrakeRightFORef, 100, connection);

			var drPB = new DataRef("system.switches." + inst.ParkingBrakeRef, 100, connection);

			drLCapt.onDataChange += (dr) => { leftCapt = Convert.ToDouble(dr.value); EvaluateLogic(); };
			drRCapt.onDataChange += (dr) => { rightCapt = Convert.ToDouble(dr.value); EvaluateLogic(); };
			drLFO.onDataChange += (dr) => { leftFO = Convert.ToDouble(dr.value); EvaluateLogic(); };
			drRFO.onDataChange += (dr) => { rightFO = Convert.ToDouble(dr.value); EvaluateLogic(); };
			drPB.onDataChange += (dr) => { parkingBrake = Convert.ToInt32(dr.value); EvaluateLogic(); };

			// Physical input (just used as a condition gate; ProSim ref here is a dummy)
			var input = new PhidgetsInput(
				inst.InputSerial, inst.InputHubPort, inst.InputChannel,
				connection, "custom.parkingbrake.input", inst.InputValue, inst.OffInputValue);

			var drInput = new DataRef("custom.parkingbrake.input", 100, connection);
			drInput.onDataChange += (dr) =>
			{

				// If your InputValue/OffInputValue are 1/0, this is fine:
				inputValue = Convert.ToInt32(dr.value);
				// If they aren’t 1/0, normalize like:
				// inputValue = (Convert.ToInt32(dr.value) == inst.InputValue) ? 1 : 0;
				EvaluateLogic();
			};

			// Output relay: optional real ProSim binding, else use a clear dummy name
			string outputRef = !string.IsNullOrWhiteSpace(inst.OutputProsimDataRef)
								? inst.OutputProsimDataRef
								: "custom.parkingbrake.output";

			brakeOutput = new PhidgetsOutput(
				inst.OutputSerial, inst.OutputHubPort, inst.OutputChannel,
				outputRef, connection, /*isGate*/ true, /*prosimDataRefOff*/ null)
			{
				Delay = inst.DelayOn,
				MaxTimeOn = inst.MaxTimeOn
			};

			brakeOutput.ErrorLog += SendErrorLog;
			brakeOutput.InfoLog += SendInfoLog;
		}

		private void EvaluateLogic()
		{
			// “Either pair”: (CAPT-left & CAPT-right) OR (FO-left & FO-right)
			bool captPair = (leftCapt > threshold) && (rightCapt > threshold);
			bool foPair = (leftFO > threshold) && (rightFO > threshold);

			Debug.WriteLine($"[ParkingBrake] CAPT(L:{leftCapt} R:{rightCapt}) FO(L:{leftFO} R:{rightFO}) In={inputValue} PB={parkingBrake}");

			if ((captPair || foPair) && inputValue == 1 && parkingBrake == 0)
			{
				// Turn relay ON; Delay/MaxTimeOn are honored by PhidgetsOutput
				brakeOutput.TurnOn(1);
				Debug.WriteLine("[ParkingBrake] Output ON (either CAPT or FO pair pressed)");
			}
		}
	}
}
