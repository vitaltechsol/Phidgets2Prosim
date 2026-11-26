using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Phidgets2Prosim
{

    //## 🎛️ Interpolation Methods Comparison

    //    This module supports 3 interpolation modes:

    //| Mode         | Description                                                                 | Use Case                                                   |
    //|--------------|-----------------------------------------------------------------------------|------------------------------------------------------------|
    //| `Linear`     | Connects points with straight lines.Fast, predictable, and simple.         | Most control inputs or sensors with linear behavior.       |
    //| `Curve`      | Applies exponential curve between points using a power factor.              | Useful for simulating human-like non-linear response (e.g., throttle sensitivity). |
    //| `Spline`     | Smooth cubic spline interpolation across multiple points.                  | Ideal when smooth transitions and acceleration/deceleration modeling are needed. |

    //---

    public enum InterpolationMode
    {
        Linear,
        Curve,
        Spline
    }

    internal class PhidgetsVoltageInput : PhidgetDevice
    {
        private VoltageRatioInput voltageRatioInput = new VoltageRatioInput();
        private VoltageInput voltageInput = new VoltageInput();

        public string ProsimDataRefOnOff { get; set; } = "";
        public double[] InputPoints { get; set; }
        public double[] OutputPoints { get; set; }
        public int DataInterval { get; set; }

        // Use voltage range input instead of ratio
        public bool UseRange { get; set; } = false;

        // Event: subscribers get the new scaled value
        public event Action<double> onScaledValueChanged;

        public bool RoundUp { get; set; } = true;
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Linear;
        public double CurvePower { get; set; } = 2.0;
        public double MinChangeTriggerValue { get; set; }
        private System.Timers.Timer debounceTimer;
        private object debounceLock = new object();
        private int? pendingStateChange = null;

        private double _scaledValue = 0.0;
        private readonly DataRef _dataRef;

        public double ScaledValue
        {
            get => _scaledValue;
            private set
            {
                if (value != _scaledValue)   // <-- simple change detection
                {
                    _scaledValue = value;
                    onScaledValueChanged?.Invoke(_scaledValue);
                }
            }
        }

        public PhidgetsVoltageInput(int serial, int hubPort, int channel, ProSimConnect connection,
            string prosimDataRef,
            string prosimDataRefOnOff,
            double[] inputPoints,
            double[] outputPoints,
            InterpolationMode interpolationMode = InterpolationMode.Spline,
            double curvePower = 2.0,
            int dataInterval = 50,
            double minChangeDetection = 0.002,
            bool useRange = false
           )
        {
            ProsimDataRef = prosimDataRef;
            ProsimDataRefOnOff = prosimDataRefOnOff;
            Connection = connection;
            Channel = channel;
            HubPort = hubPort;
            Serial = serial;
            InputPoints = inputPoints;
            OutputPoints = outputPoints;
            InterpolationMode = interpolationMode;
            CurvePower = curvePower;
            MinChangeTriggerValue = minChangeDetection;
            DataInterval = dataInterval;
            UseRange = useRange;
            _dataRef = new DataRef(ProsimDataRef, 200, Connection, true);

            if (UseRange)
            {
                OpenRange();
            }

            else
            {
                Open();
            }

            debounceTimer = new System.Timers.Timer(180); // 180ms debounce
            debounceTimer.AutoReset = false; // Only fire once per change
            debounceTimer.Elapsed += (sender, e) =>
            {
                lock (debounceLock)
                {
                    if (pendingStateChange.HasValue)
                    {
                        debouncedSwitch(pendingStateChange.Value);
                        pendingStateChange = null;
                    }
                }
            };
        }

        private void StateRangeChange(object sender, Phidget22.Events.VoltageInputVoltageChangeEventArgs e)
        {
            UpdateValue(e.Voltage);
        }

        private void StateChange(object sender, Phidget22.Events.VoltageRatioInputVoltageRatioChangeEventArgs e)
        {
            UpdateValue(e.VoltageRatio);
        }

        private void UpdateValue(double value)
        {
            double interpolated = Interpolate(value);
            ScaledValue = interpolated;

            var valueScaled = RoundUp ? Convert.ToInt32(Math.Round(interpolated)) : interpolated;

            if (ProsimDataRef != null && ProsimDataRef != "")
            {
                try
                {
                    SendInfoLog($"~~> [{HubPort}] Ch {Channel}: {value} | scaled: {valueScaled} | Ref: {ProsimDataRef} " +
                        $"| Use Range: {UseRange}");
                    _dataRef.value = Convert.ToInt32(valueScaled);
                }
                catch (Exception ex)
                {
                    SendInfoLog($"Error: Voltage Input [{HubPort}] Ch {Channel}: {value} | Ref: {ProsimDataRef}");
                    SendErrorLog(ex.ToString());
                }
                if (ProsimDataRefOnOff != "")
                {
                    lock (debounceLock)
                    {
                        pendingStateChange = valueScaled == 0 ? 0 : 1;
                        debounceTimer.Stop();  // Reset timer
                        debounceTimer.Start(); // Start again
                    }
                }
            }
        }


        // Turn switch on off after voltage changes have stopped
        private void debouncedSwitch(int state)
        {
            DataRef dataRef = new DataRef(ProsimDataRefOnOff, 200, Connection, true);
            try
            {
                SendInfoLog($"~~> [{HubPort}] Ch {Channel}: {state} || OnOffRef: {ProsimDataRefOnOff}");
                dataRef.value = state;
            }
            catch (Exception ex)
            {
                SendInfoLog($"Error: Voltage Input  ProsimDataRefOnOff [{HubPort}] Ch {Channel}: {state} | Ref: {ProsimDataRefOnOff}");
                SendErrorLog(ex.ToString());
            }
        }

        private double Interpolate(double x) =>
            VoltageInterpolator.Interpolate(x, InputPoints, OutputPoints, InterpolationMode, CurvePower);

        // Re-opens the channel after Close() (e.g. after the editor's Preview released it)
        public void Reconnect()
        {
            if (UseRange) OpenRange(); else Open();
        }

        public void Close()
        {
            if (voltageRatioInput.IsOpen)
            {
                voltageRatioInput.Close();
            }
            if (voltageInput.IsOpen)
            {
                voltageInput.Close();
            }
            SendInfoLog($"-> Detached/Closed {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
        }

        public async void Open()
        {
            try
            {
                if (!voltageRatioInput.IsOpen)
                {
                    if (HubPort >= 0)
                    {
                        voltageRatioInput.HubPort = HubPort;
                        voltageRatioInput.IsRemote = true;
                        voltageRatioInput.IsHubPortDevice = Channel == -1;
                    }
                    voltageRatioInput.Channel = Channel;
                    voltageRatioInput.VoltageRatioChange -= StateChange;   // avoid double subscription on reopen
                    voltageRatioInput.VoltageRatioChange += StateChange;

                    voltageRatioInput.DeviceSerialNumber = Serial;
                    SendInfoLog($"-> Attaching {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");

                    await OpenWithRetry(voltageRatioInput, 5000);
                    voltageRatioInput.VoltageRatioChangeTrigger = MinChangeTriggerValue;
                    voltageRatioInput.DataInterval = DataInterval;



                    SendInfoLog($"-> Attached {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
                }
                else
                {
                    SendErrorLog("Error: --> Channel (ALREADY OPEN) " + Channel + " Input " + ProsimDataRef);
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("Error: -->  Opening Channel " + Channel + " Input " + ProsimDataRef);
                SendErrorLog(ex.ToString());
            }
        }

        public async void OpenRange()
        {
            try
            {
                if (!voltageInput.IsOpen)
                {
                    if (HubPort >= 0)
                    {
                        voltageInput.HubPort = HubPort;
                        voltageInput.IsRemote = true;
                        voltageInput.IsHubPortDevice = Channel == -1;
                    }
                    if (Channel > 0)
                    {
                        voltageInput.Channel = Channel;
                    }
                    voltageInput.VoltageChange -= StateRangeChange;   // avoid double subscription on reopen
                    voltageInput.VoltageChange += StateRangeChange;
                    voltageInput.DeviceSerialNumber = Serial;
                    SendInfoLog($"-> Attaching {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");

                    await OpenWithRetry(voltageInput, 5000);
                    voltageInput.VoltageChangeTrigger = MinChangeTriggerValue;
                    voltageInput.DataInterval = DataInterval;

                    SendInfoLog($"-> Attached {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
                }
                else
                {
                    SendErrorLog("Error: --> Channel (ALREADY OPEN) " + Channel + " Input " + ProsimDataRef);
                }
            }
            catch (Exception ex)
            {
                SendErrorLog("Error: -->  Opening Channel " + Channel + " Input " + ProsimDataRef);
                SendErrorLog(ex.ToString());
            }
        }
    }
}
