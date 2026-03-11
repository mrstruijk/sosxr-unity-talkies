# Talkies

- By: Maarten R. Struijk Wilbrink
- For: Leiden University SOSXR
- Fully open source: Feel free to add to, or modify, anything you see fit.

A Unity package for communicating with external devices. Talkies covers two communication channels:

- **Serial** — USB serial to desktop or Android, with GPIO pin control for Raspberry Pi Pico-class devices.
- **MQTT** — A Unity-friendly wrapper around the M2MQTT library for publish/subscribe messaging over a broker.

## Table of Contents

- [Installation](#installation)
- [Dependencies](#dependencies)
- [Serial Communication](#serial-communication)
  - [Connecting](#connecting)
  - [Controlling GPIO pins](#controlling-gpio-pins)
  - [ByteSize — raw byte protocol](#bytesize--raw-byte-protocol)
- [MQTT](#mqtt)
  - [Quick start](#quick-start)
  - [Subscribe](#subscribe)
  - [Publish](#publish)
  - [Unsubscribe](#unsubscribe)
  - [Connection events](#connection-events)
- [Inline Documentation](#inline-documentation)
- [Contributing](#contributing)
- [License](#license)

## Installation

1. Open the Unity project you want to install this package in.
2. Open the Package Manager window.
3. Click on the `+` button and select `Add package from git URL...`.
4. Paste the URL of this repo into the text field and press `Add`. Make sure it ends with `.git`.

## Dependencies

- [EnhancedLogger](https://github.com/solo-fsw/sosxr-unity-enhancedlogger)
- [Sea Shark](https://github.com/solo-fsw/sosxr-unity-seashark)

### MQTT

The MQTT transport layer is a direct clone of [gpvigano's M2MqttUnity package](https://github.com/gpvigano/M2MqttUnity). Many thanks to their great work. See also the MQTT-specific `README.md` in `Runtime/MQTT/`.

---

## Serial Communication

### Connecting

Two connector implementations are provided, both implementing `ISerialConnect`:

| Component | Target platform | Native layer |
|---|---|---|
| `DesktopSerialConnector` | macOS / Windows | `SerialPlugin` native DLL |
| `AndroidSerialConnector` | Android | Java bridge (`com.sosxr.serial.SerialBridge`) |

Add **one** of these MonoBehaviours to a GameObject alongside the other serial components.

#### DesktopSerialConnector

```
Inspector fields:
  Selected Port Index  — index into the auto-detected port list
  Strip Bluetooth      — filter Bluetooth ports from the list (macOS)
  Baud Rate            — set automatically by ConnectInCommandMode / ConnectInDataMode
```

The component auto-detects available ports on `Awake`/`OnValidate` and connects in **command mode** (4800 baud) at `Start`. Use the Inspector buttons or call from code:

```csharp
desktopConnector.ConnectInCommandMode(); // 4800 baud
desktopConnector.ConnectInDataMode();    // 115200 baud
desktopConnector.Disconnect();
desktopConnector.RefreshPorts();         // re-scan available ports
```

> **Note:** Close Arduino IDE, Thonny, or any other serial monitor before connecting from Unity — they claim exclusive access to the port.

#### AndroidSerialConnector

Connects at a fixed 115200 baud via the Java `SerialBridge` class. Add the component to a GameObject and call `Connect()` / `Disconnect()` as needed, or use the Inspector buttons.

---

### Controlling GPIO pins

Add `PinController` alongside an `ISerialConnect` component. `PinController` communicates with the target device using a simple CSV command protocol:

```
SET,<pin>,<0|1>   →  drive pin LOW or HIGH
GET,<pin>         →  request current pin state
PING              →  connectivity check
```

Responses from the device are parsed and surfaced as C# events:

```csharp
pinController.OnPinSetEvent += (pin, value) => Debug.Log($"Pin {pin} confirmed {value}");
pinController.OnPinGetEvent += (pin, value) => Debug.Log($"Pin {pin} is {value}");
```

Public API:

```csharp
pinController.SetPin(pin: 16, value: true);   // drive HIGH
pinController.GetPin(pin: 16);                // query (async — result via OnPinGetEvent)
pinController.TogglePin(pin: 16);             // read then invert
pinController.Ping();                         // connectivity check
```

The default pin can also be configured in the Inspector and controlled via `SetDefaultPin`, `GetDefaultPin`, and `ToggleDefaultPin`.

#### SafetyPin

Add `SafetyPin` to the same GameObject as `PinController`. It listens for `OnPinSetEvent` and automatically drives any pin back LOW after a configurable timeout (default: 30 s). This prevents components (e.g. solenoids, LEDs) from being left energised indefinitely.

#### PinTimer

`PinTimer` provides a single-shot pulse: it drives a pin HIGH for a configured duration (0.15–5 s), then LOW. If a pulse is already in progress when triggered again, the active pulse is cancelled and the pin is driven LOW.

---

### ByteSize — raw byte protocol

`ByteSize` is a lower-level serial component for devices that expect single- or double-byte ASCII-encoded commands (e.g. an Arduino Leonardo with a custom firmware). It uses the same `SerialPlugin` DLL as `DesktopSerialConnector`.

```csharp
byteSize.SendCommand('p');            // single byte
byteSize.SendCommand('p', 's');       // position + speed
byteSize.SendCommand("ps");           // equivalent two-char string form
byteSize.Unlock();                    // sends 'u'
byteSize.Stop();                      // sends 's'
byteSize.Info();                      // sends 'i'
byteSize.Version();                   // sends 'v'
byteSize.ReadBuffer();                // read one byte and log it
```

The `ASCIITable` utility class handles char ↔ byte conversions, throwing `ArgumentOutOfRangeException` for non-ASCII input.

---

## MQTT

### Quick start

1. Add `MQTTClient` to a GameObject in your scene (one instance only — it enforces a singleton).
2. Configure the broker address, port, and optional credentials in the Inspector.
3. Enable **Auto Connect** to connect on startup, or call `MQTTClient.Instance.Connect()` manually.

### Subscribe

```csharp
// Subscribe with a callback — called on the main thread for every matching message
MQTTClient.Subscribe(OnMessage, topic: "myapp/sensor");

void OnMessage(string topic, byte[] payload)
{
    var text = System.Text.Encoding.UTF8.GetString(payload);
    Debug.Log($"{topic}: {text}");
}
```

Topic filters follow standard MQTT wildcard rules:
- `+` matches a single level (`myapp/+/temperature`)
- `#` matches everything at and below a level (`myapp/#`)

Subscriptions made before the broker connects are queued and flushed automatically on connection.

### Publish

```csharp
// String overload — UTF-8 encoded automatically
MQTTClient.Publish("myapp/command", "start");

// Byte array overload
MQTTClient.Publish("myapp/data", myByteArray);
```

Publications made before the broker connects are also queued and flushed on connection.

### Unsubscribe

```csharp
MQTTClient.Unsubscribe(OnMessage, topic: "myapp/sensor");
```

When the last callback for a topic is removed, the broker subscription is cancelled too.

### Connection events

```csharp
MQTTClient.OnConnected    += () => Debug.Log("MQTT connected");
MQTTClient.OnDisconnected += () => Debug.Log("MQTT disconnected");
```

Both events are invoked on the main thread.

---

## Inline Documentation

All public classes, interfaces, and methods include XML documentation comments. Open any source file or hover over a symbol in your IDE to read the docs.

## Contributing

If you find any issues or have suggestions for improvements, feel free to open an issue or submit a pull request.

## License

Under the MIT License. See the [LICENSE](LICENSE.md) file for more information.
