using System.Runtime.InteropServices;
using System.Text;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;


namespace SOSXR.Talkies
{
    /// <summary>
    ///     This works now! The biggest takeaways:
    ///     - Arduino IDE cannot be connected to the Leonardo at the same time:
    ///     - if during connect = no connect
    ///     - when during use = Arduino IDE sets the baud rate, not Unity!
    /// </summary>
    [RequireComponent(typeof(ISerialConnect))]
    public class ByteSize : MonoBehaviour
    {
        private readonly char _commandFiller = '-';
        private ISerialConnect _connector;

        // Buffers for reading data
        private char positionBuffer;
        private char speedBuffer;


        [DllImport("SerialPlugin")]
        private static extern int SerialSetBaud(int baud);


        [DllImport("SerialPlugin")]
        private static extern int SerialWrite(char position, char speed);


        [DllImport("SerialPlugin")]
        private static extern int SerialRead(ref char position, ref char speed);


        private void Awake()
        {
            _connector ??= GetComponent<ISerialConnect>();
        }


        private void ReadBuffer()
        {
            if (!_connector.IsConnected)
            {
                this.Warning("We're not connected! Cannot continue");

                return;
            }

            // Reset buffers. TODO: check if this is good.
            positionBuffer = char.MinValue;
            speedBuffer = char.MinValue;

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


        private void SendCommand(char position, char speed)
        {
            var pos = Encoding.ASCII.GetBytes(position.ToString())[0];
            var spd = Encoding.ASCII.GetBytes(speed.ToString())[0];

            if (!_connector.IsConnected)
            {
                this.Warning($"Not connected. Cannot send: {pos}:{spd}");

                return;
            }

            if (position is 'v' or 'i' or 's' or 'u' && speed == _commandFiller)
            {
                if (_connector.CurrentMode != Mode.Command)
                {
                    this.Warning("We are not in Command Mode, yet you are trying to send a command...");
                }
            }
            else
            {
                if (_connector.CurrentMode == Mode.Command)
                {
                    this.Warning("We're still in Command Mode, yet you are trying to send data?");
                }
            }

            var written = SerialWrite(position, speed);

            if (written <= 1)
            {
                this.Warning($"We may not have sent all. Written {written} bytes.");

                return;
            }

            if (position is 'v' or 'i' or 's' or 'u')
            {
                Debug.Log($"Sending command {position}"); // some reason EnhancedLogger cries in char

                return;
            }

            this.Info($"Successfully sent command - Position: {pos}, Speed: {spd}");
        }


        [Button]
        public void Unlock()
        {
            SendCommand('u', _commandFiller);
        }


        [Button]
        public void Info()
        {
            SendCommand('i', _commandFiller);
        }


        [Button]
        public void Stop()
        {
            SendCommand('s', _commandFiller);
        }


        [Button]
        public void Version()
        {
            SendCommand('v', _commandFiller);
        }


        // Optional: Public methods for external usage
        [Button]
        public void TestSendCommand(string position, string speed)
        {
            if (position.Length == 0)
            {
                this.Warning("No position provided");

                return;
            }

            if (speed.Length == 0)
            {
                this.Warning("No speed provided");

                return;
            }

            var pos = position[0];
            var spd = speed[0];

            SendCommand(pos, spd);
        }


        [Button]
        public void TestReadBuffer()
        {
            this.Error("No can do, this crashes!");

            return;

            ReadBuffer();
        }
    }
}