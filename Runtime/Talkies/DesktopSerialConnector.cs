using System;
using System.IO;
using System.Runtime.InteropServices;
using SOSXR.SeaShark;
using UnityEngine;
using ButtonAttribute = SOSXR.SeaShark.ButtonAttribute;


namespace SOSXR.Talkies
{
    /// <summary>
    ///     Baud rate presets supported by the native <c>SerialPlugin</c>.
    ///     Each value maps to a byte index that the plugin uses internally.
    ///     <list type="bullet">
    ///         <item><description><c>B4800</c> — command mode baud rate (used by <see cref="DesktopSerialConnector.ConnectInCommandMode"/>).</description></item>
    ///         <item><description><c>B115200</c> — high-speed data mode baud rate (used by <see cref="DesktopSerialConnector.ConnectInDataMode"/>).</description></item>
    ///     </list>
    /// </summary>
    public enum BaudRate : byte
    {
        B4800 = 0,
        B9600 = 1,
        B19200 = 2,
        B38400 = 3,
        B57600 = 4,
        B115200 = 5,
        TestBaud = 10
    }


    /// <summary>
    ///     Connects to a desktop serial device (macOS or Windows) via the native <c>SerialPlugin</c> DLL.
    ///     Auto-detects available ports on startup and connects in command mode (4800 baud) by default.
    ///     Implements <see cref="ISerialConnect"/> for use alongside <see cref="PinController"/>.
    /// </summary>
    public class DesktopSerialConnector : MonoBehaviour, ISerialConnect
    {
        [DisableEditing] [SerializeField] private string[] m_availablePorts = Array.Empty<string>();
        [SerializeField] private int m_selectedPortIndex = 0;
        [DisableEditing] [SerializeField] private string m_portName = "COM3";
        [DisableEditing] [SerializeField] private bool m_isConnected = false;
        [SerializeField] [DisableEditing] private BaudRate m_baudRate = BaudRate.B4800;
        [SerializeField] private bool m_stripBluetooth = true;

        public bool IsConnected => m_isConnected;


        // [Button]
        /// <summary>
        ///     Opens the serial connection on the currently selected port.
        ///     Calls <see cref="Disconnect"/> first if a connection is already open.
        ///     Does nothing in Edit mode.
        /// </summary>
        public void Connect()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (m_isConnected)
            {
                Disconnect(); // Close any existing connection
            }

            if (string.IsNullOrEmpty(m_portName))
            {
                Debug.LogWarning("No port selected. Run RefreshPorts() first.");

                return;
            }

            try
            {
                // Debug.Log($"Connecting to {m_portName}");
                var result = SerialOpen(m_portName, (byte) m_baudRate, false);

                if (result == 1)
                {
                    m_isConnected = true;
                    Debug.Log($"Connected to device on {m_portName} with baud rate {m_baudRate}");
                }
                else
                {
                    m_isConnected = false;
                    Debug.LogError($"Failed to open {m_portName}. Check connection. Is another debugger / IDE open (e.g. Thonny / Arduino IDE)?");
                }
            }
            catch (Exception ex)
            {
                m_isConnected = false;
                Debug.LogError("Is another debugger / IDE open (e.g. Thonny / Arduino IDE)?");
                Debug.LogError($"Exception while connecting: {ex.Message}");
                SerialClose();
            }
        }


        [Button]
        /// <summary>
        ///     Closes the serial connection via the native plugin.
        ///     Does nothing in Edit mode.
        /// </summary>
        public void Disconnect()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SerialClose();

            if (m_isConnected)
            {
                Debug.Log("Disconnected from device on " + m_portName);
            }

            m_isConnected = false;
        }


        [Button]
        /// <summary>
        ///     Connects at 4800 baud — the default command mode for the connected device.
        /// </summary>
        public void ConnectInCommandMode()
        {
            m_baudRate = BaudRate.B4800;
            Connect();
        }


        [Button]
        /// <summary>
        ///     Connects at 115200 baud — high-speed data mode for the connected device.
        /// </summary>
        public void ConnectInDataMode()
        {
            m_baudRate = BaudRate.B115200;
            Connect();
        }


        [DllImport("SerialPlugin")]
        private static extern int SerialOpen(string portName, byte baudIndex, bool debug);


        [DllImport("SerialPlugin")]
        private static extern void SerialClose();


        private void OnValidate()
        {
            if (m_availablePorts == null || m_availablePorts.Length == 0)
            {
                RefreshPorts();
            }
        }


        private void Awake()
        {
            RefreshPorts();
        }


        private void Start()
        {
            if (m_availablePorts.Length > 0)
            {
                ConnectInCommandMode();
            }
        }


        [Button]
        /// <summary>
        ///     Scans for available serial ports on the current platform and updates
        ///     <c>m_availablePorts</c>. Bluetooth ports are filtered out when
        ///     <c>m_stripBluetooth</c> is enabled. Disables the component if no ports are found.
        /// </summary>
        public void RefreshPorts()
        {
            #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            m_availablePorts = Directory.GetFiles("/dev/", "cu.*");

            if (m_stripBluetooth)
            {
                m_availablePorts = Array.FindAll(m_availablePorts, port => !port.Contains("Bluetooth"));
            }

            #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                m_availablePorts = System.IO.Ports.SerialPort.GetPortNames();
            #else
                Debug.LogError("SerialConnector not yet implemented for this platform. Cannot continue.");
                enabled = false;
                return;
            #endif

            if (m_availablePorts.Length == 0)
            {
                m_portName = string.Empty;

                if (Application.isPlaying)
                {
                    Debug.LogError("No serial ports found. Cannot continue. This now runs in Awake: was that too soon?");
                    enabled = false;
                }

                return;
            }

            m_selectedPortIndex = Mathf.Clamp(m_selectedPortIndex, 0, m_availablePorts.Length - 1);
            m_portName = m_availablePorts[m_selectedPortIndex];
            // Debug.Log($"Detected {m_availablePorts.Length} port(s). Selected: {m_portName}");
        }


        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
