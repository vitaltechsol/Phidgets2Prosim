﻿using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using System.Drawing;

namespace Phidgets2Prosim
{
    internal class PhidgetsVoltageOutput : PhidgetDevice
    {

        VoltageOutput voltageOutput = new VoltageOutput();
        public double ScaleFactor { get; set; }
        double lastVoltage = 0;
        public PhidgetsVoltageOutput(int deviceSerialN, int hubPort, double scaleFactor, string prosimDatmRef, ProSimConnect connection)
        {

            try
            {
                ScaleFactor = scaleFactor;
                voltageOutput.DeviceSerialNumber = deviceSerialN; 
                voltageOutput.HubPort = hubPort;
                voltageOutput.IsRemote = true;
                
                Open();

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRef, 100, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: " + ex.ToString());
            }
        }

        public async void Open()
        {

            try
            {
                if (voltageOutput.IsOpen == false)
                {
                    await Task.Run(() => voltageOutput.Open(500));
                    voltageOutput.Voltage = 0;
                }
            }
            catch (System.Exception ex)
            {
                SendErrorLog("Error: Voltage Channel " + Channel + " Input " + ProsimDataRef);
                SendErrorLog(ex.ToString());

                Debug.WriteLine("Error: Voltage Channel " + Channel + " Input " + ProsimDataRef);
                Debug.WriteLine(ex.ToString());
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {
            var value = Convert.ToInt64(dataRef.value);
            var convertedValue = value > 0 ? (value / ScaleFactor) : 0;
            if (lastVoltage != value)
            {

              // //
                try
                {
//                    Debug.WriteLine($"Voltage Changed: {dataRef.name} {value}");
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
