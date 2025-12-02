using Phidget22;
using ProSimSDK;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
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
        double targetFwdVelocity = 1.0;   // logical velocity magnitude (0..1)
        double targetBwdVelocity = 1.0;   // logical velocity magnitude (0..1)
        double prevTrim = 0;

        bool isAPOn = false;
        double flaps = 0;
        double currentVel = 0;            // logical velocity in [-1, 1]
        bool isMotorMoving = false;
        bool isPaused = false;
        System.Timers.Timer pulsateTimer;
        public bool AccelerateMotor { get; set; } = true;
        public int AccelerateMotorRate { get; set; } = 550;
        public int AccelerateMotorIntervalPause { get; set; } = 200;
        private int pulseIntervalPauseReduced = 0;

        // Mapping for physical motor range
        private readonly double rangeMin;
        private readonly double rangeMax;

        private readonly string prosimDataRefFwd = "system.gates.B_TRIM_MOTOR_UP";
        private readonly string prosimDataRefBwd = "system.gates.B_TRIM_MOTOR_DOWN";

        public Custom_TrimWheel(
            int serial,
            int hubPort,
            ProSimConnect connection,
            bool reversed,
            double dirtyUp, double dirtyDown,
            double cleanUp, double cleanDown,
            double APOnDirty,
            double APOnClean,
			bool accelerateMotor,
			double[] range
        )
        {
            dcm = new PhidgetsDCMotor(serial, hubPort, connection)
            {
                Reversed = reversed,
                CurrentLimit = 4,
                Acceleration = 100,
                TargetBrakingStrength = 1
            };
            dcm.ErrorLog += SendErrorLog;
            dcm.InfoLog += SendInfoLog;
            dcm.InitializeAsync().Wait();

            // Configure logical->physical range mapping
            if (range == null || range.Length != 2)
            {
                // Default behavior: Phidgets-style -1..1
                rangeMin = -1.0;
                rangeMax = 1.0;
			}
            else
            {
                rangeMin = range[0];
                rangeMax = range[1];
			}

			
			Debug.WriteLine($"[TrimWheel] rangeMin={rangeMin:F3} rangeMax={rangeMax:F3}");

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
            this.AccelerateMotor = accelerateMotor;

			dataRefSpeed.onDataChange += DataRef_onFlapsDataChange;
            dataRefAP.onDataChange += DataRef_onAPDataChange;

            // ProSim bindings (kept)
            DataRef dataRef = new DataRef(prosimDataRefFwd, 100, connection);
            dataRef.onDataChange += DataRef_onDataChange;
            DataRef dataRef2 = new DataRef(prosimDataRefBwd, 100, connection);
            dataRef2.onDataChange += DataRef_onDataChange;
        }

        /// <summary>
        /// Map a logical velocity in [-1, 1] to the physical motor range.
        /// Default: [-1,1] => identity
        /// If range = [0,1] => -1 -> 0, 0 -> 0.5, 1 -> 1
        /// </summary>
        private double MapVelocity(double logicalVelocity)
        {
            // Clamp logical velocity to [-1, 1]
            var v = Math.Max(-1.0, Math.Min(1.0, logicalVelocity));

            // Avoid divide-by-zero if misconfigured
            if (Math.Abs(rangeMax - rangeMin) < 1e-9)
                return 0.0;

            // Convert logical [-1,1] to normalized [0,1]
            double t = (v + 1.0) / 2.0;

            // Map normalized [0,1] into [rangeMin, rangeMax]
            double mapped = rangeMin + t * (rangeMax - rangeMin);

            Debug.WriteLine($"[TrimWheel MapVelocity] logical={v:F3} mapped={mapped:F3} range=[{rangeMin:F3},{rangeMax:F3}]");

			return mapped;

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

            if (isAPOn)
            {
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
            }
            else
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

                pulseIntervalPauseReduced = AccelerateMotorIntervalPause;

                if (dataRef.name == prosimDataRefFwd && !isPaused)
                {
                    if (value == true)
                    {
                        // Forward gate ON -> logical negative velocity
                        currentVel = targetFwdVelocity * -1;
                        isMotorMoving = true;
                        dcm.ApplyVelocity(MapVelocity(currentVel));
                        if (AccelerateMotor)
                        {
                            if (pulsateTimer == null)
                            {
                                pulsateTimer = new System.Timers.Timer();
                                pulsateTimer.Interval = AccelerateMotorRate;
                                pulsateTimer.Elapsed += Accelerate;
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
                        // Kick in opposite logical direction (+0.5)
                        dcm.ApplyVelocity(MapVelocity(0.5));
                        Thread.Sleep(200);
                        dcm.ApplyVelocity(MapVelocity(currentVel));
                    }
                }

                if (dataRef.name == prosimDataRefBwd && !isPaused)
                {
                    if (value == true)
                    {
                        // Backward gate ON -> logical positive velocity
                        currentVel = targetBwdVelocity;
                        isMotorMoving = true;
                        dcm.ApplyVelocity(MapVelocity(currentVel));
                        if (AccelerateMotor)
                        {
                            if (pulsateTimer == null)
                            {
                                pulsateTimer = new System.Timers.Timer();
                                pulsateTimer.Interval = AccelerateMotorRate;
                                pulsateTimer.Elapsed += Accelerate;
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
                        // Kick in opposite logical direction (-0.5)
                        dcm.ApplyVelocity(MapVelocity(-0.5));
                        Thread.Sleep(200);
                        dcm.ApplyVelocity(MapVelocity(currentVel));
                    }
                }
            }
            catch (Exception ex)
            {
                // Stop motor
                currentVel = 0;
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
                dcm.ApplyVelocity(MapVelocity(0)); // ensure motor stopped
            }

        }

        private async void Accelerate(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isMotorMoving)
            {
                // Pause: motor logical 0 (stop), then resume current logical velocity
                dcm.ApplyVelocity(MapVelocity(0));
                await Task.Delay(pulseIntervalPauseReduced);
                pulseIntervalPauseReduced -= 30;
                if (pulseIntervalPauseReduced < 0) { pulseIntervalPauseReduced = 0; }
                dcm.ApplyVelocity(MapVelocity(currentVel));
            }

        }

        public void Pause(bool isPaused)
        {
            dcm.Pause(isPaused);
        }
    }
}
