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
        string prosimDatmRefCCW;
        string prosimDatmRefCW;
        double targetVel = 1;

        DCMotor dcMotor = new DCMotor();
        public PhidgetsDCMotor(int hubPort, string prosimDatmRefCW, string prosimDatmRefCCW, ProSimConnect connection)
        {
            try
            {
                this.prosimDatmRefCCW = prosimDatmRefCCW;
                this.prosimDatmRefCW = prosimDatmRefCW;

                dcMotor.HubPort = hubPort;
                dcMotor.IsRemote = true;
                dcMotor.Open(5000);
                dcMotor.DeviceSerialNumber = 668534;
                dcMotor.Acceleration = 100;
                dcMotor.TargetBrakingStrength = 1;

                // Set ProSim dataref
                DataRef dataRef = new DataRef(prosimDatmRefCW, 100, connection);
                DataRef dataRef2 = new DataRef(prosimDatmRefCCW, 100, connection);
                DataRef dataRefSpeed = new DataRef("system.gauge.G_MIP_FLAP", 100, connection);

                dataRef.onDataChange += DataRef_onDataChange;
                dataRef2.onDataChange += DataRef_onDataChange;
                dataRefSpeed.onDataChange += DataRef_onFlapsDataChange;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("prosimDatmRefCW " + prosimDatmRefCW);
            }
        }

        private async void DataRef_onDataChange(DataRef dataRef)
        {

            Debug.WriteLine("trim name " + dataRef.name);
            Debug.WriteLine("tim value " + dataRef.name);


            // var name = dataRef.name;
            try
            {
                var value = Convert.ToBoolean(dataRef.value);

                Debug.WriteLine(dataRef.name);
                Debug.WriteLine(value);


                if (dataRef.name == prosimDatmRefCW)
                {
                    if (value == true)
                    {
                        dcMotor.TargetVelocity = targetVel * -1;
                    }
                    else
                    {
                        dcMotor.TargetVelocity = 0.5;
                        Thread.Sleep(100);
                        dcMotor.TargetVelocity = 0;
                    }
                }

                if (dataRef.name == prosimDatmRefCCW)
                {
                    if (value == true)
                    {
                        dcMotor.TargetVelocity = targetVel;
                    }
                    else
                    {
                        dcMotor.TargetVelocity = -0.5;
                        Thread.Sleep(100);
                        dcMotor.TargetVelocity = 0;
                    }
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }

        }

        private async void DataRef_onFlapsDataChange(DataRef dataRef)
        {
            var value = Convert.ToDouble(dataRef.value);

            Debug.WriteLine("flaps changed  " + dataRef.value);


            if (value > 1)
            {
                targetVel = 1;
            } else
            {
                targetVel = 0.6;
            }
        }

        public void changeTargetVelocity(double vel)
        {
            targetVel = vel;
        }

    }
}
