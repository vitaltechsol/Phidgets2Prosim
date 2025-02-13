using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<PhidgetsBLDCMotorInst> PhidgetsBLDCMotorInstances { get; set; }
        public List<PhidgetsVoltageOutputInst> PhidgetsVoltageOutputInstances { get; set; }
        public List<PhidgetsButtonInst> PhidgetsButtonInstances { get; set; }
    }

    public class PhidgetsOutputInst : PhidgetDevice
    {
        // (Optional) Wait specified amount of milliseconds before turning on 
        public int? DelayOn { get; set; }

        // (Optional) turn off when prosim shows on 
        public bool? Inverse { get; set; } = false;

        // (Optional) Wait specified amount of milliseconds and then turn off 
        public int? DelayOff { get; set; } = null;

        // (Optional) Use a different prosim data ref to turn off 
        public string ProsimDataRefOff { get; set; } = null;

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
        public int? DelayOff { get; set; } = null;
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

    }

    public class PhidgetsVoltageOutputInst : PhidgetDevice
    {
        // Scale the prosim input value into the output value
        public double ScaleFactor { get; set; }

        // Offset the value
        public double Offset { get; set; } = 0;

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
    }


}
