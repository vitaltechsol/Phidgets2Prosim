using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace Phidgets2Prosim
{
    internal class PhidgetsVoltageOutput : PhidgetDevice
    {
        // Existing single-channel output (kept)
        private readonly VoltageOutput voltageOutput = new VoltageOutput();

        // NEW: Optional COS output for SIN/COS mode
        private readonly VoltageOutput cosVoltageOutput = new VoltageOutput();

        public double ScaleFactor { get; set; } = 1.0;
        public double Offset { get; set; } = 0.0;

        public int Interval { get; set; } = 10;  // Timer interval in milliseconds

        // In single-channel mode this is "volts per tick".
        public double SmoothFactor { get; set; } = 0.1;

        // ---------------------------
        // NEW: SIN/COS mode settings
        // ---------------------------

        /// <summary>
        /// If true, the class outputs SIN and COS voltages instead of single-channel voltage.
        /// Default is false so existing behavior is unchanged.
        /// </summary>
        public bool UseSinCos { get; set; } = false;

        /// <summary>
        /// Output amplitude for SIN/COS. usually 10V.
        /// </summary>
        public double AmplitudeVolts { get; set; } = 10.0;

        /// <summary>
        /// If true, angle is treated as degrees and wrapped 0..360 (use for heading).
        /// For pitch/roll set false.
        /// </summary>
        public bool WrapDegrees360 { get; set; } = false;

        /// <summary>
        /// Optional wiring helpers.
        /// </summary>
        public bool SwapSinCos { get; set; } = false;
        public bool InvertSin { get; set; } = false;
        public bool InvertCos { get; set; } = false;

        /// <summary>
        /// When in SIN/COS mode, how fast to move the ANGLE per timer tick.
        /// If <= 0, it will fall back to SmoothFactor (so you don't need new config).
        /// Units are degrees per tick (since your refs are degrees).
        /// </summary>
        public double SmoothAngleStep { get; set; } = 0.0;


        /// <summary>
        /// --- Smoothing / throttling for SIN/COS mode ---
        /// Even if ProSim sends many updates per second, we'll only "pull" the target at this cadence
        /// and low-pass filter it so the needle doesn't jitter.
        /// </summary>
        public int TargetUpdateIntervalMs { get; set; } = 50;     // 20 Hz target update
        public double TargetFilterAlpha { get; set; } = 0.15;     // 0..1 (higher = snappier)
        public double DeadbandDegrees { get; set; } = 0.05;       // ignore tiny changes (deg)

        /// <summary>
        /// COS output identity. For OUT1001 modules, Channel is typically 0 and you differentiate by Serial/HubPort.
        /// If not set, it defaults to the same Serial/HubPort as SIN (not useful for two separate OUT1001s),
        /// so you should set these for the second device.
        /// </summary>
        public int? CosSerial { get; set; } = null;
        public int? CosHubPort { get; set; } = null;

        /// <summary>
        /// For multi-channel devices you can set CosChannel=1 etc.
        /// OUT1001 is single-channel so keep 0.
        /// </summary>
        public int SinChannel { get; set; } = 0;
        public int CosChannel { get; set; } = 0;

        // ---------------------------
        // Internals
        // ---------------------------

        private double lastVoltage = 0;

        private double currentPosition = 0;   // used in single-channel mode (volts)
        private double targetPosition = 0;    // used in single-channel mode (volts)

        private double currentAngleDeg = 0;   // used in SIN/COS mode (degrees)
        private double targetAngleDeg = 0;    // used in SIN/COS mode (degrees)


        private double rawAngleDeg = 0;       // latest angle from ProSim (degrees)
        private double filteredAngleDeg = 0;  // low-pass filtered target (degrees)
        private DateTime lastTargetPullUtc = DateTime.MinValue;

        private readonly Timer timer;

        public PhidgetsVoltageOutput(int deviceSerialN, int hubPort, string prosimDataRef, ProSimConnect connection)
        {
            timer = new Timer(Interval);
            timer.Elapsed += OnTimerElapsed;
            timer.AutoReset = true;

            Serial = deviceSerialN;
            HubPort = hubPort;
            ProsimDataRef = prosimDataRef;

            try
            {
                _ = Open();

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDataRef, 5, connection);
                dataRef.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                SendErrorLog("PhidgetsVoltageOutput ERROR: " + ex);
            }
        }

        public async Task Open()
        {
            try
            {
                if (voltageOutput.IsOpen)
                {
                    voltageOutput.Close();
                }

                voltageOutput.DeviceSerialNumber = Serial;
                voltageOutput.Channel = SinChannel;

                if (HubPort >= 0)
                {
                    voltageOutput.HubPort = HubPort;
                    voltageOutput.IsRemote = true;
                }

                await Task.Run(() => voltageOutput.Open(4000));
                voltageOutput.Voltage = 0;

                // Only open the COS channel/device if SIN/COS mode is enabled
                if (UseSinCos && !cosVoltageOutput.IsOpen)
                {
                    int cosSerial = CosSerial ?? Serial;
                    int cosHubPort = CosHubPort ?? HubPort;

                    cosVoltageOutput.DeviceSerialNumber = cosSerial;
                    cosVoltageOutput.Channel = CosChannel;

                    if (cosHubPort >= 0)
                    {
                        cosVoltageOutput.HubPort = cosHubPort;
                        cosVoltageOutput.IsRemote = true;
                    }

                    await Task.Run(() => cosVoltageOutput.Open(4000));
                    cosVoltageOutput.Voltage = 0;
                }
            }
            catch (Exception ex)
            {
                SendErrorLog($"Voltage output Open failed - {ProsimDataRef} to {Serial} [{HubPort}]");
                SendErrorLog(ex.Message);
                Debug.WriteLine(ex);
            }
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {
            var value = Math.Round(Convert.ToDouble(dataRef.value), 3);
            GotoValue(value, dataRef.name);
        }

        public void GotoValue(double value, string name)
        {
            // Keep existing behavior when NOT using Sin/Cos:
            // - value <= 0 becomes 0V
            // - value > 0 -> (value / ScaleFactor) + Offset
            if (!UseSinCos)
            {
                var convertedValue = value > 0 ? Math.Round((value / ScaleFactor), 2) : 0;
                convertedValue = convertedValue + Offset;

                if (convertedValue > 10 || convertedValue < -10)
                {
                    SendErrorLog($"Voltage invalid: {name} | value: {value} | Converted {convertedValue} | ScaleFactor: {ScaleFactor} | Offset {Offset}");
                    convertedValue = 0;
                }

                if (targetPosition != convertedValue)
                {
                    targetPosition = convertedValue;
                    if (!timer.Enabled)
                        timer.Start();
                }

                return;
            }

            // SIN/COS mode:
            // Angle is in degrees (you clarified pitch/roll = -90..+90, heading = 1..360).
            // We keep ScaleFactor/Offset as a generic mapping:
            // angleDeg = (value / ScaleFactor) + Offset
            double angleDeg = (ScaleFactor == 0) ? 0 : (value / ScaleFactor);
            angleDeg = Math.Round(angleDeg, 4) + Offset;

            if (WrapDegrees360)
                angleDeg = NormalizeDegrees360(angleDeg);

            // Deadband: ignore tiny changes to reduce jitter
            if (WrapDegrees360)
            {
                if (Math.Abs(ShortestDeltaDegrees(rawAngleDeg, angleDeg)) < DeadbandDegrees)
                    return;
            }
            else
            {
                if (Math.Abs(angleDeg - rawAngleDeg) < DeadbandDegrees)
                    return;
            }

            rawAngleDeg = angleDeg;

            // Ensure timer is running so we can pull/filter the target at a steady cadence
            if (!timer.Enabled)
                timer.Start();
        }

        // ---------------------------
        // Single-channel output (existing)
        // ---------------------------
        private void UpdateNeedlePosition(double convertedValue)
        {
            try
            {
                if (voltageOutput.IsOpen)
                    voltageOutput.Voltage = Convert.ToDouble(convertedValue);
            }
            catch (Exception ex)
            {
                SendErrorLog("ERROR: " + convertedValue + " to " + targetPosition);
                SendErrorLog(ex.ToString());
            }
        }

        // ---------------------------
        // Timer tick: either single-channel OR sin/cos
        // ---------------------------
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!UseSinCos)
            {
                // Existing voltage smoothing logic (kept)
                if (targetPosition > currentPosition)
                {
                    currentPosition += SmoothFactor;
                    if (currentPosition >= targetPosition)
                    {
                        currentPosition = targetPosition;
                        timer.Stop();
                    }
                }
                else
                {
                    currentPosition -= SmoothFactor;
                    if (currentPosition <= targetPosition)
                    {
                        currentPosition = targetPosition;
                        timer.Stop();
                    }
                }

                UpdateNeedlePosition(currentPosition);
                return;
            }

            // SIN/COS smoothing: move angle toward target, then output sin/cos

            // Pull/low-pass filter the raw target at a controlled rate (throttling)
            var nowUtc = DateTime.UtcNow;
            if (lastTargetPullUtc == DateTime.MinValue)
            {
                filteredAngleDeg = rawAngleDeg;
                targetAngleDeg = rawAngleDeg;
                lastTargetPullUtc = nowUtc;
            }

            if ((nowUtc - lastTargetPullUtc).TotalMilliseconds >= TargetUpdateIntervalMs)
            {
                // Update filtered target toward the latest raw angle
                if (WrapDegrees360)
                {
                    double d = ShortestDeltaDegrees(filteredAngleDeg, rawAngleDeg);
                    filteredAngleDeg = NormalizeDegrees360(filteredAngleDeg + (TargetFilterAlpha * d));
                }
                else
                {
                    filteredAngleDeg = filteredAngleDeg + (TargetFilterAlpha * (rawAngleDeg - filteredAngleDeg));
                }

                targetAngleDeg = filteredAngleDeg;
                lastTargetPullUtc = nowUtc;
            }

            double step = SmoothAngleStep > 0 ? SmoothAngleStep : Math.Abs(SmoothFactor);
            if (step <= 0) step = 0.1;

            if (WrapDegrees360)
            {
                double delta = ShortestDeltaDegrees(currentAngleDeg, targetAngleDeg);
                if (Math.Abs(delta) <= step)
                {
                    currentAngleDeg = targetAngleDeg;
                    timer.Stop();
                }
                else
                {
                    currentAngleDeg = NormalizeDegrees360(currentAngleDeg + Math.Sign(delta) * step);
                }
            }
            else
            {
                if (Math.Abs(targetAngleDeg - currentAngleDeg) <= step)
                {
                    currentAngleDeg = targetAngleDeg;
                    timer.Stop();
                }
                else
                {
                    currentAngleDeg += Math.Sign(targetAngleDeg - currentAngleDeg) * step;
                }
            }

            // Convert angle to radians for trig
            double rad = currentAngleDeg * (Math.PI / 180.0);


            double sinV = AmplitudeVolts * Math.Sin(rad);
            double cosV = AmplitudeVolts * Math.Cos(rad);

            if (InvertSin) sinV = -sinV;
            if (InvertCos) cosV = -cosV;

            if (SwapSinCos)
            {
                double tmp = sinV;
                sinV = cosV;
                cosV = tmp;
            }

            sinV = Clamp(sinV, -10.0, 10.0);
            cosV = Clamp(cosV, -10.0, 10.0);

            try
            {
                if (voltageOutput.IsOpen)
                    voltageOutput.Voltage = sinV;

                if (cosVoltageOutput.IsOpen)
                    cosVoltageOutput.Voltage = cosV;
            }
            catch (Exception ex)
            {
                SendErrorLog($"ERROR writing SIN/COS: sin={sinV:0.000} cos={cosV:0.000} angleDeg={currentAngleDeg:0.###}");
                SendErrorLog(ex.ToString());
            }
        }

        // ---------------------------
        // Helpers
        // ---------------------------
        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);

        private static double NormalizeDegrees360(double deg)
        {
            deg %= 360.0;
            if (deg < 0) deg += 360.0;
            return deg;
        }

        // signed shortest delta current->target in degrees in [-180, +180]
        private static double ShortestDeltaDegrees(double currentDeg, double targetDeg)
        {
            double c = NormalizeDegrees360(currentDeg);
            double t = NormalizeDegrees360(targetDeg);
            double d = t - c;
            if (d > 180) d -= 360;
            if (d < -180) d += 360;
            return d;
        }
    }
}
