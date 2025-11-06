using System;
using System.IO;
using System.Runtime.InteropServices;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;


namespace SOSXR.Talkies
{
    public class SerialConnector : MonoBehaviour
    {
        [DisableEditing] [SerializeField] private string[] m_availablePorts = Array.Empty<string>();
        [SerializeField] private int m_selectedPortIndex = 0;
        [DisableEditing] [SerializeField] private string m_portName = "COM3";
        [SerializeField] private int m_baudRate = 115200;
        [DisableEditing] [SerializeField] private bool m_isConnected = false;

        public bool IsConnected => m_isConnected;


        // Native plugin imports
        [DllImport("SerialPlugin")]
        private static extern int SerialOpen(string portName, int baudRate);


        [DllImport("SerialPlugin")]
        private static extern void SerialClose();


        private void OnValidate()
        {
            RefreshPorts();
        }


        private void Start()
        {
            RefreshPorts();

            if (m_availablePorts.Length > 0)
            {
                ConnectOverSerial();
            }
        }


        [Button]
        public void RefreshPorts()
        {
            #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                m_availablePorts = Directory.GetFiles("/dev/", "cu.usbmodem*");
            #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                m_availablePorts = System.IO.Ports.SerialPort.GetPortNames();
            #elif UNITY_ANDROID
                GetAndroidPort();
            #else
                this.Error("SerialConnector not yet implemented for this platform. Cannot continue.");
                enabled = false;
                return;
            #endif

            if (m_availablePorts.Length == 0)
            {
                this.Error("No serial ports found. Cannot continue.");
                m_portName = string.Empty;
                enabled = false;
                return;
            }

            m_selectedPortIndex = Mathf.Clamp(m_selectedPortIndex, 0, m_availablePorts.Length - 1);
            m_portName = m_availablePorts[m_selectedPortIndex];
            this.Verbose($"Detected {m_availablePorts.Length} port(s). Selected: {m_portName}");
        }


        /// <summary>
        ///     For Android / Quest, we're using https://github.com/mik3y/usb-serial-for-android under the hood.
        ///     On Android/Quest, you don’t pass the port name to the native plugin; SerialOpen(portName, baudRate) just ignores the string. The dummy portname is just for your UI and logging, so the existing SerialConnector code works without branching all over the place. The hoho plugin automatically detects the Pico via USB host API.
        /// </summary>
        private void GetAndroidPort()
        {
            m_availablePorts = new[] {"DUMMY-Android-USB-Port"}; // Dummy placeholder
            m_portName = m_availablePorts[0];
        }


        [Button]
        public void ConnectOverSerial()
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
                this.Verbose($"Connecting to {m_portName}...");
                var result = SerialOpen(m_portName, m_baudRate);

                if (result == 1)
                {
                    m_isConnected = true;
                    this.Success($"Connected to device on {m_portName}");
                }
                else
                {
                    this.Error($"Failed to open {m_portName}. Check connection.");
                }
            }
            catch (Exception ex)
            {
                this.Error($"Exception while connecting: {ex.Message}");
                SerialClose();
            }
        }


        [Button]
        private void Disconnect()
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


        private void OnDestroy()
        {
            Disconnect();
        }
    }
}