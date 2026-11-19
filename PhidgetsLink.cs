using Phidget22;
using System;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Direct Input → Output link (no ProSim). A voltage input (or digital input) drives the duty cycle (PWM) of a digital output.
    /// Serial / HubPort / Channel (from <see cref="PhidgetDevice"/>) describe the INPUT; the Out* values describe the output.
    /// </summary>
    internal class PhidgetsLink : PhidgetDevice
    {
        private readonly PhidgetsLinkInst _cfg;
        private VoltageRatioInput _ratio;
        private VoltageInput _volts;
        private DigitalInput _digital;
        private DigitalOutput _out;
        private double _lastDuty = -1;
        private bool _running;

        /// <summary>Raised on every update with (raw input value, resulting duty cycle 0..1).</summary>
        public event Action<double, double> Changed;

        public string LastError { get; private set; }
        public bool IsRunning => _running;
        public string InputKey => $"{_cfg.Serial}:{_cfg.HubPort}:{_cfg.Channel}";
        public string OutputKey => $"{_cfg.OutSerial}:{_cfg.OutHubPort}:{_cfg.OutChannel}";

        public PhidgetsLink(PhidgetsLinkInst cfg)
        {
            _cfg = cfg;
            Serial = cfg.Serial;
            HubPort = cfg.HubPort;
            Channel = cfg.Channel;
        }

        /// <summary>Maps a raw input value (ratio / volts, or 0/1 for digital) to a duty cycle 0..1.</summary>
        public static double Map(PhidgetsLinkInst c, double x)
        {
            double t;
            if (c.SourceType == LinkSourceType.DigitalInput)
            {
                t = x >= 0.5 ? 1 : 0;
                if (c.InvertInput) t = 1 - t;
            }
            else
            {
                double span = c.InputMax - c.InputMin;
                t = Math.Abs(span) < 1e-9 ? 0 : (x - c.InputMin) / span;
                t = Math.Max(0, Math.Min(1, t));
                if (c.InvertInput) t = 1 - t;
                if (c.CurvePower > 0 && Math.Abs(c.CurvePower - 1) > 1e-9) t = Math.Pow(t, c.CurvePower);
            }
            double duty = c.OutputMin + (c.OutputMax - c.OutputMin) * t;
            return Math.Max(0, Math.Min(1, duty));
        }

        private static void Configure(Phidget p, int serial, int hubPort, int channel)
        {
            p.DeviceSerialNumber = serial;
            if (hubPort >= 0)
            {
                p.HubPort = hubPort;
                p.IsRemote = true;
            }
            p.Channel = channel;
        }

        public async Task<bool> Start()
        {
            if (_running) return true;
            _running = true;
            LastError = null;
            try
            {
                _out = new DigitalOutput();
                Configure(_out, _cfg.OutSerial, _cfg.OutHubPort, _cfg.OutChannel);
                await Task.Run(() => _out.Open(5000));

                switch (_cfg.SourceType)
                {
                    case LinkSourceType.DigitalInput:
                        _digital = new DigitalInput();
                        Configure(_digital, Serial, HubPort, Channel);
                        _digital.StateChange += (s, e) => Update(e.State ? 1 : 0);
                        await Task.Run(() => _digital.Open(5000));
                        TryInitial(() => Update(_digital.State ? 1 : 0));
                        break;

                    default:
                        if (_cfg.UseRange)
                        {
                            _volts = new VoltageInput();
                            Configure(_volts, Serial, HubPort, Channel);
                            _volts.VoltageChange += (s, e) => Update(e.Voltage);
                            await Task.Run(() => _volts.Open(5000));
                            _volts.VoltageChangeTrigger = _cfg.MinChangeTriggerValue;
                            _volts.DataInterval = _cfg.DataInterval;
                            TryInitial(() => Update(_volts.Voltage));
                        }
                        else
                        {
                            _ratio = new VoltageRatioInput();
                            Configure(_ratio, Serial, HubPort, Channel);
                            _ratio.VoltageRatioChange += (s, e) => Update(e.VoltageRatio);
                            await Task.Run(() => _ratio.Open(5000));
                            _ratio.VoltageRatioChangeTrigger = _cfg.MinChangeTriggerValue;
                            _ratio.DataInterval = _cfg.DataInterval;
                            TryInitial(() => Update(_ratio.VoltageRatio));
                        }
                        break;
                }
                SendInfoLog($"-> Link started: [{HubPort}] Ch:{Channel} -> [{_cfg.OutHubPort}] Ch:{_cfg.OutChannel}");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                SendErrorLog($"Error starting link [{HubPort}] Ch:{Channel} -> [{_cfg.OutHubPort}] Ch:{_cfg.OutChannel}: {ex.Message}");
                Stop();
                return false;
            }
        }

        // The first reading may not be available yet; the change events will deliver it
        private static void TryInitial(Action a) { try { a(); } catch { } }

        private void Update(double x)
        {
            try
            {
                double duty = Map(_cfg, x);
                Changed?.Invoke(x, duty);
                if (_out == null || Math.Abs(duty - _lastDuty) < 0.0005) return;
                _lastDuty = duty;
                var o = _out;
                o.BeginSetDutyCycle(duty, ar => { try { o.EndSetDutyCycle(ar); } catch { } }, null);
            }
            catch (Exception ex) { SendErrorLog("Link update failed: " + ex.Message); }
        }

        public void Stop()
        {
            _running = false;
            try { if (_out != null && _out.IsOpen) _out.DutyCycle = 0; } catch { }
            foreach (var p in new Phidget[] { _ratio, _volts, _digital, _out })
            {
                try { if (p != null && p.IsOpen) p.Close(); } catch { }
            }
            _ratio = null; _volts = null; _digital = null; _out = null;
            _lastDuty = -1;
        }

        // Used by the editor test to free the channels, then hand them back
        public void Disconnect() => Stop();
        public void Reconnect() { _ = Start(); }
    }
}
