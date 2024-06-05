using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Phidgets2Prosim
{
    internal class PhidgetsDCMotor
    {
        string prosimDatmRefBwd;
        string prosimDatmRefFwd;
        double targetVelFwd = 1;
        double targetVelBwd = 1;
        double currentVel = 0;
        bool isPaused = false;
        int hubPort;
        private bool isMotorMoving = false;
        private System.Timers.Timer pulsateTimer;

        public bool pulsateMotor { get; set; } = false;
        public int PulsateMotorInterval { get; set; } = 550;
        public int PulsateMotorIntervalPause { get; set; } = 200;

        private int pulseIntervalPauseReduced = 0;


        DCMotor dcMotor = new DCMotor();
        public PhidgetsDCMotor(int hubPort, string prosimDatmRefFwd, string prosimDatmRefBwd, ProSimConnect connection)
        {
            try
            {
                this.hubPort = hubPort;
                this.prosimDatmRefBwd = prosimDatmRefBwd;
                this.prosimDatmRefFwd = prosimDatmRefFwd;

                dcMotor.HubPort = hubPort;
                dcMotor.IsRemote = true;
                dcMotor.Open(5000);
                dcMotor.DeviceSerialNumber = 668534;
                dcMotor.Acceleration = 100;
                dcMotor.TargetBrakingStrength = 1;

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRefFwd, 100, connection);
                DataRef dataRef2 = new DataRef(prosimDatmRefBwd, 100, connection);

                dataRef.onDataChange += DataRef_onDataChange;
                dataRef2.onDataChange += DataRef_onDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("prosimDatmRefCW " + prosimDatmRefFwd);
            }
        }

        private async void DataRef_onDataChange(DataRef dataRef)
        {

            Debug.WriteLine("trim name " + dataRef.name);
            // var name = dataRef.name;
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
                        if (pulsateMotor) { 
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
                dcMotor.TargetVelocity = 0;
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }

        }

        public void changeTargetFwdVelocity(double val)
        {
            targetVelFwd = val;
        }

        public void changeTargetBwdVelocity(double val)
        {
            targetVelBwd = val;
        }

        public void changeTargetVelocity(double val)
        {
            targetVelFwd = val;
            targetVelBwd = val;
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Open failed for DC Motor " + hubPort);
                Debug.WriteLine(ex.ToString());
            }
        }


        private async void PulsateMotor(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isMotorMoving)
            {
                dcMotor.TargetVelocity = 0;
                await Task.Delay(pulseIntervalPauseReduced);
                pulseIntervalPauseReduced -= 30;
                if (pulseIntervalPauseReduced < 0) { pulseIntervalPauseReduced = 0;}
                dcMotor.TargetVelocity = currentVel;
            }
        }
    }
}

