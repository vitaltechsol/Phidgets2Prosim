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

        // Opens a Phidget channel, retrying when the open times out. Right after startup a (remote) hub is often
        // still being discovered, and opening many channels at once makes some of them time out. A timed-out
        // channel is closed and re-opened a few times with a growing pause before the failure is reported.
        // Limits how many blocking Open() calls run at once. With hundreds of channels each parking a thread-pool
        // thread for its whole timeout, the pool starves and timeouts/retries balloon (tens of seconds apart).
        private static readonly System.Threading.SemaphoreSlim OpenGate = new System.Threading.SemaphoreSlim(8);

        protected async System.Threading.Tasks.Task OpenWithRetry(Phidget22.Phidget channel, int timeoutMs = 5000, int attempts = 4)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await OpenGate.WaitAsync();
                    try
                    {
                        await System.Threading.Tasks.Task.Run(() => channel.Open(timeoutMs));
                    }
                    finally
                    {
                        OpenGate.Release();
                    }
                    return;
                }
                catch (Phidget22.PhidgetException ex) when (ex.ErrorCode == Phidget22.ErrorCode.Timeout && attempt < attempts)
                {
                    SendInfoLog($"[Open] {ProsimDataRef} [{HubPort}] Ch:{Channel} timed out (attempt {attempt}/{attempts}); retrying…");
                    try { channel.Close(); } catch { }
                    await System.Threading.Tasks.Task.Delay(500 * attempt);
                }
            }
        }
    }
}