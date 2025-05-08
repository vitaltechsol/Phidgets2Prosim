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

List each hub device server name. Found in Phidgets control panel

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

**Properties:**

- `Offset`: Adjust position offset.
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
    DirtyUp: 1
    DirtyDown: 0.8
    CleanUp: 0.6
    CleanDown: 0.6
    APOnClean: 0.7
    APOnDirty: 0.5
```

**Properties:**

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



