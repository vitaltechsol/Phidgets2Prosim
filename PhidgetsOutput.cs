using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgestOuput
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        bool isGate = false;
        int delay = 0;
        string prosimDatmRef;
        string prosimDatmRefOff;

        public PhidgestOuput(int hubPort, int channel, string prosimDatmRef, ProSimConnect connection)
        {
            try
            {
                digitalOutput.HubPort = hubPort;
                digitalOutput.IsRemote = true;
                digitalOutput.Channel = channel;
                this.Open();

                this.prosimDatmRef = prosimDatmRef;

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRef, 10, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public PhidgestOuput(int hubPort, int channel, string prosimDatmRef, ProSimConnect connection, bool isGate, string prosimDatmRefOff = null) : this(hubPort, channel, prosimDatmRef, connection)
        {
           this.isGate = isGate;
            // Set ProSim dataref
            this.prosimDatmRefOff = prosimDatmRefOff;
            if (prosimDatmRefOff != null) { 
                DataRef dataRef = new DataRef(prosimDatmRefOff, 10, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
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

        private async void Open()
        {
            digitalOutput.Open(1000);
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {

            var name = dataRef.name;
            try
            {
                Debug.WriteLine("OUT " + dataRef.value + " " + dataRef.name);

                if (isGate)
                {
                    var value = Convert.ToBoolean(dataRef.value);
                    if (value == true && name == prosimDatmRef)
                    {
                        TurnOn();
                    }

                    if (value == true && name == prosimDatmRefOff)
                    {
                        TurnOff();
                    }

                    if (prosimDatmRefOff == null && value == false)
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
