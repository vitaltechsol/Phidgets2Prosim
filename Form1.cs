using System.Security.Claims;

namespace Phidgets2Prosim
{
    using System;
    using Phidget22;
    using System.Diagnostics;
    using ProSimSDK;
    using System.Windows.Forms;
    using System.Drawing;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Runtime.Remoting.Channels;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;
    using System.IO;
    using System.ComponentModel;
    using System.Linq;

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

        PhidgetsInput[] phidgetsInput = new PhidgetsInput[360];

        PhidgestOutput digitalOutput_3_8;

        Custom_TrimWheel trimWheel;
        PhidgetsBLDCMotor bldcm_00;
        PhidgetsBLDCMotor bldcm_01;

        PhidgetsMultiInput muAPU;
        PhidgetsMultiInput mu1;
        PhidgetsMultiInput mu2;
        PhidgetsMultiInput mu3;
        PhidgetsMultiInput mu4;

        static string[] hubs = { "hub5000-1", "hub5000-MIP-1", "hub5000-MIP-2", "hub5000-motors", "hub5000-OH-1", "hub5000-OH-2" };

        bool simIsPaused = false;
        private BindingList<PhidgetsOutputInst> phidgetsOutputInstances;
        private BindingList<PhidgetsInputInst> phidgetsInputInstances;


        public Form1()
        {

            InitializeComponent();

            
            // Add Phidgets Hub
            foreach (var hub in hubs)
            {
                try
                {
                    Net.AddServer(hub, hub, 5661, "", 0);
                    Debug.WriteLine("Started Hub. " + hub);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ERROR: Cannot Start Hub. " + hub + " :" + ex);
                }
            }
       
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
                Debug.WriteLine("ERROR: Cannot connect to Prosim. " + ex);
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
            Invoke(new MethodInvoker(LoadConfig));
            Invoke(new MethodInvoker(AddAllPhidgets));
        }

        private void LoadConfig()
        {

            // Read YAML from file
            string yamlContent = File.ReadAllText("config.yaml");

            // Deserialize YAML to objects
            var deserializer = new DeserializerBuilder()
                .Build();

            var config = deserializer.Deserialize<Config>(yamlContent);
            phidgetsOutputInstances = new BindingList<PhidgetsOutputInst>(config.PhidgetsOutputInstances);
            dataGridViewOutputs.DataSource = phidgetsOutputInstances;
            dataGridViewOutputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;

            phidgetsInputInstances = config.PhidgetsInputInstances != null ? new BindingList<PhidgetsInputInst>(config.PhidgetsInputInstances) : null;
            dataGridViewInputs.DataSource = phidgetsInputInstances;
            dataGridViewInputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;


            // Create instances based on the configuration
            foreach (var instance in config.PhidgetsOutputInstances)
            {
                PhidgestOutput phidgetsOutput = new PhidgestOutput(instance.DeviceSerialNo, instance.HubPort, instance.Channel,
                    instance.ProsimDataRef, connection, instance.IsGate, instance.ProsimDataRefOff, instance.IsHubPortDevice);
            }

            if (config.PhidgetsInputInstances != null)
            {
                var idx = 0;
                foreach (var instance in config.PhidgetsInputInstances)
                {
                    PhidgetsInput phidgetsInput = new PhidgetsInput(instance.DeviceSerialNo, instance.HubPort, instance.Channel, connection,
                        instance.ProsimDataRef, instance.InputValue, instance.OffInputValue);
                    idx++;
                }
            }

        }

        private void AddAllPhidgets()
        {

            var hubPedestalSlrNo = 618534;
            var hubMipSlrNo = 668522;
            var hubMip2SlrNo = 664861;
            var hubMotorsSlrNo = 668066;
            var hubOH_1_SrlNo = 668659;
            var hubOH_2_SrlNo = 668015;

            try
            {
                if (!phidgetsAdded)
                {

                    var ph = new PhidgetsHub[5];
                    var mip1 = ph[0];
                    var mip2 = ph[1];
                    var hub_ped = ph[2];
                    var hub_motors = ph[3];
                    var hub_oh_1 = ph[4];

                    mip1 = new PhidgetsHub();
                    mip2 = new PhidgetsHub();
                    hub_ped = new PhidgetsHub();
                    hub_motors = new PhidgetsHub();
                    hub_oh_1 = new PhidgetsHub();

                    hub_motors.port[4].output[0] = new PhidgestOutput(hubMotorsSlrNo, 4, 0, "system.gates.B_STICKSHAKER_FO", connection, true, null, true);
                    hub_motors.port[5].output[0] = new PhidgestOutput(hubMotorsSlrNo, 5, 0, "system.gates.B_STICKSHAKER", connection, true, null, true);

                    mip1.port[1].output[3] = new PhidgestOutput(hubMipSlrNo, 1, 3, "system.indicators.I_MIP_ASA_ATR_1", connection);
                    mip1.port[1].output[4] = new PhidgestOutput(hubMipSlrNo, 1, 4, "system.indicators.I_MIP_ASA_FMC_1", connection);
                    mip1.port[1].output[5] = new PhidgestOutput(hubMipSlrNo, 1, 5, "system.indicators.I_MIP_ASA_ATA_1", connection);
                    mip1.port[1].output[6] = new PhidgestOutput(hubMipSlrNo, 1, 6, "system.indicators.I_MIP_ASA_APA_1", connection);
                    mip1.port[1].output[7] = new PhidgestOutput(hubMipSlrNo, 1, 7, "system.indicators.I_MIP_ASA_APR_1", connection);

                    mip1.port[1].output[8] = new PhidgestOutput(hubMipSlrNo, 1, 8, "system.indicators.I_MIP_GEAR_RIGHT_TRANSIT", connection);
                   // mip1.port[1].output[9] = new PhidgestOutput(hubMipSlrNo, 1, 9, "system.indicators.I_MIP_GEAR_NOSE_DOWN", connection);
                    // mip1.port[1].output[10] = new PhidgestOutput(hubMipSlrNo, 1, 10, "system.indicators.I_MIP_GEAR_RIGHT_DOWN", connection);
                    mip1.port[1].output[11] = new PhidgestOutput(hubMipSlrNo, 1, 11, "system.indicators.I_MIP_GEAR_NOSE_TRANSIT", connection);
                    mip1.port[1].output[12] = new PhidgestOutput(hubMipSlrNo, 1, 12, "system.indicators.I_MIP_GEAR_LEFT_TRANSIT", connection);
                    mip1.port[1].output[13] = new PhidgestOutput(hubMipSlrNo, 1, 13, "system.indicators.I_MIP_GEAR_LEFT_DOWN", connection);
                    mip1.port[1].output[14] = new PhidgestOutput(hubMipSlrNo, 1, 14, "system.gates.B_GEAR_HANDLE_RELEASE", connection);
                    //    mip1.port[1].output[15] = new PhidgestOutput(hubMip, 1, 15, "system.gates.B_STANDBY_POWER", connection);
                    //    mip1.port[1].output[15].Inverse = true;

                    mip2.port[0].output[0] = new PhidgestOutput(hubMip2SlrNo, 0, 0, "system.indicators.I_MIP_LE_FLAP_TRANSIT", connection);
                    mip2.port[0].output[1] = new PhidgestOutput(hubMip2SlrNo, 0, 1, "system.indicators.I_MIP_LE_FLAP_EXT", connection);
                    mip2.port[0].output[2] = new PhidgestOutput(hubMip2SlrNo, 0, 2, "system.indicators.I_MIP_TAKEOFF_CONFIG_FO", connection);
                    mip2.port[0].output[3] = new PhidgestOutput(hubMip2SlrNo, 0, 3, "system.indicators.I_MIP_CABIN_ALTITUDE_FO", connection);
                    mip2.port[0].output[4] = new PhidgestOutput(hubMip2SlrNo, 0, 4, "system.indicators.I_MIP_AUTOLAND_FO", connection);
                    mip2.port[0].output[5] = new PhidgestOutput(hubMip2SlrNo, 0, 5, "system.indicators.I_MIP_FLAP_LOAD_RELIEF", connection);
                    mip2.port[0].output[6] = new PhidgestOutput(hubMip2SlrNo, 0, 6, "system.indicators.I_MIP_BELOW_GS_FO", connection);
                    mip2.port[0].output[7] = new PhidgestOutput(hubMip2SlrNo, 0, 7, "system.indicators.I_MIP_SPOILER_EXTEND", connection);
                    mip2.port[0].output[8] = new PhidgestOutput(hubMip2SlrNo, 0, 8, "system.indicators.I_MIP_BELOW_GS_CP", connection);
                    mip2.port[0].output[9] = new PhidgestOutput(hubMip2SlrNo, 0, 9, "system.indicators.I_MIP_SPOILER_ARMED", connection);
                    mip2.port[0].output[10] = new PhidgestOutput(hubMip2SlrNo, 0, 10, "system.indicators.I_MIP_SPEEDBRAKE_DO_NOT_ARM", connection);
                    mip2.port[0].output[11] = new PhidgestOutput(hubMip2SlrNo, 0, 11, "system.indicators.I_MIP_STAB_OUT_OF_TRIM", connection);
                    mip2.port[0].output[12] = new PhidgestOutput(hubMip2SlrNo, 0, 12, "system.indicators.I_MIP_AUTOLAND", connection);
                    mip2.port[0].output[13] = new PhidgestOutput(hubMip2SlrNo, 0, 13, "system.indicators.I_MIP_CABIN_ALTITUDE", connection);
                    mip2.port[0].output[14] = new PhidgestOutput(hubMip2SlrNo, 0, 14, "system.indicators.I_MIP_TAKEOFF_CONFIG", connection);
                    mip2.port[0].output[15] = new PhidgestOutput(hubMip2SlrNo, 0, 15, "system.indicators.I_OH_DOOR_FWD_SERVICE", connection);

                    // LE Devices
                    hub_oh_1.port[3].output[0] = new PhidgestOutput(hubOH_1_SrlNo, 3, 0, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT1", connection);
                    hub_oh_1.port[3].output[1] = new PhidgestOutput(hubOH_1_SrlNo, 3, 1, "system.indicators.I_OH_LEDEVICES_EXT_SLAT1", connection);
                    hub_oh_1.port[3].output[2] = new PhidgestOutput(hubOH_1_SrlNo, 3, 2, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT1", connection);
                    hub_oh_1.port[3].output[3] = new PhidgestOutput(hubOH_1_SrlNo, 3, 3, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT2", connection);
                    hub_oh_1.port[3].output[4] = new PhidgestOutput(hubOH_1_SrlNo, 3, 4, "system.indicators.I_OH_LEDEVICES_EXT_SLAT2", connection);
                    hub_oh_1.port[3].output[5] = new PhidgestOutput(hubOH_1_SrlNo, 3, 5, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT2", connection);
                    hub_oh_1.port[3].output[6] = new PhidgestOutput(hubOH_1_SrlNo, 3, 6, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT3", connection);
                    hub_oh_1.port[3].output[7] = new PhidgestOutput(hubOH_1_SrlNo, 3, 7, "system.indicators.I_OH_LEDEVICES_EXT_SLAT3", connection);
                    hub_oh_1.port[3].output[8] = new PhidgestOutput(hubOH_1_SrlNo, 3, 8, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT3", connection);
                    hub_oh_1.port[3].output[9] = new PhidgestOutput(hubOH_1_SrlNo, 3, 9, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT4", connection);
                    hub_oh_1.port[3].output[10] = new PhidgestOutput(hubOH_1_SrlNo, 3, 10, "system.indicators.I_OH_LEDEVICES_EXT_SLAT4", connection);
                    hub_oh_1.port[3].output[11] = new PhidgestOutput(hubOH_1_SrlNo, 3, 11, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT4", connection);
                    hub_oh_1.port[3].output[12] = new PhidgestOutput(hubOH_1_SrlNo, 3, 12, "system.indicators.I_OH_LEDEVICES_TRANS_FLAP1", connection);
                    hub_oh_1.port[3].output[13] = new PhidgestOutput(hubOH_1_SrlNo, 3, 13, "system.indicators.I_OH_LEDEVICES_EXT_FLAP1", connection);
                    hub_oh_1.port[3].output[14] = new PhidgestOutput(hubOH_1_SrlNo, 3, 14, "system.indicators.I_OH_LEDEVICES_TRANS_FLAP2", connection);
                    hub_oh_1.port[3].output[15] = new PhidgestOutput(hubOH_1_SrlNo, 3, 15, "system.indicators.I_OH_LEDEVICES_EXT_FLAP2", connection);

                    hub_oh_1.port[4].output[0] = new PhidgestOutput(hubOH_1_SrlNo, 4, 0, "system.indicators.I_OH_LEDEVICES_TRANS_FLAP3", connection);
                    hub_oh_1.port[4].output[1] = new PhidgestOutput(hubOH_1_SrlNo, 4, 1, "system.indicators.I_OH_LEDEVICES_EXT_FLAP3", connection);
                    hub_oh_1.port[4].output[2] = new PhidgestOutput(hubOH_1_SrlNo, 4, 2, "system.indicators.I_OH_LEDEVICES_TRANS_FLAP4", connection);
                    hub_oh_1.port[4].output[3] = new PhidgestOutput(hubOH_1_SrlNo, 4, 3, "system.indicators.I_OH_LEDEVICES_EXT_FLAP4", connection);
                    hub_oh_1.port[4].output[4] = new PhidgestOutput(hubOH_1_SrlNo, 4, 4, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT5", connection);
                    hub_oh_1.port[4].output[5] = new PhidgestOutput(hubOH_1_SrlNo, 4, 5, "system.indicators.I_OH_LEDEVICES_EXT_SLAT5", connection);
                    hub_oh_1.port[4].output[6] = new PhidgestOutput(hubOH_1_SrlNo, 4, 6, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT5", connection);
                    hub_oh_1.port[4].output[7] = new PhidgestOutput(hubOH_1_SrlNo, 4, 7, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT6", connection);
                    hub_oh_1.port[4].output[8] = new PhidgestOutput(hubOH_1_SrlNo, 4, 8, "system.indicators.I_OH_LEDEVICES_EXT_SLAT6", connection);
                    hub_oh_1.port[4].output[9] = new PhidgestOutput(hubOH_1_SrlNo, 4, 9, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT6", connection);
                    hub_oh_1.port[4].output[10] = new PhidgestOutput(hubOH_1_SrlNo, 4, 10, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT7", connection);
                    hub_oh_1.port[4].output[11] = new PhidgestOutput(hubOH_1_SrlNo, 4, 11, "system.indicators.I_OH_LEDEVICES_EXT_SLAT7", connection);
                    hub_oh_1.port[4].output[12] = new PhidgestOutput(hubOH_1_SrlNo, 4, 12, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT7", connection);
                    hub_oh_1.port[4].output[13] = new PhidgestOutput(hubOH_1_SrlNo, 4, 13, "system.indicators.I_OH_LEDEVICES_TRANS_SLAT8", connection);
                    hub_oh_1.port[4].output[14] = new PhidgestOutput(hubOH_1_SrlNo, 4, 14, "system.indicators.I_OH_LEDEVICES_EXT_SLAT8", connection);
                    hub_oh_1.port[4].output[15] = new PhidgestOutput(hubOH_1_SrlNo, 4, 15, "system.indicators.I_OH_LEDEVICES_FULLEXT_SLAT8", connection);

                    PhidgestOutput digitalOutput_3_0 = new PhidgestOutput(hubPedestalSlrNo, 3, 0, "system.gates.B_STANDBY_POWER", connection, true);
                    PhidgestOutput digitalOutput_3_1 = new PhidgestOutput(hubPedestalSlrNo, 3, 1, "system.gates.B_REVERSER_2_SYNC_LOCK", connection, true);
                    digitalOutput_3_1.AddDelay(1500);
                    PhidgestOutput digitalOutput_3_2 = new PhidgestOutput(hubPedestalSlrNo, 3, 2, "system.gates.B_REVERSER_1_SYNC_LOCK", connection, true);
                    digitalOutput_3_2.AddDelay(1500);
                    PhidgestOutput digitalOutput_3_3 = new PhidgestOutput(hubPedestalSlrNo, 3, 3, "system.gates.B_AC_POWER", connection, true);
                    digitalOutput_3_8 = new PhidgestOutput(hubPedestalSlrNo, 3, 8,
                         "system.gates.B_SPEED_BRAKE_DEPLOY", connection, true,
                         "system.gates.B_SPEED_BRAKE_RESTOW");

                    // Auto stow speed brake after it has open for that long
                    digitalOutput_3_8.TurnOffAfterMs = 60000;

                    PhidgestOutput digitalOutput_3_7 = new PhidgestOutput(hubPedestalSlrNo, 3, 7, "system.indicators.I_MIP_PARKING_BRAKE", connection);


                    PhidgestOutput digitalOutput_4_0 = new PhidgestOutput(hubPedestalSlrNo, 4, 0, "system.indicators.I_FIRE_1", connection);
                    PhidgestOutput digitalOutput_4_1 = new PhidgestOutput(hubPedestalSlrNo, 4, 1, "system.indicators.I_FIRE_APU", connection);
                    PhidgestOutput digitalOutput_4_2 = new PhidgestOutput(hubPedestalSlrNo, 4, 2, "system.indicators.I_FIRE_2", connection);
                    PhidgestOutput digitalOutput_4_3 = new PhidgestOutput(hubPedestalSlrNo, 4, 3, "system.indicators.I_FIRE_L_BOTTLE_DISCHARGE", connection);
                    PhidgestOutput digitalOutput_4_4 = new PhidgestOutput(hubPedestalSlrNo, 4, 4, "system.indicators.I_FIRE_R_BOTTLE_DISCHARGE", connection);
                    PhidgestOutput digitalOutput_4_5 = new PhidgestOutput(hubPedestalSlrNo, 4, 5, "system.indicators.I_FIRE_TEST_APU", connection);
                    PhidgestOutput digitalOutput_4_6 = new PhidgestOutput(hubPedestalSlrNo, 4, 6, "system.indicators.I_FIRE_APU_DET_INOPT", connection);

                    PhidgestOutput digitalOutput_4_8 = new PhidgestOutput(hubPedestalSlrNo, 4, 8, "system.indicators.B_FIRE_HANDLE_LEFT_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_10 = new PhidgestOutput(hubPedestalSlrNo, 4, 10, "system.indicators.B_FIRE_HANDLE_RIGHT_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_11 = new PhidgestOutput(hubPedestalSlrNo, 4, 11, "system.indicators.I_FIRE_ENG_1_OVT", connection);
                    PhidgestOutput digitalOutput_4_12 = new PhidgestOutput(hubPedestalSlrNo, 4, 12, "system.indicators.I_FIRE_ENG_2_OVT", connection);
                    PhidgestOutput digitalOutput_4_13 = new PhidgestOutput(hubPedestalSlrNo, 4, 13, "system.indicators.I_FIRE_WHEEL_WELL_OVT", connection);
                    PhidgestOutput digitalOutput_4_14 = new PhidgestOutput(hubPedestalSlrNo, 4, 14, "system.indicators.B_FIRE_HANDLE_APU_LOCK", connection, true);
                    PhidgestOutput digitalOutput_4_15 = new PhidgestOutput(hubPedestalSlrNo, 4, 15, "system.indicators.I_FIRE_FAULT", connection);

                    PhidgestOutput digitalOutput_5_01 = new PhidgestOutput(hubPedestalSlrNo, 5, 0, "system.indicators.I_ASP_VHF_1_SEND", connection);
                    PhidgestOutput digitalOutput_5_02 = new PhidgestOutput(hubPedestalSlrNo, 5, 1, "system.indicators.I_ASP_VHF_2_SEND", connection);

                    //hub_ped.port[3].input[0] = new PhidgetsInput(hubPedestalSlrNo, 2, 0, "system.switches.S_FIRE_FAULT_TEST", 1, connection);
                    // hub_ped.port[3].input[0] = new PhidgetsInput(hubPedestalSlrNo, 2, 1, "system.switches.S_FIRE_FAULT_TEST", 2, connection);

                    digitalInput_2_00 = new PhidgetsInput(hubPedestalSlrNo, 2, 0, connection, "system.switches.S_FIRE_FAULT_TEST", 1);
                    digitalInput_2_01 = new PhidgetsInput(hubPedestalSlrNo, 2, 1, connection, "system.switches.S_FIRE_FAULT_TEST", 2);
                    digitalInput_2_02 = new PhidgetsInput(hubPedestalSlrNo, 2, 2, connection, "system.switches.S_FIRE_HANDLE1", 1);
                    digitalInput_2_03 = new PhidgetsInput(hubPedestalSlrNo, 2, 3, connection, "system.switches.S_FIRE_PULL1", 1);
                    digitalInput_2_04 = new PhidgetsInput(hubPedestalSlrNo, 2, 4, connection, "system.switches.S_FIRE_HANDLE1", 2);
                    digitalInput_2_08 = new PhidgetsInput(hubPedestalSlrNo, 2, 8, connection, "system.switches.S_FIRE_HANDLE_APU", 1);
                    digitalInput_2_09 = new PhidgetsInput(hubPedestalSlrNo, 2, 9, connection, "system.switches.S_FIRE_PULL_APU", 1);
                    digitalInput_2_10 = new PhidgetsInput(hubPedestalSlrNo, 2, 10, connection, "system.switches.S_FIRE_HANDLE_APU", 2);
                    digitalInput_2_11 = new PhidgetsInput(hubPedestalSlrNo, 2, 11, connection, "system.switches.S_FIRE_HANDLE2", 1);
                    digitalInput_2_12 = new PhidgetsInput(hubPedestalSlrNo, 2, 12, connection, "system.switches.S_FIRE_PULL2", 1);
                    digitalInput_2_13 = new PhidgetsInput(hubPedestalSlrNo, 2, 13, connection, "system.switches.S_FIRE_HANDLE2", 2);

                    //hub_ped.port[1].input[0] = new PhidgetsInput(hubPedestalSlrNo, 1, 0, "system.switches.S_THROTTLE_FUEL_CUTOFF1", 1, connection);
                    //hub_ped.port[1].input[1] = new PhidgetsInput(hubPedestalSlrNo, 1, 1, "system.switches.S_THROTTLE_FUEL_CUTOFF2", 1, connection);
                    //hub_ped.port[1].input[2] = new PhidgetsInput(hubPedestalSlrNo, 1, 2, "system.switches.S_THROTTLE_AT_DISENGAGE", 1, connection);
                    //hub_ped.port[1].input[3] = new PhidgetsInput(hubPedestalSlrNo, 1, 3, "system.switches.S_THROTTLE_AT_DISENGAGE_2", 1, connection);
                    //hub_ped.port[1].input[4] = new PhidgetsInput(hubPedestalSlrNo, 1, 4, "system.switches.S_THROTTLE_TOGA", 1, connection);

                    digitalInput1_0 = new PhidgetsInput(hubPedestalSlrNo, 1, 0, connection, "system.switches.S_THROTTLE_FUEL_CUTOFF1", 1);
                    digitalInput1_1 = new PhidgetsInput(hubPedestalSlrNo, 1, 1, connection, "system.switches.S_THROTTLE_FUEL_CUTOFF2", 1);
                    digitalInput1_2 = new PhidgetsInput(hubPedestalSlrNo, 1, 2, connection, "system.switches.S_THROTTLE_AT_DISENGAGE", 1);
                    digitalInput1_3 = new PhidgetsInput(hubPedestalSlrNo, 1, 3, connection, "system.switches.S_THROTTLE_AT_DISENGAGE_2", 1);
                    digitalInput1_4 = new PhidgetsInput(hubPedestalSlrNo, 1, 4, connection, "system.switches.S_THROTTLE_TOGA", 1);

                    digitalInput1_08 = new PhidgetsInput(hubPedestalSlrNo, 1, 08, connection, "system.switches.S_AILERON_TRIM", 1);
                    digitalInput1_09 = new PhidgetsInput(hubPedestalSlrNo, 1, 09, connection, "system.switches.S_AILERON_TRIM", 2);
                  //  digitalInput1_10 = new PhidgetsInput(hubPedestalSlrNo, 1, 10, connection, "system.switches.S_RUDDER_TRIM", 1);
                   //  digitalInput1_11 = new PhidgetsInput(hubPedestalSlrNo, 1, 11, connection, "system.switches.S_RUDDER_TRIM", 2);

                    digitalInput1_12 = new PhidgetsInput(hubPedestalSlrNo, 1, 12, connection, "system.switches.S_ASP_VHF_1_SEND", 1);
                    digitalInput1_13 = new PhidgetsInput(hubPedestalSlrNo, 1, 13, connection, "system.switches.S_ASP_VHF_2_SEND", 1);

                    muAPU = new PhidgetsMultiInput(hubOH_2_SrlNo, 0, new int[2] { 14, 15 }, connection, "system.switches.S_OH_APU",
                    new Dictionary<string, int>()
                    {
                            {"01", 2},
                            {"10", 0},
                            {"00", 1}
                    });

                    mu1 = new PhidgetsMultiInput(hubOH_2_SrlNo, 0, new int[2] { 0, 1 }, connection, "system.switches.S_OH_IRS_SEL_R",
                        new Dictionary<string, int>()
                        {
                            {"11", 0},
                            {"10", 2},
                            {"00", 1},
                            {"01", 3}
                        });

                    mu2 = new PhidgetsMultiInput(hubOH_2_SrlNo, 0, new int[2] { 2, 3 }, connection, "system.switches.S_OH_ENG_START_L",
                        new Dictionary<string, int>()
                        {
                            {"11", 0},
                            {"10", 2},
                            {"00", 1},
                            {"01", 3}
                        });

                    mu3 = new PhidgetsMultiInput(hubOH_2_SrlNo, 0, new int[3] { 8, 9, 10 }, connection, "system.switches.S_OH_ENG_START_R",
                        new Dictionary<string, int>()
                        {
                            {"000", 0},
                            {"011", 1},
                            {"010", 2},
                            {"100", 3}
                        });

                    mu4 = new PhidgetsMultiInput(hubOH_2_SrlNo, 0, new int[3] { 11, 12, 13 }, connection, "system.switches.S_OH_ENG_START_L",
                        new Dictionary<string, int>()
                        {
                            {"000", 0},
                            {"011", 1},
                            {"010", 2},
                            {"100", 3}
                        });


                    PhidgetsVoltageOutput pvo = new PhidgetsVoltageOutput(hubMipSlrNo, 2, 500, "system.gauge.G_MIP_BRAKE_PRESSURE", connection);
                    PhidgetsVoltageOutput pvo2 = new PhidgetsVoltageOutput(hubOH_2_SrlNo, 5, 1090, "system.gauge.G_OH_EGT", connection);

                    trimWheel = new Custom_TrimWheel(0, connection, 1, 0.8, 0.5, 0.5, 0.7, 0.3);

                    bldcm_00 = new PhidgetsBLDCMotor(0, connection, false, 0,
                        "system.gates.B_THROTTLE_SERVO_POWER_RIGHT",
                        "system.analog.A_THROTTLE_RIGHT",
                        "system.gauge.G_THROTTLE_RIGHT");

                    bldcm_01 = new PhidgetsBLDCMotor(1, connection, true, 5,
                     "system.gates.B_THROTTLE_SERVO_POWER_LEFT",
                     "system.analog.A_THROTTLE_LEFT",
                     "system.gauge.G_THROTTLE_LEFT");


                    phidgetsAdded = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: Can't Initialize phidgets " + ex);
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

        private void DataRef_onDataChange(DataRef dataRef)
        {
            var name = dataRef.name;
            if (name == "simulator.pause")
            {
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
                    Debug.WriteLine("ERROR: simulator.pause " + dataRef.value);
                    Debug.WriteLine(ex.ToString());
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

        private void dataGridViewOutputs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Save changes whenever a cell is edited
            SaveYamlConfiguration();
        }
        private void SaveYamlConfiguration()
        {
            try
            {
            

                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                var config = new Config
                {
                    PhidgetsOutputInstances = phidgetsOutputInstances.ToList(),
                };

                string yamlContent = serializer.Serialize(config);

                File.WriteAllText("config2.yaml", yamlContent);

            
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving YAML configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }


    public class Config
    {
        public List<PhidgetsOutputInst> PhidgetsOutputInstances { get; set; }
        public List<PhidgetsInputInst> PhidgetsInputInstances { get; set; }

        public List<PhidgetsBLDCMotorInst> PhidgetsBLDCMotorInstances { get; set; }
        public List<PhidgetsVoltageOutputInst> PhidgetsVoltageOutputInstances { get; set; }
    }

    public class PhidgetsOutputInst
    {
        public int DeviceSerialNo { get; set; }
        public int HubPort { get; set; }
        public int Channel { get; set; }
        public string ProsimDataRef { get; set; }
        public string ProsimDataRefOff { get; set; } = null;
        public bool IsHubPortDevice { get; set; } = false;
        public bool IsGate { get; set; } = false;

    }

    public class PhidgetsInputInst
    {
        public int DeviceSerialNo { get; set; }
        public int HubPort { get; set; }
        public int Channel { get; set; }
        public string ProsimDataRef { get; set; }
        public bool IsHubPortDevice { get; set; } = false;
        public int InputValue { get; set; }
        public int OffInputValue { get; set; } = 0;
    }



    public class PhidgetsBLDCMotorInst
    {
        public int HubPort { get; set; }
        public ProSimConnect Connection { get; set; }
        public bool Reversed { get; set; }
        public int Offset { get; set; }
        public string RefTurnOn { get; set; }
        public string RefCurrentPos { get; set; }
        public string RefTargetPos { get; set; }
    }

    public class PhidgetsVoltageOutputInst
    {
        public int DeviceSerialNo { get; set; }
        public int HubPort { get; set; }
        public decimal ScaleFactor { get; set; }
        public string ProsimDataRef { get; set; }
        public ProSimConnect Connection { get; set; }
    }

   
}
