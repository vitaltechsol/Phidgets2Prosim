# Phidgets2Prosim

This application allows using many of of the VINT Phidgtes modules with Prosim-AR 737 for Microsoft Flight Simulator and P3D

While Prosim has native support for Phidgets I wanted some additional custom functionality that is currently not available.


## VINT Modules supported and features

### Outputs

- On/Off
- Inverse 
- Use a different Prosim data ref to turn off value. 
- Delay: Turn on or off after n amount of milliseconds

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

