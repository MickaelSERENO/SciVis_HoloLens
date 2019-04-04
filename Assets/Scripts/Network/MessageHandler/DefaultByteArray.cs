namespace Sereno.Network.MessageHandler
{
    public class DefaultByteArray : ServerMessage
    {
        private byte[] m_data = null;

        public DefaultByteArray(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if(Cursor == 0)
                return (byte)'a';
            return 0;
        }

        public override void Push(byte[] value)
        {
            base.Push(value);
            m_data = value;
        }

        public override int GetMaxCursor()
        {
            return 0;
        }

        public byte[] Data { get => m_data; }
    }
}
