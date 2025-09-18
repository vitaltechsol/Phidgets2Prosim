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
        public GeneralConfig GeneralConfig { get; set; }
        public List<string> PhidgetsHubsInstances { get; set; }
        public List<PhidgetsOutputInst> PhidgetsOutputInstances { get; set; }
        public List<PhidgetsAudioInst> PhidgetsAudioInstances { get; set; }
        public List<PhidgetsGateInst> PhidgetsGateInstances { get; set; }
        public List<PhidgetsInputInst> PhidgetsInputInstances { get; set; }
        public List<PhidgetsMultiInputInst> PhidgetsMultiInputInstances { get; set; }
        public List<PhidgetsVoltageInputInst> PhidgetsVoltageInputInstances { get; set; }
        public List<PhidgetsBLDCMotorInst> PhidgetsBLDCMotorInstances { get; set; }
        public List<PhidgetsVoltageOutputInst> PhidgetsVoltageOutputInstances { get; set; }
        public CustomTrimWheelInst CustomTrimWheelInstance { get; set; }
        public List<PhidgetsButtonInst> PhidgetsButtonInstances { get; set; }

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

    }

    public class PhidgetsAudioInst : PhidgetsOutputInst
    {
    }

    public class PhidgetsGateInst : PhidgetDevice
    {
        // Wait specified amount of milliseconds before turning on
        public int? DelayOn { get; set; } = null;

        public bool Inverse { get; set; } = false;

        // (Optional) Use a different prosim data ref to turn off 
        public string ProsimDataRefOff { get; set; } = null;

        // (Optional) Wait specified amount of milliseconds and then turn off 
        public int? MaxTimeOn { get; set; } = null;
    }

    public class PhidgetsInputInst : PhidgetDevice
    {
        // The desired value to send to prosim when input is on
        public int InputValue { get; set; }

        // (Optional)  The desired value to send to prosim when input is off, by default is 0
        public int OffInputValue { get; set; } = 0;

        // (Optional) Additional prosim ref to change with same input
        public string ProsimDataRef2 { get; set; } = null;
        
        // (Optional) Other additional prosim ref to change with same input
        public string ProsimDataRef3 { get; set; } = null;
    }

    public class PhidgetsVoltageInputInst : PhidgetDevice
    {
        public string ProsimDataRefOnOff { get; set; } = "";
        public List<double> InputPoints { get; set; } = new List<double> { 0.0, 1.0 };
        public List<int> OutputPoints { get; set; } = new List<int> { 0, 255 };

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

		//Maximum allowed motor velocity (0..1). Used when the error is large
		public double? MaxVelocity { get; set; }

		//Minimum velocity to overcome static friction when error is small
		public double? MinVelocity { get; set; }

		//Error distance (in position units) at which the motor reaches MaxVelocity
		public double? VelocityBand { get; set; }

		//Curve shaping factor for error-to-velocity mapping (0.5–1.0 = softer near zero)
		public double? CurveGamma { get; set; }

		//Distance threshold to enter the settled (stopped) zone.
		public double? DeadbandEnter { get; set; }

		//Distance threshold to exit the settled (stopped) zone (should be > DeadbandEnter)
		public double? DeadbandExit { get; set; }

		//Maximum allowed change in commanded velocity per control loop tick (slew limiter)
		public double?  MaxVelStepPerTick { get; set; }

		//Proportional gain (optional) to reduce steady-state error
		public double? Kp { get; set; }

		//Integral gain (optional) to remove small bias error (start at 0.0)
		public double? Ki { get; set; }

		//Derivative gain (damping) on error rate to suppress oscillations
		public double? Kd { get; set; }

		// Only integrate when |error| ≤ this band (prevents wind-up and hunting).
		// Tune ~6–12 in your position units.
		public double? IOnBand { get; set; }

		//Maximum absolute integral term value to prevent wind-up
		public double? IntegralLimit { get; set; }

		//Smoothing factor for low-pass filtering of position feedback (0..1, higher = less filtering)
		public double? PositionFilterAlpha { get; set; }

		//Interval (in milliseconds) for the control loop tick. Lower = faster updates
		public int? TickMs { get; set; }

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
    }

}
