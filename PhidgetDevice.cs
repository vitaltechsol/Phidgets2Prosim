using System;
using System.Diagnostics;
using ProSimSDK;

namespace Phidgets2Prosim
{
    public class PhidgetDevice
    {
        // Phidget serial number
        public int Serial { get; set; }

        // The Phidget Hub Port
        // Use -1 for USB
        public int HubPort { get; set; }

        // The Phidget hub channel
        // use -1 for channel when is a using the hub port as a device, for example for a relay
        public int Channel { get; set; }

        // Prosim data ref name
        public string ProsimDataRef { get; set; }

        // Prosim connection instance
        public ProSimConnect Connection { get; set; }

        public event Action<string> ErrorLog;

        // Method to send error log
        public void SendErrorLog(string logMessage)
        {
            ErrorLog?.Invoke(logMessage);
            Debug.WriteLine(logMessage);
        }

        public event Action<string> InfoLog;

        // Method to send info log
        public void SendInfoLog(string logMessage)
        {
            InfoLog?.Invoke(logMessage);
        }

    }
}