using System;
using System.Collections.Generic;
using System.Linq;
using ProSimSDK;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Instantiates every Phidgets runtime object from a loaded <see cref="Config"/>.
    /// The service owns the arrays and exposes them as read-only properties so that
    /// the form and other components can read telemetry counts without duplicating state.
    /// </summary>
    internal class PhidgetsDeviceService
    {
        // ── Public device arrays ─────────────────────────────────────────────────

        public PhidgetsOutput[]        Outputs       { get; } = new PhidgetsOutput[360];
        public PhidgetsOutput[]        Gates         { get; } = new PhidgetsOutput[360];
        public PhidgetsInput[]         Inputs        { get; } = new PhidgetsInput[360];
        public PhidgetsMultiInput[]    MultiInputs   { get; } = new PhidgetsMultiInput[360];
        public PhidgetsVoltageInput[]  VoltageInputs { get; } = new PhidgetsVoltageInput[360];
        public PhidgetsVoltageOutput[] VoltageOutputs{ get; } = new PhidgetsVoltageOutput[100];
        public PhidgetsBLDCMotor[]     BLDCMotors    { get; } = new PhidgetsBLDCMotor[10];
        public PhidgetsDCMotor[]       DCMotors      { get; } = new PhidgetsDCMotor[10];
        public PhidgetsEncoder[]       Encoders      { get; } = new PhidgetsEncoder[20];
        public List<PhidgetsButton>    Buttons       { get; } = new List<PhidgetsButton>();

        /// <summary>Custom trim-wheel instance — null until LoadOutputDevices is called.</summary>
        public Custom_TrimWheel TrimWheel { get; private set; }

        /// <summary>Custom parking-brake instance — null until LoadOutputDevices is called.</summary>
        public Custom_ParkingBrake ParkingBrake { get; private set; }

        // ── Runtime blink / dim settings (populated during LoadOutputs) ──────────

        public int    BlinkFastIntervalMs { get; internal set; } = 300;
        public int    BlinkSlowIntervalMs { get; internal set; } = 600;
        public double DefaultDimValue     { get; internal set; } = 0.7;

        // ── Events ───────────────────────────────────────────────────────────────

        public event Action<string> InfoLog;
        public event Action<string> ErrorLog;

        // ── Private state ────────────────────────────────────────────────────────

        private readonly ProSimConnect _connection;

        // ── Constructor ──────────────────────────────────────────────────────────

        public PhidgetsDeviceService(ProSimConnect connection)
        {
            _connection = connection;
        }

        // ── Load output-side devices (called at startup, before ProSim connect) ──

        public void LoadOutputDevices(Config config)
        {
            if (config?.GeneralConfig != null)
            {
                if (config.GeneralConfig.OutputBlinkFastIntervalMs > 0)
                    BlinkFastIntervalMs = config.GeneralConfig.OutputBlinkFastIntervalMs;
                if (config.GeneralConfig.OutputBlinkSlowIntervalMs > 0)
                    BlinkSlowIntervalMs = config.GeneralConfig.OutputBlinkSlowIntervalMs;
                if (config.GeneralConfig.OutputDefaultDimValue > 0)
                    DefaultDimValue = config.GeneralConfig.OutputDefaultDimValue;
            }

            LoadGates(config);
            LoadOutputs(config);
            LoadAudioOutputs(config);
            LoadVoltageOutputs(config);
            LoadBLDCMotors(config);
            LoadDCMotors(config);
            LoadEncoders(config);
            LoadButtons(config);
            LoadTrimWheel(config);
            LoadParkingBrake(config);
        }

        // ── Load input-side devices (called after ProSim connect) ────────────────

        public void LoadInputDevices(Config config)
        {
            LoadInputs(config);
            LoadMultiInputs(config);
            LoadVoltageInputs(config);
        }

        // ── Unload (disconnect) ──────────────────────────────────────────────────

        public void UnloadInputDevices()
        {
            for (int i = 0; i < Inputs.Length;        i++) { Inputs[i]?.Close();        Inputs[i] = null; }
            for (int i = 0; i < MultiInputs.Length;   i++) { MultiInputs[i]?.Close();   MultiInputs[i] = null; }
            for (int i = 0; i < VoltageInputs.Length; i++) { VoltageInputs[i]?.Close(); VoltageInputs[i] = null; }
        }

        // ── Private load helpers ─────────────────────────────────────────────────

        private void LoadGates(Config config)
        {
            if (config?.PhidgetsGateInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsGateInstances)
            {
                try
                {
                    string outRef = string.IsNullOrWhiteSpace(inst.ProsimDataRef)
                        ? "test"
                        : "system.gates." + inst.ProsimDataRef;
                    Gates[idx] = new PhidgetsOutput(
                        inst.Serial, inst.HubPort, inst.Channel, outRef, _connection, true,
                        inst.ProsimDataRefOff  != null ? "system.gates." + inst.ProsimDataRefOff  : null,
                        inst.ProsimDataRef2    != null ? "system.gates." + inst.ProsimDataRef2    : null,
                        inst.Operator);
                    Gates[idx].ErrorLog += RaiseError;
                    Gates[idx].InfoLog  += RaiseInfo;
                    if (inst.Inverse == true)                             Gates[idx].Inverse    = true;
                    if (inst.DelayOn    != null && inst.DelayOn    > 0)  Gates[idx].Delay      = (int)inst.DelayOn;
                    if (inst.MaxTimeOn  != null && inst.MaxTimeOn  > 0)  Gates[idx].MaxTimeOn  = (int)inst.MaxTimeOn;
                }
                catch (Exception ex) { RaiseError("Error loading gate config: " + ex.Message); }
                idx++;
            }
        }

        private void LoadOutputs(Config config)
        {
            if (config?.PhidgetsOutputInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsOutputInstances)
            {
                try
                {
                    string outRef = string.IsNullOrWhiteSpace(inst.ProsimDataRef)
                        ? "test"
                        : "system.indicators." + inst.ProsimDataRef;
                    Outputs[idx] = new PhidgetsOutput(
                        inst.Serial, inst.HubPort, inst.Channel, outRef, _connection, false,
                        inst.ProsimDataRefOff != null ? "system.indicators." + inst.ProsimDataRefOff : null);
                    Outputs[idx].ErrorLog += RaiseError;
                    Outputs[idx].InfoLog  += RaiseInfo;
                    Outputs[idx].BlinkFastIntervalMs = BlinkFastIntervalMs;
                    Outputs[idx].BlinkSlowIntervalMs = BlinkSlowIntervalMs;
                    if (inst.Inverse == true)                            Outputs[idx].Inverse   = true;
                    if (inst.DelayOn   != null && inst.DelayOn   > 0)   Outputs[idx].Delay     = (int)inst.DelayOn;
                    if (inst.MaxTimeOn != null && inst.MaxTimeOn > 0)   Outputs[idx].MaxTimeOn = (int)inst.MaxTimeOn;
                    if (inst.ValueOff  != 0)                            Outputs[idx].ValueOff  = inst.ValueOff;
                    if (inst.ValueOn   != 1)                            Outputs[idx].ValueOn   = inst.ValueOn;
                    if (inst.ValueDim  != DefaultDimValue)              Outputs[idx].ValueDim  = inst.ValueDim;
                    if (!string.IsNullOrEmpty(inst.UserVariable))
                    {
                        Outputs[idx].UserVariable = inst.UserVariable;
                        RaiseInfo($"[WIRING] Output Hub:{inst.HubPort} Ch:{inst.Channel} UserVariable='{inst.UserVariable}'");
                    }
                }
                catch (Exception ex) { RaiseError("Error loading output config: " + ex.Message); }
                idx++;
            }
        }

        private void LoadAudioOutputs(Config config)
        {
            if (config?.PhidgetsAudioInstances == null) return;
            // Audio outputs share the Outputs array starting from index 0 of the audio list
            int idx = 0;
            foreach (var inst in config.PhidgetsAudioInstances)
            {
                try
                {
                    Outputs[idx] = new PhidgetsOutput(
                        inst.Serial, inst.HubPort, inst.Channel,
                        "system.audio." + inst.ProsimDataRef, _connection, false,
                        inst.ProsimDataRefOff != null ? "system.audio." + inst.ProsimDataRefOff : null);
                    Outputs[idx].ErrorLog += RaiseError;
                    Outputs[idx].InfoLog  += RaiseInfo;
                    if (inst.DelayOn   != null && inst.DelayOn   > 0) Outputs[idx].Delay     = (int)inst.DelayOn;
                    if (inst.MaxTimeOn != null && inst.MaxTimeOn > 0) Outputs[idx].MaxTimeOn = (int)inst.MaxTimeOn;
                }
                catch (Exception ex) { RaiseError("Error loading audio config: " + ex.Message); }
                idx++;
            }
        }

        private void LoadVoltageOutputs(Config config)
        {
            if (config?.PhidgetsVoltageOutputInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsVoltageOutputInstances)
            {
                try
                {
                    VoltageOutputs[idx] = new PhidgetsVoltageOutput(
                        inst.Serial, inst.HubPort,
                        "system.gauge." + inst.ProsimDataRef, _connection);
                    var vo = VoltageOutputs[idx];
                    vo.ScaleFactor            = inst.ScaleFactor;
                    vo.Offset                 = inst.Offset;
                    vo.Interval               = inst.Interval;
                    vo.SmoothFactor           = inst.SmoothFactor;
                    vo.UseSinCos              = inst.UseSinCos;
                    vo.AmplitudeVolts         = inst.AmplitudeVolts;
                    vo.WrapDegrees360         = inst.WrapDegrees360;
                    vo.SwapSinCos             = inst.SwapSinCos;
                    vo.InvertSin              = inst.InvertSin;
                    vo.InvertCos              = inst.InvertCos;
                    vo.SmoothAngleStep        = inst.SmoothAngleStep;
                    vo.TargetUpdateIntervalMs = inst.TargetUpdateIntervalMs;
                    vo.TargetFilterAlpha      = inst.TargetFilterAlpha;
                    vo.DeadbandDegrees        = inst.DeadbandDegrees;
                    vo.CosSerial              = inst.CosSerial;
                    vo.CosHubPort             = inst.CosHubPort;
                    vo.SinChannel             = inst.SinChannel;
                    vo.CosChannel             = inst.CosChannel;
                    vo.ErrorLog              += RaiseError;
                    vo.InfoLog               += RaiseInfo;
                    _ = vo.Open();
                }
                catch (Exception ex) { RaiseError("Error loading voltage output config: " + ex.Message); }
                idx++;
            }
        }

        private void LoadBLDCMotors(Config config)
        {
            if (config?.PhidgetsBLDCMotorInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsBLDCMotorInstances)
            {
                try
                {
                    var opts = new MotorTuningOptions
                    {
                        MaxVelocity       = inst.Options.MaxVelocity,
                        MinVelocity       = inst.Options.MinVelocity,
                        VelocityBand      = inst.Options.VelocityBand,
                        CurveGamma        = inst.Options.CurveGamma,
                        DeadbandEnter     = inst.Options.DeadbandEnter,
                        DeadbandExit      = inst.Options.DeadbandExit,
                        MaxVelStepPerTick = inst.Options.MaxVelStepPerTick,
                        Kp                = inst.Options.Kp,
                        Ki                = inst.Options.Ki,
                        Kd                = inst.Options.Kd,
                        IOnBand           = inst.Options.IOnBand,
                        IntegralLimit     = inst.Options.IntegralLimit,
                    };
                    BLDCMotors[idx] = new PhidgetsBLDCMotor(
                        inst.Serial, inst.HubPort, _connection,
                        inst.Reversed, inst.Offset,
                        inst.RefTurnOn, inst.RefCurrentPos, inst.RefTargetPos,
                        inst.Acceleration, opts, inst.RefTurnOn2);
                    BLDCMotors[idx].ErrorLog += RaiseError;
                    BLDCMotors[idx].InfoLog  += RaiseInfo;
                }
                catch (Exception ex) { RaiseError("Error loading BLDC motor config: " + ex.Message); }
                idx++;
            }
        }

        private void LoadDCMotors(Config config)
        {
            if (config?.PhidgetsDCMotorInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsDCMotorInstances)
            {
                try
                {
                    var opts = new MotorTuningOptions
                    {
                        MaxVelocity         = inst.Options?.MaxVelocity,
                        MinVelocity         = inst.Options?.MinVelocity,
                        VelocityBand        = inst.Options?.VelocityBand,
                        CurveGamma          = inst.Options?.CurveGamma,
                        DeadbandEnter       = inst.Options?.DeadbandEnter,
                        DeadbandExit        = inst.Options?.DeadbandExit,
                        MaxVelStepPerTick   = inst.Options?.MaxVelStepPerTick,
                        Kp                  = inst.Options?.Kp,
                        Ki                  = inst.Options?.Ki,
                        Kd                  = inst.Options?.Kd,
                        IOnBand             = inst.Options?.IOnBand,
                        IntegralLimit       = inst.Options?.IntegralLimit,
                        PositionFilterAlpha = inst.Options?.PositionFilterAlpha,
                        TickMs              = inst.Options?.TickMs,
                        SetpointSlewPerTick = inst.Options?.SetpointSlewPerTick,
                    };

                    DCMotors[idx] = new PhidgetsDCMotor(
                        inst.Serial, inst.HubPort, _connection, options: opts)
                    {
                        Reversed              = inst.Reversed,
                        CurrentLimit          = inst.CurrentLimit,
                        Acceleration          = inst.Acceleration > 0 ? inst.Acceleration : 50,
                        TargetBrakingStrength = 1,
                    };
                    DCMotors[idx].ErrorLog += RaiseError;
                    DCMotors[idx].InfoLog  += RaiseInfo;

                    // Optional voltage-input feedback (position sensor)
                    if (inst.VoltageInput != null)
                    {
                        var voltageIn = new PhidgetsVoltageInput(
                            inst.VoltageInput.Serial,
                            inst.VoltageInput.HubPort,
                            inst.VoltageInput.Channel,
                            _connection,
                            prosimDataRef:      "",
                            prosimDataRefOnOff: "",
                            inputPoints:        inst.VoltageInput.InputPoints.ToArray(),
                            outputPoints:       inst.VoltageInput.OutputPoints.ToArray());
                        voltageIn.MinChangeTriggerValue = inst.VoltageInput.MinChangeTriggerValue;
                        voltageIn.ErrorLog += RaiseError;
                        voltageIn.InfoLog  += RaiseInfo;
                        DCMotors[idx].VoltageInput = voltageIn;
                    }

                    DCMotors[idx].InitializeAsync().Wait();

                    // Subscribe to ProSim target ref if configured
                    if (!string.IsNullOrWhiteSpace(inst.RefTargetPos))
                        DCMotors[idx].UseRefTarget(inst.RefTargetPos);

                    idx++;
                }
                catch (Exception ex)
                {
                    RaiseError("Error loading DC motor config (index " + idx + "): " + ex.Message);
                    idx++;
                }
            }
        }

        private void LoadEncoders(Config config)
        {
            if (config?.PhidgetsEncoderInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsEncoderInstances)
            {
                try
                {
                    string encRef = "system.encoders." + inst.ProsimDataRef;
                    Encoders[idx] = new PhidgetsEncoder(
                        inst.Serial, inst.HubPort, inst.Channel,
                        encRef, _connection);
                    Encoders[idx].ScaleFactor  = inst.ScaleFactor;
                    Encoders[idx].ErrorLog    += RaiseError;
                    Encoders[idx].InfoLog     += RaiseInfo;
                }
                catch (Exception ex) { RaiseError("Error loading encoder config: " + ex.Message); }
                idx++;
            }
        }

        private void LoadButtons(Config config)
        {
            if (config?.PhidgetsButtonInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsButtonInstances)
            {
                try
                {
                    string btnRef = string.IsNullOrWhiteSpace(inst.ProsimDataRef)
                        ? "test"
                        : "system.switches." + inst.ProsimDataRef;
                    var b = new PhidgetsButton(
                        idx, inst.Name, _connection,
                        btnRef, inst.InputValue, inst.OffInputValue);
                    b.ErrorLog += RaiseError;
                    b.InfoLog  += RaiseInfo;
                    Buttons.Add(b);
                    idx++;
                }
                catch (Exception ex) { RaiseError("Error loading button config: " + ex.Message); }
            }
            // Always add a Pause button so the user can toggle simulator pause
            try
            {
                var pause = new PhidgetsButton(idx, "Pause", _connection, "simulator.pause", true, false);
                pause.ErrorLog += RaiseError;
                pause.InfoLog  += RaiseInfo;
                Buttons.Add(pause);
            }
            catch (Exception ex) { RaiseError("Error adding Pause button: " + ex.Message); }
        }

        private void LoadTrimWheel(Config config)
        {
            var inst = config?.CustomTrimWheelInstance;
            if (inst == null) return;
            try
            {
                TrimWheel = new Custom_TrimWheel(
                    inst.Serial,
                    inst.HubPort,
                    _connection,
                    inst.Reversed,
                    inst.DirtyUp,
                    inst.DirtyDown,
                    inst.CleanUp,
                    inst.CleanDown,
                    inst.APOnDirty,
                    inst.APOnClean,
                    inst.Accelerate,
                    inst.Range != null ? inst.Range.ToArray() : new double[] { -1.0, 1.0 },
                    inst.Encoder);
                TrimWheel.ErrorLog += RaiseError;
                TrimWheel.InfoLog  += RaiseInfo;
                RaiseInfo("TrimWheel loaded (serial " + inst.Serial + ").");
            }
            catch (Exception ex)
            {
                RaiseError("Error loading TrimWheel config: " + ex.Message);
            }
        }

        private void LoadParkingBrake(Config config)
        {
            var inst = config?.CustomParkingBrakeInstance;
            if (inst == null) return;
            try
            {
                ParkingBrake = new Custom_ParkingBrake(
                    _connection,
                    switchVariable:             inst.SwitchVariable,
                    relayVariable:              inst.RelayVariable,
                    toeBrakeThreshold:          inst.ToeBrakeThreshold,
                    releaseToeBrakeThreshold:   inst.ReleaseToeBrakeThreshold,
                    releaseVariable:            inst.ReleaseVariable,
                    releaseDelayOnMs:           inst.ReleaseDelayOnMs,
                    releaseMaxTimeOnMs:         inst.ReleaseMaxTimeOnMs);
                ParkingBrake.ErrorLog += RaiseError;
                ParkingBrake.InfoLog  += RaiseInfo;
                RaiseInfo("Custom_ParkingBrake loaded (Variable-driven).");
            }
            catch (Exception ex)
            {
                RaiseError("Error loading Custom_ParkingBrake: " + ex.Message);
            }
        }

        private void LoadInputs(Config config)
        {
            if (config?.PhidgetsInputInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsInputInstances)
            {
                try
                {
                    string inRef = string.IsNullOrWhiteSpace(inst.ProsimDataRef)
                        ? "test"
                        : "system.switches." + inst.ProsimDataRef;
                    Inputs[idx] = new PhidgetsInput(
                        inst.Serial, inst.HubPort, inst.Channel,
                        _connection, inRef,
                        inst.InputValue, inst.OffInputValue);
                    Inputs[idx].ErrorLog += RaiseError;
                    Inputs[idx].InfoLog  += RaiseInfo;
                    if (!string.IsNullOrEmpty(inst.ProsimDataRef2))
                        Inputs[idx].ProsimDataRef2 = inst.ProsimDataRef2;
                    if (!string.IsNullOrEmpty(inst.ProsimDataRef3))
                        Inputs[idx].ProsimDataRef3 = inst.ProsimDataRef3;
                    if (!string.IsNullOrEmpty(inst.UserVariable))
                    {
                        Inputs[idx].UserVariable = inst.UserVariable;
                        RaiseInfo("[WIRING] Input Hub:" + inst.HubPort + " Ch:" + inst.Channel + " UserVariable='" + inst.UserVariable + "'");
                    }
                }
                catch (Exception ex) { RaiseError("Error loading input config: " + ex.Message); }
                idx++;
            }
            RaiseInfo("Inputs loaded (" + idx + ").");
        }

        private void LoadMultiInputs(Config config)
        {
            if (config?.PhidgetsMultiInputInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsMultiInputInstances)
            {
                try
                {
                    string inRef = string.IsNullOrWhiteSpace(inst.ProsimDataRef)
                        ? "test"
                        : "system.switches." + inst.ProsimDataRef;
                    MultiInputs[idx] = new PhidgetsMultiInput(
                        inst.Serial, inst.HubPort, inst.Channels.ToArray(),
                        _connection, inRef, inst.Mappings);
                    MultiInputs[idx].ErrorLog += RaiseError;
                    MultiInputs[idx].InfoLog  += RaiseInfo;
                }
                catch (Exception ex) { RaiseError("Error loading multi-input config: " + ex.Message); }
                idx++;
            }
            RaiseInfo("Multi-inputs loaded (" + idx + ").");
        }

        private void LoadVoltageInputs(Config config)
        {
            if (config?.PhidgetsVoltageInputInstances == null) return;
            int idx = 0;
            foreach (var inst in config.PhidgetsVoltageInputInstances)
            {
                try
                {
                    string viRef = string.IsNullOrWhiteSpace(inst.ProsimDataRef)
                        ? "test"
                        : "system.analog." + inst.ProsimDataRef;
                    string viOnOffRef = !string.IsNullOrEmpty(inst.ProsimDataRefOnOff)
                        ? "system.switches." + inst.ProsimDataRefOnOff
                        : "";
                    VoltageInputs[idx] = new PhidgetsVoltageInput(
                        inst.Serial, inst.HubPort, inst.Channel, _connection,
                        viRef, viOnOffRef,
                        inst.InputPoints.ToArray(), inst.OutputPoints.ToArray(),
                        inst.InterpolationMode, inst.CurvePower,
                        inst.DataInterval, inst.MinChangeTriggerValue, inst.UseRange);
                    VoltageInputs[idx].RoundUp   = true;
                    VoltageInputs[idx].ErrorLog += RaiseError;
                    VoltageInputs[idx].InfoLog  += RaiseInfo;
                }
                catch (Exception ex) { RaiseError("Error loading voltage input config: " + ex.Message); }
                idx++;
            }
            RaiseInfo("Voltage inputs loaded (" + idx + ").");
        }

        // ── Logging helpers ──────────────────────────────────────────────────────

        private void RaiseInfo(string msg)  => InfoLog?.Invoke(msg);
        private void RaiseError(string msg) => ErrorLog?.Invoke(msg);
    }
}
