using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
	internal class PhidgetsInput : PhidgetDevice
	{
		DigitalInput digitalInput = new DigitalInput();

		public int InputValue { get; set; }
		public string ProsimDataRef2 { get; set; } = null;
		public string ProsimDataRef3 { get; set; } = null;

		public int OffInputValue { get; set; } = 0;
		public string UserVariable { get; set; } = null;

		public PhidgetsInput(int serial, int hubPort, int channel, ProSimConnect connection, string prosimDataRef, int inputValue = 1, int offInputValue = 0)
		{

			ProsimDataRef = prosimDataRef;
			Connection = connection;
			Channel = channel;
			HubPort = hubPort;
			Serial = serial;

			OffInputValue = offInputValue;
			InputValue = inputValue;

			Open();
		}
		private void DigitalInput_Detach(object sender, Phidget22.Events.DetachEventArgs e)
		{
			// No local write here - we don't know what the physical state did while
			// disconnected, and guessing would be worse than staying at the last
			// known value. This exists mainly so a dropout is visible in the log
			// instead of silently freezing our cached state with no trace.
			SendErrorLog($"[Input] DETACHED {ProsimDataRef} [{HubPort}] Ch:{Channel} - hub/network connection lost.");
		}

		private async void DigitalInput_Attach(object sender, Phidget22.Events.AttachEventArgs e)
		{
			// Fires on the initial open too, not just reconnects - harmless either way.
			// On a reconnect specifically, this is the only way we find out the
			// physical state may have changed while we had no event stream at all -
			// StateChange only fires on an edge, and there's no edge to see if we
			// were disconnected when it happened.
			//
			// Right after Attach fires, Phidget22 may not have received the actual
			// value from the device yet (throws PhidgetException 0x33 - "Unknown or
			// Invalid Value" - if read too early). Retry briefly rather than giving
			// up on the first attempt.
			const int maxAttempts = 5;
			const int retryDelayMs = 100;

			for (int attempt = 1; attempt <= maxAttempts; attempt++)
			{
				try
				{
					bool currentState = digitalInput.State;
					SendInfoLog($"[Input] (RE)ATTACHED {ProsimDataRef} [{HubPort}] Ch:{Channel} - current physical state: {currentState}. Re-syncing.");
					WriteState(currentState);
					return;
				}
				catch (Phidget22.PhidgetException) when (attempt < maxAttempts)
				{
					await Task.Delay(retryDelayMs);
				}
				catch (System.Exception ex)
				{
					SendErrorLog($"[Input] Failed to re-sync state on attach for {ProsimDataRef}");
					SendErrorLog(ex.ToString());
					return;
				}
			}

			SendErrorLog($"[Input] Gave up reading state after attach for {ProsimDataRef} - value never became available after {maxAttempts} attempts.");
		}

		private void StateChange(object sender, Phidget22.Events.DigitalInputStateChangeEventArgs e)
		{
			SendInfoLog($"--> [{HubPort}] Ch {Channel}: {e.State} | Ref: {ProsimDataRef} - inputValue: {InputValue} - offInputValue {OffInputValue}");
			WriteState(e.State);
		}

		private void WriteState(bool on)
		{
			// If ref is "test" or blank, SKIP ProSim write but still update the Variable (if any)
			if (string.IsNullOrWhiteSpace(ProsimDataRef) || ProsimDataRef == "test")
			{
				if (!string.IsNullOrEmpty(UserVariable))
				{
					var newVal = on ? InputValue : OffInputValue;
					VariableManager.Set(UserVariable, newVal);
					SendInfoLog($"[Input→Var] {UserVariable} = {newVal}");
				}
				return;
			}

			DataRef dataRef = new DataRef(ProsimDataRef, 1000, Connection, true);
			try
			{
				dataRef.value = on ? InputValue : OffInputValue;

				// Mirror to Variable too (when configured)
				if (!string.IsNullOrEmpty(UserVariable))
				{
					var newVal = on ? InputValue : OffInputValue;
					VariableManager.Set(UserVariable, newVal);
					SendInfoLog($"[Input→Var] {UserVariable} = {newVal}");
				}
			}
			catch (System.Exception ex)
			{
				SendErrorLog("Error: Input " + ProsimDataRef + " - Value:" + InputValue);
				SendErrorLog(ex.ToString());
			}
		}

		public void Close()
		{
			try
			{
				if (digitalInput != null && digitalInput.IsOpen)
				{

					digitalInput.Close();
				}
				SendInfoLog($"-> Detached/Closed {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
			}
			catch (System.Exception ex)
			{
				SendInfoLog($"ERROR Detaching / Closing {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
				SendErrorLog(ex.Message);
			}
		}

		public async void Open()
		{

			try
			{
				if (digitalInput.IsOpen == false)
				{

					if (HubPort >= 0)
					{
						digitalInput.HubPort = HubPort;
						digitalInput.IsRemote = true;
						// use -1 for channel when is a IsHubPortDevice
						digitalInput.IsHubPortDevice = Channel == -1;
					}

					digitalInput.Channel = Channel;
					digitalInput.StateChange += StateChange;
					digitalInput.Attach += DigitalInput_Attach;
					digitalInput.Detach += DigitalInput_Detach;
					digitalInput.DeviceSerialNumber = Serial;
					await Task.Run(() => digitalInput.Open(5000));
					SendInfoLog($"-> Attached {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
				}
				else
				{
					SendErrorLog("Error: --> Channel (ALREADY OPEN)" + Channel + " Input " + ProsimDataRef + " - Value:" + InputValue);
				}
			}
			catch (System.Exception ex)
			{
				SendErrorLog("Error: --> Channel " + Channel + " Input " + ProsimDataRef + " - Value:" + InputValue);
				SendErrorLog(ex.ToString());
			}
		}

	}
}