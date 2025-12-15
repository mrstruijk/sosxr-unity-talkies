using System;
using System.IO;
using System.Runtime.InteropServices;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;
using ButtonAttribute = SOSXR.SeaShark.ButtonAttribute;


namespace SOSXR.Talkies
{
    public enum Mode
    {
        None,
        Command,
        Data
    }


    public class DesktopSerialConnector : MonoBehaviour, ISerialConnect
    {
        [DisableEditing] [SerializeField] private string[] m_availablePorts = Array.Empty<string>();
        [SerializeField] private int m_selectedPortIndex = 0;
        [DisableEditing] [SerializeField] private string m_portName = "COM3";
        [SerializeField] [DisableEditing] private int m_currentBaudRate = -1;
        [DisableEditing] [SerializeField] private bool m_isConnected = false;
        [SerializeField] private Mode m_currentMode = Mode.Command;
        private readonly int _commandBaudRate = 4800;
        private readonly int _dataBaudRate = 115200;
        public Mode CurrentMode => m_currentMode;
        public bool IsConnected => m_isConnected;


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
                this.Verbose($"Connecting to {m_portName} at {GetBaudFromMode()}...");
                var result = SerialOpen(m_portName, GetBaudFromMode(), true);

                if (result == 1)
                {
                    m_isConnected = true;
                    this.Success($"Connected to device on {m_portName} with baud rate {GetBaudFromMode()}");
                }
                else
                {
                    m_currentBaudRate = -1;
                    m_isConnected = false;
                    this.Error($"Failed to open {m_portName}. Check connection. Is another debugger / IDE open (e.g. Thonny / Arduino IDE)?");
                }
            }
            catch (Exception ex)
            {
                m_currentBaudRate = -1;
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


        private int GetBaudFromMode()
        {
            if (m_currentMode == Mode.Command)
            {
                return _commandBaudRate;
            }

            if (m_currentMode == Mode.Data)
            {
                return _dataBaudRate;
            }

            this.Error("Invalid Mode");

            return -1;
        }


        [Button]
        public void ConnectInCommandMode()
        {
            m_currentMode = Mode.Command;
            Connect();
        }


        [Button]
        public void ConnectInDataMode()
        {
            m_currentMode = Mode.Data;
            Connect();
        }


        [DllImport("SerialPlugin")]
        private static extern int SerialOpen(string portName, int baudRate, bool debug);


        [DllImport("SerialPlugin")]
        private static extern void SerialClose();


        private void OnValidate()
        {
            RefreshPorts();
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
            m_availablePorts = Directory.GetFiles("/dev/", "cu.usbmodem*");
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