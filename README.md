M2MQTT for Unity
====================
This is a simple [Unity3d](http://unity3d.com/) project for using [M2MQTT](https://github.com/eclipse/paho.mqtt.m2mqtt) with Unity.
The M2MQTT library was modified to run also on UWP/HoloLens.

An example scene is provided with a UI for controlling the connection to the broker and for testing messaging.

**Requires Unity 2017.1 or higher.**

![image]()

## Installation

1. Open the Unity project you want to install this package in.
2. Open the Package Manager window.
3. Click on the `+` button and select `Add package from git URL...`.
4. Paste the URL of this repo into the text field and press `Add`. Make sure it ends with `.git`.

## Dependency

Depends on [EnhancedLogger](https://github.com/solo-fsw/sosxr-unity-enhancedlogger)

## Getting started

An example scene is provided in M2MqttUnity/Examples/Scenes/M2MqttUnity_Test, with a UI for controlling the connection to a MQTT broker and to test publishing and receiving messages.
You can find in the same folder also a scene slightly changed to test the project in VR/AR/MR (M2MqttUnity_TestXR).

### The Broker

Setup the Mosquitto broker. A good explanation on how to set this up on Linux / Raspberry Pi can be found [here](http://www.steves-internet-guide.com/install-mosquitto-linux/).

## Building on different platform and devices

This project was tested with different versions of Unity (2017.1.0, 2017.1.4, 2018.2).

### Unity - All Platforms

These setting were used for all the platforms:

* Other settings:
    * *Scripting Define Symbols* = SSL
* Resolution
    * *Default is fullscreen* = no
    * *Run in Background* = yes

### Unity - Android

* Set the *Package Name*
* for **GearVR**:
    * check Virtual Reality Supported (or XR for newer versions of Unity)
    * add *Oculus* in *Virtual Reality SDKs*
    * set *Minimum API Level* to 19
    * put your "oculussig..." file(s) in Assets/Plugins/Android/Assets

### Unity - Universal Windows Platform:

* build with Unity 2017.1.4 or newer
* set the *Package Name*
* *Other settings*: set *API Compatibility Level* to .NET 4.6
* *Publishing settings*: check *InternetClient* in *Capabilities*
* *Scripting Backend* set to .NET (deprecated in 2018.2) or IL2CPP (not for 2017.1)
* for **HoloLens**:
    * check *Virtual Reality Supported* (or XR for newer versions of Unity)
    * add *Windows Holographic* (*Windows Mixed Reality* in newer versions) in *Virtual Reality SDKs*

SSL connection problems found with some combination of Unity versions/platforms.

## (Micro)Python



### Contributing

Contributions from you are welcome!

If you find bugs or you have any new idea for improvements and new features you can raise an issue on GitHub (please follow the suggested template, filling the proper sections).

### License

Released under the [MIT License](https://github.com/gpvigano/M2MqttUnity/blob/master/LICENSE.txt).

The included (slightly modified) [M2MQTT](https://github.com/eclipse/paho.mqtt.m2mqtt) library is licensed under [Eclipse Public License 1.0](https://github.com/eclipse/paho.mqtt.m2mqtt/blob/master/LICENSE).