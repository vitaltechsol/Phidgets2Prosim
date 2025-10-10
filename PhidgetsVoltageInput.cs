using Phidget22;
using ProSimSDK;
using System;
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
        private VoltageRatioInput voltageInput = new VoltageRatioInput();
        public string ProsimDataRefOnOff { get; set; } = "";
        public double[] InputPoints { get; set; }
        public int[] OutputPoints { get; set; }
        public int DataInterval { get; set; }
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Linear;
        public double CurvePower { get; set; } = 2.0;
        public double MinChangeTriggerValue { get; set; }
        private double[] splineA, splineB, splineC, splineD;
        private bool splineInitialized = false;
        private System.Timers.Timer debounceTimer;
        private object debounceLock = new object();
        private int? pendingStateChange = null;

        public PhidgetsVoltageInput(int serial, int hubPort, int channel, ProSimConnect connection,
            string prosimDataRef,
            string prosimDataRefOnOff,
            double[] inputPoints,
            int[] outputPoints,
            InterpolationMode interpolationMode = InterpolationMode.Spline,
            double curvePower = 2.0,
            int dataInterval = 50,
            double minChangeDetection = 0.002
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

            if (InterpolationMode == InterpolationMode.Spline)
            {
                ComputeSplineCoefficients();
            }
            Open();

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

        private void StateChange(object sender, Phidget22.Events.VoltageRatioInputVoltageRatioChangeEventArgs e)
        {
            double value = e.VoltageRatio;
            double interpolated = Interpolate(value);
            int valueScaled = (int)Math.Round(interpolated);

            DataRef dataRef = new DataRef(ProsimDataRef, 200, Connection, true);
            try
            { 
                SendInfoLog($"~~> [{HubPort}] Ch {Channel}: {value} | scaled: {valueScaled} | Ref: {ProsimDataRef}");
                dataRef.value = valueScaled;
            }
            catch (Exception ex)
            {
                SendInfoLog($"Error: Voltage Input  [{HubPort}] Ch {Channel}: {value} | Ref: {ProsimDataRef}");
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

        private double Interpolate(double x)
        {
            switch (InterpolationMode)
            {
                case InterpolationMode.Curve:
                    return CurveInterpolate(x);
                case InterpolationMode.Spline:
                    return SplineInterpolate(x);
                default:
                    return LinearInterpolate(x);
            }
        }

        private double LinearInterpolate(double x)
        {
            for (int i = 0; i < InputPoints.Length - 1; i++)
            {
                double x0 = InputPoints[i];
                double x1 = InputPoints[i + 1];
                double y0 = OutputPoints[i];
                double y1 = OutputPoints[i + 1];

                if (x >= x0 && x <= x1)
                {
                    double t = (x - x0) / (x1 - x0);
                    return y0 + (y1 - y0) * t;
                }
            }

            return x <= InputPoints[0] ? OutputPoints[0] : OutputPoints[OutputPoints.Length - 1];
        }


    private double CurveInterpolate(double x)
        {
            for (int i = 0; i < InputPoints.Length - 1; i++)
            {
                double x0 = InputPoints[i];
                double x1 = InputPoints[i + 1];
                double y0 = OutputPoints[i];
                double y1 = OutputPoints[i + 1];

                if (x >= x0 && x <= x1)
                {
                    double t = (x - x0) / (x1 - x0);
                    t = Math.Pow(t, CurvePower);
                    return y0 + (y1 - y0) * t;
                }
            }

            return x <= InputPoints[0] ? OutputPoints[0] : OutputPoints[OutputPoints.Length - 1];
        }

        private void ComputeSplineCoefficients()
        {
            int n = InputPoints.Length;
            splineA = new double[n];
            splineB = new double[n - 1];
            splineC = new double[n];
            splineD = new double[n - 1];

            for (int i = 0; i < n; i++)
                splineA[i] = OutputPoints[i];

            double[] h = new double[n - 1];
            for (int i = 0; i < n - 1; i++)
                h[i] = InputPoints[i + 1] - InputPoints[i];

            double[] alpha = new double[n - 1];
            for (int i = 1; i < n - 1; i++)
                alpha[i] = (3 / h[i]) * (splineA[i + 1] - splineA[i]) - (3 / h[i - 1]) * (splineA[i] - splineA[i - 1]);

            double[] l = new double[n];
            double[] mu = new double[n];
            double[] z = new double[n];
            l[0] = 1;
            mu[0] = z[0] = 0;

            for (int i = 1; i < n - 1; i++)
            {
                l[i] = 2 * (InputPoints[i + 1] - InputPoints[i - 1]) - h[i - 1] * mu[i - 1];
                mu[i] = h[i] / l[i];
                z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
            }

            l[n - 1] = 1;
            z[n - 1] = splineC[n - 1] = 0;

            for (int j = n - 2; j >= 0; j--)
            {
                splineC[j] = z[j] - mu[j] * splineC[j + 1];
                splineB[j] = (splineA[j + 1] - splineA[j]) / h[j] - h[j] * (splineC[j + 1] + 2 * splineC[j]) / 3;
                splineD[j] = (splineC[j + 1] - splineC[j]) / (3 * h[j]);
            }

            splineInitialized = true;
        }

        private double SplineInterpolate(double x)
        {
            if (!splineInitialized) ComputeSplineCoefficients();

            int i;
            for (i = 0; i < InputPoints.Length - 1; i++)
            {
                if (x >= InputPoints[i] && x <= InputPoints[i + 1])
                    break;
            }

            if (i >= InputPoints.Length - 1)
                i = InputPoints.Length - 2;

            double dx = x - InputPoints[i];
            return splineA[i] + splineB[i] * dx + splineC[i] * dx * dx + splineD[i] * dx * dx * dx;
        }

        public void Close()
        {
            voltageInput.Close();
            SendInfoLog($"-> Detached/Closed {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");
        }

        public async void Open()
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

                    voltageInput.Channel = Channel;
                    voltageInput.VoltageRatioChange += StateChange;
                    voltageInput.DeviceSerialNumber = Serial;
                    SendInfoLog($"-> Attaching {ProsimDataRef} to  [{HubPort}] Ch:{Channel}");

                    await Task.Run(() => voltageInput.Open(5000));
                    voltageInput.VoltageRatioChangeTrigger = MinChangeTriggerValue;
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
