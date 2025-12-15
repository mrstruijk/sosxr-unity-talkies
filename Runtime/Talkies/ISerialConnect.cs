namespace SOSXR.Talkies
{
    public interface ISerialConnect
    {
        bool IsConnected { get; }

        Mode CurrentMode { get; }

        void Connect();

        void Disconnect();
    }
}