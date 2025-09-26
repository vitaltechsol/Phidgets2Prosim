# Installation and Configuration Guide for Phidgets2Prosim

## Overview

This manual provides detailed instructions for installing Phidgets2Prosim and updating the `config.yaml` file to configure various Phidget device instances. Each instance type corresponds to a specific phidget device.

## [Installation Guide](#installation-guide)

1. **Download the Software**

   - Visit the [GitHub Releases](https://github.com/vitaltechsol/Phidgets2Prosim/releases) page. (Don't need to download the source files)
   - Download the latest `.zip` file.

2. **Install the Software**

   - Unzip the downloaded file to a desired location.

3. **Configure the Software**

   - Locate the `config.yaml` file in the installation folder.
   - Edit the `config.yaml` file to match your device configuration.
   - Use `config_sample.yaml` as a reference for more detailed configuration examples.

4. **Run the Software**

   - Execute `Phidgets2Prosim.exe` to start the application.


## Config.yaml General Structure

This guide covers the essential structure and properties for configuring Phidget devices in `config.yaml`. Adjust values based on your system requirements.

The `config.yaml` file consists of several sections:

- `GeneralConfig`: General settings for the ProSim connection.
- `PhidgetsHubsInstances`: Lists all hub devices.
- Various `Phidgets` device sections for different types of instances.

##  Prosim datarefs list
 
[Prosim Data Refs List](https://docs.google.com/spreadsheets/d/1CSBgNBA1x6DjuY8paAMT7_TFc9DFpK48wiUeUQj5Lz8/edit)

## Index

- [Installation Guide](#installation-guide)
- [General Configuration](#general-configuration)
- [Hub Instances](#hub-instances)
- [Phidgets Inputs](#phidgetsinputinst)
- [Phidgets Multi-Inputs](#phidgetsmultiinputinst)
- [Phidgets Output](#phidgetsoutputinst)
- [Phidgets Gates](#phidgetsgateinst)
- [Phidgets Audio Gates](#phidgetsaudioinst)
- [Phidgets BL DC Motors](#phidgetsbldcmotorinst)
- [Phidgets Voltage Outputs (For gauges)](#phidgetsvoltageoutputinst)
- [Phidgets Voltage Input (For pots)](#phidgetsvoltageinputinst)
- [Custom Trim Wheel](#customtrimwheelinst)
- [Phidgets Buttons](#phidgetsbuttoninst)
- [Validation](#validation)


### Example Basic Configuration. Single input and Single Output

```yaml
GeneralConfig:
  Schema: 1.0    
  ProSimIP: 127.0.0.1
PhidgetsHubsInstances:
   - hub5000-1

PhidgetsOutputInstances:
  - Serial: 668522
    HubPort: 1
    Channel: 9
    ProsimDataRef: I_MIP_GEAR_NOSE_DOWN

PhidgetsInputInstances:
  - Serial: 618534
    HubPort: 2
    Channel: 0
    ProsimDataRef: S_FIRE_FAULT_TEST
    InputValue: 1
```

## [General Configuration](#general-configuration)

```yaml
GeneralConfig:
  Schema: 1.0
  ProSimIP: <ProSim_IP_Address>
  OutputBlinkFastIntervalMs: 300
  OutputBlinkSlowIntervalMs: 600
  OutputDefaultDimValue: 0.7
```

- `Schema`: Version of the configuration schema (DO NOT CHANGE).
- `ProSimIP`: IP address for the ProSim connection. Update with your prosim server IP.
- `OutputBlinkFastIntervalMs`: (Optional) Default value used for fast blinking outputs in milliseconds. Default is 300.
- `OutputBlinkSlowIntervalMs`: (Optional) Default value used for slow blinking outputs in milliseconds. Default is 600.
- `OutputDefaultDimValue`:  (Optional) Default value used for dim output state, when not specified in the output itself. Default is 0.7 (0 is full dim, 1 is full bright)

## [Hub Instances](#hub-instances)

```yaml
PhidgetsHubsInstances:
  - hub5000-1
  - hub5000-2
```

List each hub device server name. Found in Phidgets control panel. You can change the hub name using the Phidgets hub web interface and use a more descriptive name. Hubs are initialized here but each instace uses the Serial number to talk to the hub. These do not need to be reference again. But they must be initialized.

## [Phidgets Inputs](#phidgetsinputinst)

```yaml
PhidgetsInputInstances:
  - Serial: 618534
    HubPort: 2
    Channel: 0
    ProsimDataRef: S_FIRE_FAULT_TEST
    InputValue: 1
    OffInputValue: 0

    ProsimDataRef2: S_FIRE_SECONDARY
    ProsimDataRef3: S_FIRE_TERTIARY
```

**Properties:**

- `Serial`: Phidget device serial number.
- `HubPort`: Hub port number (-1 for USB).
- `Channel`: Channel number.
- `ProsimDataRef`: Data reference for ProSim.
- `InputValue`: Value sent when input is active.
- `OffInputValue`: Value sent when input is inactive.
- `ProsimDataRef2/3`: Additional optional data references that will turn on of off with the input.

## [Phidgets Multi-Inputs](#phidgetsMultiinputinst)

```yaml
PhidgetsMultiInputInstances:
  - Serial: 668015
    HubPort: 0
    Channels: [14, 15]
    ProsimDataRef: S_OH_APU
    Mappings:
      "01": 2
      "10": 0
      "00": 1
```

**Properties:**

- `Channels`: List of channels grouped together.
- `Mappings`: Key-value pairs for input combinations.


## [Phidgets Outputs](#phidgetsoutputinst)

```yaml
PhidgetsOutputInstances:
  - Serial: 668522
    HubPort: 1
    Channel: 9
    ProsimDataRef: I_MIP_GEAR_NOSE_DOWN

    DelayOn: 500
    Inverse: true
    DelayOff: 300
    ProsimDataRefOff: I_MIP_GEAR_NOSE_UP
```

**Properties:**

- `Serial`: Phidget device serial number.
- `HubPort`: Hub port number (-1 for USB).
- `Channel`: Channel number.
- `ProsimDataRef`: Data reference for ProSim.
 
- `Inverse`: (optional) Inverts the on/off behavior.
- `DelayOn`: (optional) Delay in milliseconds before turning on.
- `MaxTimeOn`: (optional) Maximum time to stay on in milliseconds before turning off.
- `ProsimDataRefOff`: (optional) Alternate data reference to turn off.
- `ValueOn`: (optional) Value when prosim output is on (1 is 100%), default is 1
- `ValueOff`: (optional) Value when prosim output is off (0 is 0%), default is 0
- `ValueDim`: (optional) Value when prosim output is dim (0.7 is 70%), default is 0.7

## [Phidgets Gates](#phidgetsgateinst)

```yaml
PhidgetsGateInstances:
  - Serial: 668066
    HubPort: 4
    Channel: 1
    ProsimDataRef: B_STICKSHAKER_FO
```

**Properties:** Same as `PhidgetsOutputInstances`. But used for ProSim Gate refs

## [Phidgets Audio Gates](#phidgetsaudioinst)

```yaml
PhidgetsAudioInstances:
  - Serial: 618534
    HubPort: 5
    Channel: 8
    ProsimDataRef: "Gear_warning"
```

Same properties as `PhidgetsOutputInstances`. But used for ProSim Audio gate refs



## [Phidgets BL DC Motors](#phidgetsbldcmotorinst)

```yaml
PhidgetsBLDCMotorInstances:
  - Serial: 668066
    HubPort: 0
    Reversed: false
    Offset: 0
    RefTurnOn: system.gates.B_THROTTLE_SERVO_POWER_RIGHT
    RefCurrentPos: system.analog.A_THROTTLE_RIGHT
    RefTargetPos: system.gauge.G_THROTTLE_RIGHT
    Acceleration: 0.8
```
In aditon to the above basic parameters, you can use the following PID parameters(optional) to fine tune the motors motion profile, 
detailed description of these parameters can be found at the end of this document.
```
    MaxVelocity: 0.20
    MinVelocity: 0.02
    VelocityBand: 250.0
    CurveGamma: 0.55
    DeadbandEnter: 2.0
    DeadbandExit: 4.0
    MaxVelStepPerTick: 0.008
    Kp: 0.0
    Ki: 0.0
    Kd: 0.0
    IOnBand: 8.0
    IntegralLimit: 0.3
    PositionFilterAlpha: 0.30
    TickMs: 20
```

**Properties:**

- `Offset`: Adjust position offset. If one Motor is not synced with the other, a little offset can even them out.
- `Reversed`: Invert motor direction.
- `RefTurnOn`: ProSim ref to activate motor.
- `RefCurrentPos`: Current position ref.
- `RefTargetPos`: Target position ref.
- `Acceleration`: Speed acceleration (0.1-1.0).

## [Phidgets Voltage Output](#phidgetsvoltageoutputinst)

```yaml
PhidgetsVoltageOutputInstances:
  - Serial: 668522
    HubPort: 2
    ScaleFactor: 500
    Offset: 0
    ProsimDataRef: G_MIP_BRAKE_PRESSURE
```

**Properties:**

- `ScaleFactor`: Adjusts ProSim input to analogue voltage output.
- `Offset`: Offset applied to the output.

## [Phidgets Voltage Input](#phidgetsvoltageinputinst)

*Voltage ration input is used here**

```yaml
PhidgetsVoltageInputInstances: 
  - Serial: 742112
    HubPort: 5
    Channel: 0
    ProsimDataRef: A_ASP2_VHF_1_VOLUME

  - Serial: 742112
    HubPort: 5
    Channel: 1
    InputPoints: [0, 0.2, 0.4, 0.8, 1]
    OutputPoints: [0,100, 500, 800, 1024]
    DataInterval: 20
    InterpolationMode: Curve
    CurvePower: 3.0
    ProsimDataRef: A_ASP2_VHF_2_VOLUME
    MinChangeTriggerValue: 0.005
```

**Properties:**

- `InputPoints`: (optional) Array of input values to interpolate from phidgets. The default is `[0, 1]` This means the minimum input value from phidgets is 0, and max is 1. The range in between can have up to 4 decimal points. for example `[0, 0.2543, 1]`  
- `OutputPoints`: (optional) Array of output values to interpolate to prosim. The default is `[0, 255]` This means the minimum input value sent to prosim is 0, and max is 255. The output is while integer numbers  `[0, 100, 255]`  .
- `DataInterval`: (optional) How long to wait to send the next data in ms. The default is 50. Minimum is 20. Lower values sends data faster. 
- `InterpolationMode`: (optional) `Linear`, `Curve`, or `Spline`. Default is `Linear`
- `CurvePower`: (optional). Applies exponential curve between points using a power factor. Default is 2.0
- `MinChangeTriggerValue`: (optional) Minimum value detected to send data. Default is 0.002. This mean value needs to change by at least 0.002 for it to show up. Use higher values to filter out noise.

## [Custom Trim Wheel](#customtrimwheelinst)

```yaml
CustomTrimWheelInstance:
    Serial: 742112
    Channel: 5
    DirtyUp: 1
    DirtyDown: 0.8
    CleanUp: 0.6
    CleanDown: 0.6
    APOnClean: 0.7
    APOnDirty: 0.5
```
Values are for speed, where 1 is 100% and 0 is 0%. Up and down speeds are different because of the air forces it takes to move the trim tab.

**Properties:**
- `Channel`: Use this for the hub port
- `DirtyUp/Down`: Speed with dirty configuration.
- `CleanUp/Down`: Speed with clean configuration.
- `APOnClean/Dirty`: Speed under autopilot.

## [Phidgets Button](#phidgetsbuttoninst)

```yaml
PhidgetsButtonInstances:
  - Name: Door Unlock
    ProsimDataRef: S_FLIGHT_DEK_DOOR
    InputValue: 2
    OffInputValue: 0
```

**Properties:**

- `Name`: Button name.
- `InputValue`: Value when pressed.
- `OffInputValue`: Value when released.

## [Validation](#validation)

Ensure each section follows the YAML format correctly, with proper indentation and spacing. Restart the application after any configuration changes to apply updates.

## BLDC PID Tuning Guide

Motor tuning parameters (short descriptions)

- `MaxVelocity` (0–1)
Top speed the controller will command when the position error is large. Higher = faster moves, more risk of overshoot.

- `MinVelocity` (0–1)
Smallest speed used when close to target (to overcome stiction). Too high causes buzz/dither; too low can stall.

- `VelocityBand` (position units)
Error size at which the controller reaches MaxVelocity. Smaller band = more aggressive (higher speed for the same error).

- `CurveGamma` (≈0.5–1.5)
Shapes how speed grows as error increases. Lower = softer near zero (gentler approach), higher = more bite near zero.

- `DeadbandEnter` (position units)
If |error| falls below this, the loop declares “settled” and stops the motor. Tighten only after you’re stable.

- `DeadbandExit` (position units)
Must be > DeadbandEnter. If |error| rises above this, motion resumes. The gap provides hysteresis (prevents chatter).

- `MaxVelStepPerTick` (0–1 per tick)
Software slew-rate limit on velocity commands (extra smoothing on top of hardware acceleration). Lower = smoother.

- `Kp` (proportional gain)
Adds push proportional to error. Too high → overshoot/oscillation; too low → residual error.

- `Ki` (integral gain)
Accumulated push to remove steady bias. Use tiny values; enable only after P is stable. Can introduce slow oscillation if too high.

- `Kd` (derivative gain, optional)
Damps changes by reacting to error rate. Helpful for overshoot; noisy signals limit its usefulness. Keep very small.

- `IOnBand` (position units, optional)
Only allow the integrator to act when |error| is above this band. Prevents integral “pumping” at the setpoint.

- `IntegralLimit` (0–1)
Anti-windup clamp for the integral term. Smaller = safer; loosen only if you need more integral authority.

- `PositionFilterAlpha` (0–1)
EMA smoothing of measured position:
filtered = alpha*new + (1-alpha)*prev.
Higher alpha = less smoothing (more responsive, more noise). Lower alpha = more smoothing (more lag, less noise).

- `TickMs` (ms)
Control loop period. Lower value (faster loop) = quicker response but can require smaller MaxVelStepPerTick.

- `Acceleration` (0-1 hardware)
Device-level ramp on velocity changes. Acts like built-in slew. Combine with MaxVelStepPerTick for very smooth motion.

- `TargetBrakingStrength` (0–1, hardware)
How aggressively the device resists motion when stopping. Higher = snappier stop; too high can induce jerk.

#######################################
A simple, repeatable tuning workflow
#######################################

Goal: first make it stable and smooth, then accurate, then fast.

0) Establish a smooth baseline

Set: Kp = 0, Ki = 0, Kd = 0 (no PID yet).

Use conservative values:
MaxVelocity 0.20, MinVelocity 0.02–0.03,
VelocityBand 200–300, CurveGamma 0.55–0.65,
DeadbandEnter 3, DeadbandExit 6,
MaxVelStepPerTick 0.004–0.008,
PositionFilterAlpha 0.18–0.30,
TickMs 20, Acceleration 0.3–1.0.

Verify: long and short moves are smooth, no hunting at rest. If there’s dither, lower MinVelocity, increase DeadbandEnter/Exit, or lower CurveGamma.

1) Improve accuracy (P then tiny I)

Add small P: start Kp = 0.0003.
If still short of target, try 0.0004. If it overshoots, back down.

Add tiny I only if a small steady error remains: start Ki = 0.00008–0.00012.
Set IntegralLimit = 0.12–0.18.
If supported, set IOnBand ≈ a bit wider than DeadbandExit (e.g., 8–12) so I only works when you’re meaningfully away from target.

2) Trim the stop window

Once stable and accurate, narrow DeadbandEnter (e.g., from 3 → 2.5 → 2.0). Keep DeadbandExit larger (e.g., 6) to preserve hysteresis.

3) Make it feel more responsive (optional)

Reduce VelocityBand slightly (e.g., 250 → 200) and/or raise CurveGamma a touch (0.60 → 0.65) for more authority near zero.

If motion gets edgy, undo one change or lower MaxVelStepPerTick.

4) Use D only if needed (optional)

If you still see overshoot with clean signals, try very small Kd (e.g., 0.001–0.002). If noise causes twitching, remove Kd and rely on slew + Acceleration.

Quick “if/then” cheat sheet

Wiggle near setpoint → lower MinVelocity; widen DeadbandEnter/Exit; reduce Ki; lower CurveGamma; increase PositionFilterAlpha; reduce MaxVelStepPerTick; lower Acceleration.

Stops short → raise Kp a bit; slightly reduce VelocityBand; add tiny Ki with small IntegralLimit.

Overshoot on long moves → lower Kp; lower IntegralLimit or Ki; increase MaxVelStepPerTick smoothing (or raise hardware Acceleration slightly); optionally add tiny Kd.

Feels sluggish → increase MaxVelocity; reduce VelocityBand; raise CurveGamma slightly; reduce filtering (PositionFilterAlpha up a bit).

Buzzing/stiction → raise MinVelocity slightly; ensure hardware Acceleration isn’t too low; keep MaxVelStepPerTick reasonable (not too tiny).

########################################
Typical starting presets (copy/paste)
########################################

Smooth & safe (good first try)
```
MaxVelocity: 0.20
MinVelocity: 0.02
VelocityBand: 250
CurveGamma: 0.60
DeadbandEnter: 3.0
DeadbandExit: 6.0
MaxVelStepPerTick: 0.005
Kp: 0.0003
Ki: 0.00010
Kd: 0.0
IOnBand: 10.0
IntegralLimit: 0.15
PositionFilterAlpha: 0.22
TickMs: 20
```

Faster but still civil
```
MaxVelocity: 0.30
MinVelocity: 0.022
VelocityBand: 200
CurveGamma: 0.65
DeadbandEnter: 2.5
DeadbandExit: 6.0
MaxVelStepPerTick: 0.006
Kp: 0.00035
Ki: 0.00010
Kd: 0.0 (add 0.001 only if needed)
IOnBand: 10–12
IntegralLimit: 0.15
PositionFilterAlpha: 0.20
TickMs: 20
```


