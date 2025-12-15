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
        private readonly byte _commandFiller = 0;
        private ISerialConnect _connector;

        // Buffers for reading data
        private byte positionBuffer;
        private byte speedBuffer;


        [DllImport("SerialPlugin")]
        private static extern int SerialSetBaud(int baud);


        /// <summary>
        ///     C++ `unsigned char` is 1 byte. In C# this is 2 byte.
        ///     To match, C# needs to use `byte`.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        [DllImport("SerialPlugin")]
        private static extern int SerialWrite(byte position, byte speed);


        /// <summary>
        ///     C++ `unsigned char` is 1 byte. In C# this is 2 byte.
        ///     To match, C# needs to use `byte`.
        /// </summary>
        /// <returns></returns>
        [DllImport("SerialPlugin")]
        private static extern int SerialRead();


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

            var result = SerialRead();

            if (result < 0)
            {
                return; // No data available
            }

            var receivedByte = (byte) result;

            // Check for error codes (high byte values that represent negative chars)
            if (receivedByte == 255) // -1 as unsigned byte
            {
                this.Warning("Arduino Error: Servo is locked");
            }
            else if (receivedByte == 254) // -2 as unsigned byte
            {
                this.Warning("Arduino Error: Servo is moving");
            }
            else if (receivedByte <= 180) // Valid servo position
            {
                this.Info($"Servo reached final position: {receivedByte}");
            }
            else
            {
                this.Warning($"Unexpected byte received: {receivedByte}");
            }
        }


        private void SendCommand(byte position, byte speed)
        {
            var pos = Encoding.ASCII.GetBytes(position.ToString())[0];
            var spd = Encoding.ASCII.GetBytes(speed.ToString())[0];

            if (!_connector.IsConnected)
            {
                this.Warning($"Not connected. Cannot send: {pos}:{spd}");

                return;
            }

            /*
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
            */

            var written = SerialWrite(position, speed);

            if (written <= 1)
            {
                this.Warning($"We may not have sent all. Written {written} bytes.");

                return;
            }

            /*if (position is 'v' or 'i' or 's' or 'u')
            {
                Debug.Log($"Sending command {position}"); // some reason EnhancedLogger cries in char

                return;
            }
            */

            this.Info($"Successfully sent command - Position: {pos}, Speed: {spd}");
        }


        private byte AsByte(char c)
        {
            return (byte) (c - '0');
        }


        private byte AsByte(string s)
        {
            if (s.Length > 1)
            {
                this.Warning($"Too many characters provided {s}");
            }

            return AsByte(s[0]);
        }


        [Button]
        public void Unlock()
        {
            SendCommand(AsByte('u'), _commandFiller);
        }


        [Button]
        public void Info()
        {
            SendCommand(AsByte('i'), _commandFiller);
        }


        [Button]
        public void Stop()
        {
            SendCommand(AsByte('s'), _commandFiller);
        }


        [Button]
        public void Version()
        {
            SendCommand(AsByte('v'), _commandFiller);
        }


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

            var pos = AsByte(position[0]);
            var spd = AsByte(speed[0]);

            SendCommand(pos, spd);
        }


        [Button]
        public void TestReadBuffer()
        {
            // this.Error("No can do, this crashes!");

            //return;

            ReadBuffer();
        }
    }
}