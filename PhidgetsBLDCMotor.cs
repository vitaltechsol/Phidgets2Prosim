using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Phidgets2Prosim
{
    internal class PhidgetsBLDCMotor
    {
        double targetVelFast = 0.4;
        double targetVelFastest = 1;
        double targetVelSlow = 0.1;
        bool reversed;
        int hubPort;
        int offset = 0;

        bool motorOn = false;
        double currentPosition = 0;
        int threshold = 5;


        BLDCMotor dcMotor = new BLDCMotor();
        public PhidgetsBLDCMotor(int hubPort, ProSimConnect connection, bool reversed, int offset, string refTurnOn, string refCurrentPos, string refTargetPos)
        {
            try
            {
                this.hubPort = hubPort;

                dcMotor.HubPort = hubPort;
                dcMotor.IsRemote = true;
                dcMotor.Open(5000);
                dcMotor.DeviceSerialNumber = 668066;
                dcMotor.Acceleration = 0.8;
                dcMotor.TargetBrakingStrength = 1;

                this.reversed = reversed;
                this.offset = offset;

                // Set ProSim datarefs
                DataRef dataRef_currentPos = new DataRef(refCurrentPos, 100, connection);
                DataRef dataRef_turnOn = new DataRef(refTurnOn, 100, connection);
                DataRef dataRef_target = new DataRef(refTargetPos, 100, connection);

                dataRef_currentPos.onDataChange += DataRef_onCurrentPostChanged;
                dataRef_turnOn.onDataChange += DataRef_onTurnOnChanged;
                dataRef_target.onDataChange += DataRef_onTargetChanged;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async void DataRef_onCurrentPostChanged(DataRef dataRef)
        {
            if (motorOn)
            {
                var value = Convert.ToDouble(dataRef.value);
                currentPosition = value;
            }
        }

        private async void DataRef_onTurnOnChanged(DataRef dataRef)
        {
            var newValue = Convert.ToBoolean(dataRef.value);
            if (motorOn == true && newValue == false)
            {
                // stop motor
                dcMotor.TargetVelocity = 0;
            }
            motorOn = newValue;
        }

        private async void DataRef_onTargetChanged(DataRef dataRef)
        {
            if (motorOn)
            {
                var targetPosition = Convert.ToDouble(dataRef.value) - offset;
                Debug.WriteLine("Targetfor " + hubPort + " is " + targetPosition);

                // Calculate the direction based on the difference between current and target positions

                // Check if the motor is within the acceptable range
                double diff = Math.Abs(currentPosition - targetPosition);
                Debug.WriteLine("diff " + diff);

                if (Math.Abs(diff) <= threshold)

                {
                    Debug.WriteLine("Motor " + hubPort + " reached the target position.");
                    dcMotor.TargetVelocity = 0;
                } else
                {
                    int direction = Math.Sign(targetPosition - currentPosition) * (reversed ? -1 : 1);
                    Debug.WriteLine("direction " + direction);

                    double targetVel = targetVelFast;


                    targetVel = diff > 100 ? 100 : diff;
                    targetVel = targetVel / 100;

                    Debug.WriteLine("targetVel " + targetVel);


                    //if (diff < 20)
                    //{
                    //    Debug.WriteLine("Go Slow." + hubPort);
                    //    targetVel = targetVelSlow;
                    //}
                    //if (diff > 40)
                    //{
                    //    Debug.WriteLine("Go Fast." + hubPort);
                    //    targetVel = targetVelFastest;
                    //}

                    dcMotor.TargetVelocity = targetVel * direction;
                }
            }
        }

        public void changeTargetVelocity(double vel)
        {
            targetVelFast = vel;
        }

    }
}
