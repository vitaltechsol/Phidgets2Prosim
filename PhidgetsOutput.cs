using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using YamlDotNet.Core.Tokens;
using System.Threading;

namespace Phidgets2Prosim
{
    internal class PhidgetsOutput : PhidgetDevice
    {
        DigitalOutput digitalOutput = new DigitalOutput();
        private CancellationTokenSource blinkCancellation;
        public int BlinkIntervalFastMs { get; set; } = 300; // Default blink interval
        public int BlinkIntervalSlowMs { get; set; } = 600; // Default blink interval



        //bool isGate = false;

        public int TurnOffAfterMs { get; set; } = 0;
        public bool Inverse { get; set; } = false;
        public bool IsGate { get; set; }
        public string ProsimDataRefOff { get; set; }
        public int Delay { get; set; } = 0;
        public double ValueOn { get; set; } = 1;
        public double ValueOff { get; set; } = 0;
        public double ValueDim { get; set; } = 0.7;


        public PhidgetsOutput(int serial, int hubPort, int channel, string prosimDataRef, ProSimConnect connection, bool isGate = false, string prosimDataRefOff = null)
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
                //Debug.WriteLine("<-- Listening to " + prosimDataRef + " to channel:" + channel);
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


        public async void TurnOn(double dimValue)
        {
            try
            {
                if (Delay > 0)
                {
                    var taskDelay = Task.Delay(Delay);
                    await taskDelay;
                }

                digitalOutput.DutyCycle = dimValue;
                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [ON ({dimValue})] | Ref: {ProsimDataRef}");

                // Turn off after specified time(ms)
                if (TurnOffAfterMs > 0)
                {
                    SendInfoLog("Start OFF Delay " + TurnOffAfterMs + " for " + ProsimDataRef + " - Channel " + Channel);
                    var taskDelay2 = Task.Delay(TurnOffAfterMs);
                    await taskDelay2;
                    TurnOff(ValueOff);
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("Turn On Failed for " + ProsimDataRef + " - Channel " + Channel);
                SendErrorLog(ex.ToString());
            }
        }

        public void TurnOff(double offValue)
        {
            try
            {
                digitalOutput.DutyCycle = offValue;
                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [OFF ({offValue})] | Ref: {ProsimDataRef}");

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
                digitalOutput.Close();
                await Task.Run(() => digitalOutput.Open(10000));
                //Debug.WriteLine("<-- OPENED " + ProsimDataRef + " to channel:" + Channel);
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
            SendInfoLog($"OUT Changed: {dataRef.name} {dataRef.value}");

            try
            {
                if (IsGate)
                {
                    var value = Convert.ToBoolean(dataRef.value);
                    if (value == true && name == ProsimDataRef)
                    {
                        if (Inverse)
                        {
                            TurnOff(ValueOff);
                        }
                        else
                        {
                            TurnOn(ValueOn);
                        }
                    }

                    if (value == true && name == ProsimDataRefOff)
                    {
                        SendInfoLog("Turn Off from ProsimDataRefOff" + dataRef.value + " " + dataRef.name);
                        if (Inverse)
                        {
                            TurnOn(ValueOn);
                        }
                        else
                        {
                            TurnOff(ValueOff);
                        }
                    }

                    if (ProsimDataRefOff == null && value == false)
                    {
                        if (Inverse)
                        {
                            TurnOn(ValueOn);
                        }
                        else
                        {
                            TurnOff(ValueOff);
                        }
                    }
                }
                else { 
                    var value = Convert.ToInt32(dataRef.value);

                    if (value == 4) //blink fast
                    {
                        if (blinkCancellation == null)
                        {
                            blinkCancellation = new CancellationTokenSource();
                            _ = BlinkAsyncFast(blinkCancellation.Token);
                        }
                    }
                    else if (value == 3) //blink slow
                        {
                            if (blinkCancellation == null)
                            {
                                blinkCancellation = new CancellationTokenSource();
                                _ = BlinkAsyncSlow(blinkCancellation.Token);
                            }
                        }
                        else
                    {
                        StopBlinking();
                        if (Inverse)
                        {   // Dim
                            if (value == 1)
                            {
                                TurnOn(ValueDim);
                            }
                            // On
                            if (value == 2)
                            {
                                TurnOff(ValueOff);
                            }
                            // Off
                            else if (value == 0)
                            {
                                TurnOn(ValueOn);
                            }
                        } else
                        {
                            // Dim
                            if (value == 1)
                            {
                                TurnOn(ValueDim);
                            }
                            // On
                            if (value == 2)
                            {
                                TurnOn(ValueOn);
                            }
                            // Off
                            else if (value == 0)
                            {
                                TurnOff(ValueOff);
                            }
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

        private async Task BlinkAsyncSlow(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    digitalOutput.DutyCycle = digitalOutput.DutyCycle == 1 ? 0 : 1;
                    await Task.Delay(BlinkIntervalSlowMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                digitalOutput.DutyCycle = 0; // Ensure output is off when stopping
            }
        }

        private async Task BlinkAsyncFast(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    digitalOutput.DutyCycle = digitalOutput.DutyCycle == 1 ? 0 : 1;
                    await Task.Delay(BlinkIntervalFastMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                digitalOutput.DutyCycle = 0; // Ensure output is off when stopping
            }
        }
        private void StopBlinking()
        {
            blinkCancellation?.Cancel();
            blinkCancellation = null;
            digitalOutput.DutyCycle = 0;
        }
    }
}
