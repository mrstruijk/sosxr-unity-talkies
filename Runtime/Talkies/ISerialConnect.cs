namespace SOSXR.Talkies
{
    public interface ISerialConnect
    {
        bool IsConnected { get; }


        void Connect();


        void Disconnect();
    }
}