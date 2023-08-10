using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    internal class PhidgestInput
    {
        DigitalInput digitalInput = new DigitalInput();
        int inputValue;
        static DataRef dataRef;
        string prosimDatmRef;
        ProSimConnect connection;


        public PhidgestInput(int hubPort, int channel, string prosimDatmRef, int inputValue, ProSimConnect connection) {

            this.prosimDatmRef = prosimDatmRef;
            this.connection = connection;

            digitalInput.HubPort = hubPort;
            digitalInput.IsRemote = true;
            digitalInput.Channel = channel;
            digitalInput.StateChange += StateChange;
            digitalInput.DeviceSerialNumber = 618534;

            digitalInput.Open(5000);

            this.inputValue = inputValue;
            // Set ProSim dataref
            // dataRef = new DataRef(prosimDatmRef, 100, connection);
        }

        private void StateChange(object sender, Phidget22.Events.DigitalInputStateChangeEventArgs e)
        {

            
           Debug.WriteLine("**** State: " + e.State);
           DataRef dataRef = new DataRef(prosimDatmRef, 10, connection);

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
            digitalInput.Open(5000);
        }

    }
}
