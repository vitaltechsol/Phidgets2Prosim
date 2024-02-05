using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;

namespace Phidgets2Prosim
{
    internal class PhidgestOutput : PhidgetDevice
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        //bool isGate = false;
       
        int delay = 0;
        public int TurnOffAfterMs { get; set; } = 0;
        public bool Inverse { get; set; } = false;

        public bool IsGate { get; set; }
        public string ProsimDatmRefOff { get; set; }

        public PhidgestOutput(int serial, int hubPort, int channel, string prosimDatmRef, ProSimConnect connection, bool isGate = false, string prosimDatmRefOff = null, bool isHubPortDevice = false)
        {
            IsGate = isGate;
            Channel = channel;
            // Set ProSim dataref
            ProsimDataRef = prosimDatmRefOff;
            Serial = serial;
            if (prosimDatmRefOff != null) { 
                DataRef dataRef = new DataRef(prosimDatmRefOff, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            IsHubPortDevice = isHubPortDevice;

            try
            {
                ProsimDataRef = prosimDatmRef;

                digitalOutput.HubPort = hubPort;
                digitalOutput.IsRemote = true;
                digitalOutput.IsHubPortDevice = isHubPortDevice;
                digitalOutput.Channel = channel;
                digitalOutput.DeviceSerialNumber = serial;
                SendInfoLog("<-- Listening to " + prosimDatmRef + " to channel:" + channel);

                Open();


                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRef, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                SendErrorLog(ex.ToString());
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
                SendInfoLog("--> Channel " + Channel + ": ON");
                SendInfoLog("  |-- Ref: " + ProsimDataRef);


                // Turn off after specified time(ms)
                if (TurnOffAfterMs > 0)
                {
                    SendInfoLog("Start Delay " + TurnOffAfterMs + " for " + ProsimDataRef + " - Channel " + Channel);
                    var taskDelay2 = Task.Delay(TurnOffAfterMs);
                    await taskDelay2;
                    TurnOff();
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("Turn On Failed for " + ProsimDataRef + " - Channel " + Channel);
                SendErrorLog(ex.ToString());
            }
        }

        public void TurnOff()
        {
            try
            {
                SendInfoLog("<-- Channel " + Channel + ": OFF");
                SendInfoLog("  |-- Ref: " + ProsimDataRef);
                digitalOutput.DutyCycle = 0;
            }
            catch (Exception ex)
            {
                SendErrorLog("Turn Off Failed for channel " + ProsimDataRef + " - Channel " + Channel);
                SendErrorLog(ex.ToString());
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
                SendErrorLog("ERROR: Open failed for " + ProsimDataRef + " Serial:" + digitalOutput.DeviceSerialNumber);
                SendErrorLog(ex.ToString());
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
                    if (value == true && name == ProsimDataRef)
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
                        SendInfoLog("Turn Off from ref" + dataRef.value + " " + dataRef.name);
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
                SendErrorLog("DataRef_onDataChange failed for " + ProsimDataRef + " ch:" + Channel);
                SendErrorLog("value " + dataRef.value);
                SendErrorLog(ex.ToString());
            }
        }

    }
}
