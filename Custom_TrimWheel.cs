using Phidget22;
using ProSimSDK;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        double targetFwdVelocity;
        double targetBwdVelocity;
        double prevTrim = 0;

        bool isAPOn = false;
        double flaps = 0;

        public Custom_TrimWheel(int serial, int hubPort, ProSimConnect connection, 
            double dirtyUp, double dirtyDown, 
            double cleanUp, double cleanDown, 
            double APOnDirty,
            double APOnClean)
        {
            dcm = new PhidgetsDCMotor(serial, hubPort, "system.gates.B_TRIM_MOTOR_UP", "system.gates.B_TRIM_MOTOR_DOWN", connection);
            dcm.pulsateMotor = true;
            dcm.ErrorLog += SendErrorLog;
            dcm.InfoLog += SendInfoLog;

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
            var newTrim = Math.Round((double)dataRef.value, 2);
            Debug.WriteLine("trim changed  " + (prevTrim - newTrim).ToString());
            prevTrim = newTrim;

        }

        private async void UpdateVelocity()
        {

            Debug.WriteLine("UpdateVelocity isApon  " + isAPOn);
            Debug.WriteLine("UpdateVelocity Flasp  " + flaps);


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


            dcm.changeTargetFwdVelocity(targetFwdVelocity);
            dcm.changeTargetBwdVelocity(targetBwdVelocity);
        }

        public void pause(bool isPaused)
        {
            dcm.pause(isPaused);
        }
    }
}
