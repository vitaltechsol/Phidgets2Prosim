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
		public string Variable { get; set; } = null;

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
		private void StateChange(object sender, Phidget22.Events.DigitalInputStateChangeEventArgs e)
		{
			SendInfoLog($"--> [{HubPort}] Ch {Channel}: {e.State} | Ref: {ProsimDataRef} - inputValue: {InputValue} - offInputValue {OffInputValue}");

			// If ref is "test" or blank, SKIP ProSim write but still update the Variable (if any)
			if (string.IsNullOrWhiteSpace(ProsimDataRef) || ProsimDataRef == "test")
			{
				if (!string.IsNullOrEmpty(Variable))
				{
					var newVal = e.State ? InputValue : OffInputValue;
					VariableManager.Set(Variable, newVal);
					SendInfoLog($"[Input→Var] {Variable} = {newVal}");
				}
				return;
			}

			DataRef dataRef = new DataRef(ProsimDataRef, 1000, Connection, true);
			try
			{
				dataRef.value = e.State ? InputValue : OffInputValue;

				// Mirror to Variable too (when configured)
				if (!string.IsNullOrEmpty(Variable))
				{
					var newVal = e.State ? InputValue : OffInputValue;
					VariableManager.Set(Variable, newVal);
					SendInfoLog($"[Input→Var] {Variable} = {newVal}");
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
                digitalInput.Close();
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
