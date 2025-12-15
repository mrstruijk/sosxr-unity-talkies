using System.Runtime.InteropServices;
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
        [Tooltip("Careful: if theres an issue in reading, this will make your life a little more interesting than it needs to be.")]
        [SerializeField] private bool m_readEveryFrame;

        private readonly char _commandFiller = '-'; // Any char that's not (often) used for commands.

        private ISerialConnect _connector;

        // Buffers for reading data
        private byte positionBuffer;
        private byte speedBuffer;


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


        public void SendCommand(byte position, byte speed)
        {
            var posChar = ASCIITable.ToASCII(position);
            var spdChar = ASCIITable.ToASCII(speed);

            if (!_connector.IsConnected)
            {
                this.Warning("Not connected! Cannot send command.");

                return;
            }

            var written = SerialWrite(position, speed);

            if (written <= 1)
            {
                this.Warning($"We may not have sent all. Written {written} bytes.");

                return;
            }

            this.Verbose($"Successfully sent command. Position byte: {position} (ascii: {posChar}) Speed byte: {speed} (ascii: {spdChar})");
        }


        [Button]
        public void SendCommand(string position, string speed)
        {
            if (position.Length != 1)
            {
                this.Warning("Incorrect position provided. Needs to be a single character.");

                return;
            }

            if (speed.Length != 1)
            {
                this.Warning("Incorrect speed provided. Needs to be a single character.");

                return;
            }

            SendCommand(ASCIITable.FromASCII(position), ASCIITable.FromASCII(speed));
        }


        [Button]
        public void SendCommand(string posSpeed)
        {
            if (posSpeed.Length != 2)
            {
                this.Warning("This requires 2 characters");

                return;
            }

            SendCommand(ASCIITable.FromASCII(posSpeed[0]), ASCIITable.FromASCII(posSpeed[1]));
        }


        [Button]
        public void Unlock()
        {
            SendCommand(ASCIITable.FromASCII('u'), ASCIITable.FromASCII(_commandFiller));
        }


        [Button]
        public void Info()
        {
            SendCommand(ASCIITable.FromASCII('i'), ASCIITable.FromASCII(_commandFiller));
        }


        [Button]
        public void Stop()
        {
            SendCommand(ASCIITable.FromASCII('s'), ASCIITable.FromASCII(_commandFiller));
        }


        [Button]
        public void Version()
        {
            SendCommand(ASCIITable.FromASCII('v'), ASCIITable.FromASCII(_commandFiller));
        }


        private void Update()
        {
            if (m_readEveryFrame)
            {
                ReadBuffer();
            }
        }


        [Button] // Test reading a single byte from the serial line. Useful if not also done in Update().
        public void ReadBuffer()
        {
            if (!_connector.IsConnected)
            {
                this.Warning("We're not connected! Cannot continue");

                return;
            }

            var result = SerialRead();

            if (result < 0)
            {
                // this.Verbose("There is no data on the line");

                return; // No data available
            }

            var receivedByte = (byte) result;
            var receivedASCII = ASCIITable.ToASCII(receivedByte);

            this.Info($"Received byte:{receivedByte} (ascii:{receivedASCII})");
        }
    }
}