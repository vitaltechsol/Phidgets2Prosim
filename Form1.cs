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
        PhidgetsInput digitalInput_2_00;
        PhidgetsInput digitalInput_2_01;
        PhidgetsInput digitalInput_2_02;
        PhidgetsInput digitalInput_2_03;
        PhidgetsInput digitalInput_2_04;
        PhidgetsInput digitalInput_2_05;
        PhidgetsInput digitalInput_2_06;
        PhidgetsInput digitalInput_2_07;
        PhidgetsInput digitalInput_2_08;
        PhidgetsInput digitalInput_2_09;
        PhidgetsInput digitalInput_2_10;
        PhidgetsInput digitalInput_2_11;
        PhidgetsInput digitalInput_2_12;
        PhidgetsInput digitalInput_2_13;

        PhidgetsInput digitalInput1_0;
        PhidgetsInput digitalInput1_1;
        PhidgetsInput digitalInput1_2;
        PhidgetsInput digitalInput1_3;
        PhidgetsInput digitalInput1_4;

        PhidgetsInput digitalInput1_08;
        PhidgetsInput digitalInput1_09;
        PhidgetsInput digitalInput1_10;
        PhidgetsInput digitalInput1_11;

        PhidgetsInput digitalInput1_12;
        PhidgetsInput digitalInput1_13;

        PhidgestOutput digitalOutput_3_8;

        Custom_TrimWheel trimWheel;
        PhidgetsBLDCMotor bldcm_00;
        PhidgetsBLDCMotor bldcm_01;
        PhidgetsHub[] ph = new PhidgetsHub[3];

        
        bool simIsPaused = false;

        public Form1()
        {
            // Add Phidgets Hub
            try { 
            Net.AddServer("serverName1", "hub5000-1", 5661, "", 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cannot Start Hub. " + ex);
            }

            try
            {
                Net.AddServer("serverName2", "hub5000-MIP-1", 5661, "", 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cannot Start Hub.hub5000-MIP-1 " + ex);
            }

            try
            {
                Net.AddServer("serverName3", "hub5000-MIP-2", 5661, "", 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cannot Start Hub. hub5000-MIP-2 " + ex);
            }

            try
            {
                Net.AddServer("serverName4", "hub5000-motors", 5661, "", 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Cannot Start Hub motors " + ex);
            }

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

            var hubPedestal = 618534;
            var hubMip = 668522;
            var hubMip2 = 664861;

            try
            { 
                if (!phidgetsAdded) {

                    var ph = new PhidgetsHub[3];
                    var mip1 = ph[0];
                    var mip2 = ph[1];

                    mip1 = new PhidgetsHub();
                    mip2 = new PhidgetsHub();

                    mip1.port[1].output[3] = new PhidgestOutput(hubMip, 1, 3, "system.indicators.I_MIP_ASA_ATR_1", connection);
                    mip1.port[1].output[4] = new PhidgestOutput(hubMip, 1, 4, "system.indicators.I_MIP_ASA_FMC_1", connection);
                    mip1.port[1].output[5] = new PhidgestOutput(hubMip, 1, 5, "system.indicators.I_MIP_ASA_ATA_1", connection);
                    mip1.port[1].output[6] = new PhidgestOutput(hubMip, 1, 6, "system.indicators.I_MIP_ASA_APA_1", connection);
                    mip1.port[1].output[7] = new PhidgestOutput(hubMip, 1, 7, "system.indicators.I_MIP_ASA_APR_1", connection);

                    mip1.port[1].output[8] = new PhidgestOutput(hubMip, 1, 8, "system.indicators.I_MIP_GEAR_RIGHT_TRANSIT", connection);
                    mip1.port[1].output[9] = new PhidgestOutput(hubMip, 1, 9, "system.indicators.I_MIP_GEAR_NOSE_DOWN", connection);
                    mip1.port[1].output[10] = new PhidgestOutput(hubMip, 1, 10, "system.indicators.I_MIP_GEAR_RIGHT_DOWN", connection);
                    mip1.port[1].output[11] = new PhidgestOutput(hubMip, 1, 11, "system.indicators.I_MIP_GEAR_NOSE_TRANSIT", connection);
                    mip1.port[1].output[12] = new PhidgestOutput(hubMip, 1, 12, "system.indicators.I_MIP_GEAR_LEFT_TRANSIT", connection);
                    mip1.port[1].output[13] = new PhidgestOutput(hubMip, 1, 13, "system.indicators.I_MIP_GEAR_LEFT_DOWN", connection);
                    mip1.port[1].output[14] = new PhidgestOutput(hubMip, 1, 14, "system.gates.B_GEAR_HANDLE_RELEASE", connection);
                    mip1.port[1].output[15] = new PhidgestOutput(hubMip, 1, 15, "system.gates.B_STANDBY_POWER", connection);
                    mip1.port[1].output[15].Inverse = true;

                    mip2.port[0].output[0] = new PhidgestOutput(hubMip2, 0, 0, "system.indicators.I_MIP_LE_FLAP_TRANSIT", connection);
                    mip2.port[0].output[1] = new PhidgestOutput(hubMip2, 0, 1, "system.indicators.I_MIP_LE_FLAP_EXT", connection);
                    mip2.port[0].output[2] = new PhidgestOutput(hubMip2, 0, 2, "system.indicators.I_MIP_TAKEOFF_CONFIG_FO", connection);
                    mip2.port[0].output[3] = new PhidgestOutput(hubMip2, 0, 3, "system.indicators.I_MIP_CABIN_ALTITUDE_FO", connection);
                    mip2.port[0].output[4] = new PhidgestOutput(hubMip2, 0, 4, "system.indicators.I_MIP_AUTOLAND_FO", connection);
                    mip2.port[0].output[5] = new PhidgestOutput(hubMip2, 0, 5, "system.indicators.I_MIP_FLAP_LOAD_RELIEF", connection);
                    mip2.port[0].output[6] = new PhidgestOutput(hubMip2, 0, 6, "system.indicators.I_MIP_BELOW_GS_FO", connection);
                    mip2.port[0].output[7] = new PhidgestOutput(hubMip2, 0, 7, "system.indicators.I_MIP_SPOILER_EXTEND", connection);
                    mip2.port[0].output[8] = new PhidgestOutput(hubMip2, 0, 8, "system.indicators.I_MIP_BELOW_GS_CP", connection);
                    mip2.port[0].output[9] = new PhidgestOutput(hubMip2, 0, 9, "system.indicators.I_MIP_SPOILER_ARMED", connection);
                    mip2.port[0].output[10] = new PhidgestOutput(hubMip2, 0, 10, "system.indicators.I_MIP_SPEEDBRAKE_DO_NOT_ARM", connection);
                    mip2.port[0].output[11] = new PhidgestOutput(hubMip2, 0, 11, "system.indicators.I_MIP_STAB_OUT_OF_TRIM", connection);
                    mip2.port[0].output[12] = new PhidgestOutput(hubMip2, 0, 12, "system.indicators.I_MIP_AUTOLAND", connection);
                    mip2.port[0].output[13] = new PhidgestOutput(hubMip2, 0, 13, "system.indicators.I_MIP_CABIN_ALTITUDE", connection);
                    mip2.port[0].output[14] = new PhidgestOutput(hubMip2, 0, 14, "system.indicators.I_MIP_TAKEOFF_CONFIG", connection);
                    mip2.port[0].output[15] = new PhidgestOutput(hubMip2, 0, 15, "system.indicators.I_OH_DOOR_FWD_SERVICE", connection);


                    PhidgestOutput digitalOutput_3_0 = new PhidgestOutput(hubPedestal, 3, 0, "system.gates.B_STANDBY_POWER", connection, true);
                    PhidgestOutput digitalOutput_3_1 = new PhidgestOutput(hubPedestal, 3, 1, "system.gates.B_REVERSER_2_SYNC_LOCK", connection, true);
                    digitalOutput_3_1.AddDelay(1500);
                    PhidgestOutput digitalOutput_3_2 = new PhidgestOutput(hubPedestal, 3, 2, "system.gates.B_REVERSER_1_SYNC_LOCK", connection, true);
                    digitalOutput_3_2.AddDelay(1500);
                    PhidgestOutput digitalOutput_3_3 = new PhidgestOutput(hubPedestal, 3, 3, "system.gates.B_AC_POWER", connection, true);
                   digitalOutput_3_8 = new PhidgestOutput(hubPedestal, 3, 8,
                        "system.gates.B_SPEED_BRAKE_DEPLOY", connection, true,
                        "system.gates.B_SPEED_BRAKE_RESTOW");

                    // Auto stow speed brake after it has open for that long
                        digitalOutput_3_8.TurnOffAfterMs = 60000;

                    PhidgestOutput digitalOutput_3_7 = new PhidgestOutput(hubPedestal, 3, 7, "system.indicators.I_MIP_PARKING_BRAKE", connection);


                    PhidgestOutput digitalOutput_4_0 = new PhidgestOutput(hubPedestal, 4, 0, "system.indicators.I_FIRE_1", connection);
                    PhidgestOutput digitalOutput_4_1 = new PhidgestOutput(hubPedestal, 4, 1, "system.indicators.I_FIRE_APU", connection);
                    PhidgestOutput digitalOutput_4_2 = new PhidgestOutput(hubPedestal, 4, 2, "system.indicators.I_FIRE_2", connection);
                    PhidgestOutput digitalOutput_4_3 = new PhidgestOutput(hubPedestal, 4, 3, "system.indicators.I_FIRE_L_BOTTLE_DISCHARGE", connection);
                    PhidgestOutput digitalOutput_4_4 = new PhidgestOutput(hubPedestal, 4, 4, "system.indicators.I_FIRE_R_BOTTLE_DISCHARGE", connection);
                    PhidgestOutput digitalOutput_4_5 = new PhidgestOutput(hubPedestal, 4, 5, "system.indicators.I_FIRE_TEST_APU", connection);
                    PhidgestOutput digitalOutput_4_6 = new PhidgestOutput(hubPedestal, 4, 6, "system.indicators.I_FIRE_APU_DET_INOPT", connection);

                    PhidgestOutput digitalOutput_4_8 = new PhidgestOutput(hubPedestal, 4, 8, "system.indicators.B_FIRE_HANDLE_LEFT_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_10 = new PhidgestOutput(hubPedestal, 4, 10, "system.indicators.B_FIRE_HANDLE_RIGHT_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_11 = new PhidgestOutput(hubPedestal, 4, 11, "system.indicators.I_FIRE_ENG_1_OVT", connection);
                    PhidgestOutput digitalOutput_4_12 = new PhidgestOutput(hubPedestal, 4, 12, "system.indicators.I_FIRE_ENG_2_OVT", connection);
                    PhidgestOutput digitalOutput_4_13 = new PhidgestOutput(hubPedestal, 4, 13, "system.indicators.I_FIRE_WHEEL_WELL_OVT", connection);
                    PhidgestOutput digitalOutput_4_14 = new PhidgestOutput(hubPedestal, 4, 14, "system.indicators.B_FIRE_HANDLE_APU_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_15 = new PhidgestOutput(hubPedestal, 4, 15, "system.indicators.I_FIRE_FAULT", connection);

                    PhidgestOutput digitalOutput_5_01 = new PhidgestOutput(hubPedestal, 5, 0, "system.indicators.I_ASP_VHF_1_SEND", connection);
                    PhidgestOutput digitalOutput_5_02 = new PhidgestOutput(hubPedestal, 5, 1, "system.indicators.I_ASP_VHF_2_SEND", connection);


                    digitalInput_2_00 = new PhidgetsInput(2, 0, "system.switches.S_FIRE_FAULT_TEST", 1, connection);
                    digitalInput_2_01 = new PhidgetsInput(2, 1, "system.switches.S_FIRE_FAULT_TEST", 2, connection);
                    digitalInput_2_02 = new PhidgetsInput(2, 2, "system.switches.S_FIRE_HANDLE1", 1, connection);
                    digitalInput_2_03 = new PhidgetsInput(2, 3, "system.switches.S_FIRE_PULL1", 1, connection);
                    digitalInput_2_04 = new PhidgetsInput(2, 4, "system.switches.S_FIRE_HANDLE1", 2, connection);

                    digitalInput_2_08 = new PhidgetsInput(2, 8, "system.switches.S_FIRE_HANDLE_APU", 1, connection);
                    digitalInput_2_09 = new PhidgetsInput(2, 9, "system.switches.S_FIRE_PULL_APU", 1, connection);
                    digitalInput_2_10 = new PhidgetsInput(2, 10, "system.switches.S_FIRE_HANDLE_APU", 2, connection);


                    digitalInput_2_11 = new PhidgetsInput(2, 11, "system.switches.S_FIRE_HANDLE2", 1, connection);
                    digitalInput_2_12 = new PhidgetsInput(2, 12, "system.switches.S_FIRE_PULL2", 1, connection);
                    digitalInput_2_13 = new PhidgetsInput(2, 13, "system.switches.S_FIRE_HANDLE2", 2, connection);

                    digitalInput1_0 = new PhidgetsInput(1, 0, "system.switches.S_THROTTLE_FUEL_CUTOFF1", 1, connection);
                    digitalInput1_1 = new PhidgetsInput(1, 1, "system.switches.S_THROTTLE_FUEL_CUTOFF2", 1, connection);
                    digitalInput1_2 = new PhidgetsInput(1, 2, "system.switches.S_THROTTLE_AT_DISENGAGE", 1, connection);
                    digitalInput1_3 = new PhidgetsInput(1, 3, "system.switches.S_THROTTLE_AT_DISENGAGE_2", 1, connection);
                    digitalInput1_4 = new PhidgetsInput(1, 4, "system.switches.S_THROTTLE_TOGA", 1, connection);

                    digitalInput1_10 = new PhidgetsInput(1, 10, "system.switches.S_RUDDER_TRIM", 1, connection);
                    digitalInput1_11 = new PhidgetsInput(1, 11, "system.switches.S_RUDDER_TRIM", 2, connection);
                    digitalInput1_08 = new PhidgetsInput(1, 08, "system.switches.S_AILERON_TRIM", 1, connection);
                    digitalInput1_09 = new PhidgetsInput(1, 09, "system.switches.S_AILERON_TRIM", 2, connection);

                    digitalInput1_12 = new PhidgetsInput(1, 12, "system.switches.S_ASP_VHF_1_SEND", 1, connection);
                    digitalInput1_13 = new PhidgetsInput(1, 13, "system.switches.S_ASP_VHF_2_SEND", 1, connection);

                    PhidgetsVoltageOutput pvo = new PhidgetsVoltageOutput(2, "system.gauge.G_MIP_BRAKE_PRESSURE", connection);
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
                   // trimWheel.pause(simIsPaused);
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

        private void btnSpeedBrake_Click(object sender, EventArgs e)
        {
            digitalOutput_3_8.TurnOff();
        }
    }
}