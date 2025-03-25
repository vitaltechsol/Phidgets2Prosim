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
        public int BlinkFastIntervalMs { get; set; } = 300; // Default blink interval
        public int BlinkSlowIntervalMs { get; set; } = 600; // Default blink interval
        public int MaxTimeOn { get; set; } = 0;
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
            HubPort = hubPort;
            // Set ProSim dataref
            ProsimDataRef = prosimDataRef;
            ProsimDataRefOff = prosimDataRefOff;
            Serial = serial;
            Connection = connection;
      
            try
            {
                if (HubPort >= 0)
                {
                    digitalOutput.HubPort = HubPort;
                    digitalOutput.IsRemote = true;
                    // use -1 for channel when is a IsHubPortDevice
                    digitalOutput.IsHubPortDevice = Channel == -1;
                }
                digitalOutput.Channel = Channel;
                digitalOutput.DeviceSerialNumber = Serial;
                Debug.WriteLine("<-- INIT " + ProsimDataRef + " to channel:" + Channel);
                // Set ProSim dataref
                //DataRef dataRef = new DataRef(prosimDataRef, 5, connection);
                //dataRef.onDataChange += DataRef_onDataChange;

                //if (prosimDataRefOff != null)
                //{
                //    DataRef dataRef2 = new DataRef(prosimDataRefOff, 100, connection);
                //    dataRef2.onDataChange += DataRef_onDataChange;
                //}

            }
            catch (Exception ex)
            {
                SendErrorLog(ex.ToString());
            }

        }

        public async Task TurnOn(double dutyCycle)
        {
            try
            {
                if (Delay > 0)
                {
                    var taskDelay = Task.Delay(Delay);
                    await taskDelay;
                }


                digitalOutput.BeginSetDutyCycle(dutyCycle, delegate (IAsyncResult result)
                {
                    digitalOutput.EndSetDutyCycle(result);

                }, null);

                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [ON ({dutyCycle})] | Ref: {ProsimDataRef}");

                // Turn off after specified time(ms)
                if (MaxTimeOn > 0)
                {
                    SendInfoLog("Start OFF Delay " + MaxTimeOn + " for " + ProsimDataRef + " - Channel " + Channel);
                    var taskDelay2 = Task.Delay(MaxTimeOn);
                    await taskDelay2;
                    await TurnOff(ValueOff);
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("Turn On Failed for " + ProsimDataRef + " - Channel " + Channel);
                SendErrorLog(ex.ToString());
            }

        }

        public async Task TurnOff(double dutyCycle)
        {
            try
            {
                digitalOutput.BeginSetDutyCycle(dutyCycle, delegate (IAsyncResult result)
                {

                    digitalOutput.EndSetDutyCycle(result);

                }, null);
                SendInfoLog($"<-- [{HubPort}] Ch {Channel}: [OFF ({dutyCycle})] | Ref: {ProsimDataRef}");

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

        public async Task Open()
        {
            Debug.WriteLine("<-- OPENING " + ProsimDataRef + " to channel:" + Channel);
            try
            {
                await Task.Run(()=>digitalOutput.Open(10000));
            }
            catch (Exception ex)
            {
                SendErrorLog("ERROR: Open failed for " + ProsimDataRef + " Serial:" + digitalOutput.DeviceSerialNumber);
                SendErrorLog(ex.ToString());
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {
            _ = HandleDataChangeAsync(dataRef.name, dataRef.value);
        }

  
        public async Task HandleDataChangeAsync(string name, object refValue)
        {

            try
            {
                if (IsGate)
                {
                    var value = Convert.ToBoolean(refValue);
                    if (value == true && name == ProsimDataRef)
                    {
                        if (Inverse)
                        {
                            await TurnOff(ValueOff);
                        }
                        else
                        {
                            await TurnOn(ValueOn);
                        }
                    }

                    if (value == true && name == ProsimDataRefOff)
                    {
                        SendInfoLog("Turn Off from ProsimDataRefOff" + refValue + " " + name);
                        if (Inverse)
                        {
                            await TurnOn(ValueOn);
                        }
                        else
                        {
                            await TurnOff(ValueOff);
                        }
                    }

                    if (ProsimDataRefOff == null && value == false)
                    {
                        if (Inverse)
                        {
                            await TurnOn(ValueOn);
                        }
                        else
                        {
                            await TurnOff(ValueOff);
                        }
                    }
                }
                else
                {
                    var value = Convert.ToInt32(refValue);
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
                        if (Inverse)
                        {   // Dim
                            if (value == 1)
                            {
                                await TurnOn(ValueDim);
                            }
                            // On
                            if (value == 2)
                            {
                                await TurnOff(ValueOff);
                                await StopBlinking();
                            }
                            // Off
                            else if (value == 0)
                            {
                                 await TurnOn(ValueOn);
                            }
                        }
                        else
                        {
                            // Dim
                            if (value == 1)
                            {
                                await TurnOn(ValueDim);
                            }
                            // On
                            if (value == 2)
                            {
                                await TurnOn(ValueOn);
                            }
                            // Off
                            else if (value == 0)
                            {
                                await TurnOff(ValueOff);
                                await StopBlinking();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("DataRef_onDataChange failed for " + ProsimDataRef + " ch:" + Channel);
                SendErrorLog("value " + refValue);
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
                    await Task.Delay(BlinkSlowIntervalMs, token);
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
                    await Task.Delay(BlinkFastIntervalMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                digitalOutput.DutyCycle = 0; // Ensure output is off when stopping
            }
        }
        private async Task StopBlinking()
        {
            blinkCancellation?.Cancel();
            blinkCancellation = null;
        }
    }
}
