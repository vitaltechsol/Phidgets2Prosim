using Phidget22;
using ProSimSDK;
using System.Diagnostics;

namespace Phidgets2Prosim
{
    internal class PhidgetsInput
    {
        DigitalInput digitalInput = new DigitalInput();
        int inputValue;
        int offInputValue;
        string prosimDataRef;
        ProSimConnect connection;


        public PhidgetsInput(int serial, int hubPort, int channel, ProSimConnect connection, string prosimDataRef, int inputValue, int offInputValue = 0)
        {

            this.prosimDataRef = prosimDataRef;
            this.connection = connection;
            this.offInputValue = offInputValue;

            digitalInput.HubPort = hubPort;
            digitalInput.IsRemote = true;
            digitalInput.Channel = channel;
            digitalInput.StateChange += StateChange;
            digitalInput.DeviceSerialNumber = serial; 
            Open();

            this.inputValue = inputValue;
        }

        private void StateChange(object sender, Phidget22.Events.DigitalInputStateChangeEventArgs e)
        {
            // Set ProSim dataref
            Debug.WriteLine("**** State: " + e.State);
            DataRef dataRef = new DataRef(prosimDataRef, 100, connection);

            try
            {
                if (e.State == true)
                {
                    dataRef.value = inputValue;
                }
                else
                {
                    dataRef.value = offInputValue;
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error: Input " + prosimDataRef + " - Value:" + inputValue);
                Debug.WriteLine(ex.ToString());
            }
        }

        public void Close()
        {
            digitalInput.Close();
        }

        public void Open()
        {
            digitalInput.Open(500);
        }

    }
}
