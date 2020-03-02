# VTOL VR Telemetry Mod
Hello and welcome!

This is a very simple mod used to gather various telemetry regarding the player's flight. This data is then sent via a UDP socket out to a waiting application (Data logger, Motion Simulator, etc).

Current Telemetry Data Available:
* Heading
* Pitch
* Roll
* X/Y/Z Acceleration

Default Settings:
Destination IP: 127.0.0.1
Destination Port: 4123

# SimTools
This data can be fed to a motion chair, I've put together a plugin for SimTools here:
https://github.com/nebriv/VTOLVR-SimTools
