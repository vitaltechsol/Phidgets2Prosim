using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsInput : PhidgetDevice
    {
        DigitalInput digitalInput = new DigitalInput();

        public int InputValue { get; set; }
        public int OffInputValue { get; set; } = 0;

        public PhidgetsInput(int serial, int hubPort, int channel, ProSimConnect connection, string prosimDataRef, int inputValue, int offInputValue = 0)
        {

            ProsimDataRef = prosimDataRef;
            Connection = connection;
            Channel = channel;
            HubPort = hubPort;

            OffInputValue = offInputValue;
            InputValue = inputValue;

            digitalInput.HubPort = hubPort;
            digitalInput.IsRemote = true;
            digitalInput.Channel = channel;
            digitalInput.StateChange += StateChange;
            digitalInput.DeviceSerialNumber = serial;
            SendInfoLog("->****Attached " + prosimDataRef + " to Ch:" + channel);

            Open();
        }

        private void StateChange(object sender, Phidget22.Events.DigitalInputStateChangeEventArgs e)
        {
            // Set ProSim dataref
            SendInfoLog($"--> [{HubPort}] Ch {Channel}: {e.State}");
            SendInfoLog($"  |-- Ref: {ProsimDataRef} - inputValue: {InputValue} - offInputValue {OffInputValue}");
            
            if (ProsimDataRef == "test")
            {
                return;
            }

            DataRef dataRef = new DataRef(ProsimDataRef, 100, Connection);

            try
            {
                if (e.State == true)
                {
                    dataRef.value = InputValue;
                }
                else
                {
                    dataRef.value = OffInputValue;
                }
            }
            catch (System.Exception ex)
            {
                SendErrorLog("Error: Input " + ProsimDataRef + " - Value:" + InputValue);
                SendErrorLog(ex.ToString());
                Debug.WriteLine("Error: Input " + ProsimDataRef + " - Value:" + InputValue);
                Debug.WriteLine(ex.ToString());
            }
        }

        public void Close()
        {
            digitalInput.Close();
        }

        public async void Open()
        {

            try
            {
                if (digitalInput.IsOpen == false)
                {
                    await Task.Run(() => digitalInput.Open(500));
                }
            }
            catch (System.Exception ex)
            {
                SendErrorLog("Error: --> Channel " + Channel + " Input " + ProsimDataRef + " - Value:" + InputValue);
                SendErrorLog(ex.ToString());

                Debug.WriteLine("Error: --> Channel " + Channel + " Input " + ProsimDataRef + " - Value:" + InputValue);
                Debug.WriteLine(ex.ToString());
            }
        }

    }
}
