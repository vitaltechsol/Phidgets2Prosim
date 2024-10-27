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
    using System.Threading.Tasks;

    public partial class Form1 : Form
    {

        ProSimConnect connection = new ProSimConnect();
        bool phidgetsAdded = false;

        PhidgetsInput[] phidgetsInputPreview = new PhidgetsInput[360];
        PhidgetsInput[] phidgetsInput = new PhidgetsInput[360];
        PhidgestOutput[] phidgetsOutput = new PhidgestOutput[360];
        PhidgestOutput[] phidgetsGate = new PhidgestOutput[360];
        PhidgetsVoltageOutput[] phidgetsVoltageOutput = new PhidgetsVoltageOutput[100];
        private List<PhidgetsButton> PhidgetsButtonList = new List<PhidgetsButton>();


        PhidgestOutput digitalOutput_3_8;

        Custom_TrimWheel trimWheel;
        PhidgetsBLDCMotor bldcm_00;
        PhidgetsBLDCMotor bldcm_01;

        PhidgetsMultiInput muAPU;
        PhidgetsMultiInput mu1;
        PhidgetsMultiInput mu2;
        PhidgetsMultiInput mu3;
        PhidgetsMultiInput mu4;
        PhidgetsMultiInput mu5;
        PhidgetsMultiInput mu6;
        PhidgetsMultiInput mu7;
        PhidgetsMultiInput mu8;

        //  static string[] hubs = { "hub5000-1", "hub5000-MIP-1", "hub5000-MIP-2", "hub5000-motors", "hub5000-OH-1", "hub5000-OH-2" };

        bool simIsPaused = false;
        private BindingList<PhidgetsOutputInst> phidgetsOutputInstances;
        private BindingList<PhidgetsGateInst> phidgetsGateInstances;
        private BindingList<PhidgetsInputInst> phidgetsInputInstances;
        private BindingList<PhidgetsVoltageOutputInst> phidgetsVoltageOutputInstances;
        private BindingList<PhidgetsButtonInst> phidgetsButtonInstances;


        public Form1()
        {
            InitializeComponent();
            this.Shown += new System.EventHandler(Form1_Shown);
            this.FormClosed += new FormClosedEventHandler(Form1_Closed);

        }

        async void connectToProSim(string prosimIP)
        {
            connectionStatusLabel.Text = "CONNECTING....";

            try
            {
                DisplayInfoLog("Prosim connecting");
                connection.Connect(prosimIP);
            }
            catch (Exception ex)
            {
                DisplayErrorLog("ERROR: Cannot connect to Prosim. " + ex);
            }
        }

        void connection_onDisconnect()
        {
            Invoke(new MethodInvoker(updateStatusLabel));
        }

        // When we connect to ProSim737 system, update the status label and start filling the table
        void connection_onConnect()
        {
            Invoke(new MethodInvoker(updateStatusLabel));
            // Invoke(new MethodInvoker(AddAllPhidgets));
            Invoke(new MethodInvoker(LoadConfigIns));
        }

        private async void LoadConfigOuts()
        {
            try
            {
                // Read YAML from file
                string yamlContent = File.ReadAllText("config.yaml");

                // Deserialize YAML to objects
                var deserializer = new DeserializerBuilder()
                    .Build();

                var config = deserializer.Deserialize<Config>(yamlContent);
                // Create instances based on the configuration


                // Add Phidgets Hub
                if (config.PhidgetsHubsIntances != null)
                {

                    foreach (var hub in config.PhidgetsHubsIntances)
                    {
                        try
                        {
                            Net.AddServer(hub, hub, 5661, "", 0);
                            DisplayInfoLog("Hub Added: " + hub);
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Cannot find Hub. " + hub + " :" + ex);
                        }
                    }

                }

                // wait for hubs to connect
                var taskDelay = Task.Delay(config.PhidgetsHubsIntances.Count * 1000);
                await taskDelay;

                // OUTPUTS
                if (config.PhidgetsOutputInstances != null)
                {
                    phidgetsOutputInstances = new BindingList<PhidgetsOutputInst>(config.PhidgetsOutputInstances);
                    dataGridViewOutputs.DataSource = phidgetsOutputInstances;
                    dataGridViewOutputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsOutputInstances)
                    {
                        try
                        {
                            phidgetsOutput[idx] = new PhidgestOutput(instance.Serial, instance.HubPort, instance.Channel,
                                "system.indicators." + instance.ProsimDataRef, connection, false, null);
                            phidgetsOutput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsOutput[idx].InfoLog += DisplayInfoLog;
                            if (instance.OnDelay != null && instance.OnDelay > 0)
                            {
                                phidgetsOutput[idx].Delay = Convert.ToInt32(instance.OnDelay);
                            }
                            if (instance.TurnOffAfterMs != null && instance.TurnOffAfterMs > 0)
                            {
                                phidgetsOutput[idx].TurnOffAfterMs = Convert.ToInt32(instance.TurnOffAfterMs);
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }

                // GATES
                if (config.PhidgetsGateInstances != null)
                {

                    phidgetsGateInstances = new BindingList<PhidgetsGateInst>(config.PhidgetsGateInstances);
                    dataGridViewGates.DataSource = phidgetsGateInstances;
                    dataGridViewGates.CellEndEdit += dataGridViewOutputs_CellEndEdit;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsGateInstances)
                    {
                        try
                        {
                            phidgetsGate[idx] = new PhidgestOutput(instance.Serial, instance.HubPort, instance.Channel,
                                "system.gates." + instance.ProsimDataRef, connection, true, 
                                instance.ProsimDataRefOff != null ? "system.gates." + instance.ProsimDataRefOff : null); ;
                            phidgetsGate[idx].ErrorLog += DisplayErrorLog;
                            phidgetsGate[idx].InfoLog += DisplayInfoLog;
                            if (instance.Inverse == true)
                            {
                                phidgetsGate[idx].Inverse = true;
                            }
                            if (instance.OnDelay != null && instance.OnDelay > 0)
                            {
                                phidgetsGate[idx].Delay = Convert.ToInt32(instance.OnDelay);
                            }
                            if (instance.TurnOffAfterMs != null && instance.TurnOffAfterMs > 0)
                            {
                                phidgetsGate[idx].TurnOffAfterMs = Convert.ToInt32(instance.TurnOffAfterMs);
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }

                // Voltage Output
                if (config.PhidgetsVoltageOutputInstances != null)
                {
                    phidgetsVoltageOutputInstances = new BindingList<PhidgetsVoltageOutputInst>(config.PhidgetsVoltageOutputInstances);
                    dataGridViewVoltageOut.DataSource = phidgetsVoltageOutputInstances;
                    dataGridViewVoltageOut.CellEndEdit += dataGridViewOutputs_CellEndEdit;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsVoltageOutputInstances)
                    {
                        try
                        {
                            phidgetsVoltageOutput[idx] = new PhidgetsVoltageOutput(instance.Serial, instance.HubPort, 
                                "system.gauge." + instance.ProsimDataRef, connection);


                            phidgetsVoltageOutput[idx].ScaleFactor = instance.ScaleFactor;
                            phidgetsVoltageOutput[idx].Offset = instance.Offset;
                            phidgetsVoltageOutput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsVoltageOutput[idx].InfoLog += DisplayInfoLog;
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line for Voltage Output");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }

                DisplayInfoLog("Prosim IP:" + config.GeneralConfig.ProSimIP);
                lblPsIP.Text = config.GeneralConfig.ProSimIP;


                // Wait for outs to finish
                var taskDelay2 = Task.Delay(3000);
                await taskDelay2;

                connectToProSim(config.GeneralConfig.ProSimIP);

            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error loading config");
                DisplayErrorLog(ex.ToString());
            }

        }

        private async void LoadConfigIns()
        {

           
            try
            {
                // Read YAML from file
                string yamlContent = File.ReadAllText("config.yaml");

                // Deserialize YAML to objects
                var deserializer = new DeserializerBuilder()
                    .Build();

                // Wait before starting
                var taskDelay = Task.Delay(2000);
                await taskDelay;

                var config = deserializer.Deserialize<Config>(yamlContent);
                // Create instances based on the configuration


                // INPUTS
                if (config.PhidgetsInputInstances != null)
                {
                    phidgetsInputInstances = config.PhidgetsInputInstances != null ? new BindingList<PhidgetsInputInst>(config.PhidgetsInputInstances) : null;
                    dataGridViewInputs.DataSource = phidgetsInputInstances;
                    dataGridViewInputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsInputInstances)
                    {
                        try
                        {
                            phidgetsInput[idx] = new PhidgetsInput(instance.Serial, instance.HubPort, instance.Channel, connection,
                                "system.switches." + instance.ProsimDataRef, instance.InputValue, instance.OffInputValue);
                            phidgetsInput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsInput[idx].InfoLog += DisplayInfoLog;
                            if (instance.ProsimDataRef2 != null)
                            {
                                phidgetsInput[idx].ProsimDataRef2 = instance.ProsimDataRef2;
                            }
                            if (instance.ProsimDataRef3 != null)
                            {
                                phidgetsInput[idx].ProsimDataRef3 = instance.ProsimDataRef3;
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }

                // Buttons
                if (config.PhidgetsButtonInstances != null)
                {
                    phidgetsButtonInstances = config.PhidgetsButtonInstances != null ? new BindingList<PhidgetsButtonInst>(config.PhidgetsButtonInstances) : null;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsButtonInstances)
                    {
                        try
                        {
                            PhidgetsButtonList.Add(new PhidgetsButton(
                                idx, 
                                instance.Name, 
                                connection, 
                                "system.switches." + instance.ProsimDataRef, 
                                instance.InputValue, 
                                instance.OffInputValue)
                            );
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }

                    // Clear the FlowLayoutPanel before adding buttons
                    buttonsFlowLayoutPanel.Controls.Clear();

                    foreach (var app in PhidgetsButtonList)
                    {
                        Button appButton = new Button();
                        appButton.Width = 160;
                        appButton.Height = 45;
                        appButton.Text = app.Name;
                        appButton.MouseDown += new MouseEventHandler(app.StateChangeOn);
                        appButton.MouseUp += new MouseEventHandler(app.StateChangeOff);
                        app.ErrorLog += DisplayErrorLog;
                        app.InfoLog += DisplayInfoLog;

                        buttonsFlowLayoutPanel.Controls.Add(appButton);
                    }
                }

                var hubOH_2_SrlNo = 668015;
                var hubMip_1_SrlNo = 668522;

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

                mu2 = new PhidgetsMultiInput(hubOH_2_SrlNo, 0, new int[2] { 2, 3 }, connection, "system.switches.S_OH_IRS_SEL_L",
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

                mu5 = new PhidgetsMultiInput(hubMip_1_SrlNo, 4, new int[3] { 4, 5, 6 }, connection, "system.switches.S_MIP_N1_SET",
                    new Dictionary<string, int>()
                    {
                            {"100", 2},
                            {"010", 1},
                            {"110", 0},
                            {"001", 3}
                    });

                mu6 = new PhidgetsMultiInput(hubMip_1_SrlNo, 4, new int[3] { 7, 8, 9 }, connection, "system.switches.S_MIP_N1_SET_VALUE",
                    new Dictionary<string, int>()
                    {
                            {"001", 4},
                            {"101", 2},
                            {"111", 0},
                            {"110", 1},
                            {"010", 3}
                    });

                mu7 = new PhidgetsMultiInput(hubMip_1_SrlNo, 4, new int[3] { 10, 11, 12 }, connection, "system.switches.S_MIP_SPD_REF_VALUE",
                    new Dictionary<string, int>()
                    {
                            {"001", 4},
                            {"101", 2},
                            {"111", 0},
                            {"110", 1},
                            {"010", 3}
                    });

                mu8 = new PhidgetsMultiInput(hubMip_1_SrlNo, 4, new int[3] { 13, 14, 15 }, connection, "system.switches.S_MIP_SPDREF",
                    new Dictionary<string, int>()
                    {
                            {"100", 0},
                            {"010", 1},
                            {"110", 2},
                            {"001", 3},
                            {"101", 4},
                            {"011", 5},
                            {"111", 6}
                    });


                muAPU.ErrorLog += DisplayErrorLog;
                muAPU.InfoLog += DisplayInfoLog;
                mu1.ErrorLog += DisplayErrorLog;
                mu1.InfoLog += DisplayInfoLog;
                mu2.ErrorLog += DisplayErrorLog;
                mu2.InfoLog += DisplayInfoLog;
                mu3.ErrorLog += DisplayErrorLog;
                mu3.InfoLog += DisplayInfoLog;
                mu4.ErrorLog += DisplayErrorLog;
                mu4.InfoLog += DisplayInfoLog;
                mu5.ErrorLog += DisplayErrorLog;
                mu5.InfoLog += DisplayInfoLog;
                mu6.ErrorLog += DisplayInfoLog;
                mu6.InfoLog += DisplayInfoLog;
                mu7.ErrorLog += DisplayErrorLog;
                mu7.InfoLog += DisplayInfoLog;
                mu8.ErrorLog += DisplayErrorLog;
                mu8.InfoLog += DisplayInfoLog;
                    

            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error loading config");
                DisplayErrorLog(ex.ToString());
            }

        }

        private void Form1_InfoLog(string obj)
        {
            throw new NotImplementedException();
        }

        private void AddAllPhidgets()
        {

            //var hubPedestalSlrNo = 618534;

            //Possible code to display inputs
            //var oh1 = 668659;

            //// Load for test
            //var idx = 0;
            //for (var hubIdx = 0; hubIdx < inHubs.Length; hubIdx++) 
            //{
            //    for (var chIdx = 0; chIdx < 16; chIdx++)
            //    {
            //        phidgetsInputPreview[idx] = new PhidgetsInput(oh1, inHubs[hubIdx], chIdx, connection, "test", 1);
            //        phidgetsInputPreview[idx].ErrorLog += DisplayErrorLog;
            //        phidgetsInputPreview[idx].InfoLog += DisplayInfoLog;
            //        idx++;
            //    }
            //}   
            

            try
            {
                if (!phidgetsAdded)
                {
                    // trimWheel = new Custom_TrimWheel(0, connection, 1, 0.8, 0.5, 0.5, 0.7, 0.3);
                    trimWheel = new Custom_TrimWheel(0, connection, 1, 0.8, 0.6, 0.6, 0.7, 0.5);

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
                if (!simIsPaused) 
                { 
                    DisplayInfoLog("Prosim CONNECTED");
                    connectionStatusLabel.Text = "Connected";
                    connectionStatusLabel.ForeColor = Color.LimeGreen;
                }

                if (simIsPaused)
                {
                    DisplayInfoLog("Prosim Paused");
                    connectionStatusLabel.Text = "Paused";
                    connectionStatusLabel.ForeColor = Color.OrangeRed;
                }
            }
            else
            {
                DisplayInfoLog("Prosim DISCONNECTED");
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
                    if (trimWheel != null)
                    {
                        trimWheel?.pause(simIsPaused);
                    }
                    bldcm_00?.pause(simIsPaused);
                    bldcm_01?.pause(simIsPaused);

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

        // Method to display error log
        private void DisplayErrorLog(string errorMessage)
        {
            if (txtLog.InvokeRequired)
            {
                // If we're not on the UI thread, invoke this method on the UI thread
                txtLog.Invoke(new Action(() => DisplayErrorLog(errorMessage)));
            }
            else
            {
                // If we're on the UI thread, directly update the TextBox
                txtLog.AppendText(DateTime.Now.ToLongTimeString() + " - ** ERROR ** : " + errorMessage + Environment.NewLine);
            }
        }

        private async void DisplayInfoLog(string infoMessage)
        {

            if (txtLog.InvokeRequired)
            {
                // If we're not on the UI thread, invoke this method on the UI thread
                txtLog.Invoke(new Action(() => DisplayInfoLog(infoMessage)));
            }
            else
            {
                // If we're on the UI thread, directly update the TextBox
                txtLog.AppendText(DateTime.Now.ToLongTimeString() + ": " + infoMessage + Environment.NewLine);
            }
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            LoadConfigOuts();
            AddAllPhidgets();

            // Register Prosim to receive connect and disconnect events
            connection.onConnect += connection_onConnect;
            connection.onDisconnect += connection_onDisconnect;

            DataRef dataRef = new DataRef("simulator.pause", 100, connection);
            dataRef.onDataChange += DataRef_onDataChange;

        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine("closed");
        }
    }




    public class Config
    {
        public GeneralConfig GeneralConfig { get; set; }
        public List<string> PhidgetsHubsIntances { get; set; }
        public List<PhidgetsOutputInst> PhidgetsOutputInstances { get; set; }
        public List<PhidgetsGateInst> PhidgetsGateInstances { get; set; }
        public List<PhidgetsInputInst> PhidgetsInputInstances { get; set; }
        public List<PhidgetsBLDCMotorInst> PhidgetsBLDCMotorInstances { get; set; }
        public List<PhidgetsVoltageOutputInst> PhidgetsVoltageOutputInstances { get; set; }
        public List<PhidgetsButtonInst> PhidgetsButtonInstances { get; set; }
    }

    public class PhidgetsOutputInst : PhidgetDevice
    {
        public int? OnDelay { get; set; }
        public bool Inverse { get; set; } = false;
        public int? TurnOffAfterMs { get; set; } = null;

    }

    public class PhidgetsGateInst : PhidgetDevice
    {
        public int? OnDelay { get; set; } = null;
        public bool Inverse { get; set; } = false;
        public string ProsimDataRefOff { get; set; } = null;
        public int? TurnOffAfterMs { get; set; } = null;

    }

    public class PhidgetsInputInst : PhidgetDevice
    {
        public int InputValue { get; set; }
        public int OffInputValue { get; set; } = 0;
        public string ProsimDataRef2 { get; set; } = null;
        public string ProsimDataRef3 { get; set; } = null;

    }


    public class PhidgetsBLDCMotorInst : PhidgetDevice
    {
        public int Offset { get; set; }
        public bool Reversed { get; set; }
        public string RefTurnOn { get; set; }
        public string RefCurrentPos { get; set; }
        public string RefTargetPos { get; set; }
    }

    public class PhidgetsVoltageOutputInst : PhidgetDevice
    {
        public double ScaleFactor { get; set; }
        public double Offset { get; set; } = 0;
        
    }

    public class PhidgetsButtonInst : PhidgetDevice
    {
        public string Name { get; set; }
        public int InputValue { get; set; }
        public int OffInputValue { get; set; } = 0;
    }

    public class GeneralConfig
    {
        public string ProSimIP { get; set; }
        public string Schema { get; set; }
    }



}
