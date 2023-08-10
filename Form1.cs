using System.Security.Claims;

namespace Phidgets2Prosim
{
    using System;
    using Phidget22;
    using System.Diagnostics;
    using ProSimSDK;
    using System.Windows.Forms;
    using System.Drawing;
    using System.Diagnostics.Eventing.Reader;

    public partial class Form1 : Form
    {

        ProSimConnect connection = new ProSimConnect();
        VoltageOutput voltageOutput0 = new VoltageOutput();
        DCMotor dcMotor0 = new DCMotor();
        bool phidgetsAdded = false;


        public Form1()
        {
            // Add Phidgets Hub
            Net.AddServer("serverName2", "hub5000-2", 5661, "", 0);
            Net.AddServer("serverName1", "hub5000-1", 5661, "", 0);

            InitializeComponent();

            connectToProSim();

            // Register Prosim to receive connect and disconnect events
            connection.onConnect += connection_onConnect;
            connection.onDisconnect += connection_onDisconnect;



        }

        void connectToProSim()
        {
            try
            {
                connectionStatusLabel.Text = "CONNECTING....";
                Debug.WriteLine("Prosim connecting");
                connection.Connect("192.168.1.142", false);
                updateStatusLabel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cannot connect to Prosim. " + ex);
            }
        }

        void connection_onDisconnect()
        {
            Debug.WriteLine("Prosim DISCONNECTED");
            Invoke(new MethodInvoker(updateStatusLabel));
        }

        // When we connect to ProSim737 system, update the status label and start filling the table
        void connection_onConnect()
        {
            Debug.WriteLine("Prosim CONNECTED");
            Invoke(new MethodInvoker(updateStatusLabel));
            Invoke(new MethodInvoker(AddAllPhidgets));
        }


        private void AddAllPhidgets()
        {

            if (!phidgetsAdded) {
                PhidgestOuput digitalOutput0 = new PhidgestOuput(3, 0, "system.gates.B_STANDBY_POWER", connection, true);
                PhidgestOuput digitalOutput1 = new PhidgestOuput(3, 1, "system.gates.B_REVERSER_2_SYNC_LOCK", connection, true);
                digitalOutput1.AddDelay(1500);
                PhidgestOuput digitalOutput2 = new PhidgestOuput(3, 2, "system.gates.B_REVERSER_1_SYNC_LOCK", connection, true);
                digitalOutput2.AddDelay(1500);
                PhidgestOuput digitalOutput3 = new PhidgestOuput(3, 3, "system.gates.B_STANDBY_POWER", connection, true);
                PhidgestOuput digitalOutput4 = new PhidgestOuput(3, 4, "system.indicators.I_FIRE_1", connection);
                PhidgestOuput digitalOutput5 = new PhidgestOuput(3, 5, "system.indicators.I_FIRE_2", connection);
                PhidgestOuput digitalOutput6 = new PhidgestOuput(3, 6, "system.indicators.I_FIRE_APU", connection);
                PhidgestOuput digitalOutput8 = new PhidgestOuput(3, 8, "system.gates.B_SPEED_BRAKE_DEPLOY", connection, true, "system.gates.B_SPEED_BRAKE_RESTOW");


                PhidgestInput digitalInput1 = new PhidgestInput(5, 1, "system.switches.S_FIRE_FAULT_TEST", 2, connection);
                PhidgestInput digitalInput2 = new PhidgestInput(5, 0, "system.switches.S_FIRE_FAULT_TEST", 1, connection);

                PhidgetsVoltageOutput pvo = new PhidgetsVoltageOutput(2, "system.gauge.G_MIP_BRAKE_PRESSURE", connection);

                PhidgetsDCMotor dcm = new PhidgetsDCMotor(0, "system.gates.B_TRIM_MOTOR_UP", "system.gates.B_TRIM_MOTOR_DOWN", connection);
                phidgetsAdded = true;
            } 

            
        }

        void updateStatusLabel()
        {
            if (connection.isConnected)
            {
                connectionStatusLabel.Text = "Connected";
                connectionStatusLabel.ForeColor = Color.LimeGreen;
            }
            else
            {
                connectionStatusLabel.Text = "Disconnected";
                connectionStatusLabel.ForeColor = Color.Red;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}