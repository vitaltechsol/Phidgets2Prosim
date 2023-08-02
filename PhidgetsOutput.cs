using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
    internal class PhidgestOuput
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        bool isGate = false;
        int delay = 0;

        public PhidgestOuput(int hubPort, int channel, string prosimDatmRef, ProSimConnect connection) {
            digitalOutput.HubPort = hubPort;
            digitalOutput.IsRemote = true;
            digitalOutput.Channel = channel;
            digitalOutput.Open(1000);


            // Set ProSim dataref
            DataRef dataRef = new DataRef(prosimDatmRef, 10, connection);
            dataRef.onDataChange += DataRef_onDataChange;
        }

        public PhidgestOuput(int hubPort, int channel, string prosimDatmRef, ProSimConnect connection, bool isGate) : this(hubPort, channel, prosimDatmRef, connection)
        {
           this.isGate = isGate;
        }

        public void AddDelay(int delay)
        {
            this.delay = delay;
        }

        public async void TurnOn()
        {
            if (delay > 0) { 
                var taskDelay = Task.Delay(delay); 
                await taskDelay;
            }
            digitalOutput.DutyCycle = 1;
        }

        public void TurnOff()
        {
            digitalOutput.Open(1000);
            digitalOutput.DutyCycle = 0;
        }

        public void Close()
        {
            digitalOutput.Close();
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {



            // var name = dataRef.name;
            try
            {
                Debug.WriteLine("OUT " + dataRef.value + " " + dataRef.name);

                if (isGate)
                {
                    var value = Convert.ToBoolean(dataRef.value);
                    if (value == true)
                    {
                        TurnOn();
                    } else
                    {
                        TurnOff();
                    }
                }
                else { 

                    var value = Convert.ToInt32(dataRef.value);

                    if (value == 2)
                    {
                        TurnOn();
                    }

                    if (value == 0)
                    {
                        TurnOff();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }
        }

    }
}
