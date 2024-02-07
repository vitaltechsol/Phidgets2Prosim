using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;

namespace Phidgets2Prosim
{
    internal class PhidgetsVoltageOutput
    {

        VoltageOutput voltageOutput = new VoltageOutput();
        double scaleFactor;
        double lastVoltage = 0;
        public PhidgetsVoltageOutput(int deviceSerialN, int hubPort, double scaleFactor, string prosimDatmRef, ProSimConnect connection)
        {

            try
            {
                this.scaleFactor = scaleFactor;
                voltageOutput.DeviceSerialNumber = deviceSerialN; 
                voltageOutput.HubPort = hubPort;
                voltageOutput.IsRemote = true;
                voltageOutput.Open(2000);
                voltageOutput.Voltage = 0;

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRef, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: " + ex.ToString());
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {
            var value = Convert.ToInt64(dataRef.value);
            var convertedValue = value > 0 ? (value / scaleFactor) : 0;
            if (lastVoltage != value)
            {

                Debug.WriteLine($"Voltage Changed: {dataRef.name} {value}");
                try
                {
                    Debug.WriteLine(dataRef.name);
                    // Debug.WriteLine(value);
                    // Debug.WriteLine(convertedValue);
                    voltageOutput.Voltage = Convert.ToDouble(convertedValue);
                    lastVoltage = value;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ERROR: " + value + " to " + convertedValue);
                    Debug.WriteLine(ex.ToString());
                }
                
            }
        }

    }
}
