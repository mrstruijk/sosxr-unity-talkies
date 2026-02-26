using System.Runtime.InteropServices;
using SOSXR.EnhancedLogger;
using SOSXR.SeaShark;
using UnityEngine;


namespace SOSXR.Talkies
{
    /// <summary>
    ///     Low-level byte-based serial communication component.
    ///     Sends single or dual-byte commands to a connected device (e.g. an Arduino Leonardo)
    ///     via the native <c>SerialPlugin</c> and reads single-byte responses.
    ///     <para>
    ///         <b>Note:</b> the Arduino IDE must not be connected to the same device simultaneously;
    ///         if it is, Unity will fail to connect or the IDE will override the baud rate.
    ///     </para>
    /// </summary>
    [RequireComponent(typeof(ISerialConnect))]
    public class ByteSize : MonoBehaviour
    {
        [Tooltip("Careful: if theres an issue in reading, this will make your life a little more interesting than it needs to be.")]
        [SerializeField] private bool m_readEveryFrame;

        [SerializeField] [DisableEditing] private float m_sendTime;
        [SerializeField] [DisableEditing] private float m_readTime;
        [SerializeField] [DisableEditing] private float m_duration;

        private readonly char _commandFiller = '-'; // Any char that's not (often) used for commands.

        private ISerialConnect _connector;

        // Buffers for reading data
        private byte positionBuffer;
        private byte speedBuffer;


        /// <summary>
        ///     C++ `unsigned char` is 1 byte. In C# this is 2 byte.
        ///     To match, C# needs to use `byte`.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [DllImport("SerialPlugin")]
        private static extern int SerialWrite(byte command);


        /// <summary>
        ///     C++ `unsigned char` is 1 byte. In C# this is 2 byte.
        ///     To match, C# needs to use `byte`.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        [DllImport("SerialPlugin")]
        private static extern int SerialWriteTwo(byte position, byte speed);


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


        /// <summary>
        ///     Sends a single-byte command to the connected device.
        /// </summary>
        /// <param name="command">The byte to send (maps to an ASCII command character).</param>
        public void SendCommand(byte command)
        {
            var commChar = ASCIITable.ToASCII(command);

            if (!_connector.IsConnected)
            {
                this.Warning("Not connected! Cannot send command.");

                return;
            }

            var written = SerialWrite(command);

            if (written < 1)
            {
                this.Warning($"We may not have sent all. Written {written} bytes.");

                return;
            }

            m_sendTime = Time.time;

            this.Verbose($"Successfully sent command. Command byte: {command} (ascii: {commChar})");
        }


        /// <summary>
        ///     Sends a position byte and a speed byte to the connected device as two separate bytes.
        /// </summary>
        /// <param name="command">Position byte (ASCII-mapped).</param>
        /// <param name="speed">Speed byte (ASCII-mapped).</param>
        public void SendCommand(byte command, byte speed)
        {
            var posChar = ASCIITable.ToASCII(command);
            var spdChar = ASCIITable.ToASCII(speed);

            if (!_connector.IsConnected)
            {
                this.Warning("Not connected! Cannot send command.");

                return;
            }

            var written = SerialWriteTwo(command, speed);

            if (written <= 1)
            {
                this.Warning($"We may not have sent all. Written {written} bytes.");

                return;
            }

            m_sendTime = Time.time;

            this.Verbose($"Successfully sent command. Position byte: {command} (ascii: {posChar}) Speed byte: {speed} (ascii: {spdChar})");
        }


        [Button]
        /// <summary>
        ///     Sends a position and speed command using single-character strings.
        ///     Each string must be exactly one character long.
        /// </summary>
        /// <param name="position">A single character representing the position command.</param>
        /// <param name="speed">A single character representing the speed command.</param>
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
        /// <summary>
        ///     Sends a combined position+speed command from a two-character string.
        ///     The first character is the position command, the second is the speed command.
        /// </summary>
        /// <param name="posSpeed">A two-character string encoding position and speed.</param>
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
        /// <summary>Sends the <c>'u'</c> (unlock) command to the device.</summary>
        public void Unlock()
        {
            SendCommand(ASCIITable.FromASCII('u'));
        }


        /// <summary>Sends the <c>'i'</c> (info) command, asking the device to report its state.</summary>
        [Button]
        public void Info()
        {
            SendCommand(ASCIITable.FromASCII('i'));
        }


        /// <summary>Sends the <c>'s'</c> (stop) command to the device.</summary>
        [Button]
        public void Stop()
        {
            SendCommand(ASCIITable.FromASCII('s'));
        }


        /// <summary>Sends the <c>'v'</c> (version) command to the device.</summary>
        [Button]
        public void Version()
        {
            SendCommand(ASCIITable.FromASCII('v'));
        }


        private void Update()
        {
            if (m_readEveryFrame)
            {
                ReadBuffer();
            }
        }


        /// <summary>
        ///     Reads a single byte from the serial line via the native plugin.
        ///     Logs the received byte and the round-trip duration since the last <c>SendCommand</c> call.
        ///     Safe to call manually or from <c>Update</c> (controlled by <c>m_readEveryFrame</c>).
        /// </summary>
        [Button]
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

            m_readTime = Time.time;
            m_duration = m_readTime - m_sendTime;

            this.Info($"Received byte:{receivedByte} (ascii:{receivedASCII}) - duration since send: {m_duration:F4} seconds");
        }
    }
}