using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    internal class PhidgetsInput
    {
        DigitalInput digitalInput = new DigitalInput();
        int inputValue;
        string prosimDatmRef;
        ProSimConnect connection;


        public PhidgetsInput(int hubPort, int channel, string prosimDatmRef, int inputValue, ProSimConnect connection) {

            this.prosimDatmRef = prosimDatmRef;
            this.connection = connection;

            digitalInput.HubPort = hubPort;
            digitalInput.IsRemote = true;
            digitalInput.Channel = channel;
            digitalInput.StateChange += StateChange;
            digitalInput.DeviceSerialNumber = 618534;
            Open();

            this.inputValue = inputValue;
            // Set ProSim dataref
        }

        private void StateChange(object sender, Phidget22.Events.DigitalInputStateChangeEventArgs e)
        {

            
           Debug.WriteLine("**** State: " + e.State);
           DataRef dataRef = new DataRef(prosimDatmRef, 100, connection);

            try
            {
                if (e.State == true)
                {
                    dataRef.value = inputValue;
                }
                else
                {
                    dataRef.value = 0;
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Input Error " + ex);
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
