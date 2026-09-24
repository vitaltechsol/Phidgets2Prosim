using System;
using ProSimSDK;
using System.Diagnostics;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Runtime object for a default input that writes a constant value to a ProSim DataRef.
    /// Follows the pattern of other runtime device objects in the app.
    /// </summary>
    internal class DefaultIntput : PhidgetDevice
    {
        private readonly DataRef _dataRef;
        public int DefaultInputValue { get; private set; }

        public DefaultIntput(string prosimDataRef, int defaultValue, ProSimConnect connection)
        {
            ProsimDataRef = prosimDataRef;
            DefaultInputValue = defaultValue;
            Connection = connection;

            try
            {
                if (!string.IsNullOrWhiteSpace(ProsimDataRef) && Connection != null)
                {
                    _dataRef = new DataRef(ProsimDataRef, 200, Connection, true);
                    _dataRef.value = DefaultInputValue;
                    SendInfoLog($"-> DefaultIntput set {ProsimDataRef} = {DefaultInputValue}");
                }
                else
                {
                    SendErrorLog($"DefaultIntput skipped - invalid ref or connection: {ProsimDataRef}");
                }
            }
            catch (Exception ex)
            {
                SendErrorLog($"Error setting DefaultIntput {ProsimDataRef}: {ex}");
                Debug.WriteLine(ex.ToString());
            }
        }

        public void Close()
        {
            try
            {
                // nothing to close for DataRef but clear reference
                // Optionally set to 0 or leave as-is
                // _dataRef = null; // cannot reassign readonly
                SendInfoLog($"-> DefaultIntput closed {ProsimDataRef}");
            }
            catch (Exception ex)
            {
                SendErrorLog($"Error closing DefaultIntput {ProsimDataRef}: {ex.Message}");
            }
        }
    }
}
