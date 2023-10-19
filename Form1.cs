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
        bool phidgetsAdded = false;
        PhidgestInput digitalInput_2_00;
        PhidgestInput digitalInput_2_01;
        PhidgestInput digitalInput_2_02;
        PhidgestInput digitalInput_2_03;
        PhidgestInput digitalInput_2_04;
        PhidgestInput digitalInput_2_05;
        PhidgestInput digitalInput_2_06;
        PhidgestInput digitalInput_2_07;
        PhidgestInput digitalInput_2_08;

        PhidgestInput digitalInput1_0;
        PhidgestInput digitalInput1_1;
        PhidgestInput digitalInput1_2;
        PhidgestInput digitalInput1_3;
        PhidgestInput digitalInput1_4;

        PhidgestInput digitalInput1_08;
        PhidgestInput digitalInput1_09;
        PhidgestInput digitalInput1_10;
        PhidgestInput digitalInput1_11;

        Custom_TrimWheel trimWheel;
        PhidgetsBLDCMotor bldcm_00;
        PhidgetsBLDCMotor bldcm_01;

        bool simIsPaused = false;

        public Form1()
        {
            // Add Phidgets Hub
            Net.AddServer("serverName1", "hub5000-1", 5661, "", 0);
            Net.AddServer("serverName2", "hub5000-2", 5661, "", 0);
            Net.AddServer("serverName3", "hub5000-motors", 5661, "", 0);


            InitializeComponent();
            connectToProSim();

            // Register Prosim to receive connect and disconnect events
            connection.onConnect += connection_onConnect;
            connection.onDisconnect += connection_onDisconnect;

            DataRef dataRef = new DataRef("simulator.pause", 100, connection);
            dataRef.onDataChange += DataRef_onDataChange;
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
            try { 
                if (!phidgetsAdded) {
                    PhidgestOutput digitalOutput3_0 = new PhidgestOutput(3, 0, "system.gates.B_STANDBY_POWER", connection, true);
                    PhidgestOutput digitalOutput_3_1 = new PhidgestOutput(3, 1, "system.gates.B_REVERSER_2_SYNC_LOCK", connection, true);
                    digitalOutput_3_1.AddDelay(1500);
                    PhidgestOutput digitalOutput_3_2 = new PhidgestOutput(3, 2, "system.gates.B_REVERSER_1_SYNC_LOCK", connection, true);
                    digitalOutput_3_2.AddDelay(1500);
                    PhidgestOutput digitalOutput_3_3 = new PhidgestOutput(3, 3, "system.gates.B_AC_POWER", connection, true);
                    PhidgestOutput digitalOutput_3_8 = new PhidgestOutput(3, 8, "system.gates.B_SPEED_BRAKE_DEPLOY", connection, true, "system.gates.B_SPEED_BRAKE_RESTOW");
                    PhidgestOutput digitalOutput_3_7 = new PhidgestOutput(3, 7, "system.indicators.I_MIP_PARKING_BRAKE", connection);


                    PhidgestOutput digitalOutput_4_0 = new PhidgestOutput(4, 0, "system.indicators.I_FIRE_1", connection);
                    PhidgestOutput digitalOutput_4_1 = new PhidgestOutput(4, 1, "system.indicators.I_FIRE_APU", connection);
                    PhidgestOutput digitalOutput_4_2 = new PhidgestOutput(4, 2, "system.indicators.I_FIRE_2", connection);
                    PhidgestOutput digitalOutput_4_3 = new PhidgestOutput(4, 3, "system.indicators.I_FIRE_L_BOTTLE_DISCHARGE", connection);
                    PhidgestOutput digitalOutput_4_4 = new PhidgestOutput(4, 4, "system.indicators.I_FIRE_R_BOTTLE_DISCHARGE", connection);
                    PhidgestOutput digitalOutput_4_5 = new PhidgestOutput(4, 5, "system.indicators.I_FIRE_TEST_APU", connection);
                    PhidgestOutput digitalOutput_4_6 = new PhidgestOutput(4, 6, "system.indicators.I_FIRE_APU_DET_INOPT", connection);

                    PhidgestOutput digitalOutput_4_8 = new PhidgestOutput(4, 8, "system.indicators.B_FIRE_HANDLE_LEFT_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_10 = new PhidgestOutput(4, 10, "system.indicators.B_FIRE_HANDLE_RIGHT_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_11 = new PhidgestOutput(4, 11, "system.indicators.I_FIRE_ENG_1_OVT", connection);
                    PhidgestOutput digitalOutput_4_12 = new PhidgestOutput(4, 12, "system.indicators.I_FIRE_ENG_2_OVT", connection);
                    PhidgestOutput digitalOutput_4_13 = new PhidgestOutput(4, 13, "system.indicators.I_FIRE_WHEEL_WELL_OVT", connection);
                    PhidgestOutput digitalOutput_4_14 = new PhidgestOutput(4, 14, "system.indicators.B_FIRE_HANDLE_APU_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_15 = new PhidgestOutput(4, 15, "system.indicators.I_FIRE_FAULT", connection);


                    digitalInput_2_00 = new PhidgestInput(2, 1, "system.switches.S_FIRE_FAULT_TEST", 2, connection);
                    digitalInput_2_01 = new PhidgestInput(2, 0, "system.switches.S_FIRE_FAULT_TEST", 1, connection);
                    digitalInput_2_02 = new PhidgestInput(2, 2, "system.switches.S_FIRE_HANDLE1", 1, connection);
                    digitalInput_2_03 = new PhidgestInput(2, 3, "system.switches.S_FIRE_PULL1", 1, connection);
                    digitalInput_2_04 = new PhidgestInput(2, 4, "system.switches.S_FIRE_HANDLE1", 2, connection);

                    digitalInput1_0 = new PhidgestInput(1, 0, "system.switches.S_THROTTLE_FUEL_CUTOFF1", 1, connection);
                    digitalInput1_1 = new PhidgestInput(1, 1, "system.switches.S_THROTTLE_FUEL_CUTOFF2", 1, connection);
                    digitalInput1_2 = new PhidgestInput(1, 2, "system.switches.S_THROTTLE_AT_DISENGAGE", 1, connection);
                    digitalInput1_3 = new PhidgestInput(1, 3, "system.switches.S_THROTTLE_AT_DISENGAGE_2", 1, connection);
                    digitalInput1_4 = new PhidgestInput(1, 4, "system.switches.S_THROTTLE_TOGA", 1, connection);

                    digitalInput1_10 = new PhidgestInput(1, 10, "system.switches.S_RUDDER_TRIM", 1, connection);
                    digitalInput1_11 = new PhidgestInput(1, 11, "system.switches.S_RUDDER_TRIM", 2, connection);
                    digitalInput1_08 = new PhidgestInput(1, 08, "system.switches.S_AILERON_TRIM", 1, connection);
                    digitalInput1_09 = new PhidgestInput(1, 09, "system.switches.S_AILERON_TRIM", 2, connection);


                    PhidgetsVoltageOutput pvo = new PhidgetsVoltageOutput(2, "system.gauge.G_MIP_BRAKE_PRESSURE", connection);

                   // PhidgetsDCMotor dcm = new PhidgetsDCMotor(0, "system.gates.B_TRIM_MOTOR_UP", "system.gates.B_TRIM_MOTOR_DOWN", connection);

                    trimWheel = new Custom_TrimWheel(0, connection, 1, 0.8, 0.5, 0.5, 0.7, 0.3);

                    bldcm_00 = new PhidgetsBLDCMotor(1, connection, false, 0,
                        "system.gates.B_THROTTLE_SERVO_POWER_RIGHT",
                        "system.analog.A_THROTTLE_RIGHT",
                        "system.gauge.G_THROTTLE_RIGHT");

                    bldcm_01 = new PhidgetsBLDCMotor(0, connection, true, 5,
                     "system.gates.B_THROTTLE_SERVO_POWER_LEFT",
                     "system.analog.A_THROTTLE_LEFT",
                     "system.gauge.G_THROTTLE_LEFT");


                    phidgetsAdded = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cann't Initialize phidgets " + ex);
            }



        }

        void updateStatusLabel()
        {
            if (connection.isConnected)
            {
                connectionStatusLabel.Text = "Connected";
                connectionStatusLabel.ForeColor = Color.LimeGreen;
                
                if (simIsPaused)
                {
                    connectionStatusLabel.Text = "Paused";
                    connectionStatusLabel.ForeColor = Color.OrangeRed;
                }
            }
            else
            {
                connectionStatusLabel.Text = "Disconnected";
                connectionStatusLabel.ForeColor = Color.Red;
            }

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("**** restart phidgets: ");


            digitalInput1_0.Close();
            digitalInput1_1.Close();
            digitalInput1_2.Close();
            digitalInput1_3.Close();
            digitalInput1_4.Close();

            digitalInput1_0.Open();
            digitalInput1_1.Open(); 
            digitalInput1_2.Open();
            digitalInput1_3.Open(); 
            digitalInput1_4.Open();
        }



        private void DataRef_onDataChange(DataRef dataRef)
        {
            var name = dataRef.name;
            if (name == "simulator.pause") { 
                try
                {
                    simIsPaused = Convert.ToBoolean(dataRef.value);
                    Debug.WriteLine("Sim paused Changed " + dataRef.value + " " + dataRef.name);

                    // Pause motors
                    trimWheel.pause(simIsPaused);
                    bldcm_00.pause(simIsPaused);
                    bldcm_01.pause(simIsPaused);

                    Invoke(new MethodInvoker(updateStatusLabel));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    Debug.WriteLine("simulator.pause " + dataRef.value);
                }
            }
        }


        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}