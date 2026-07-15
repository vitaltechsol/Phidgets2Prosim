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
        int logTabIndex = 0;
        int OutputBlinkFastIntervalMs = 300;
        int OutputBlinkSlowIntervalMs = 600;
        double OutputDefaultDimValue = 0.7;
        bool configsInsLoaded = false;

        PhidgetsInput [] phidgetsInputPreview = new PhidgetsInput[360];
        PhidgetsInput[] phidgetsInput = new PhidgetsInput[360];
        PhidgetsMultiInput[] phidgetsMultiInput = new PhidgetsMultiInput[360];
        PhidgetsVoltageInput[] phidgetsVoltageInput = new PhidgetsVoltageInput[360];
        PhidgetsOutput[] phidgetsOutput = new PhidgetsOutput[360];
        PhidgetsOutput[] phidgetsGate = new PhidgetsOutput[360];
        PhidgetsVoltageOutput[] phidgetsVoltageOutput = new PhidgetsVoltageOutput[100];
        PhidgetsBLDCMotor[] phidgetsBLDCMotors = new PhidgetsBLDCMotor[10];
        PhidgetsDCMotor[] phidgetsDCMotors = new PhidgetsDCMotor[10];
        PhidgetsEncoder[] phidgetsEncoders = new PhidgetsEncoder[20];
        private List<PhidgetsButton> PhidgetsButtonList = new List<PhidgetsButton>();
        // Define a dictionary to store custom colors for tabs
        private Dictionary<int, Color> tabColors = new Dictionary<int, Color>();


        PhidgetsOutput digitalOutput_3_8;

        Custom_TrimWheel trimWheel;
        PhidgetsBLDCMotor bldcm_00;
        PhidgetsBLDCMotor bldcm_01;
		Custom_ParkingBrake customParkingBrake;

		bool simIsPaused = false;
        private BindingList<PhidgetsOutputInst> phidgetsOutputInstances;
        private BindingList<PhidgetsAudioInst> phidgetsAudioInstances;
        private BindingList<PhidgetsGateInst> phidgetsGateInstances;
        private InputsUI inputsUI;
        private EncodersUI encodersUI;
        private ProsimConnectionUI prosimConnectionUI;
        private BindingList<PhidgetsMultiInputInst> phidgetsMultiInputInstances;
        private BindingList<PhidgetsVoltageInputInst> PhidgetsVoltageInputInstances;
        private BindingList<PhidgetsVoltageOutputInst> phidgetsVoltageOutputInstances;
        private BindingList<PhidgetsButtonInst> phidgetsButtonInstances;
        private BindingList<PhidgetsBLDCMotorInst> phidgetsBLDCMotorInstances;
        private BindingList<PhidgetsDCMotorInst> phidgetsDCMotorInstances;
        private BindingList<PhidgetsEncoderInst> phidgetsEncoderInstances;

        public Form1()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.ph2pr;
            this.Shown += new System.EventHandler(Form1_Shown);
            this.FormClosed += new FormClosedEventHandler(Form1_Closed);
            // Initialize InputsUI abstraction
            inputsUI = new InputsUI(
                cboInputHub,
                cboInputHubPort,
                cboInputChannel,
                txtInputProsimRef,
                cboInputOnValue,
                cboInputOffValue,
                btnAddInput,
                dataGridViewInputs,
                DisplayInfoLog,
                DisplayErrorLog,
                () => GetCurrentHubs()
            );
            // Initialize EncodersUI abstraction
            encodersUI = new EncodersUI(
                cboEncoderHub,
                cboEncoderHubPort,
                cboEncoderChannel,
                txtEncoderProsimRef,
                txtEncoderScaleFactor,
                btnAddEncoder,
                dataGridViewEncoders,
                DisplayInfoLog,
                DisplayErrorLog,
                () => GetCurrentHubs()
            );
            // Initialize ProsimConnectionUI
            prosimConnectionUI = new ProsimConnectionUI(
                txtProsimIP,
                btnSaveProsimIP,
                btnConnectProsim,
                btnDisconnectProsim,
                connectionStatusLabel,
                connection,
                DisplayInfoLog,
                DisplayErrorLog,
                updateStatusLabel
            );
        }

        async void connectToProSim(string prosimIP)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => connectToProSim(prosimIP)));
                return;
            }

            connectionStatusLabel.Text = "CONNECTING TO " + prosimIP;

            try
            {
                DisplayInfoLog("Prosim connecting");
                await Task.Run(() => connection.Connect(prosimIP));
            }
            catch (Exception ex)
            {
                DisplayErrorLog("ERROR: Cannot connect to Prosim. " + ex);
            }
        }

        void connection_onDisconnect()
        {
            BeginInvoke(new MethodInvoker(updateStatusLabel));
            prosimConnectionUI?.OnConnectionStateChanged();
            if (configsInsLoaded)
            {
                BeginInvoke(new MethodInvoker(UnloadConfigIns));
            }
        }

        // When we connect to ProSim737 system, update the status label and start filling the table
        void connection_onConnect()
        {
            BeginInvoke(new MethodInvoker(updateStatusLabel));
            prosimConnectionUI?.OnConnectionStateChanged();
            Task.Run(() => { try { LoadConfigIns(); } catch (Exception ex) { DisplayErrorLog("Error loading inputs: " + ex.Message); } });
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
                            if (!hub.Enabled) continue;
                            Net.AddServer(hub.Name, hub.Name, 5661, "", 0);
                            Net.EnableServer(hub.Name);
                            DisplayInfoLog("Hub Added: " + hub.Name + " (Serial: " + hub.Serial + ")");
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Cannot find Hub. " + hub.Name + " :" + ex);
                        }
                    }

                }

                inputsUI.PopulateInputHubDropdown(config.PhidgetsHubsInstances ?? new List<PhidgetsHubInst>());
                encodersUI.PopulateEncoderHubDropdown(config.PhidgetsHubsInstances ?? new List<PhidgetsHubInst>());

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

                    BeginInvoke(new Action(() => {
                        phidgetsGateInstances = new BindingList<PhidgetsGateInst>(config.PhidgetsGateInstances);
                        dataGridViewGates.DataSource = phidgetsGateInstances;
                        dataGridViewGates.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    }));

                    var idx = 0;
                    foreach (var instance in config.PhidgetsGateInstances)
                    {
                        try
                        {
							var outRef = string.IsNullOrWhiteSpace(instance.ProsimDataRef)
	                        ? "test"   // sentinel => output will ignore ProSim and use Variable only
	                        : "system.gates." + instance.ProsimDataRef;

							phidgetsGate[idx] = new PhidgetsOutput(
							instance.Serial, instance.HubPort, instance.Channel,
							outRef, connection, true,
							instance.ProsimDataRefOff != null ? "system.gates." + instance.ProsimDataRefOff : null,
							instance.ProsimDataRef2 != null ? "system.gates." + instance.ProsimDataRef2 : null,
							instance.Operator);



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
                    BeginInvoke(new Action(() => {
                        phidgetsOutputInstances = new BindingList<PhidgetsOutputInst>(config.PhidgetsOutputInstances);
                        dataGridViewOutputs.DataSource = phidgetsOutputInstances;
                        dataGridViewOutputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    }));

                    var idx = 0;
                    foreach (var instance in config.PhidgetsOutputInstances)
                    {
                        try
                        {
							var outRef = string.IsNullOrWhiteSpace(instance.ProsimDataRef)
	                        ? "test"   // << special sentinel: means “don’t hook to ProSim, use Variable only”
	                        : "system.indicators." + instance.ProsimDataRef;

							phidgetsOutput[idx] = new PhidgetsOutput(
	                        instance.Serial, instance.HubPort, instance.Channel,
	                        outRef, connection, false,
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
							if (instance.UserVariable != null)
                            {
								phidgetsOutput[idx].UserVariable = instance.UserVariable;
							}
							if (!string.IsNullOrEmpty(instance.UserVariable))
							{
								phidgetsOutput[idx].UserVariable = instance.UserVariable;
								DisplayInfoLog($"[WIRING] Output Hub:{instance.HubPort} Ch:{instance.Channel} UserVariable='{instance.UserVariable}'");
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
                    BeginInvoke(new Action(() => {
                        phidgetsAudioInstances = new BindingList<PhidgetsAudioInst>(config.PhidgetsAudioInstances);
                        dataGridViewOutputs.DataSource = phidgetsOutputInstances;
                        dataGridViewOutputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    }));

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
                    BeginInvoke(new Action(() => {
                        phidgetsVoltageOutputInstances = new BindingList<PhidgetsVoltageOutputInst>(config.PhidgetsVoltageOutputInstances);
                        dataGridViewVoltageOut.DataSource = phidgetsVoltageOutputInstances;
                        dataGridViewVoltageOut.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    }));

                    var idx = 0;
                    foreach (var instance in config.PhidgetsVoltageOutputInstances)
                    {
                        try
                        {
                            phidgetsVoltageOutput[idx] = new PhidgetsVoltageOutput(instance.Serial, instance.HubPort, 
                                "system.gauge." + instance.ProsimDataRef, connection);


                            phidgetsVoltageOutput[idx].ScaleFactor = instance.ScaleFactor;
                            phidgetsVoltageOutput[idx].Offset = instance.Offset;
                            phidgetsVoltageOutput[idx].Interval = instance.Interval;
                            phidgetsVoltageOutput[idx].SmoothFactor = instance.SmoothFactor;
                            phidgetsVoltageOutput[idx].UseSinCos = instance.UseSinCos;
                            phidgetsVoltageOutput[idx].AmplitudeVolts = instance.AmplitudeVolts;
                            phidgetsVoltageOutput[idx].WrapDegrees360 = instance.WrapDegrees360;
                            phidgetsVoltageOutput[idx].SwapSinCos = instance.SwapSinCos;
                            phidgetsVoltageOutput[idx].InvertSin = instance.InvertSin;
                            phidgetsVoltageOutput[idx].InvertCos = instance.InvertCos;
                            phidgetsVoltageOutput[idx].SmoothAngleStep = instance.SmoothAngleStep;
                            phidgetsVoltageOutput[idx].TargetUpdateIntervalMs = instance.TargetUpdateIntervalMs;
                            phidgetsVoltageOutput[idx].TargetFilterAlpha = instance.TargetFilterAlpha;
                            phidgetsVoltageOutput[idx].DeadbandDegrees = instance.DeadbandDegrees;
                            phidgetsVoltageOutput[idx].CosSerial = instance.CosSerial;
                            phidgetsVoltageOutput[idx].CosHubPort = instance.CosHubPort;
                            phidgetsVoltageOutput[idx].SinChannel = instance.SinChannel;
                            phidgetsVoltageOutput[idx].CosChannel = instance.CosChannel;
                            phidgetsVoltageOutput[idx].ErrorLog += DisplayErrorLog;
                            phidgetsVoltageOutput[idx].InfoLog += DisplayInfoLog;

                            _ = phidgetsVoltageOutput[idx].Open();
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
								MaxVelocity = instance.Options.MaxVelocity,
								MinVelocity = instance.Options.MinVelocity,
								VelocityBand = instance.Options.VelocityBand,
								CurveGamma = instance.Options.CurveGamma,
								DeadbandEnter = instance.Options.DeadbandEnter,
								DeadbandExit = instance.Options.DeadbandExit,
								MaxVelStepPerTick = instance.Options.MaxVelStepPerTick,
								Kp = instance.Options.Kp,
								Ki = instance.Options.Ki,
								Kd = instance.Options.Kd,
                                IOnBand = instance.Options.IOnBand,
                                IntegralLimit = instance.Options.IntegralLimit,
								PositionFilterAlpha = instance.Options.PositionFilterAlpha,
								TickMs = instance.Options.TickMs
							};

							phidgetsBLDCMotors[idx] = new PhidgetsBLDCMotor(
								serial: instance.Serial,
								hubPort: instance.HubPort,
								connection: connection,
								reversed: instance.Reversed,
								offset: instance.Offset,
								refTurnOn: instance.RefTurnOn,
								refCurrentPos: instance.RefCurrentPos,
								refTargetPos: instance.RefTargetPos,
								acceleration: instance.Acceleration,
								options: opts,
								refTurnOn2: instance.RefTurnOn2
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

                // DC Motors
                if (config.PhidgetsDCMotorInstances != null)
                {

                    BeginInvoke(new Action(() => {
                        phidgetsDCMotorInstances = new BindingList<PhidgetsDCMotorInst>(config.PhidgetsDCMotorInstances);
                        dataGridDCMotors.DataSource = phidgetsDCMotorInstances;
                        dataGridDCMotors.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                    }));

                    var idx = 0;
                    foreach (var instance in config.PhidgetsDCMotorInstances)
                    {
                        try
                        {
                            var opts = new MotorTuningOptions
                            {
                                MaxVelocity = instance.Options.MaxVelocity,
                                MinVelocity = instance.Options.MinVelocity,
                                VelocityBand = instance.Options.VelocityBand,
                                CurveGamma = instance.Options.CurveGamma,
                                DeadbandEnter = instance.Options.DeadbandEnter,
                                DeadbandExit = instance.Options.DeadbandExit,
                                MaxVelStepPerTick = instance.Options.MaxVelStepPerTick,
                                Kp = instance.Options.Kp,
                                Ki = instance.Options.Ki,
                                Kd = instance.Options.Kd,
                                IOnBand = instance.Options.IOnBand,
                                IntegralLimit = instance.Options.IntegralLimit,
                                PositionFilterAlpha = instance.Options.PositionFilterAlpha,
                                TickMs = instance.Options.TickMs
                            };

                            phidgetsDCMotors[idx] = new PhidgetsDCMotor(
                                instance.Serial,
                                instance.HubPort,
                                connection,
                                options: opts
                                )

                            {
                                //Reversed = false,
                                Reversed = instance.Reversed,
								CurrentLimit = instance.CurrentLimit,
								Acceleration = (instance.Acceleration > 0) ? instance.Acceleration : 50
                            };

                            if (instance.VoltageInput != null)
                            {
                                var voltageIn = new PhidgetsVoltageInput(
                                  instance.VoltageInput.Serial,
                                  instance.VoltageInput.HubPort,
                                  instance.VoltageInput.Channel,
                                  connection,
                                  "", "",
                                   instance.VoltageInput.InputPoints.ToArray(),
                                   instance.VoltageInput.OutputPoints.ToArray()
                                  );
                                voltageIn.MinChangeTriggerValue = instance.VoltageInput.MinChangeTriggerValue;

                                phidgetsDCMotors[idx].VoltageInput = voltageIn;
                            }

                            await phidgetsDCMotors[idx].InitializeAsync();
                            phidgetsDCMotors[idx].UseRefTarget("system.gauge.G_PED_ELEV_TRIM");

                            idx++;
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error loading DC Motor Test");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                }

                // Custom - Trim wheel
                if (config.CustomTrimWheelInstance != null)
                {
                    var instance  = config.CustomTrimWheelInstance;
                    try
                    {
                        trimWheel = new Custom_TrimWheel(
                            instance.Serial,
                            instance.HubPort,
                            connection,
                            instance.Reversed,
                            instance.DirtyUp,
                            instance.DirtyDown,
                            instance.CleanUp,
                            instance.CleanDown,
                            instance.APOnDirty,
                            instance.APOnDirty,
							instance.Accelerate,
							instance.Range.ToArray(),
                            instance.Encoder
                        );

                        trimWheel.ErrorLog += DisplayErrorLog;
                        trimWheel.InfoLog += DisplayInfoLog;
                    }
                    catch (Exception ex)
                    {
                        DisplayErrorLog("Error loading config line for Custom Trim Wheel");
                        DisplayErrorLog(ex.ToString());
                    }
                }

				// Custom - Parking Brake
				if (config.CustomParkingBrakeInstance != null )
				{
					var c = config.CustomParkingBrakeInstance;
					try
					{
						var pb = new Custom_ParkingBrake(
							connection,
							switchVariable: c.SwitchVariable,
							relayVariable: c.RelayVariable,
							toeBrakeThreshold: c.ToeBrakeThreshold
						);

						pb.ErrorLog += DisplayErrorLog;
						pb.InfoLog += DisplayInfoLog;
						DisplayInfoLog("Custom_ParkingBrake loaded (Variable-driven).");
					}
					catch (Exception ex)
					{
						DisplayErrorLog("Error loading Custom_ParkingBrake");
						DisplayErrorLog(ex.ToString());
					}
				}

				// Encoders
				if (config.PhidgetsEncoderInstances != null)
				{
					var idx = 0;
					foreach (var instance in config.PhidgetsEncoderInstances)
					{
                        var psRef = "system.encoders." + instance.ProsimDataRef;  

                        try
						{
							phidgetsEncoders[idx] = new PhidgetsEncoder(
								instance.Serial,
								instance.HubPort,
								instance.Channel,
                                psRef,
								connection
							);
							phidgetsEncoders[idx].ScaleFactor = instance.ScaleFactor;
							phidgetsEncoders[idx].ErrorLog += DisplayErrorLog;
							phidgetsEncoders[idx].InfoLog += DisplayInfoLog;
						}
						catch (Exception ex)
						{
							DisplayErrorLog("Error loading config line for Encoder");
							DisplayErrorLog(ex.ToString());
						}
						idx++;
					}
				}

                DisplayInfoLog("Prosim IP:" + config.GeneralConfig.ProSimIP);
                DisplayInfoLog("Opening outputs:" + totalOuts);

                // Use ProsimConnectionUI to display and manage the IP
                // Wait for outs to finish
                var taskDelay2 = Task.Delay((totalOuts + 10) * 40);
                await taskDelay2;
                DisplayInfoLog("Outputs loaded successfully");
                DisplayInfoLog("Connecting to Prosim");
                connectToProSim(config.GeneralConfig.ProSimIP);

            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error loading config");
                DisplayErrorLog(ex.ToString());
            }
        }

        private async Task LoadConfigInsUI()
        {
            DisplayInfoLog("Loading Inputs...");
            try
            {
                // Use InputsUI to load and bind the grid from config.yaml
                inputsUI.LoadInputsFromConfig("config.yaml");

                string yamlContent = File.ReadAllText("config.yaml");
                var deserializer = new DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yamlContent);

                if (config.PhidgetsMultiInputInstances != null)
                {
                    phidgetsMultiInputInstances = new BindingList<PhidgetsMultiInputInst>(config.PhidgetsMultiInputInstances);
                    dataGridViewMultiInputs.DataSource = phidgetsMultiInputInstances;
                    dataGridViewMultiInputs.CellEndEdit -= dataGridViewOutputs_CellEndEdit;
                    dataGridViewMultiInputs.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                }

                if (config.PhidgetsVoltageInputInstances != null)
                {
                    PhidgetsVoltageInputInstances = new BindingList<PhidgetsVoltageInputInst>(config.PhidgetsVoltageInputInstances);
                    dataGridViewVoltageIn.DataSource = PhidgetsVoltageInputInstances;
                    dataGridViewVoltageIn.CellEndEdit -= dataGridViewOutputs_CellEndEdit;
                    dataGridViewVoltageIn.CellEndEdit += dataGridViewOutputs_CellEndEdit;
                }

                // Remove the manual encoder loading since encodersUI handles it now
            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error loading input UI configs: " + ex.Message);
            }
            DisplayInfoLog("Loading Inputs Done");
        }

        private async void LoadConfigIns()
        {
            try
            {
                // Restore config variable for other sections
                string yamlContent = File.ReadAllText("config.yaml");
                var deserializer = new DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yamlContent);

                // --- Restore original instantiation of PhidgetsInput[] for runtime logic ---
                if (config.PhidgetsInputInstances != null)
                {
                    DisplayInfoLog("Starting Inputs ... ");
                    var idx = 0;
                    foreach (var instance in config.PhidgetsInputInstances)
                    {
                        try
                        {
                            var inRef = string.IsNullOrWhiteSpace(instance.ProsimDataRef)
                                ? "test"                                  // skip ProSim write, use Variable only
                                : "system.switches." + instance.ProsimDataRef;

                            phidgetsInput[idx] = new PhidgetsInput(
                                instance.Serial,
                                instance.HubPort,
                                instance.Channel,
                                connection,
                                inRef,
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
                            if (!string.IsNullOrEmpty(instance.UserVariable))
                            {
                                phidgetsInput[idx].UserVariable = instance.UserVariable;
                                DisplayInfoLog($"[WIRING] Input Hub:{instance.HubPort} Ch:{instance.Channel} UserVariable='{instance.UserVariable}'");
                            }
                            if (instance.UserVariable != null)
                            {
                                phidgetsInput[idx].UserVariable = instance.UserVariable;
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorLog("Error reloading config line");
                            DisplayErrorLog(ex.ToString());
                        }
                        idx++;
                    }
                    DisplayInfoLog("Starting Inputs done");
                }


                // MULTI INPUTS
                if (config.PhidgetsMultiInputInstances != null)
                {
                    DisplayInfoLog("Starting MultiInputs ... ");
                    var idx = 0;
                    foreach (var instance in config.PhidgetsMultiInputInstances)
                    {
                        try
                        {
							var inRef = string.IsNullOrWhiteSpace(instance.ProsimDataRef)
	                        ? "test"   // sentinel => PhidgetsInput will skip ProSim write but still mirror Variable
	                        : "system.switches." + instance.ProsimDataRef;

							phidgetsMultiInput[idx] = new PhidgetsMultiInput(
                                instance.Serial,
                                instance.HubPort,
                                instance.Channels.ToArray(),
                                connection,
                                inRef,
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

                    DisplayInfoLog("Starting MultiInputs done");
                }


                // VOLTAGE INPUTS
                if (config.PhidgetsVoltageInputInstances != null)
                {
                    DisplayInfoLog("Starting Voltage Inputs ... ");
                    var idx = 0;
                    foreach (var instance in config.PhidgetsVoltageInputInstances)
                    {
                        try
                        {

							var inRef = string.IsNullOrWhiteSpace(instance.ProsimDataRef)
	                        ? "test"   // sentinel => PhidgetsInput will skip ProSim write but still mirror Variable
	                        : "system.analog." + instance.ProsimDataRef;

							phidgetsVoltageInput[idx] = new PhidgetsVoltageInput(
                                instance.Serial,
                                instance.HubPort,
                                instance.Channel,
                                connection,
                                inRef,
                                instance.ProsimDataRefOnOff != "" ? "system.switches." + instance.ProsimDataRefOnOff : "",
                                instance.InputPoints.ToArray(),
                                instance.OutputPoints.ToArray(),
                                instance.InterpolationMode,
                                instance.CurvePower,
                                instance.DataInterval,
                                instance.MinChangeTriggerValue,
                                instance.UseRange);
                            phidgetsVoltageInput[idx].RoundUp = true;
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

                    DisplayInfoLog("Starting Voltage Inputs done");
                }

                // Buttons
                if (config.PhidgetsButtonInstances != null)
                {
                    DisplayInfoLog("Starting Buttons ... ");
                    phidgetsButtonInstances = config.PhidgetsButtonInstances != null ? new BindingList<PhidgetsButtonInst>(config.PhidgetsButtonInstances) : null;

                    var idx = 0;
                    foreach (var instance in config.PhidgetsButtonInstances)
                    {
                        try
                        {
							var inRef = string.IsNullOrWhiteSpace(instance.ProsimDataRef)
	                        ? "test"   // sentinel => PhidgetsInput will skip ProSim write but still mirror Variable
	                        : "system.switches." + instance.ProsimDataRef;

							PhidgetsButtonList.Add(new PhidgetsButton(
                                idx, 
                                instance.Name, 
                                connection, 
                                inRef,
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

                    // Add Puase
                    PhidgetsButtonList.Add(new PhidgetsButton(
                                idx,
                                "Pause",
                                connection,
                                "simulator.pause",
                                true,
                                false)
                            );

                    // Clear the FlowLayoutPanel before adding buttons
                    Invoke(new Action(() => {
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
                    }));

                    DisplayInfoLog("Starting Buttons done ");
                }
                configsInsLoaded = true;
                DisplayInfoLog("Loading Inputs configs completed successfully");
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
                            if (phidgetsInput[idx] != null)
                            {
                                phidgetsInput[idx].Close();
                            }
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
                    //phidgetsInputInstances = config.PhidgetsInputInstances != null ? new BindingList<PhidgetsInputInst>(config.PhidgetsInputInstances) : null

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
                updatePauseLabel();
            }
            else
            {
                DisplayInfoLog("Prosim DISCONNECTED");
                connectionStatusLabel.Text = "Disconnected";
                connectionStatusLabel.ForeColor = Color.Red;
            }

        }

        void updatePauseLabel()
        {
            if (connection.isConnected)
            {
                if (simIsPaused)
                {
                    DisplayInfoLog("Prosim Paused");
                    connectionStatusLabel.Text = "Paused";
                    connectionStatusLabel.ForeColor = Color.OrangeRed;
                } else
                {
                    connectionStatusLabel.Text = "Connected";
                    connectionStatusLabel.ForeColor = Color.LimeGreen;
                }
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
                        trimWheel?.Pause(simIsPaused);
                    }

                    Invoke(new MethodInvoker(updatePauseLabel));
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
            // Load Prosim IP from config
            prosimConnectionUI.LoadProsimIPFromConfig();

            // Check schema version and migrate if needed before loading config.
            // Only proceed with loading if the config is on schema 2.0.
            bool configReady = CheckAndMigrateConfig();

            if (configReady)
            {
                //  LoadConfigOuts();
                Task.Run(async () => { try { await LoadConfigOuts(); } catch (Exception ex) { DisplayErrorLog("Error loading config: " + ex.Message); } });
                Task.Run(async () => { try { await LoadConfigInsUI(); } catch (Exception ex) { DisplayErrorLog("Error loading input configs: " + ex.Message); } });

            }
            else
            {
                DisplayErrorLog("Config is still schema 1.0 – migration was not completed. Please restart and complete the hub migration before connecting.");
            }

            // Register Prosim to receive connect and disconnect events
            connection.onConnect += connection_onConnect;
            connection.onDisconnect += connection_onDisconnect;

            DataRef dataRef = new DataRef("simulator.pause", 100, connection);
            dataRef.onDataChange += DataRef_onDataChange;
        }

        /// <summary>
        /// Reads config.yaml, detects schema 1.0 and if found opens ManageHubsForm
        /// so the user can confirm/discover hub serials, then rewrites as schema 2.0.
        /// Returns true if the config is at schema 2.0 and safe to load, false otherwise.
        /// </summary>
        private bool CheckAndMigrateConfig()
        {
            try
            {
                if (!File.Exists("config.yaml")) return true; // nothing to migrate

                string yamlContent = File.ReadAllText("config.yaml");

                // Use IgnoreUnmatchedProperties so unknown YAML keys (outputs, inputs, etc.)
                // don't throw during the schema-check-only deserialization.
                var migrationDeserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();

                // Read only GeneralConfig to check the schema version safely
                var schemaCheck = migrationDeserializer.Deserialize<ConfigSchemaCheck>(yamlContent);
                string schema = schemaCheck?.GeneralConfig?.Schema ?? "1.0";

                if (schema != "1.0") return true; // Already on 2.0 or later

                DisplayInfoLog("Config schema 1.0 detected – starting migration to schema 2.0...");

                // Read V1 config (hubs are plain strings); other keys are ignored safely
                var configV1 = migrationDeserializer.Deserialize<ConfigV1>(yamlContent);

                // Convert the old string hub names to PhidgetsHubInst stubs (no serial yet)
                var stubHubs = new List<PhidgetsHubInst>();
                if (configV1.PhidgetsHubsInstances != null)
                {
                    foreach (var name in configV1.PhidgetsHubsInstances)
                    {
                        if (!string.IsNullOrWhiteSpace(name))
                            stubHubs.Add(new PhidgetsHubInst { Name = name, Serial = 0, Enabled = true });
                    }
                }

                // Open ManageHubsForm so the user can scan the network and confirm serials
                using (var form = new ManageHubsForm(stubHubs))
                {
                    form.Text = "Migrate Hubs to Schema 2.0 – Confirm / Scan for Hubs";
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        UpgradeConfigToV2(yamlContent, configV1, form.Hubs);
                        DisplayInfoLog("Config successfully migrated to schema 2.0.");
                        return true;
                    }
                    else
                    {
                        DisplayInfoLog("Hub migration cancelled by user. Config not upgraded.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error checking config schema: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Rewrites config.yaml in schema 2.0 format, replacing the GeneralConfig Schema
        /// value and the PhidgetsHubsInstances section with structured hub objects.
        /// </summary>
        private void UpgradeConfigToV2(string originalYaml, ConfigV1 configV1, List<PhidgetsHubInst> hubs)
        {
            try
            {
                // Back up the original
                File.WriteAllText("config.yaml.v1.bak", originalYaml);

                var lines = originalYaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var result = new System.Text.StringBuilder();
                bool inHubsSection = false;
                bool hubsSectionWritten = false;

                // Build the new hubs block
                var hubsBlock = new System.Text.StringBuilder();
                hubsBlock.AppendLine("PhidgetsHubsInstances:");
                foreach (var hub in hubs)
                {
                    hubsBlock.AppendLine("   - Name: " + hub.Name);
                    hubsBlock.AppendLine("     Serial: " + hub.Serial);
                    hubsBlock.AppendLine("     Enabled: " + hub.Enabled.ToString().ToLower());
                }

                foreach (string line in lines)
                {
                    string trimmed = line.TrimStart();

                    // Update schema version in place
                    if (trimmed.StartsWith("Schema:"))
                    {
                        result.AppendLine(line.Substring(0, line.IndexOf("Schema:")) + "Schema: 2.0");
                        continue;
                    }

                    if (trimmed.StartsWith("PhidgetsHubsInstances:"))
                    {
                        inHubsSection = true;
                        result.Append(hubsBlock.ToString());
                        hubsSectionWritten = true;
                        continue;
                    }

                    if (inHubsSection)
                    {
                        // Skip old (string) hub lines
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(" ") || line.StartsWith("\t"))
                            continue;
                        else
                            inHubsSection = false;
                    }

                    if (!inHubsSection)
                        result.AppendLine(line);
                }

                if (!hubsSectionWritten)
                    result.Append(hubsBlock.ToString());

                File.WriteAllText("config.yaml", result.ToString().TrimEnd() + Environment.NewLine);
                DisplayInfoLog("config.yaml upgraded to schema 2.0. Backup saved as config.yaml.v1.bak");
            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error upgrading config to schema 2.0: " + ex.Message);
            }
        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            Invoke(new MethodInvoker(UnloadConfigIns));
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

        private void btnManageHubs_Click(object sender, EventArgs e)
        {
            try
            {
                string yamlContent = File.ReadAllText("config.yaml");
                var deserializer = new DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yamlContent);

                var currentHubs = config.PhidgetsHubsInstances ?? new List<PhidgetsHubInst>();

                using (var form = new ManageHubsForm(currentHubs))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        SaveHubsToConfig(form.Hubs);
                        DisplayInfoLog("Hubs configuration saved. Restart to apply changes.");
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error opening Manage Hubs: " + ex.Message);
            }
        }

        private void SaveHubsToConfig(List<PhidgetsHubInst> hubs)
        {
            try
            {
                string content = File.ReadAllText("config.yaml");

                // Build the new hubs YAML section
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("PhidgetsHubsInstances:");
                foreach (var hub in hubs)
                {
                    sb.AppendLine("   - Name: " + hub.Name);
                    sb.AppendLine("     Serial: " + hub.Serial);
                    sb.AppendLine("     Enabled: " + hub.Enabled.ToString().ToLower());
                }

                // Find and replace the PhidgetsHubsInstances section
                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var result = new System.Text.StringBuilder();
                bool inHubsSection = false;
                bool hubsSectionWritten = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();

                    if (trimmed.StartsWith("PhidgetsHubsInstances:"))
                    {
                        inHubsSection = true;
                        result.Append(sb.ToString());
                        hubsSectionWritten = true;
                        continue;
                    }

                    if (inHubsSection)
                    {
                        // Still in the hubs section if the line is indented or empty
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith(" ") || line.StartsWith("\t"))
                        {
                            continue; // skip old hub lines
                        }
                        else
                        {
                            inHubsSection = false;
                        }
                    }

                    if (!inHubsSection)
                    {
                        result.AppendLine(line);
                    }
                }

                if (!hubsSectionWritten)
                {
                    result.Append(sb.ToString());
                }

                File.WriteAllText("config.yaml", result.ToString().TrimEnd() + Environment.NewLine);
                DisplayInfoLog("Hubs config saved to config.yaml");
            }
            catch (Exception ex)
            {
                DisplayErrorLog("Error saving hubs config: " + ex.Message);
            }
        }

        private void btnDCMotor1Go_Click(object sender, EventArgs e)
        {
            double target = Convert.ToDouble(txtDCMotor1Target.Text);
            phidgetsDCMotors[Convert.ToInt32(txtDCMotorIdx.Text)].OnTargetMoving(target);
        }

        // Inputs UI logic is now handled by InputsUI.cs
        // Helper to get current hubs for dropdown
        private List<PhidgetsHubInst> GetCurrentHubs()
        {
            try
            {
                string yamlContent = File.ReadAllText("config.yaml");
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                var config = deserializer.Deserialize<Config>(yamlContent);
                return config.PhidgetsHubsInstances ?? new List<PhidgetsHubInst>();
            }
            catch
            {
                return new List<PhidgetsHubInst>();
            }
        }
    }
}
