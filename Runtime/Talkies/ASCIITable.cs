using System;


namespace SOSXR.SeaShark
{
    /// <summary>
    /// See: https://www.ascii-code.com/
    /// For more info.
    /// </summary>
    public static class ASCIITable
    {
        /// <summary>
        ///     Get the correct byte for a given ASCII char
        /// </summary>
        /// <param name="ascii"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static byte FromASCII(char ascii)
        {
            if (ascii > 0x7F)
            {
                throw new ArgumentOutOfRangeException($"'{ascii}' is not ASCII");
            }

            return (byte) ascii;
        }


        /// <summary>
        ///     Get the correct byte for a given ASCII char
        /// </summary>
        /// <param name="ascii"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte FromASCII(string ascii)
        {
            if (string.IsNullOrEmpty(ascii))
            {
                throw new ArgumentException("String is null or empty");
            }

            if (ascii.Length > 1)
            {
                throw new ArgumentOutOfRangeException($"String {ascii} is too long");
            }

            return FromASCII(ascii[0]);
        }


        /// <summary>
        ///     Get the corresponding ASCII char for a given byte
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static char ToASCII(byte b)
        {
            /*
            if (b > 0x7F)
            {
                throw new ArgumentOutOfRangeException($"{b} is not valid ASCII");
            }
            if (b < 0x20)
            {
                throw new ArgumentOutOfRangeException($"{b} is a non-printable ASCII character");
            }
            */

            return (char) b;
        }
    }
}