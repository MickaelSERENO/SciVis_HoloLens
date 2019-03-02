namespace Sereno.Network.MessageHandler
{
    public class EmptyMessage : ServerMessage
    {
        public EmptyMessage(ServerType type) : base(type)
        {}
        
        public override byte GetCurrentType()
        {
            return (byte)'i';
        }
    }
}