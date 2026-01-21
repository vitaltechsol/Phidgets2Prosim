using Phidgets2Prosim;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;

internal class PhidgetsEncoder : PhidgetDevice
{
    private readonly Phidget22.Encoder encoder = new Phidget22.Encoder();

    public bool Enabled { get; set; } = true;

    public double ScaleFactor { get; set; } = 1;

    public int PositionChangeTrigger { get; set; } = 1;


private readonly Func<bool> _shouldUpdate;

    private readonly DataRef _dataRef;

    public PhidgetsEncoder(
        int deviceSerialNumber,
        int hubPort,
        int channel,
        string prosimDataRef,
        ProSimConnect connection,
        Func<bool> shouldUpdate = null)
    {
        Serial = deviceSerialNumber;
        HubPort = hubPort;
        Channel = channel;
        ProsimDataRef = prosimDataRef;
        Connection = connection;
        _shouldUpdate = shouldUpdate ?? (() => true);
        _dataRef = new DataRef(ProsimDataRef, 200, Connection, true);
        encoder.PositionChange += Encoder_PositionChange;
        _ = Open();
    }

    public async Task Open()
    {
        try
        {
            if (!encoder.IsOpen && Enabled)
            {
                encoder.DeviceSerialNumber = Serial;
                encoder.Channel = Channel;

                if (HubPort >= 0)
                {
                    encoder.HubPort = HubPort;
                    encoder.IsRemote = true;
                }

                await Task.Run(() => encoder.Open(4000));
                encoder.PositionChangeTrigger = PositionChangeTrigger;

            }
        }
        catch (Exception ex)
        {
            SendErrorLog($"Encoder Open failed - {ProsimDataRef} to {Serial} [{HubPort}]");
            SendErrorLog(ex.Message);
            Debug.WriteLine(ex);
        }
    }

    private void Encoder_PositionChange(object sender, Phidget22.Events.EncoderPositionChangeEventArgs e)
    {
        if (!Enabled) return;
        if (!_shouldUpdate()) return;

        try
        {
            int value = (int)Math.Ceiling(e.PositionChange * ScaleFactor);
            SendInfoLog($"Encoder > {ProsimDataRef} to {e.PositionChange} ({value}) - [{HubPort}] Ch {Channel}");
            _dataRef.value = value;
        }
        catch (Exception ex)
        {
            SendErrorLog($"Error: Encoder {ProsimDataRef} to {e.PositionChange} - [{HubPort}] Ch {Channel}");
            SendErrorLog(ex.ToString());
        }
    }
}
