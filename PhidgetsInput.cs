using Phidget22;
using ProSimSDK;
using System.Diagnostics;

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
            SendInfoLog("--> Channel " + Channel + ":" + e.State);
            SendInfoLog("  |-- Ref: " + ProsimDataRef + " - inputValue: " + InputValue + " - offInputValue:" + OffInputValue);
            
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
            }
        }

        public void Close()
        {
            digitalInput.Close();
        }

        public void Open()
        {

            try
            {
                digitalInput.Open(500);
            }
            catch (System.Exception ex)
            {
                SendErrorLog("Error: Input " + ProsimDataRef + " - Value:" + InputValue);
                SendErrorLog(ex.ToString());
            }
        }

    }
}
