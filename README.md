# VAM Sync

**VAM Plugin that adds support for stroking/vibrating devices thru Buttplug.io**

Download Latest: [v3](https://github.com/intiface/VaMSync/releases/)

## IMPORTANT SUPPORT NOTE

The current maintainer of VaMSync is also the maintainer of [Buttplug.io](https://buttplug.io), and does not use VaM. While filing of issues is appeciated, few if any bugs on the VaM side will be able to be addressed by the buttplug.io team.

If you are interested in helping with VaM-side issues, please comment either in the issues or the discussions area!

## VAM Modes

### Oscillate Mode

[Instructions](Docs/OscilateMode.md)

Oscillate mode simply tells the device to move up and down at a certain speed, what makes this mode even more powerful is the ability to set an optional target AnimationPattern; VAMLaunch will take over this pattern and automatically adjust it's play time and speed to match the oscillation.

<img src="Docs/Images/osc_mode.gif" width="600"/>

### AnimationPattern Mode

[Instructions](Docs/AnimationPatternMode.md)

AnimationPattern mode works like oscillation mode but in reverse. Instead of an oscillation driving a motion and pattern, a target pattern is driving the motion directly.

<img src="Docs/Images/pattern_mode.gif" width="600"/>

### • Influence Zone Mode (Experimental)

[Instructions](Docs/InfluenceZoneMode.md)

Influence Zone Mode: The original mode is still here with some additional quality of life improvements. Zone mode enables motion by analysing the motion of an atom within a zone of influence. This mode is the least accurate of the three modes, but it is the most adaptable.

<img src="Docs/Images/zone_mode.gif" width="600"/>

### • New trigger actions
You can now make it easier for users of your scene to control their device through three new triggers:
- startLaunch
- stopLaunch
- toggleLaunch


## Installation

Inside the .zip you will find two things:

- The VAMLaunch server installer: vamlaunch-installer.exe
- The VAM Plugin ("VAMLaunch" folder)

Start by running the installation program, you can install this to anywhere on your computer.

<img src="Docs/Images/installer.PNG" width="300"/>

Once this is installed, copy the VAMLaunch plugin folder to this location:

YOUR_VAM_LOCATION/Saves/Scripts

## Making sure your Launch device is ready

I highly suggest following the instructions [here](https://github.com/FredTungsten/ScriptPlayer/wiki/Installation)
and testing your device with ScriptPlayer first to confirm you have everything
set up correctly.

If your device can connect to ScriptPlayer then it is highly likely it will work
with this plugin.

## Starting The Plugin

VAMLaunch can be loaded onto any Atom in VAM. Simply select an Atom (In this case we have created a sphere Atom)

Go to the Plugins tab and press "Add Plugin", select the ADD_ME.cslist file found in:

"YOUR_VAM_LOCATION/Saves/Scripts/VAMLaunch" (If you followed the above installing instructions)

<img src="Docs/Images/loadplugin.PNG" width="600"/>

## Plugin Menu

To open the plugin menu press "Open Custom UI" next to the VAMLaunch plugin in the list.

You will be shown this interface:

<img src="Docs/Images/options.PNG" width="400"/>

In the above image, the area marked in red contains the main options, and they will remain visible regardless of what mode VAMLaunch is in:

- "Pause Launch": By default your device should begin paused, simply untick this to begin sending messages to the device. (You can also use the "startLaunch", "stopLaunch" and "toggleLaunch" triggers to control this value from an in game button for example).
- "Simulator": This slider is moved automatically, its position represents a guess of how your real device will behave when interacting with VAM. This is very useful for fine tuning your scenes to get the most accurate motions out of the device.
- "Motion Source": Here you can select different motion sources for VAMLaunch, choosing one will change what is in this menu to Motion Source specific features.

The rest of the options are explained in the instructions for each Motion Source Mode:
- [Oscillate Mode](Docs/OscilateMode.md)
- [AnimationPattern Mode](Docs/AnimationPatternMode.md)
- [Influence Zone Mode](Docs/InfluenceZoneMode.md)

## Support The Project

If you enjoy using VAMLaunch, you can support the project through PayPal:

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.me/zengineering)

Thank you!

## License

Buttplug and VAMLaunch are distributed under the BSD 3-Clause License.

[View Full Licence](LICENSE)
