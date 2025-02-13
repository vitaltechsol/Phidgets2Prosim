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

        public PhidgetsInput(int serial, int hubPort, int channel, ProSimConnect connection, string prosimDataRef, int inputValue, int offInputValue = 0)
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
            // Set ProSim dataref
            SendInfoLog($"--> [{HubPort}] Ch {Channel}: {e.State} | Ref: {ProsimDataRef} - inputValue: {InputValue} - offInputValue {OffInputValue}");

            if (ProsimDataRef == "test")
            {
                return;
            }

            DataRef dataRef = new DataRef(ProsimDataRef, 1000, Connection, true);
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
            }
        }


        public void Close()
        {
            digitalInput.Close();
            SendInfoLog($"-> Detached/Closed {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
        }

        public async void Open()
        {

            try
            {
                if (digitalInput.IsOpen == false)
                {

                    digitalInput.HubPort = HubPort;
                    digitalInput.IsRemote = true;
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
