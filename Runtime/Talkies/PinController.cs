using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SOSXR.EnhancedLogger;
using UnityEngine;
using Button = SOSXR.SeaShark.ButtonAttribute;


[RequireComponent(typeof(SerialConnector))]
public class PinController : MonoBehaviour
{
    [UnityEngine.Header("Pin Control")]
    [SerializeField] [Range(0, 29)] private int m_defaultPin = 16;

    [HideInInspector] [SerializeField] private SerialConnector m_serialConnector;

    private readonly StringBuilder receiveBuffer = new();
    private readonly byte[] readBuffer = new byte[1024];

    private readonly int _ledPin = 25;

    private readonly List<int> _pinList = new();

    public event Action<int, bool> OnPinGetEvent;
    public event Action<int, bool> OnPinSetEvent;


    [DllImport("SerialPlugin")]
    private static extern int SerialWrite(byte[] data, int length);


    [DllImport("SerialPlugin")]
    private static extern int SerialRead(byte[] buffer, int bufferSize);


    private void OnValidate()
    {
        if (m_serialConnector == null)
        {
            m_serialConnector = GetComponent<SerialConnector>();
        }
    }


    private void Update()
    {
        ReadBuffer();
    }


    private void ReadBuffer()
    {
        if (!m_serialConnector.IsConnected)
        {
            this.Warning("We're not connected! Cannot continue");

            return;
        }

        var bytesRead = SerialRead(readBuffer, readBuffer.Length);

        if (bytesRead <= 0)
        {
            return;
        }

        var chunk = Encoding.ASCII.GetString(readBuffer, 0, bytesRead);
        receiveBuffer.Append(chunk);

        var bufferStr = receiveBuffer.ToString();
        int newlineIndex;

        while ((newlineIndex = bufferStr.IndexOf('\n')) >= 0)
        {
            var line = bufferStr.Substring(0, newlineIndex).Trim();

            if (line.Length > 0)
            {
                ProcessResponse(line);
            }

            bufferStr = bufferStr.Substring(newlineIndex + 1);
        }

        receiveBuffer.Clear();
        receiveBuffer.Append(bufferStr);
    }


    private void ProcessResponse(string response)
    {
        var parts = response.Split(',');

        if (parts.Length < 2)
        {
            return;
        }

        var status = parts[0];
        var command = parts[1];

        if (status == "ERR")
        {
            this.Error($"Pico Error: {response}");

            return;
        }

        if (command == "SET" && parts.Length >= 4)
        {
            var pin = int.Parse(parts[2]);
            var value = int.Parse(parts[3]);
            OnPinSet(pin, value == 1);
        }
        else if (command == "GET" && parts.Length >= 4)
        {
            var pin = int.Parse(parts[2]);
            var value = int.Parse(parts[3]);
            OnPinGet(pin, value == 1);
        }
        else // This should include the PING/PONG debug response
        {
            this.Debug($"Pico: {response}");
        }
    }


    private void SendCommand(string command)
    {
        if (!m_serialConnector.IsConnected)
        {
            this.Warning($"Not connected. Cannot send: {command}");

            return;
        }

        var fullCommand = command + "\n";
        var data = Encoding.ASCII.GetBytes(fullCommand);
        var written = SerialWrite(data, data.Length);

        if (written != data.Length)
        {
            this.Error($"Write failed. Sent {written}/{data.Length} bytes for command: {command}");
        }
        else
        {
            this.Verbose($"Sent: {command}");
        }
    }


    /// <summary>
    ///     Basic debug method.
    /// </summary>
    [Button(space: 10, horizontalLine: true)]
    public void Ping()
    {
        SendCommand("PING");
    }


    /// <summary>
    ///     Handy debug method to toggle the onboard LED. Also demonstrates reading pin state before acting on it.
    /// </summary>
    [Button]
    public void ToggleLED()
    {
        TogglePin(_ledPin, (pin, currentValue) =>
        {
            var newValue = !currentValue;
            SetPin(pin, newValue);
            this.Debug($"LED toggled from {HighLow(currentValue)} to {HighLow(newValue)}");
        });
    }


    [Button(space: 10, horizontalLine: true)]
    public void SetDefaultPin(bool value)
    {
        SendCommand($"SET,{m_defaultPin},{(value ? 1 : 0)}");
    }


    [Button]
    public void GetDefaultPin()
    {
        SendCommand($"GET,{m_defaultPin}");
    }


    [Button]
    public void ToggleDefaultPin()
    {
        TogglePin(m_defaultPin, (pin, currentValue) =>
        {
            var newValue = !currentValue;
            SetPin(pin, newValue);
            this.Success($"Toggled pin {pin} from {HighLow(currentValue)} to {HighLow(newValue)}");
        });
    }


    [Button(space: 10, horizontalLine: true)]
    public void SetPin(int pin, bool value)
    {
        if (!_pinList.Contains(pin))
        {
            _pinList.Add(pin);
        }

        SendCommand($"SET,{pin},{(value ? 1 : 0)}");
    }


    [Button]
    public void GetPin(int pin)
    {
        SendCommand($"GET,{pin}");
    }


    [Button]
    public void TogglePin(int pin)
    {
        TogglePin(pin, (p, currentValue) =>
        {
            var newValue = !currentValue;


            SetPin(pin, newValue);
            this.Success($"Toggled pin {pin} from {HighLow(currentValue)} to {HighLow(newValue)}");
        });
    }


    private void OnPinSet(int pin, bool value)
    {
        this.Verbose($"We asked pin {pin} to be set to {HighLow(value)}.");
        OnPinSetEvent?.Invoke(pin, value);
    }


    private void OnPinGet(int pin, bool value)
    {
        this.Verbose($"Device states that pin {pin} is now {HighLow(value)}.");
        OnPinGetEvent?.Invoke(pin, value);
    }


    /// <summary>
    ///     This method gets the current pin value, and then creates a small handler which will get invoked when the OnPinGetEvent is invoked: thus when we know the current value.
    ///     You can then set the value of the pin to the opposite that it was previously. Example:
    ///     GetAndSetPin(_ledPin, (pin, currentValue) => { SetPin(pin, !currentValue); });
    /// </summary>
    /// <param name="pin"></param>
    /// <param name="callback"></param>
    private void TogglePin(int pin, Action<int, bool> callback)
    {
        OnPinGetEvent += handler;
        GetPin(pin);

        return;


        void handler(int p, bool val)
        {
            if (p == pin)
            {
                callback?.Invoke(p, val);
                OnPinGetEvent -= handler;
            }
        }
    }


    /// <summary>
    ///     Small helper method that renames TRUE/FALSE to HIGH/LOW, which is more in line with GPIO-lingo.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private string HighLow(bool value)
    {
        return value ? "HIGH" : "LOW";
    }


    private void OnDisable()
    {
        foreach (var pin in _pinList)
        {
            SetPin(pin, false);
        }
    }
}