using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using YamlDotNet.Core.Tokens;

namespace Phidgets2Prosim
{
    internal class PhidgestOutput : PhidgetDevice
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        //bool isGate = false;
       
        public int TurnOffAfterMs { get; set; } = 0;
        public bool Inverse { get; set; } = false;

        public bool IsGate { get; set; }
        public string ProsimDataRefOff { get; set; }

        public int Delay { get; set; } = 0;


        public PhidgestOutput(int serial, int hubPort, int channel, string prosimDataRef, ProSimConnect connection, bool isGate = false, string prosimDataRefOff = null)
        {
            IsGate = isGate;
            Channel = channel;
            // Set ProSim dataref
            ProsimDataRef = prosimDataRef;
            ProsimDataRefOff = prosimDataRefOff;
            Serial = serial;
      
            try
            {
                // use -1 for hubPort when is not a network hub

                if (hubPort >= 0)
                { 
                    digitalOutput.HubPort = hubPort;
                    digitalOutput.IsRemote = true;
                    // use -1 for channel when is a IsHubPortDevice
                    digitalOutput.IsHubPortDevice = channel == -1;
                }
                digitalOutput.Channel = channel;
                digitalOutput.DeviceSerialNumber = serial;
                SendInfoLog("<-- Listening to " + prosimDataRef + " to channel:" + channel);

                Open();

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDataRef, 50, connection);

                dataRef.onDataChange += DataRef_onDataChange;

                if (prosimDataRefOff != null)
                {
                    DataRef dataRef2 = new DataRef(prosimDataRefOff, 100, connection);
                    dataRef2.onDataChange += DataRef_onDataChange;
                }


            }
            catch (Exception ex)
            {
                SendErrorLog(ex.ToString());
            }

        }


        public async void TurnOn()
        {
            try
            {
               // digitalOutput.Open(5000);
                if (Delay > 0)
                {
                    var taskDelay = Task.Delay(Delay);
                    await taskDelay;
                }

                digitalOutput.DutyCycle = 1;
                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [ON] | Ref: {ProsimDataRef}");

                // Turn off after specified time(ms)
                if (TurnOffAfterMs > 0)
                {
                    SendInfoLog("Start OFF Delay " + TurnOffAfterMs + " for " + ProsimDataRef + " - Channel " + Channel);
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
                digitalOutput.DutyCycle = 0;
                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [OFF] | Ref: {ProsimDataRef}");

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

            Debug.WriteLine($"OUT Changed: {dataRef.name} {dataRef.value}");

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

                    if (value == true && name == ProsimDataRefOff)
                    {
                        SendInfoLog("Turn Off from ProsimDataRefOff" + dataRef.value + " " + dataRef.name);
                        if (Inverse)
                        {
                            TurnOn();
                        }
                        else
                        {
                            TurnOff();
                        }
                    }

                    if (ProsimDataRefOff == null && value == false)
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
