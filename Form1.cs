using System.Security.Claims;

namespace Phidgets2Prosim
{
    using System;
    using Phidget22;
    using System.Diagnostics;
    using ProSimSDK;
    using System.Windows.Forms;

    public partial class Form1 : Form
    {

        ProSimConnect connection = new ProSimConnect();
        VoltageOutput voltageOutput0 = new VoltageOutput();


        public Form1()
        {
            // Add Phidgets Hub
            Net.AddServer("serverName", "hub5000-2", 5661, "", 0);

            InitializeComponent();

            // Register Prosim to receive connect and disconnect events
            connection.onConnect += connection_onConnect;
            connection.onDisconnect += connection_onDisconnect;

            voltageOutput0.HubPort = 0;
            voltageOutput0.IsRemote = true;
            voltageOutput0.DeviceSerialNumber = 668522;

            try
            {
                voltageOutput0.Open(15000);
                voltageOutput0.Voltage = 0;
                voltageOutput0.Enabled = true;
                Debug.WriteLine("Voltage enabled");
            }
            catch (PhidgetException ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("PhidgetException " + ex.ErrorCode + " (" + ex.Description + "): " + ex.Detail);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            connectToProSim();
        }

        private void setDataRefs()
        {
            // Get all of the DataRefs from ProSim737 System
            DataRef dataRef = new DataRef("system.gauge.G_MIP_BRAKE_PRESSURE", 100, connection);
            dataRef.onDataChange += DataRef_onDataChange;
        }

        private void DataRef_onDataChange(DataRef dataRef)
        {

            if (IsDisposed)
                return;

            // var name = dataRef.name;
            try
            {
                var value = Convert.ToDouble(dataRef.value);
                var convertedValue = value > 0 ? (value / 500) : 0;

                voltageOutput0.Voltage = convertedValue;
                Debug.WriteLine("Brake Press");
                Debug.WriteLine(value);
                Debug.WriteLine(convertedValue);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("value " + dataRef.value);
            }
        }

        void connectToProSim()
        {
            try
            {
                Debug.WriteLine("Prosim connecting");
                connection.Connect("192.168.1.142");

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cannot connect to Prosim. " + ex);
            }
        }

        void connection_onDisconnect()
        {
            Debug.WriteLine("Prosim DISCONNECTED");
        }

        // When we connect to ProSim737 system, update the status label and start filling the table
        void connection_onConnect()
        {
            Debug.WriteLine("Prosim CONNECTED");
            Invoke(new MethodInvoker(setDataRefs));

        }

    }
}