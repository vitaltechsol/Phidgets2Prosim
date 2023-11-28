using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgestOutput
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        bool isGate = false;
        int delay = 0;
        string prosimDatmRef;
        string prosimDatmRefOff;
        public int TurnOffAfterMs { get; set; } = 0;
        public bool Inverse { get; set; } = false;

        public PhidgestOutput(int deviceSerialNo, int hubPort, int channel, string prosimDatmRef, ProSimConnect connection)
        {
            try
            {
                digitalOutput.HubPort = hubPort;
                digitalOutput.IsRemote = true;
                digitalOutput.Channel = channel;
                digitalOutput.DeviceSerialNumber = deviceSerialNo;
                Open();

                this.prosimDatmRef = prosimDatmRef;

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRef, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public PhidgestOutput(int deviceSerialNo, int hubPort, int channel, string prosimDatmRef, ProSimConnect connection, bool isGate, string prosimDatmRefOff = null) : this(deviceSerialNo, hubPort, channel, prosimDatmRef, connection)
        {
           this.isGate = isGate;
            // Set ProSim dataref
            this.prosimDatmRefOff = prosimDatmRefOff;
            if (prosimDatmRefOff != null) { 
                DataRef dataRef = new DataRef(prosimDatmRefOff, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
        }

        public void AddDelay(int delay)
        {
            this.delay = delay;
        }

        public async void TurnOn()
        {
            try
            {
                digitalOutput.Open(1000);

                if (delay > 0)
                {
                    var taskDelay = Task.Delay(delay);
                    await taskDelay;
                }

                digitalOutput.DutyCycle = 1;

                // Turn off after specified time(ms)
                if (TurnOffAfterMs > 0)
                {
                    Debug.WriteLine("Start Delay " + TurnOffAfterMs);
                    var taskDelay2 = Task.Delay(TurnOffAfterMs);
                    await taskDelay2;
                    TurnOff();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void TurnOff()
        {
            try
            {
                Debug.WriteLine("Torn Off");
                digitalOutput.Open(1000);
                digitalOutput.DutyCycle = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void Close()
        {
            digitalOutput.Close();
        }

        private async void Open()
        {
            try
            {
                digitalOutput.Open(500);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
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
                        Debug.WriteLine("Torn on " + dataRef.value + " " + dataRef.name);
                        if (Inverse)
                        {
                            TurnOff();
                        }
                        else
                        {
                            TurnOn();
                        }
                    }

                    if (value == true && name == prosimDatmRefOff)
                    {
                        Debug.WriteLine("Torn Off from ref" + dataRef.value + " " + dataRef.name);
                        if (Inverse)
                        {
                            TurnOn();
                        }
                        else
                        {
                            TurnOff();
                        }
                    }

                    if (prosimDatmRefOff == null && value == false)
                    {
                        Debug.WriteLine("Torn Off" + dataRef.value + " " + dataRef.name);

                        if (Inverse)
                        {
                            TurnOn();
                        }
                        else
                        {
                            TurnOff();
                        }
                    }
                }
                else { 

                    var value = Convert.ToInt32(dataRef.value);

                    if (value == 2)
                    {
                        if (Inverse)
                        {
                            TurnOff();
                        } else
                        {
                            TurnOn();
                        }
                        
                    }

                    if (value == 0)
                    {
                        if (Inverse)
                        {
                            TurnOn();
                        }
                        else
                        {
                            TurnOff();
                        }
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
