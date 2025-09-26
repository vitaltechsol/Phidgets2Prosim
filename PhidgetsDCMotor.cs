using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Phidgets2Prosim
{

    internal class PhidgetsDCMotor : PhidgetDevice
    {
        string prosimDatmRefBwd;
        string prosimDatmRefFwd;
        double targetVelFwd = 1;
        double targetVelBwd = 1;
        double currentVel = 0;
        bool isPaused = false;
        private bool isMotorMoving = false;
        private System.Timers.Timer pulsateTimer;

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


        DCMotor dcMotor = new DCMotor();

        // ==== VoltageInput feedback ====
        public int TargetVoltageInputHub { get; set; } = -1;
        public int TargetVoltageInputPort { get; set; } = -1;
        public int TargetVoltageInputChannel { get; set; } = 0;
        private VoltageInput _vin;

        // ==== Control tuning (initialized via MotorTuningOptions) ====
        private double MaxVelocity = 0.25;
        private double MinVelocity = 0.035;
        private double VelocityBand = 0.50;
        private double CurveGamma = 0.65;
        private double DeadbandEnter = 0.01;
        private double DeadbandExit = 0.02;
        private double MaxVelStepPerTick = 0.01;
        private double Kp = 0.0;
        private double Ki = 0.0;
        private double Kd = 0.0;
        private double IntegralLimit = 0.25;
        private double PositionFilterAlpha = 0.25;
        private int TickMs = 20;

        // ==== State for PID control ====
        private double _targetVoltage = 0.0;
        private double _feedbackVoltage = 0.0;
        private double _filteredVoltage = 0.0;
        private double _velCmd = 0.0;
        private double _integral = 0.0;
        private double _lastError = 0.0;
        private bool _isSettled = true;

        public PhidgetsDCMotor(
            int serial,
            int hubPort,
            string prosimDataRefFwd,
            string prosimDataRefBwd,
            ProSimConnect connection,
            MotorTuningOptions options = null)
        {
            try
            {
                HubPort = hubPort;
                this.prosimDatmRefBwd = prosimDataRefBwd;
                this.prosimDatmRefFwd = prosimDataRefFwd;

                if (options != null)
                {
                    if (options.MaxVelocity.HasValue) MaxVelocity = options.MaxVelocity.Value;
                    if (options.MinVelocity.HasValue) MinVelocity = options.MinVelocity.Value;
                    if (options.VelocityBand.HasValue) VelocityBand = options.VelocityBand.Value;
                    if (options.CurveGamma.HasValue) CurveGamma = options.CurveGamma.Value;
                    if (options.DeadbandEnter.HasValue) DeadbandEnter = options.DeadbandEnter.Value;
                    if (options.DeadbandExit.HasValue) DeadbandExit = options.DeadbandExit.Value;
                    if (options.MaxVelStepPerTick.HasValue) MaxVelStepPerTick = options.MaxVelStepPerTick.Value;
                    if (options.Kp.HasValue) Kp = options.Kp.Value;
                    if (options.Ki.HasValue) Ki = options.Ki.Value;
                    if (options.Kd.HasValue) Kd = options.Kd.Value;
                    if (options.IntegralLimit.HasValue) IntegralLimit = options.IntegralLimit.Value;
                    if (options.PositionFilterAlpha.HasValue) PositionFilterAlpha = options.PositionFilterAlpha.Value;
                    if (options.TickMs.HasValue) TickMs = options.TickMs.Value;
                }

                if (HubPort >= 0)
                {
                    dcMotor.HubPort = HubPort;
                    dcMotor.IsRemote = true;
                }
                dcMotor.Open(5000);
                dcMotor.DeviceSerialNumber = serial;
                dcMotor.Acceleration = 100;
                dcMotor.TargetBrakingStrength = 1;

                // ProSim bindings
                if (prosimDataRefFwd != "") { 
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

        public void AttachTargetVoltageInput()
        {
            _vin?.Close();
            _vin = new VoltageInput
            {
                DeviceSerialNumber = TargetVoltageInputHub,
                HubPort = TargetVoltageInputPort,
                Channel = TargetVoltageInputChannel,
                IsRemote = true
            };
            _vin.Open(5000);
            _vin.VoltageChange += (s, e) =>
            {
                _feedbackVoltage = e.Voltage;
                if (_filteredVoltage == 0.0) _filteredVoltage = _feedbackVoltage;
            };
        }

        public async Task MoveToTarget(double voltage)
        {
            if (_vin == null) throw new InvalidOperationException("VoltageInput not attached.");
            if (voltage < 0) voltage = 0;
            if (voltage > 5) voltage = 5;

            _targetVoltage = voltage;

            while (true)
            {
                // filter feedback
                _filteredVoltage = PositionFilterAlpha * _feedbackVoltage + (1 - PositionFilterAlpha) * _filteredVoltage;
                double error = _targetVoltage - _filteredVoltage;

                // deadband hysteresis
                if (_isSettled)
                {
                    if (Math.Abs(error) > DeadbandExit) _isSettled = false;
                }
                else
                {
                    if (Math.Abs(error) < DeadbandEnter) _isSettled = true;
                }

                double desiredVel = 0.0;
                double dt = TickMs / 1000.0;

                if (!_isSettled)
                {
                    // base velocity
                    double norm = Math.Min(1.0, Math.Abs(error) / VelocityBand);
                    double shaped = Math.Pow(norm, CurveGamma);
                    desiredVel = Math.Sign(error) * (MinVelocity + (MaxVelocity - MinVelocity) * shaped);

                    // PID terms
                    double p = Kp * error;
                    if (Math.Abs(error) < VelocityBand)
                    {
                        _integral += Ki * error * dt;
                        _integral = Math.Max(-IntegralLimit, Math.Min(IntegralLimit, _integral));
                    }
                    else
                    {
                        _integral = 0.0;
                    }

                    double d = Kd * ((error - _lastError) / Math.Max(1e-6, dt));
                    desiredVel += p + _integral - d;
                    _lastError = error;

                    if (desiredVel > 1) desiredVel = 1;
                    if (desiredVel < -1) desiredVel = -1;
                }
                else
                {
                    _integral = 0;
                    _lastError = 0;
                    desiredVel = 0;
                }

                // slew limit
                double step = desiredVel - _velCmd;
                if (step > MaxVelStepPerTick) step = MaxVelStepPerTick;
                if (step < -MaxVelStepPerTick) step = -MaxVelStepPerTick;
                _velCmd += step;

                dcMotor.TargetVelocity = _velCmd;
                Debug.WriteLine($"target {_targetVoltage:F2}V feedback {_filteredVoltage:F2}V err {error:F2}V vel {_velCmd:F3}");

                if (_isSettled && Math.Abs(_velCmd) < MinVelocity * 0.5)
                {
                    Debug.WriteLine($"target 0");
                    dcMotor.TargetVelocity = 0;
                    break;
                }

                await Task.Delay(TickMs);
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

        private async void Open()
        {
            try
            {
                await Task.Run(() => dcMotor.Open(500));
                SendErrorLog("DC Motor Connected " + HubPort + "ch" + Channel);
            }
            catch (Exception ex)
            {
                SendErrorLog("Open failed for DC Motor " + HubPort);
                SendErrorLog(ex.ToString());
            }
        }

        /// Map a logical velocity in [Range[0], Range[1]] to physical [-1, 1].
        private double MapToPhysical(double logical)
        {
            double rMin = range[0], rMax = range[1];
            double t = (logical - rMin) / (rMax - rMin); // 0..1
            double physical = -1 + (t * 2);              // -1..1
            if (double.IsNaN(physical) || double.IsInfinity(physical)) return 0;
            return Math.Max(-1, Math.Min(1, physical));  // clamp
        }

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

    }
}
