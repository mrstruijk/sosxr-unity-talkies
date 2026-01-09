using System;
using System.IO;
using System.Runtime.InteropServices;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;
using ButtonAttribute = SOSXR.SeaShark.ButtonAttribute;


namespace SOSXR.Talkies
{
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


    public class DesktopSerialConnector : MonoBehaviour, ISerialConnect
    {
        [DisableEditing] [SerializeField] private string[] m_availablePorts = Array.Empty<string>();
        [SerializeField] private int m_selectedPortIndex = 0;
        [DisableEditing] [SerializeField] private string m_portName = "COM3";
        [DisableEditing] [SerializeField] private bool m_isConnected = false;
        [SerializeField] [DisableEditing] private BaudRate m_baudRate = BaudRate.B4800;
        [SerializeField] private bool m_filterBluetooth = true;
        
        public bool IsConnected => m_isConnected;


        // [Button]
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
                this.Warning("No port selected. Run RefreshPorts() first.");

                return;
            }

            try
            {
                this.Verbose($"Connecting to {m_portName}");
                var result = SerialOpen(m_portName, (byte) m_baudRate, false);

                if (result == 1)
                {
                    m_isConnected = true;
                    this.Success($"Connected to device on {m_portName} with baud rate {m_baudRate}");
                }
                else
                {
                    m_isConnected = false;
                    this.Error($"Failed to open {m_portName}. Check connection. Is another debugger / IDE open (e.g. Thonny / Arduino IDE)?");
                }
            }
            catch (Exception ex)
            {
                m_isConnected = false;
                this.Error("Is another debugger / IDE open (e.g. Thonny / Arduino IDE)?");
                this.Error($"Exception while connecting: {ex.Message}");
                SerialClose();
            }
        }


        [Button]
        public void Disconnect()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            SerialClose();

            if (m_isConnected)
            {
                this.Success("Disconnected from device on " + m_portName);
            }

            m_isConnected = false;
        }


        [Button]
        public void ConnectInCommandMode()
        {
            m_baudRate = BaudRate.B4800;
            Connect();
        }


        [Button]
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
        public void RefreshPorts()
        {
            #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            m_availablePorts = Directory.GetFiles("/dev/", "cu.*");

            if (m_stripBlueTooth)
            {
                m_availablePorts = Array.FindAll(m_availablePorts, port => !port.Contains("Bluetooth"));
            }
            
            #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                m_availablePorts = System.IO.Ports.SerialPort.GetPortNames();
            #else
                this.Error("SerialConnector not yet implemented for this platform. Cannot continue.");
                enabled = false;
                return;
            #endif

            if (m_availablePorts.Length == 0)
            {
                m_portName = string.Empty;

                if (Application.isPlaying)
                {
                    this.Error("No serial ports found. Cannot continue. This now runs in Awake: was that too soon?");
                    enabled = false;
                }

                return;
            }

            m_selectedPortIndex = Mathf.Clamp(m_selectedPortIndex, 0, m_availablePorts.Length - 1);
            m_portName = m_availablePorts[m_selectedPortIndex];
            this.Verbose($"Detected {m_availablePorts.Length} port(s). Selected: {m_portName}");
        }


        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
