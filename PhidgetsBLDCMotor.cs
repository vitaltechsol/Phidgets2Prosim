using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Phidgets2Prosim
{
    internal class PhidgetsBLDCMotor : PhidgetDevice
    {
        double targetVelFast = 0.4;
        bool reversed;
        int offset = 0;
        bool motorOn = false;
        double currentPosition = 0;
        int threshold = 5;
        double currentVel = 0;
        bool isPaused = false;

        BLDCMotor dcMotor = new BLDCMotor();
        public PhidgetsBLDCMotor(int deviceSerialNumber, int hubPort, ProSimConnect connection, bool reversed, int offset, string refTurnOn, string refCurrentPos, string refTargetPos, double acceleration)
        {
            try
            {
                HubPort = hubPort;

                dcMotor.HubPort = hubPort;
                dcMotor.IsRemote = true;
                dcMotor.Open(5000);
                dcMotor.DeviceSerialNumber = deviceSerialNumber;
                dcMotor.Acceleration = acceleration;
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
                currentVel = 0;
                dcMotor.TargetVelocity = currentVel;
            }
            motorOn = newValue;
        }

        private async void DataRef_onTargetChanged(DataRef dataRef)
        {
            if (motorOn)
            {
                var targetPosition = Convert.ToDouble(dataRef.value) - offset;
                //  Debug.WriteLine("Targetfor " + hubPort + " is " + targetPosition);
                // Calculate the direction based on the difference between current and target positions

                // Check if the motor is within the acceptable range
                double diff = Math.Abs(currentPosition - targetPosition);
                Debug.WriteLine("diff " + diff);

                if (Math.Abs(diff) <= threshold)

                {
                    // Debug.WriteLine("Motor " + hubPort + " reached the target position.");
                    currentVel = 0;
                    dcMotor.TargetVelocity = currentVel;
                } else
                {
                    int direction = Math.Sign(targetPosition - currentPosition) * (reversed ? -1 : 1);
                   // Debug.WriteLine("direction " + direction);

                    double targetVel = targetVelFast;
                    targetVel = diff > 100 ? 100 : diff;
                    targetVel = targetVel / 100;

                    currentVel = targetVel * direction;
                    if (!isPaused)
                    {
                        dcMotor.TargetVelocity = currentVel;
                    }
                }
            }
        }

        public void changeTargetVelocity(double vel)
        {
            targetVelFast = vel;
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
                Debug.WriteLine("Open failed for BL DC Motor " + HubPort);
                Debug.WriteLine(ex.ToString());
            }
        }

    }
}
