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
    using YamlDotNet.Core.Tokens;
    using System.Runtime.InteropServices.ComTypes;

    public partial class Form1 : Form
    {

        ProSimConnect connection = new ProSimConnect();
        bool phidgetsAdded = false;
        int logTabIndex = 7;
        int OutputBlinkFastIntervalMs = 300;
        int OutputBlinkSlowIntervalMs = 600;
        double OutputDefaultDimValue = 0.7;

        PhidgetsInput [] phidgetsInputPreview = new PhidgetsInput[360];
        PhidgetsInput[] phidgetsInput = new PhidgetsInput[360];
        PhidgetsMultiInput[] phidgetsMultiInput = new PhidgetsMultiInput[360];
        PhidgetsVoltageInput[] phidgetsVoltageInput = new PhidgetsVoltageInput[360];
        PhidgetsOutput[] phidgetsOutput = new PhidgetsOutput[360];
        PhidgetsOutput[] phidgetsGate = new PhidgetsOutput[360];
        PhidgetsVoltageOutput[] phidgetsVoltageOutput = new PhidgetsVoltageOutput[100];
        PhidgetsBLDCMotor[] phidgetsBLDCMotors = new PhidgetsBLDCMotor[10];
        private List<PhidgetsButton> PhidgetsButtonList = new List<PhidgetsButton>();
        // Define a dictionary to store custom colors for tabs
        private Dictionary<int, Color> tabColors = new Dictionary<int, Color>();


        PhidgetsOutput digitalOutput_3_8;

        Custom_TrimWheel trimWheel;
        PhidgetsBLDCMotor bldcm_00;
        PhidgetsBLDCMotor bldcm_01;

        bool simIsPaused = false;
        private BindingList<PhidgetsOutputInst> phidgetsOutputInstances;
        private BindingList<PhidgetsAudioInst> phidgetsAudioInstances;
        private BindingList<PhidgetsGateInst> phidgetsGateInstances;
        private BindingList<PhidgetsInputInst> phidgetsInputInstances;
        private BindingList<PhidgetsMultiInputInst> phidgetsMultiInputInstances;
        private BindingList<PhidgetsVoltageInputInst> PhidgetsVoltageInputInstances;
        private BindingList<PhidgetsVoltageOutputInst> phidgetsVoltageOutputInstances;
        private BindingList<PhidgetsButtonInst> phidgetsButtonInstances;
        private BindingList<PhidgetsBLDCMotorInst> phidgetsBLDCMotorInstances;


        public Form1()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.ph2pr;
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
            Invoke(new MethodInvoker(UnloadConfigIns));
        }

        // When we connect to ProSim737 system, update the status label and start filling the table
        void connection_onConnect()
        {
            Invoke(new MethodInvoker(updateStatusLabel));
            Invoke(new MethodInvoker(LoadConfigIns));
        }

        private async Task LoadConfigOuts()
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

                OutputBlinkFastIntervalMs = config.GeneralConfig.OutputBlinkFastIntervalMs;
                OutputBlinkSlowIntervalMs = config.GeneralConfig.OutputBlinkSlowIntervalMs;

                if (config.GeneralConfig.OutputDefaultDimValue > 0)
                {
                    OutputDefaultDimValue = config.GeneralConfig.OutputDefaultDimValue;
                }


                var totalOuts = 0;


                // Add Phidgets Hub
                if (config.PhidgetsHubsInstances != null)
                {

                    foreach (var hub in config.PhidgetsHubsInstances)
                    {
                        try
                        {
                            Net.AddServer(hub, hub, 5661, "", 0);
                            Net.EnableServer(hub);
                            DisplayInfoLog("Hub Added: " + hub);
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Cannot find Hub. " + hub + " :" + ex);
                        }
                    }

                }

                //// Code to test all lights on
                //var t = 0;
                //while (true)
                //{
                //    DateTime startDate = DateTime.Now;
                //    //Console.WriteLine("Current Time with Milliseconds: " + startDate.ToString("HH:mm:ss.fff"));

                //    for (int i = 0; i < totalOuts; i++)
                //    {
                //        // await Task.Run(() => phidgetsOutput[i].TurnOn(t));
                //        await Task.Run(() =>  phidgetsOutput[i].HandleDataChangeAsync("name", t));

                //    }


                //    DateTime endDate = DateTime.Now;
                //    //Console.WriteLine("Current Time DONE: " + endDate.ToString("HH:mm:ss.fff"));
                //    TimeSpan difference = endDate - startDate;

                //    // Display the difference
                //    Console.WriteLine($"Difference:  {difference.Milliseconds} mil, {difference.Seconds} seconds");

                //    t = t == 1 ? 0 : 1;
                //    //t = t == 1 ? 0 : 1;


                //    var taskDelay = Task.Delay(2000);
                //    await taskDelay;
                //}


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
                            phidgetsGate[idx] = new PhidgetsOutput(instance.Serial, instance.HubPort, instance.Channel,
                                "system.gates." + instance.ProsimDataRef, connection, true,
                                instance.ProsimDataRefOff != null ? "system.gates." + instance.ProsimDataRefOff : null); ;
                            phidgetsGate[idx].ErrorLog += DisplayErrorLog;
                            phidgetsGate[idx].InfoLog += DisplayInfoLog;
                            if (instance.Inverse == true)
                            {
                                phidgetsGate[idx].Inverse = true;
                            }
                            if (instance.DelayOn != null && instance.DelayOn > 0)
                            {
                                phidgetsGate[idx].Delay = Convert.ToInt32(instance.DelayOn);
                            }
                            if (instance.MaxTimeOn != null && instance.MaxTimeOn > 0)
                            {
                                phidgetsGate[idx].MaxTimeOn = Convert.ToInt32(instance.MaxTimeOn);
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                    totalOuts += idx;
                }

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
                            phidgetsOutput[idx] = new PhidgetsOutput(
                                    instance.Serial, instance.HubPort, instance.Channel,
                                    "system.indicators." + instance.ProsimDataRef, connection, false,
                                    instance.ProsimDataRefOff != null ? "system.indicators." + instance.ProsimDataRefOff : null
                                );
                            phidgetsOutput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsOutput[idx].InfoLog += DisplayInfoLog;
                            phidgetsOutput[idx].BlinkFastIntervalMs = OutputBlinkFastIntervalMs;
                            phidgetsOutput[idx].BlinkSlowIntervalMs = OutputBlinkSlowIntervalMs;

                            if (instance.Inverse == true)
                            {
                                phidgetsOutput[idx].Inverse = true;
                            }
                            if (instance.DelayOn != null && instance.DelayOn > 0)
                            {
                                phidgetsOutput[idx].Delay = Convert.ToInt32(instance.DelayOn);
                            }
                            if (instance.MaxTimeOn != null && instance.MaxTimeOn > 0)
                            {
                                phidgetsOutput[idx].MaxTimeOn = Convert.ToInt32(instance.MaxTimeOn);
                            }
                            if (instance.ValueOff != 0)
                            {
                                phidgetsOutput[idx].ValueOff = instance.ValueOff;
                            }
                            if (instance.ValueOn != 1)
                            {
                                phidgetsOutput[idx].ValueOn = instance.ValueOn;
                            }
                            if (instance.ValueDim != OutputDefaultDimValue)
                            {
                                phidgetsOutput[idx].ValueDim = instance.ValueDim;
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }

                    totalOuts += idx;
                }

                // Audio OUTPUTS
                if (config.PhidgetsAudioInstances != null)
                {
                    phidgetsAudioInstances = new BindingList<PhidgetsAudioInst>(config.PhidgetsAudioInstances);
                    dataGridViewOutputs.DataSource = phidgetsOutputInstances;
                    dataGridViewOutputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsAudioInstances)
                    {
                        try
                        {
                            phidgetsOutput[idx] = new PhidgetsOutput(
                                    instance.Serial, instance.HubPort, instance.Channel,
                                    "system.audio." + instance.ProsimDataRef, connection, false,
                                    instance.ProsimDataRefOff != null ? "system.audio." + instance.ProsimDataRefOff : null
                                 );
                            phidgetsOutput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsOutput[idx].InfoLog += DisplayInfoLog;
                            if (instance.DelayOn != null && instance.DelayOn > 0)
                            {
                                phidgetsOutput[idx].Delay = Convert.ToInt32(instance.DelayOn);
                            }
                            if (instance.MaxTimeOn != null && instance.MaxTimeOn > 0)
                            {
                                phidgetsOutput[idx].MaxTimeOn = Convert.ToInt32(instance.MaxTimeOn);
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                    totalOuts += idx;
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
                    totalOuts += idx;
                }

                // BLDC Motors
                if (config.PhidgetsBLDCMotorInstances != null)
                {
                    var idx = 0;
                    foreach (var instance in config.PhidgetsBLDCMotorInstances)
                    {
                        try
                        {
							var opts = new MotorTuningOptions
							{
								MaxVelocity = instance.MaxVelocity,
								MinVelocity = instance.MinVelocity,
								VelocityBand = instance.VelocityBand,
								CurveGamma = instance.CurveGamma,
								DeadbandEnter = instance.DeadbandEnter,
								DeadbandExit = instance.DeadbandExit,
								MaxVelStepPerTick = instance.MaxVelStepPerTick,
								Kp = instance.Kp,
								Ki = instance.Ki,
								Kd = instance.Kd,
								IntegralLimit = instance.IntegralLimit,
								PositionFilterAlpha = instance.PositionFilterAlpha,
								TickMs = instance.TickMs
							};

							phidgetsBLDCMotors[idx] = new PhidgetsBLDCMotor(
								deviceSerialNumber: instance.Serial,
								hubPort: instance.HubPort,
								connection: connection,
								reversed: instance.Reversed,
								offset: instance.Offset,
								refTurnOn: instance.RefTurnOn,
								refCurrentPos: instance.RefCurrentPos,
								refTargetPos: instance.RefTargetPos,
								acceleration: instance.Acceleration,
								options: opts
							);


							phidgetsBLDCMotors[idx].ErrorLog += DisplayErrorLog;
                            phidgetsBLDCMotors[idx].InfoLog += DisplayInfoLog;
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line for Voltage Output");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                    totalOuts += idx;
                }

                // Custom - Trim wheel
                if (config.CustomTrimWheelInstance != null)
                {
                    var instance  = config.CustomTrimWheelInstance;
                    try
                    {
                        trimWheel = new Custom_TrimWheel(
                            instance.Serial, 
                            instance.Channel, 
                            connection,
                            instance.DirtyUp,
                            instance.DirtyDown,
                            instance.CleanUp,
                            instance.CleanDown,
                            instance.APOnDirty,
                            instance.APOnDirty
                        );
                        trimWheel.ErrorLog += DisplayErrorLog;
                        trimWheel.InfoLog += DisplayInfoLog;
                    }
                    catch (Exception ex)
                    {
                        DisplayErrorLog("Error loading config line for Voltage Output");
                        DisplayErrorLog(ex.ToString());
                    }
                }

                DisplayInfoLog("Prosim IP:" + config.GeneralConfig.ProSimIP);
                DisplayInfoLog("Opening outputs:" + totalOuts);
          
                lblPsIP.Text = config.GeneralConfig.ProSimIP;
                // Wait for outs to finish
                var taskDelay2 = Task.Delay((totalOuts + 10) * 40);
                await taskDelay2;
                DisplayInfoLog("Connecting to Prosim");
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
            DisplayInfoLog("Loading Inputs configs ... ");

            try
            {
                // Read YAML from file
                string yamlContent = File.ReadAllText("config.yaml");

                // Deserialize YAML to objects
                var deserializer = new DeserializerBuilder()
                    .Build();

                // Wait before starting
                //var taskDelay = Task.Delay(2000);
                //await taskDelay;

                var config = deserializer.Deserialize<Config>(yamlContent);
                // Create instances based on the configuration

                ////Possible code to display inputs
                //var hub = 668522;

                //// Load for test
                //var ip_idx = 0;
                //for (var hubIdx = 4; hubIdx < 5; hubIdx++)
                //{
                //    for (var chIdx = 0; chIdx < 16; chIdx++)
                //    {
                //        phidgetsInputPreview[ip_idx] = new PhidgetsInput(hub, hubIdx, chIdx, connection, "test", 1);
                //        phidgetsInputPreview[ip_idx].ErrorLog += DisplayErrorLog;
                //        phidgetsInputPreview[ip_idx].InfoLog += DisplayInfoLog;
                //        ip_idx++;
                //    }
                //}

                // INPUTS
                if (config.PhidgetsInputInstances != null)
                {
                    DisplayInfoLog("Loading Inputs ... ");
                    phidgetsInputInstances = config.PhidgetsInputInstances != null ? new BindingList<PhidgetsInputInst>(config.PhidgetsInputInstances) : null;
                    dataGridViewInputs.DataSource = phidgetsInputInstances;
                    dataGridViewInputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    var idx = 0;
                    foreach (var instance in config.PhidgetsInputInstances)
                    {
                        try
                        {
                            phidgetsInput[idx] = new PhidgetsInput(
                                instance.Serial,
                                instance.HubPort,
                                instance.Channel,
                                connection,
                                "system.switches." + instance.ProsimDataRef,
                                instance.InputValue,
                                instance.OffInputValue);
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
                            DisplayErrorLog("Error reloading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }

                    DisplayInfoLog("Loading Inputs done");
                }

                // MULTI INPUTS
                if (config.PhidgetsMultiInputInstances != null)
                {
                    DisplayInfoLog("Loading MultiInputs ... ");
                    phidgetsMultiInputInstances = config.PhidgetsMultiInputInstances != null ? new BindingList<PhidgetsMultiInputInst>(config.PhidgetsMultiInputInstances) : null;
                    dataGridViewMultiInputs.DataSource = phidgetsMultiInputInstances;
                    dataGridViewMultiInputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    var idx = 0;
                    foreach (var instance in config.PhidgetsMultiInputInstances)
                    {
                        try
                        {

                            phidgetsMultiInput[idx] = new PhidgetsMultiInput(
                                instance.Serial,
                                instance.HubPort,
                                instance.Channels.ToArray(),
                                connection,
                                "system.switches." + instance.ProsimDataRef,
                                instance.Mappings);
                            phidgetsMultiInput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsMultiInput[idx].InfoLog += DisplayInfoLog;

                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error reloading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }

                    DisplayInfoLog("Loading MultiInputs done");
                }


                // VOLTAGE INPUTS
                if (config.PhidgetsVoltageInputInstances != null)
                {
                    DisplayInfoLog("Loading Voltage Inputs ... ");
                    PhidgetsVoltageInputInstances = config.PhidgetsVoltageInputInstances != null ? new BindingList<PhidgetsVoltageInputInst>(config.PhidgetsVoltageInputInstances) : null;
                    dataGridViewVoltageIn.DataSource = PhidgetsVoltageInputInstances;
                    dataGridViewVoltageIn.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    var idx = 0;
                    foreach (var instance in config.PhidgetsVoltageInputInstances)
                    {
                        try
                        {
                            phidgetsVoltageInput[idx] = new PhidgetsVoltageInput(
                                instance.Serial,
                                instance.HubPort,
                                instance.Channel,
                                connection,
                                "system.analog." + instance.ProsimDataRef,
                                instance.ProsimDataRefOnOff != "" ? "system.switches." + instance.ProsimDataRefOnOff : "",
                                instance.InputPoints.ToArray(),
                                instance.OutputPoints.ToArray(),
                                instance.InterpolationMode,
                                instance.CurvePower,
                                instance.DataInterval,
                                instance.MinChangeTriggerValue);
                            phidgetsVoltageInput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsVoltageInput[idx].InfoLog += DisplayInfoLog;

                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading config line for Voltage Inputs");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }

                    DisplayInfoLog("Loading Voltage Inputs done");
                }

                // Buttons
                if (config.PhidgetsButtonInstances != null)
                {
                    DisplayInfoLog("Loading Buttons ... ");
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
                        appButton.Width = 142;
                        appButton.Height = 45;
                        appButton.Text = app.Name;
                        appButton.MouseDown += new MouseEventHandler(app.StateChangeOn);
                        appButton.MouseUp += new MouseEventHandler(app.StateChangeOff);
                        app.ErrorLog += DisplayErrorLog;
                        app.InfoLog += DisplayInfoLog;

                        buttonsFlowLayoutPanel.Controls.Add(appButton);
                    }

                    DisplayInfoLog("Loading Buttons done ");
                }
            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error loading config");
                DisplayErrorLog(ex.ToString());
            }

        }

        private async void UnloadConfigIns()
        {
            try
            {
                // Read YAML from file
                string yamlContent = File.ReadAllText("config.yaml");

                // Deserialize YAML to objects
                var deserializer = new DeserializerBuilder()
                    .Build();

                // Wait before starting
                var taskDelay = Task.Delay(1000);
                await taskDelay;

                var config = deserializer.Deserialize<Config>(yamlContent);
                // Create instances based on the configuration


                // INPUTS
                if (config.PhidgetsInputInstances != null)
                {
                    DisplayInfoLog("Unloading inputs...");
                    //phidgetsInputInstances = config.PhidgetsInputInstances != null ? new BindingList<PhidgetsInputInst>(config.PhidgetsInputInstances) : null;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsInputInstances)
                    {
                        try
                        {
                            phidgetsInput[idx].Close();
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error closing input");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }

                // INPUTS
                if (config.PhidgetsMultiInputInstances != null)
                {
                    DisplayInfoLog("Unloading multi-inputs...");
                    //phidgetsInputInstances = config.PhidgetsInputInstances != null ? new BindingList<PhidgetsInputInst>(config.PhidgetsInputInstances) : null;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsMultiInputInstances)
                    {
                        try
                        {
                            phidgetsMultiInput[idx].Close();
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error closing input");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }

                // VOLTAGE INPUTS
                if (config.PhidgetsVoltageInputInstances != null)
                {
                    DisplayInfoLog("Unloading voltage-inputs...");

                    var idx = 0;
                    foreach (var instance in config.PhidgetsVoltageInputInstances)
                    {
                        try
                        {
                            phidgetsVoltageInput[idx].Close();
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error closing voltage input");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }


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

        //private void AddAllPhidgets()
        //{


        //    try
        //    {
        //        if (!phidgetsAdded)
        //        {
        //            trimWheel = new Custom_TrimWheel(668534, 0, connection, 1, 0.8, 0.6, 0.6, 0.7, 0.5);
        //            trimWheel.ErrorLog += DisplayErrorLog;
        //            trimWheel.InfoLog += DisplayInfoLog;
        //            phidgetsAdded = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("ERROR: Can't Initialize phidgets " + ex);
        //    }
        //}

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
                    bldcm_00?.Pause(simIsPaused);
                    bldcm_01?.Pause(simIsPaused);

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
            tabGroups.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabGroups.DrawItem += MyTabControl_DrawItem;
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
                txtLog.ForeColor = Color.Red;
                tabLog.BackColor = Color.Red;
                txtLog.Focus();
                tabGroups.SelectedIndex = logTabIndex; // log tab
                tabColors[logTabIndex] = Color.Red; // Set the color for the second tab (index 1) to red
                tabGroups.Invalidate(); // Trigger a redraw to apply the color
                txtLog.AppendText(DateTime.Now.ToLongTimeString() + " - ** ERROR ** : " + errorMessage + Environment.NewLine);
            }
        }

        private void MyTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = sender as TabControl;
            var currentTab = tabControl.TabPages[e.Index];

            // Check if a custom color is set for the tab
            Color tabColor = tabColors.ContainsKey(e.Index) ? tabColors[e.Index] : Color.Black;

            //// Draw the background (optional)
            //e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

            // Draw the tab text with the specified color
            TextRenderer.DrawText(e.Graphics, currentTab.Text, tabControl.Font,
                                  e.Bounds, tabColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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

           //  LoadConfigOuts();
           this.BeginInvoke(new Action(async () => await LoadConfigOuts()));

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

        private void btnLogOk_Click(object sender, EventArgs e)
        {
            txtLog.ForeColor = Color.Black;
            tabLog.BackColor = Color.White;
            tabColors[logTabIndex] = Color.Black;
        }

        private void btnLogClear_Click(object sender, EventArgs e)
        {
            txtLog.ForeColor = Color.Black;
            tabLog.BackColor = Color.White;
            tabColors[logTabIndex] = Color.Black;
            txtLog.Text = string.Empty;
        }
    }
}
