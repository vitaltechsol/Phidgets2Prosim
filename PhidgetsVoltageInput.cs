using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsVoltageInput : PhidgetDevice
    {
        VoltageRatioInput voltageInput = new VoltageRatioInput();

        double lastValue = 0.0;

        public int DataInterval { get; set; }
        public double MinInputVal { get; set; }
        public double MaxOutputVal { get; set; }

        public double MinVoltChangeTrigger {  get; set; }
        public int MaxProsimVal { get; set; }
        public string ProsimDataRefWhenOff { get; set; } = null;

        public PhidgetsVoltageInput(int serial, int hubPort, int channel, ProSimConnect connection,
            string prosimDataRef,
            int dataInterval = 50,
            int maxProsimVal = 255,
            double minInputVal = 0,
            double maxOutputVal = 1,
            double minChangeDetection = 0.4,
            string prosimDataRefWhenOff = ""
            )
        {
            ProsimDataRef = prosimDataRef;
            Connection = connection;
            Channel = channel;
            HubPort = hubPort;
            Serial = serial;
            MaxProsimVal = maxProsimVal;
            MinInputVal = minInputVal;
            MaxOutputVal = maxOutputVal;
            ProsimDataRefWhenOff = prosimDataRefWhenOff;
            MinVoltChangeTrigger = minChangeDetection;
            DataInterval = dataInterval;
        }
        private void StateChange(object sender, Phidget22.Events.VoltageRatioInputVoltageRatioChangeEventArgs e)
        {
            // Set ProSim dataref
            double value = (double)e.VoltageRatio;

            //if (Math.Abs(value - lastValue) > MinChangeDetection)
            //{
                lastValue = value;
                double normalized = (value - MinInputVal) / (MaxOutputVal - MinInputVal);
                double scaled = normalized * MaxProsimVal;
                int valueScaled = (int)Math.Round(scaled);

                DataRef dataRef = new DataRef(ProsimDataRef, 200, Connection, true);
                try
                {
                    if (valueScaled > 0)
                    {
                        SendInfoLog($"~~> [{HubPort}] Ch {Channel}: {value} | scaled: {valueScaled} | Ref: {ProsimDataRef}");

                        dataRef.value = valueScaled;
                    }
                    else
                    {
                       //  dataRef.value = OffInputValue;
                    }

                }
                catch (System.Exception ex)
                {
                    SendInfoLog($"Error: Voltage Input  [{HubPort}] Ch {Channel}: {e.VoltageRatio} | Ref: {ProsimDataRef}");

                    SendErrorLog(ex.ToString());
                }
            //}
        }


        public void Close()
        {
            voltageInput.Close();
            SendInfoLog($"-> Detached/Closed {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
        }

        public async void Open()
        {

            try
            {
                if (voltageInput.IsOpen == false)
                {

                    if (HubPort >= 0)
                    {
                        voltageInput.HubPort = HubPort;
                        voltageInput.IsRemote = true;
                        // use -1 for channel when is a IsHubPortDevice
                        voltageInput.IsHubPortDevice = Channel == -1;
                    }

                    voltageInput.Channel = Channel;
                    voltageInput.VoltageRatioChange += StateChange;
                    voltageInput.DeviceSerialNumber = Serial;
                    SendInfoLog($"-> Attaching {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");

                    await Task.Run(() => voltageInput.Open(5000));
                    voltageInput.DataInterval = DataInterval;
                    voltageInput.SensorValueChangeTrigger = 0.1;
                    SendInfoLog($"-> Attached {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
                }
                else
                {
                    SendErrorLog("Error: --> Channel (ALREADY OPEN)" + Channel + " Input " + ProsimDataRef);
                }
            }
            catch (System.Exception ex)
            {
                SendErrorLog("Error: -->  Opening Channel " + Channel + " Input " + ProsimDataRef);
                SendErrorLog(ex.ToString());
            }
        }

    }
}
