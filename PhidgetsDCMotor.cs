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


        DCMotor dcMotor = new DCMotor();
        public PhidgetsDCMotor(int hubPort, string prosimDatmRefFwd, string prosimDatmRefBwd, ProSimConnect connection)
        {
            try
            {
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


                if (dataRef.name == prosimDatmRefFwd && !isPaused)
                {
                    if (value == true)
                    {
                        currentVel = targetVelFwd * -1;
                        dcMotor.TargetVelocity = currentVel;
                    }
                    else
                    {
                        currentVel = 0;
                        dcMotor.TargetVelocity = 0.2;
                        Thread.Sleep(100);
                        dcMotor.TargetVelocity = currentVel;
                    }
                }

                if (dataRef.name == prosimDatmRefBwd && !isPaused)
                {
                    if (value == true)
                    {
                        currentVel = targetVelBwd;
                        dcMotor.TargetVelocity = currentVel;
                    }
                    else
                    {
                        currentVel = 0;
                        dcMotor.TargetVelocity = -0.2;
                        Thread.Sleep(100);
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

    }
}
