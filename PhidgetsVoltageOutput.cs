using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using System.Drawing;
using System.Timers;
using YamlDotNet.Core.Tokens;
using System.Xml.Linq;

namespace Phidgets2Prosim
{
    internal class PhidgetsVoltageOutput : PhidgetDevice
    {

        VoltageOutput voltageOutput = new VoltageOutput();
        public double ScaleFactor { get; set; }
        public double Offset { get; set; } = 0;

        public int Interval { get; set; } = 10;  // Timer interval in milliseconds

        public double SmoothFactor { get; set; } = 0.1;  // Smooth factor for gradual movement

        double lastVoltage = 0;

        private double currentPosition = 0;
        private double targetPosition = 0;
        private Timer timer;

        public PhidgetsVoltageOutput(int deviceSerialN, int hubPort, string prosimDataRef, ProSimConnect connection)
        {
            timer = new Timer(Interval);
            timer.Elapsed += OnTimerElapsed;
            timer.AutoReset = true;
            Serial = deviceSerialN;
            HubPort = hubPort;
            ProsimDataRef = prosimDataRef;
            try
            {
                Open();
                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDataRef, 5, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                SendErrorLog("PhidgetsVoltageOutput ERROR: " + ex.ToString());
            }
        }

        public async Task Open()
        {

            try
            {
                if (voltageOutput.IsOpen == false)
                {
                    voltageOutput.DeviceSerialNumber = Serial;
                    if (HubPort >= 0)
                    {
                        voltageOutput.HubPort = HubPort;
                        voltageOutput.IsRemote = true;
                    }
                    await Task.Run(() => voltageOutput.Open(4000));
                    voltageOutput.Voltage = 0;
                }
            }
            catch (System.Exception ex)
            {
                SendErrorLog($"Voltage output Open failed - {ProsimDataRef} to {Serial} [{HubPort}]");
                SendErrorLog(ex.Message.ToString());
                Debug.WriteLine(ex.ToString());
            }
        }

        public void  GotoValue(double value, string name )
        {
            var convertedValue = value > 0 ? Math.Round((value / ScaleFactor),2) : 0;
            convertedValue = convertedValue + Offset;
            if (convertedValue > 10 || convertedValue < -10)
            {
                SendErrorLog($"Voltage invalid: {name} | value: {value} | Converted {convertedValue} | ScaleFactor: {ScaleFactor} | Offset {Offset}");
                convertedValue = 0;
            }

            if (targetPosition != convertedValue)
            {
                // Debug.WriteLine($"Voltage Changed: {name} {value} new target {convertedValue}");
                targetPosition = convertedValue;
                if (!timer.Enabled)
                {
                    timer.Start();
                }
                    
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {
           var value = Math.Round(Convert.ToDouble(dataRef.value), 3);
           //  Debug.WriteLine($"ataRef.value: {dataRef.value}");
           GotoValue(value, dataRef.name );
        }

        private void UpdateNeedlePosition(double convertedValue)
        {
            try
            {
                // Debug.WriteLine($"updating position to {convertedValue}");
                if (voltageOutput.IsOpen)
                {
                    voltageOutput.Voltage = Convert.ToDouble(convertedValue);
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("ERROR: " + convertedValue + " to " + targetPosition);
                SendErrorLog(ex.ToString());
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Move the needle towards the target position smoothly
            if (targetPosition > currentPosition)
            {
                currentPosition += SmoothFactor;
                // Stop the timer if the needle has reached the target position
                if ((currentPosition >= targetPosition))
                {
                    currentPosition = targetPosition;
                    // Debug.WriteLine($"Target position, Stopping: {currentPosition} to {targetPosition}");
                    timer.Stop();
                }
            } else
            {
                currentPosition -= SmoothFactor;
                if ((currentPosition <= targetPosition))
                {
                    currentPosition = targetPosition;
                    // Debug.WriteLine($"Target position, Stopping: {currentPosition} to {targetPosition}");
                    timer.Stop();
                }
            }

            // Debug.WriteLine($"Timer position set at {currentPosition} to {targetPosition}");
            // Update the needle position here
            UpdateNeedlePosition(currentPosition);
        }
    }
}
