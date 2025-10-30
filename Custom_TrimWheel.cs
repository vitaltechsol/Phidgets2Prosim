using Phidget22;
using ProSimSDK;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{

    internal class Custom_TrimWheel : PhidgetDevice
    {
        PhidgetsDCMotor dcm;
        double dirtyUp;
        double dirtyDown;
        double cleanUp;
        double cleanDown;
        double APOnDirty;
        double APOnClean;
		double targetFwdVelocity = 1.0;
        double targetBwdVelocity = 1.0;
        double prevTrim = 0;

        bool isAPOn = false;
        double flaps = 0;
        double currentVel = 0;
        bool isMotorMoving = false;
        bool isPaused = false;
        System.Timers.Timer pulsateTimer;
            
        public bool pulsateMotor { get; set; } = true;
        public int PulsateMotorInterval { get; set; } = 550;
        public int PulsateMotorIntervalPause { get; set; } = 200;
        private int pulseIntervalPauseReduced = 0;

        private readonly string prosimDataRefFwd = "system.gates.B_TRIM_MOTOR_UP";
        private readonly string prosimDataRefBwd = "system.gates.B_TRIM_MOTOR_DOWN";

        public Custom_TrimWheel(int serial, int hubPort, ProSimConnect connection, 
            double dirtyUp, double dirtyDown, 
            double cleanUp, double cleanDown, 
            double APOnDirty,
            double APOnClean)
        {
            dcm = new PhidgetsDCMotor(serial, hubPort, connection)
            {
                Reversed = false,
                CurrentLimit = 4,
                Acceleration = 100,
                TargetBrakingStrength = 1
            };
            dcm.ErrorLog += SendErrorLog;
            dcm.InfoLog += SendInfoLog;
            dcm.InitializeAsync().Wait();

            DataRef dataRefSpeed = new DataRef("system.gauge.G_MIP_FLAP", 100, connection);
            DataRef dataRefAP = new DataRef("system.gates.B_PITCH_CMD", 100, connection);

            var dataRefTrim = new DataRef("system.gauge.G_PED_ELEV_TRIM", 500, connection);
            dataRefTrim.onDataChange += DataRef_trim_onDataChange;

            this.dirtyUp = dirtyUp;
            this.dirtyDown = dirtyDown;
            this.cleanUp = cleanUp;
            this.cleanDown = cleanDown;
            this.APOnDirty = APOnDirty;
            this.APOnClean = APOnClean;

            dataRefSpeed.onDataChange += DataRef_onFlapsDataChange;
            dataRefAP.onDataChange += DataRef_onAPDataChange;

            // ProSim bindings (kept)
            DataRef dataRef = new DataRef(prosimDataRefFwd, 100, connection);
            dataRef.onDataChange += DataRef_onDataChange;
            DataRef dataRef2 = new DataRef(prosimDataRefBwd, 100, connection);
            dataRef2.onDataChange += DataRef_onDataChange;
        }


        private async void DataRef_onFlapsDataChange(DataRef dataRef)
        {
            var value = Convert.ToDouble(dataRef.value);
            Debug.WriteLine("flaps changed  " + dataRef.value);
            flaps = value;
            UpdateVelocity();
        }

        private async void DataRef_onAPDataChange(DataRef dataRef)
        {
            var value = Convert.ToBoolean(dataRef.value);
            Debug.WriteLine("AP changed  " + dataRef.value);
            isAPOn = value;
            UpdateVelocity();
        }

        private void DataRef_trim_onDataChange(DataRef dataRef)
        {
            // txtCDU1.Text = dataRef.name;
            //var newTrim = Math.Round((double)dataRef.value, 2);
            //Debug.WriteLine("trim changed  " + (prevTrim - newTrim).ToString());
            //prevTrim = newTrim;

        }

        private async void UpdateVelocity()
        {

            Debug.WriteLine("UpdateVelocity is AP on  " + isAPOn);
            Debug.WriteLine("UpdateVelocity Flaps  " + flaps);

            if (isAPOn) { 
                // Dirty
                if (flaps > 1)
                {
                    targetFwdVelocity = APOnDirty;
                    targetBwdVelocity = APOnDirty;
                }
                // Clean
                else
                {
                    targetFwdVelocity = APOnClean;
                    targetBwdVelocity = APOnClean;
                }
            } else
            {
                // Dirty
                if (flaps > 1)
                {
                    targetFwdVelocity = dirtyUp;
                    targetBwdVelocity = dirtyDown;
                }
                // Clean
                else
                {
                    targetFwdVelocity = cleanUp;
                    targetBwdVelocity = cleanDown;
                }
            }

            Debug.WriteLine("UpdateVelocity fw   " + targetFwdVelocity);
            Debug.WriteLine("UpdateVelocity bw   " + targetBwdVelocity);
        }

        private async void DataRef_onDataChange(DataRef dataRef)
        {
            Debug.WriteLine("trim name " + dataRef.name);
            try
            {
                var value = Convert.ToBoolean(dataRef.value);
                Debug.WriteLine("trim value " + dataRef.value + " | Paused:" + isPaused);

                pulseIntervalPauseReduced = PulsateMotorIntervalPause;

                if (dataRef.name == prosimDataRefFwd && !isPaused)
                {
                    if (value == true)
                    {
                        currentVel = targetFwdVelocity * -1;
                        isMotorMoving = true;
                        dcm.SetTargetVelocity(currentVel);
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
                        // Kickback when stopping
                        currentVel = 0;
                        dcm.SetTargetVelocity(0.5);
                        Thread.Sleep(200);
                        dcm.SetTargetVelocity(currentVel);
                    }
                }

                if (dataRef.name == prosimDataRefBwd && !isPaused)
                {
                    if (value == true)
                    {
                        currentVel = targetBwdVelocity;
                        isMotorMoving = true;
                        dcm.SetTargetVelocity(currentVel);
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
                        dcm.SetTargetVelocity(-0.5);
                        Thread.Sleep(200);
                        dcm.SetTargetVelocity(currentVel);
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

        private async void PulsateMotor(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isMotorMoving)
            {
                dcm.SetTargetVelocity(0);
                await Task.Delay(pulseIntervalPauseReduced);
                pulseIntervalPauseReduced -= 30;
                if (pulseIntervalPauseReduced < 0) { pulseIntervalPauseReduced = 0; }
                dcm.SetTargetVelocity(currentVel);
            }
        }


        public void Pause(bool isPaused)
        {
            dcm.Pause(isPaused);
        }
    }
}
