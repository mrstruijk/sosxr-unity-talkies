using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SOSXR.SeaShark;
using UnityEngine;
using HeaderAttribute = SOSXR.SeaShark.HeaderAttribute;
using ButtonAttribute = SOSXR.SeaShark.ButtonAttribute;
using Random = UnityEngine.Random;


namespace SOSXR.Talkies
{
    /// <summary>
    ///     Controls GPIO pins on a Raspberry Pi Pico (or compatible device) connected via serial.
    ///     Communicates using a simple CSV command protocol: <c>SET,&lt;pin&gt;,&lt;0|1&gt;</c> and
    ///     <c>GET,&lt;pin&gt;</c>. Responses are parsed and surfaced via <see cref="OnPinSetEvent"/>
    ///     and <see cref="OnPinGetEvent"/>.
    ///     <para>
    ///         Requires an <see cref="ISerialConnect"/> component on the same GameObject.
    ///         Consider also adding <see cref="SafetyPin"/> to automatically drive pins LOW
    ///         after a configurable timeout.
    ///     </para>
    /// </summary>
    [RequireComponent(typeof(ISerialConnect))]
    public class PinController : MonoBehaviour
    {
        [Header("Pin Control")]
        [SerializeField] [Range(0, 29)] private int m_defaultPin = 16;

        [Header("Debug")]
        [SerializeField] private bool m_debugToggleLED = true;
        [SerializeField] [ShowIf(nameof(m_debugToggleLED))] private Vector2 m_debugToggleRange = new(0.1f, 0.5f);
        [SerializeField] private int m_desiredBaud;
        [SerializeField] [DisableEditing] private int _currentBaud;

        private readonly StringBuilder receiveBuffer = new();
        private readonly byte[] readBuffer = new byte[1024];

        private readonly int _ledPin = 25;

        private readonly List<int> _pinList = new();

        private ISerialConnect _connector;

        /// <summary>Fired after a SET command confirmation is received, with the pin number and the new value.</summary>
        public event Action<int, bool> OnPinGetEvent;
        /// <summary>Fired when a GET response is received, reporting the pin number and its current value.</summary>
        public event Action<int, bool> OnPinSetEvent;


        [DllImport("SerialPlugin")]
        private static extern int SerialSetBaud(int baud);


        [DllImport("SerialPlugin")]
        private static extern int SerialWrite(byte[] data, int length);


        [DllImport("SerialPlugin")]
        private static extern int SerialRead(byte[] buffer, int bufferSize);


        [DllImport("SerialPlugin")]
        private static extern int SerialWrite2(byte pos, byte speed);


        [DllImport("SerialPlugin")]
        private static extern int SerialRead2(out byte pos, out byte speed);


        private void OnValidate()
        {
            _connector ??= GetComponent<ISerialConnect>();
        }


        private void Start()
        {
            if (GetComponent<SafetyPin>() == null)
            {
                Debug.LogWarning($"You're running this without {nameof(SafetyPin)}. Is that wise?");
            }

            if (m_debugToggleLED && m_debugToggleRange != Vector2.zero)
            {
                StartCoroutine(DebugToggleCR());
            }
        }


        private IEnumerator DebugToggleCR()
        {
            for (;;)
            {
                var duration = Random.Range(m_debugToggleRange.x, m_debugToggleRange.y);

                yield return new WaitForSeconds(duration);

                ToggleLED();
            }
        }


        private void Update()
        {
            // SetBaud(m_desiredBaud);

            ReadBuffer();
        }


        [Button(space: 10, horizontalLine: true)]
        private void SetBaud(int baud)
        {
            if (!_connector.IsConnected)
            {
                Debug.LogWarning("We're not connected! Cannot continue");

                return;
            }

            var ok = SerialSetBaud(baud);

            if (ok == 1)
            {
                // Debug.Log("Baud successfully updated to " + baud);
            }
            else
            {
                Debug.LogError("Failed to set baud to " + baud);
            }
        }


        private void ReadBuffer()
        {
            if (!_connector.IsConnected)
            {
                Debug.LogWarning("We're not connected! Cannot continue");

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
                Debug.LogError($"Pico Error: {response}");

                return;
            }

            if (command == "SET" && parts.Length >= 4)
            {
                var pin = int.Parse(parts[2]);
                var value = int.Parse(parts[3]);
                var boolValue = value == 1;
                OnPinSet(pin, boolValue);
            }
            else if (command == "GET" && parts.Length >= 4)
            {
                var pin = int.Parse(parts[2]);
                var value = int.Parse(parts[3]);
                var boolValue = value == 1;
                OnPinGet(pin, boolValue);
            }
            else // This should include the PING/PONG debug response
            {
                Debug.Log($"Pico: {response}");
            }
        }


        private void SendCommand(string command)
        {
            if (!_connector.IsConnected)
            {
                Debug.LogWarning($"Not connected. Cannot send: {command}");

                return;
            }

            var fullCommand = command + "\n";
            var data = Encoding.ASCII.GetBytes(fullCommand);
            var written = SerialWrite(data, data.Length);

            if (written != data.Length)
            {
                Debug.LogError($"Write failed. Sent {written}/{data.Length} bytes for command: {command}");
            }
            else
            {
                // Debug.Log($"Sent: {command}");
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
                Debug.Log($"LED toggled from {HighLow(currentValue)} to {HighLow(newValue)}");
            });
        }


        /// <summary>
        ///     Sends a SET command for the default pin configured in the Inspector.
        /// </summary>
        /// <param name="value"><c>true</c> sets the pin HIGH; <c>false</c> sets it LOW.</param>
        [Button(space: 10, horizontalLine: true)]
        public void SetDefaultPin(bool value)
        {
            SendCommand($"SET,{m_defaultPin},{(value ? 1 : 0)}");
        }


        /// <summary>Sends a GET command for the default pin, which triggers <see cref="OnPinGetEvent"/> when the response arrives.</summary>
        [Button]
        public void GetDefaultPin()
        {
            SendCommand($"GET,{m_defaultPin}");
        }


        /// <summary>Reads the current value of the default pin, then sets it to the opposite state.</summary>
        [Button]
        public void ToggleDefaultPin()
        {
            TogglePin(m_defaultPin, (pin, currentValue) =>
            {
                var newValue = !currentValue;
                SetPin(pin, newValue);
                Debug.Log($"Toggled pin {pin} from {HighLow(currentValue)} to {HighLow(newValue)}");
            });
        }


        /// <summary>
        ///     Sets a specific GPIO pin HIGH or LOW. Tracks the pin internally so it can be driven
        ///     LOW on disable.
        /// </summary>
        /// <param name="pin">GPIO pin number on the target device.</param>
        /// <param name="value"><c>true</c> = HIGH, <c>false</c> = LOW.</param>
        [Button(space: 10, horizontalLine: true)]
        public void SetPin(int pin, bool value)
        {
            if (!_pinList.Contains(pin))
            {
                _pinList.Add(pin);
            }

            SendCommand($"SET,{pin},{(value ? 1 : 0)}");
        }


        /// <summary>
        ///     Sends a GET command for the given pin. The response arrives asynchronously via <see cref="OnPinGetEvent"/>.
        /// </summary>
        /// <param name="pin">GPIO pin number to query.</param>
        [Button]
        public void GetPin(int pin)
        {
            SendCommand($"GET,{pin}");
        }


        /// <summary>
        ///     Reads the current value of the specified pin, then sets it to the opposite state.
        /// </summary>
        /// <param name="pin">GPIO pin number to toggle.</param>
        [Button]
        public void TogglePin(int pin)
        {
            TogglePin(pin, (p, currentValue) =>
            {
                var newValue = !currentValue;


                SetPin(pin, newValue);
                Debug.Log($"Toggled pin {pin} from {HighLow(currentValue)} to {HighLow(newValue)}");
            });
        }


        private void OnPinSet(int pin, bool value)
        {
            // Debug.Log($"We asked pin {pin} to be set to {HighLow(value)}.");
            OnPinSetEvent?.Invoke(pin, value);
        }


        private void OnPinGet(int pin, bool value)
        {
            // Debug.Log($"Device states that pin {pin} is now {HighLow(value)}.");
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

            StopAllCoroutines();
        }
    }
}
