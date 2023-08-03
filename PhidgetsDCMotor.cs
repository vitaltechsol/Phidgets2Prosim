using Phidget22;
using ProSimSDK;
using System.Diagnostics;


namespace Phidgets2Prosim
{
    internal class PhidgetsDCMotor
    {

        DCMotor dcMotor = new DCMotor();
        public PhidgetsDCMotor(int hubPort, string prosimDatmRef, ProSimConnect connection)
        {
            dcMotor.HubPort = hubPort;
            dcMotor.IsRemote = true;
            dcMotor.Open(5000);

            // Set ProSim dataref
            DataRef dataRef = new DataRef(prosimDatmRef, 10, connection);
            dataRef.onDataChange += DataRef_onDataChange;
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {

            // var name = dataRef.name;
            try
            {
                var value = Convert.ToDouble(dataRef.value);
                var convertedValue = value > 0 ? (value / 500) : 0;

                Debug.WriteLine(dataRef.name);
                Debug.WriteLine(value);
                Debug.WriteLine(convertedValue);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }
        }

    }
}
