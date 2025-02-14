# Installation and Configuration Guide for Phidgets2Prosim

## Overview

This manual provides detailed instructions for installing Phidgets2Prosim and updating the `config.yaml` file to configure various Phidget device instances. Each instance type corresponds to a specific phidget device.

## [Installation Guide](#installation-guide)

1. **Download the Software**

   - Visit the [GitHub Releases](https://github.com/vitaltechsol/Phidgets2Prosim/releases) page.
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
```

- `Schema`: Version of the configuration schema (DO NOT CHANGE).
- `ProSimIP`: IP address for the ProSim connection. Update with your prosim server IP

## [Hub Instances](#hub-instances)

```yaml
PhidgetsHubsInstances:
  - hub5000-1
  - hub5000-2
```

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
- 


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
 
- `DelayOn`: (optional) Delay in milliseconds before turning on.
- `Inverse`: (optional) Inverts the on/off behavior.
- `DelayOff`: (optional) Delay before turning off.
- `ProsimDataRefOff`: (optional) Alternate data reference to turn off.

## [Phidgets Gates](#phidgetsgateinst)

```yaml
PhidgetsGateInstances:
  - Serial: 668066
    HubPort: 4
    Channel: -1
    ProsimDataRef: B_STICKSHAKER_FO
    DelayOn: 1000
    Inverse: true
    ProsimDataRefOff: B_STICKSHAKER_OFF
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



