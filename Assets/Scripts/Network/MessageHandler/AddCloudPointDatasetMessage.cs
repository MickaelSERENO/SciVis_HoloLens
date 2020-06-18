using System;

namespace Sereno.Network.MessageHandler
{
    public class AddCloudDatasetMessage : ServerMessage
    {
        public Int32 DataID;
        public String Path;

        public AddCloudDatasetMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if (Cursor == 0)
                return (byte)'I';
            if (Cursor == 1)
                return (byte)'s';
            return 0;
        }

        public override void Push(string value)
        {
            Path = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DataID = value;
            else
                return;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 1;
        }
    }
}