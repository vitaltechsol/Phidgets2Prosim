using Phidget22;
using ProSimSDK;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Phidgets2Prosim
{
    internal class PhidgetsButton : PhidgetDevice
    {
        public string Name { get; set; }
        public int InputValue { get; set; }
        public int OffInputValue { get; set; } = 0;

        public PhidgetsButton(int index, string name, ProSimConnect connection, string prosimDataRef, int inputValue, int offInputValue = 0)
        {
            ProsimDataRef = prosimDataRef;
            Connection = connection;
            OffInputValue = offInputValue;
            InputValue = inputValue;
            Name = name;

            SendInfoLog("->****Attached " + prosimDataRef + " to button: " + index);
        }

        public void StateChangeOn(object sender, EventArgs e)
        {
            DataRef dataRef = new DataRef(ProsimDataRef, 100, Connection);
            try
            {
                SendInfoLog($"--> Button Ref On: {ProsimDataRef} - inputValue: {InputValue}");
                dataRef.value = InputValue;
            }
            catch (System.Exception ex)
            {
                SendErrorLog("Error: Button " + ProsimDataRef + " - Value:" + InputValue);
                SendErrorLog(ex.ToString());
                Debug.WriteLine("Error: Button " + ProsimDataRef + " - Value:" + InputValue);
                Debug.WriteLine(ex.ToString());
            }
        }
        public void StateChangeOff(object sender, EventArgs e)
        {
            DataRef dataRef = new DataRef(ProsimDataRef, 100, Connection);
            try
            {
                SendInfoLog($"--> Button Ref Off: {ProsimDataRef} - offInputValue {OffInputValue}");
                dataRef.value = OffInputValue;
            }
            catch (System.Exception ex)
            {
                SendErrorLog("Error: Button " + ProsimDataRef + " - Value:" + OffInputValue);
                SendErrorLog(ex.ToString());
                Debug.WriteLine("Error: Button " + ProsimDataRef + " - Value:" + OffInputValue);
                Debug.WriteLine(ex.ToString());
            }
        }

    }
}
