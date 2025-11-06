using System;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;


namespace SOSXR.Talkies
{
    public class AndroidSerialConnector : MonoBehaviour, ISerialConnect
    {
        //#if UNITY_ANDROID && !UNITY_EDITOR
        [DisableEditing] [SerializeField] private bool m_isConnected = false;

        private readonly int _baudRate = 115200;

        private AndroidJavaClass serialClass;
        private AndroidJavaObject activity;
        public bool IsConnected => m_isConnected;


        [Button]
        public void Connect()
        {
            try
            {
                m_isConnected = serialClass.CallStatic<bool>("open", activity, _baudRate);

                this.Verbose(m_isConnected
                    ? "Connected to USB device"
                    : "Failed to connect to USB device");
            }
            catch (Exception ex)
            {
                this.Error($"Exception while connecting: {ex}");
                m_isConnected = false;
            }
        }


        [Button]
        public void Disconnect()
        {
            if (!m_isConnected)
            {
                return;
            }

            serialClass.CallStatic("close");
            m_isConnected = false;
            this.Success("Disconnected from USB device");
        }


        private void Awake()
        {
            serialClass = new AndroidJavaClass("com.sosxr.serial.SerialBridge");

            activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");
        }


        public int SerialWrite(byte[] data)
        {
            return m_isConnected ? serialClass.CallStatic<int>("write", data, data.Length) : 0;
        }


        public int SerialRead(byte[] buffer)
        {
            return m_isConnected ? serialClass.CallStatic<int>("read", buffer, buffer.Length) : 0;
        }


        private void OnDestroy()
        {
            Disconnect();
        }
        /*#else

        public bool IsConnected
        {
            get
            {
                this.Warning("This is the Android Serial connector, which cannot be connected on non-Android systems");
                return false;
            }
        }


        public void Connect()
        {
            this.Warning("This is the Android Serial connector, which cannot Connect on non-Android systems");
        }


        public void Disconnect()
        {
            this.Warning("This is the Android Serial connector, which cannot Disconnect on non-Android systems");
        }


        #endif*/
    }
}