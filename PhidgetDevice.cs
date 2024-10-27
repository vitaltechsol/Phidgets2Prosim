using System;
using System.Diagnostics;
using ProSimSDK;

namespace Phidgets2Prosim
{
    public class PhidgetDevice
    {
        public int Channel { get; set; }
        public int HubPort { get; set; }
        public bool IsRemote { get; set; }
        public int Serial { get; set; }
        public string ProsimDataRef { get; set; }
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