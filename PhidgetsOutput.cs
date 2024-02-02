using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;

namespace Phidgets2Prosim
{
    internal class PhidgestOutput
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        //bool isGate = false;
        int delay = 0;
        public int TurnOffAfterMs { get; set; } = 0;
        public bool Inverse { get; set; } = false;

        //private bool isHubPortDevice = false;

        public int DeviceSerialNo { get; set; }
        public int HubPort { get; set; }
        public int Channel { get; set; }
        public string ProsimDatmRef { get; set; }
        public ProSimConnect Connection { get; set; }
        public bool IsGate { get; set; }
        public string ProsimDatmRefOff { get; set; }
        public bool IsHubPortDevice { get; set; }

        public PhidgestOutput(int deviceSerialNo, int hubPort, int channel, string prosimDatmRef, ProSimConnect connection, bool isGate = false, string prosimDatmRefOff = null, bool isHubPortDevice = false)
        {
            IsGate = isGate;
            Channel = channel;
            // Set ProSim dataref
            ProsimDatmRef = prosimDatmRefOff;
            if (prosimDatmRefOff != null) { 
                DataRef dataRef = new DataRef(prosimDatmRefOff, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            IsHubPortDevice = isHubPortDevice;

            try
            {
                ProsimDatmRef = prosimDatmRef;

                digitalOutput.HubPort = hubPort;
                digitalOutput.IsRemote = true;
                digitalOutput.IsHubPortDevice = isHubPortDevice;
                digitalOutput.Channel = channel;
                digitalOutput.DeviceSerialNumber = deviceSerialNo;
                Debug.WriteLine("<-****Attached " + prosimDatmRef + " to Ch:" + channel);

                Open();


                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRef, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
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
               // digitalOutput.Open(5000);

                if (delay > 0)
                {
                    var taskDelay = Task.Delay(delay);
                    await taskDelay;
                }

                digitalOutput.DutyCycle = 1;
                Debug.WriteLine("--> Channel " + Channel + ": ON");
                Debug.WriteLine("  |-- Ref: " + ProsimDatmRef);


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
                Debug.WriteLine("<-- Channel " + Channel + ": OFF");
                Debug.WriteLine("  |-- Ref: " + ProsimDatmRef);
                digitalOutput.DutyCycle = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: Turn Off Failed for channel " + ProsimDatmRef);
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
                await Task.Run(() => digitalOutput.Open(500));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: Open failed for " + ProsimDatmRef + " Serial:" + digitalOutput.DeviceSerialNumber);
                Debug.WriteLine(ex.ToString());
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {
       
            var name = dataRef.name;
            try
            {
                if (IsGate)
                {
                    var value = Convert.ToBoolean(dataRef.value);
                    if (value == true && name == ProsimDatmRef)
                    {
                        if (Inverse)
                        {
                            TurnOff();
                        }
                        else
                        {
                            TurnOn();
                        }
                    }

                    if (value == true && name == ProsimDatmRefOff)
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

                    if (ProsimDatmRefOff == null && value == false)
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
                Debug.WriteLine("ERROR: DataRef_onDataChange failed for " + ProsimDatmRef + " ch:" + Channel);
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }
        }

    }
}
