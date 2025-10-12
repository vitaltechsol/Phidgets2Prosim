using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace Phidgets2Prosim
{

    public class Config
    {
        public GeneralConfig GeneralConfig { get; set; } = new GeneralConfig(); //added this
        public List<string> PhidgetsHubsInstances { get; set; }
        public List<PhidgetsOutputInst> PhidgetsOutputInstances { get; set; }
        public List<PhidgetsAudioInst> PhidgetsAudioInstances { get; set; }
        public List<PhidgetsGateInst> PhidgetsGateInstances { get; set; }
        public List<PhidgetsInputInst> PhidgetsInputInstances { get; set; }
        public List<PhidgetsMultiInputInst> PhidgetsMultiInputInstances { get; set; }
        public List<PhidgetsVoltageInputInst> PhidgetsVoltageInputInstances { get; set; }
        public List<PhidgetsBLDCMotorInst> PhidgetsBLDCMotorInstances { get; set; }
        public List<PhidgetsDCMotorInst> PhidgetsDCMotorInstances { get; set; }
        public List<PhidgetsVoltageOutputInst> PhidgetsVoltageOutputInstances { get; set; }
        public CustomTrimWheelInst CustomTrimWheelInstance { get; set; }
        public List<UserVariableInst> UserVariableInstances { get; set; }
        public CustomParkingBrakeInst CustomParkingBrakeInstance { get; set; }
        public List<PhidgetsButtonInst> PhidgetsButtonInstances { get; set; }

    }

    public class UserVariableInst
    {
        public string Name { get; set; } // e.g., "ParkingBrakeSwitch", "ParkingBrakeRelay"
    }

    public class PhidgetsOutputInst : PhidgetDevice
    {
        // (Optional) Wait specified amount of milliseconds before turning on 
        public int? DelayOn { get; set; }

        // (Optional) turn off when prosim shows on 
        public bool? Inverse { get; set; } = false;

        // (Optional) Wait maximum amount of milliseconds and then turn off if gate is still on
        public int? MaxTimeOn { get; set; } = null;

        // (Optional) Use a different prosim data ref to turn off 
        public string ProsimDataRefOff { get; set; } = null;

        // (Optional) Value when on (1 is 100%), default is 1 
        public double ValueOn { get; set; } = 1;
        // (Optional) Value when off (0 is 0%), default is 0
        public double ValueOff { get; set; } = 0;

        // (Optional) Value when dim (0.7 is 70%), default is 0.7
        public double ValueDim { get; set; } = 0.7;

        public string UserVariable { get; set; } = null;
    }

    public class PhidgetsAudioInst : PhidgetsOutputInst
    {
    }

    public class PhidgetsGateInst : PhidgetDevice
    {
        // (Optional)Wait specified amount of milliseconds before turning on
        public int? DelayOn { get; set; } = null;

        public bool Inverse { get; set; } = false;

        // (Optional)Use a different prosim data ref to turn off 
        public string ProsimDataRefOff { get; set; } = null;

        // (Optional)Wait specified amount of milliseconds and then turn off 
        public int? MaxTimeOn { get; set; } = null;
    }

    public class PhidgetsInputInst : PhidgetDevice
    {
        // The desired value to send to prosim when input is on
        public int InputValue { get; set; }

        // (Optional)The desired value to send to prosim when input is off, by default is 0
        public int OffInputValue { get; set; } = 0;
        public string UserVariable { get; set; } = null;

        // (Optional)Additional prosim ref to change with same input
        public string ProsimDataRef2 { get; set; } = null;

        // (Optional)Other additional prosim ref to change with same input
        public string ProsimDataRef3 { get; set; } = null;

    }

    public class PhidgetsVoltageInputInst : PhidgetDevice       //########################################################
    {
        public string ProsimDataRefOnOff { get; set; } = "";
        public List<double> InputPoints { get; set; } = new List<double> { 0.0, 1.0 };
        public List<double> OutputPoints { get; set; } = new List<double> { 0, 255 };
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Linear;
        public double MinChangeTriggerValue { get; set; } = 0.002;
        public int DataInterval { get; set; } = 50;
        public double CurvePower { get; set; } = 2.0;

    }

    public class PhidgetsMultiInputInst : PhidgetDevice
    {
        // List of channels to use as a group
        public List<int> Channels { get; set; }

        // True table input mapping
        public Dictionary<string, int> Mappings { get; set; }
    }


    public class PhidgetsBLDCMotorInst : PhidgetDevice
    {
        // Add or remove offset if trying to match other motors
        public int Offset { get; set; }

        // Reverse the direction of the motor based on the refs
        public bool Reversed { get; set; }

        // Prosim ref to turn on the motor
        public string RefTurnOn { get; set; }

        // Prosim ref to use for current position
        public string RefCurrentPos { get; set; }

        // Prosim ref to use for target position
        public string RefTargetPos { get; set; }

        // Acceleration values between 0.1 and 1.0
        public double Acceleration { get; set; }

        public MotorTuningOptions Options  { get; set; } = new MotorTuningOptions();
    }


    public class PhidgetsDCMotorInst : PhidgetDevice   
    {
        public string prosimDataRefBwd { get; set; } = "";

        public string prosimDataRefFwd { get; set; } = "";

        // Acceleration values between 1 and 100
        public double Acceleration { get; set; }

        public PhidgetsVoltageInputInst VoltageInput { get; set; } = null;

        public MotorTuningOptions Options { get; set; } = new MotorTuningOptions();
    }


    public class PhidgetsVoltageOutputInst : PhidgetDevice
    {
        // Scale the prosim input value into the output value
        public double ScaleFactor { get; set; }

        // Offset the value
        public double Offset { get; set; } = 0;

    }

    public class CustomTrimWheelInst : PhidgetDevice
    {
        // Speed when dirty config. Nose up
        public double DirtyUp { get; set; }

        // Speed when dirty config. Nose down
        public double DirtyDown { get; set; }

        // Speed when clean config. Nose up
        public double CleanUp { get; set; }

        // Speed when clean config. Nose down
        public double CleanDown { get; set; }

        // Speed when Auto Pilot is on. Clean config
        public double APOnClean { get; set; }

        // Speed when Auto Pilot is on. Dirty config
        public double APOnDirty { get; set; }
    }

    public class PhidgetsButtonInst : PhidgetDevice
    {
        // Name of the button
        public string Name { get; set; }
        // value to send to prosim when button is on
        public int InputValue { get; set; }

        // value to send to prosim when button is off
        public int OffInputValue { get; set; } = 0;
    }

    public class GeneralConfig
    {
        public string ProSimIP { get; set; }
        public string Schema { get; set; }
        // Default value used for fast blinking outputs
        public int OutputBlinkFastIntervalMs { get; set; } = 300;
        // Default value used for fast blinking outputs
        public int OutputBlinkSlowIntervalMs { get; set; } = 600;
        // Default value used for dim output state when not specified
        public double OutputDefaultDimValue { get; set; } = 0.7;
        public class VariableInst { public string Name { get; set; } }
    }
    public class CustomParkingBrakeInst
    {
        public string SwitchVariable { get; set; }
        public string RelayVariable { get; set; }
        public int ToeBrakeThreshold { get; set; } = 1000;
    }
}