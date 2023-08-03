﻿using Phidget22;
using ProSimSDK;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace Phidgets2Prosim
{
    internal class PhidgetsDCMotor
    {
        string prosimDatmRefCCW;
        string prosimDatmRefCW;

        DCMotor dcMotor = new DCMotor();
        public PhidgetsDCMotor(int hubPort, string prosimDatmRefCW, string prosimDatmRefCCW, ProSimConnect connection)
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
            DataRef dataRef = new DataRef(prosimDatmRefCW, 10, connection);
            DataRef dataRef2 = new DataRef(prosimDatmRefCCW, 10, connection);

            dataRef.onDataChange += DataRef_onDataChange;
            dataRef2.onDataChange += DataRef_onDataChange;


            //system.gates.B_TRIM_MOTOR_DOWN
            // system.gates.B_TRIM_MOTOR_UP
            //system.numerical.N_TRIM_MOTOR_VALUE

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

                if (value == true)
                {
                    if (dataRef.name == prosimDatmRefCW)
                    {
                        dcMotor.TargetVelocity = 1;
                    }

                    if (dataRef.name == prosimDatmRefCCW)
                    {
                        dcMotor.TargetVelocity = -1;
                    }
                } else
                {
                    dcMotor.TargetVelocity = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }
        }

    }
}
