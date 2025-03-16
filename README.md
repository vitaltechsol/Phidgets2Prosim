# Phidgets2Prosim

This application allows using many of of the  Phidgets VINT modules with Prosim-AR 737 for Microsoft Flight Simulator and P3D

While Prosim has native support for Phidgets I wanted some additional custom functionality that is currently not available specially when using OEM panels.

1. **Installation and Manual**

   - Visit the [Manual and Installation page](https://github.com/vitaltechsol/Phidgets2Prosim/tree/master/manual#readme) page.


## VINT Modules supported and features

### Outputs / Gates

- On/Off
- Inverse logic
- Use a different Prosim data ref to turn off value. 
- Delay: Turn on after n amount of milliseconds
- Maximum Time On: Turn off after n amount off milliseconds
- Fast and Slow Blink: Detects blinking state from prosim, makes output blink
- Individual brightness control for On, Off and Dim

### Inputs
- Inputs
- Inverse
- Truth table: Use a combination of inputs to trigger a single input. For example input 1 needs to be on, input 2 needs to be off, input 3 needs to be on. This is commonly used in OEM components

### DC Motors
- Forward movement by data ref
- Backwards movement by data ref
- Speed control

### Brushless DC Motors
- On/Off
- Current position control 
- Target position control

### Voltage Output
- Control OEM gauges/indicators that support analogue voltage

### Custom trim wheel
- Use a DC motor to control trim wheel
- Different speeds for all modes (clean config, dirty config, auto pilot on or off)
- Programmatic break/stop

