﻿using Phidget22;
using ProSimSDK;
using System.Diagnostics;


namespace Phidgets2Prosim
{
    internal class PhidgetsVoltageOutput
    {

        VoltageOutput voltageOutput = new VoltageOutput();
        public PhidgetsVoltageOutput(int hubPort, string prosimDatmRef, ProSimConnect connection)
        {

            try
            {
                voltageOutput.DeviceSerialNumber = 668522;
                voltageOutput.HubPort = hubPort;
                voltageOutput.IsRemote = true;
                voltageOutput.Open(15000);
                voltageOutput.Voltage = 0;
                voltageOutput.Enabled = true;

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRef, 10, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {

            try
            {
                var value = Convert.ToDouble(dataRef.value);
                var convertedValue = value > 0 ? (value / 500) : 0;

                voltageOutput.Voltage = convertedValue;
                Debug.WriteLine(dataRef.name);
                Debug.WriteLine(value);
                Debug.WriteLine(convertedValue);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }
        }

    }
}