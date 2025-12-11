using System.Runtime.InteropServices;
using System.Text;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;


namespace SOSXR.Talkies
{
    [RequireComponent(typeof(ISerialConnect))]
    public class ByteSize : MonoBehaviour
    {
        private ISerialConnect _connector;

        // Buffers for reading data
        private byte positionBuffer;
        private byte speedBuffer;


        [DllImport("SerialPlugin")]
        private static extern int SerialSetBaud(int baud);


        [DllImport("SerialPlugin")]
        private static extern int SerialWrite(byte position, byte speed);


        [DllImport("SerialPlugin")]
        private static extern int SerialRead(ref byte position, ref byte speed);


        private void Awake()
        {
            _connector ??= GetComponent<ISerialConnect>();
        }


        [Button(space: 10, horizontalLine: true)]
        private void SetBaud(int baud)
        {
            if (!_connector.IsConnected)
            {
                this.Warning("We're not connected! Cannot continue");

                return;
            }

            var ok = SerialSetBaud(baud);

            if (ok == 1)
            {
                this.Verbose("Baud successfully updated to " + baud);
            }
            else
            {
                this.Error("Failed to set baud to " + baud);
            }
        }


        private void ReadBuffer()
        {
            if (!_connector.IsConnected)
            {
                this.Warning("We're not connected! Cannot continue");

                return;
            }

            // Reset buffers
            positionBuffer = 0;
            speedBuffer = 0;

            var bytesRead = SerialRead(ref positionBuffer, ref speedBuffer);

            if (bytesRead <= 0)
            {
                this.Warning("No data received from serial port");

                return;
            }

            // Give some feedback on what we're receiving from the MicroController
            if (bytesRead == 1)
            {
                this.Verbose($"Received partial data - Position: {positionBuffer}");
            }
            else if (bytesRead == 2)
            {
                this.Info($"Received complete data - Position: {positionBuffer}, Speed: {speedBuffer}");
            }
            else
            {
                this.Warning($"Unexpected byte count received: {bytesRead}");
            }
        }


        private void SendCommand(byte position, byte speed)
        {
            if (!_connector.IsConnected)
            {
                this.Warning($"Not connected. Cannot send: {position}:{speed}");

                return;
            }

            var written = SerialWrite(position, speed);

            // Handle errors
            if (written == 2)
            {
                this.Verbose($"Successfully sent command - Position: {position}, Speed: {speed}");
            }
            else if (written == 1)
            {
                this.Warning($"Partial write - Only {written} byte sent. Position: {position}, Speed: {speed}");
            }
            else
            {
                this.Error($"Failed to send command - Position: {position}, Speed: {speed}");
            }
        }


        [Button]
        public void Unlock()
        {
            SetBaud(48000);
            var asBytes = Encoding.ASCII.GetBytes("u");
            var asByte = asBytes[0];
            SendCommand(asByte, 0);
        }


        // Optional: Public methods for external usage
        [Button]
        public void TestSendCommand(int position, int speed)
        {
            SendCommand((byte) position, (byte) speed);
        }


        [Button]
        public void TestReadBuffer()
        {
            ReadBuffer();
        }
    }
}