namespace SOSXR.Talkies
{
    /// <summary>
    ///     Contract for serial connection implementations. Provides a unified API for
    ///     opening and closing a serial device connection across platforms (Android, Desktop).
    /// </summary>
    public interface ISerialConnect
    {
        /// <summary>Gets a value indicating whether the serial device is currently connected.</summary>
        bool IsConnected { get; }


        /// <summary>Opens the serial connection to the device.</summary>
        void Connect();


        /// <summary>Closes the serial connection and releases the device.</summary>
        void Disconnect();
}