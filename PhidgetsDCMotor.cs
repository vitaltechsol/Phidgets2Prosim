using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsDCMotor : MotorBase
    {
        // ===== Your existing fields/APIs preserved =====
        string prosimDatmRefBwd;
        string prosimDatmRefFwd;
        double targetVelFwd = 1;
        double targetVelBwd = 1;
        double currentVel = 0;
        bool isPaused = false;
        private bool isMotorMoving = false;
        private System.Timers.Timer pulsateTimer;
        private string _refTargetPos = "";


        // (Optional) Move motor to target position based on a prosim gauge reference
        public string RefTargetPos
        {
            get => _refTargetPos;
            set
            {
                _refTargetPos = value;
                UseRefTarget(value); 
            }
        }

        // (Optional, Required with RefTargetPos) Gauge reference range
        public double[] TargetPosMap { get; set; } = new double[] { 0, 255 };

        // (Optional, Required with RefTargetPos) Scale motor target position based on analog input range
        public double[] TargetPosScaleMap { get; set; } = new double[] { 0, 5 };

        public bool pulsateMotor { get; set; } = false;
        public int PulsateMotorInterval { get; set; } = 550;
        public int PulsateMotorIntervalPause { get; set; } = 200;
        private int pulseIntervalPauseReduced = 0;

        private double[] range = new double[] { -1, 1 };
        public double[] Range
        {
            get => range;
            set
            {
                if (value == null || value.Length != 2)
                    throw new ArgumentException("Range must be a double[2], e.g., new[] { -1, 1 } or new[] { 0, 1 }.");
                if (value[0] == value[1])
                    throw new ArgumentException("Range min and max cannot be the same.");
                double a = value[0], b = value[1];
                range = a <= b ? new double[] { a, b } : new double[] { b, a };
            }
        }

        public void changeTargetFwdVelocity(double val) => targetVelFwd = val;
        public void changeTargetBwdVelocity(double val) => targetVelBwd = val;
        public void changeTargetVelocity(double val) { targetVelFwd = val; targetVelBwd = val; }

        private readonly DCMotor dcMotor = new DCMotor();

        public PhidgetsDCMotor(
            int serial,
            int hubPort,
            ProSimConnect connection,
            string prosimDataRefFwd,
            string prosimDataRefBwd,
            double acceleration,
            MotorTuningOptions options = null
        ) : base(options)
        {
            try
            {
                HubPort = hubPort;
                Serial = serial;
                Connection = connection;
                Acceleration = acceleration;
                this.prosimDatmRefBwd = prosimDataRefBwd;
                this.prosimDatmRefFwd = prosimDataRefFwd;

                if (HubPort >= 0)
                {
                    dcMotor.HubPort = HubPort;
                    dcMotor.IsRemote = true;
                }
                dcMotor.DeviceSerialNumber = serial;
                dcMotor.Acceleration = Acceleration;
                dcMotor.TargetBrakingStrength = 1;
                dcMotor.CurrentLimit = 4;
                SendInfoLog($"DC Motor Connected {Serial}: {HubPort}");

                // ProSim bindings (kept)
                if (prosimDataRefFwd != "")
                {
                    DataRef dataRef = new DataRef(prosimDataRefFwd, 100, connection);
                    dataRef.onDataChange += DataRef_onDataChange;
                }
                if (prosimDataRefBwd != "")
                {
                    DataRef dataRef2 = new DataRef(prosimDataRefBwd, 100, connection);
                    dataRef2.onDataChange += DataRef_onDataChange;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("prosimDatmRefCW " + prosimDataRefFwd);
            }
        }

        private async void DataRef_onDataChange(DataRef dataRef)
        {
            Debug.WriteLine("trim name " + dataRef.name);
            try
            {
                var value = Convert.ToBoolean(dataRef.value);

                Debug.WriteLine(dataRef.name);
                Debug.WriteLine(value);

                pulseIntervalPauseReduced = PulsateMotorIntervalPause;

                if (dataRef.name == prosimDatmRefFwd && !isPaused)
                {
                    if (value == true)
                    {
                        currentVel = targetVelFwd * -1;

                        isMotorMoving = true;
                        dcMotor.TargetVelocity = currentVel;
                        if (pulsateMotor)
                        {
                            if (pulsateTimer == null)
                            {
                                pulsateTimer = new System.Timers.Timer();
                                pulsateTimer.Interval = PulsateMotorInterval;
                                pulsateTimer.Elapsed += PulsateMotor;
                                pulsateTimer.Start();
                            }
                        }
                    }
                    else
                    {
                        isMotorMoving = false;
                        if (pulsateTimer != null)
                        {
                            pulsateTimer.Stop();
                            pulsateTimer.Dispose();
                            pulsateTimer = null;
                        }
                        currentVel = 0;
                        dcMotor.TargetVelocity = 0.5;
                        Thread.Sleep(200);
                        dcMotor.TargetVelocity = currentVel;
                    }
                }

                if (dataRef.name == prosimDatmRefBwd && !isPaused)
                {
                    if (value == true)
                    {
                        currentVel = targetVelBwd;
                        isMotorMoving = true;
                        dcMotor.TargetVelocity = currentVel;
                        if (pulsateMotor)
                        {
                            if (pulsateTimer == null)
                            {
                                pulsateTimer = new System.Timers.Timer();
                                pulsateTimer.Interval = PulsateMotorInterval;
                                pulsateTimer.Elapsed += PulsateMotor;
                                pulsateTimer.Start();
                            }
                        }
                    }
                    else
                    {
                        isMotorMoving = false;
                        if (pulsateTimer != null)
                        {
                            pulsateTimer.Stop();
                            pulsateTimer.Dispose();
                            pulsateTimer = null;
                        }
                        currentVel = 0;
                        dcMotor.TargetVelocity = -0.5;
                        Thread.Sleep(200);
                        dcMotor.TargetVelocity = currentVel;
                    }
                }
            }
            catch (Exception ex)
            {
                // Stop motor
                currentVel = 0;
                // dcMotor.TargetVelocity = 0;
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }

        }

        private async void DataRef_onTargetChange(DataRef dataRef)
        {
            try
            {
                var value = Convert.ToDouble(dataRef.value);

                Debug.WriteLine($"Target ref name:{dataRef.name} - Moving to: {dataRef.value} ");

                await OnTargetMoving (
                    movingTo: value,
                    targetMap: TargetPosMap,
                    scaleMap: TargetPosScaleMap
                );

            }
            catch (Exception ex)
            {
                // Stop motor
                currentVel = 0;
                // dcMotor.TargetVelocity = 0;
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }

        }

        private void UseRefTarget(string refTargetName)
        {
            if (refTargetName != "")
            {
                DataRef dataRef = new DataRef(refTargetName, 100, Connection);
                dataRef.onDataChange += DataRef_onTargetChange;
            }
        }
        public void pause(bool isPaused)
        {
            this.isPaused = isPaused;
            if (isPaused == true)
            {
                dcMotor.TargetVelocity = 0;
            }
            else
            {
                dcMotor.TargetVelocity = currentVel;
            }
        }

        public async void Open()
        {
            try
            {
                await Task.Run(() => dcMotor.Open(500));
                dcMotor.Acceleration = Acceleration;
                dcMotor.TargetBrakingStrength = 1;
                dcMotor.CurrentLimit = 4;
                SendInfoLog($"DC Motor Connected {Serial}: {HubPort}");
            }
            catch (Exception ex)
            {
                SendErrorLog($"Open Fail for DC Motor {Serial}: {HubPort}");
                SendErrorLog(ex.ToString());
            }
        }
/*
        /// Map a logical velocity in [Range[0], Range[1]] to physical [-1, 1].
        private double MapToPhysical(double logical)
        {
            double rMin = range[0], rMax = range[1];
            double t = (logical - rMin) / (rMax - rMin); // 0..1
            double physical = -1 + (t * 2);              // -1..1
            if (double.IsNaN(physical) || double.IsInfinity(physical)) return 0;
            return Math.Max(-1, Math.Min(1, physical));  // clamp
        }
*/
        private async void PulsateMotor(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isMotorMoving)
            {
                dcMotor.TargetVelocity = 0;
                await Task.Delay(pulseIntervalPauseReduced);
                pulseIntervalPauseReduced -= 30;
                if (pulseIntervalPauseReduced < 0) { pulseIntervalPauseReduced = 0; }
                dcMotor.TargetVelocity = currentVel;
            }
        }

        protected override void ApplyVelocity(double velocity)
        {

            if (!dcMotor.Attached) return;

            dcMotor.TargetVelocity = velocity;
            SendInfoLog($"[DC MOTOR ApplyVelocity] sent {velocity:F3}");
            Debug.WriteLine($"[{TS()}] [ApplyVelocity] sent {velocity:F3}, CurrentLimit={dcMotor.CurrentLimit}, Braking={dcMotor.BrakingStrength}");

        }

        protected static string TS() => DateTime.UtcNow.ToString("HH:mm:ss.fff");

    }
}
