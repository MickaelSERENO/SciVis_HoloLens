using System;

namespace Sereno.Network.MessageHandler
{
    public class AddSubDatasetMessage : ServerMessage
    {
        public Int32 DatasetID;
        public Int32 SubDatasetID;
        public String Name;

        public AddSubDatasetMessage(ServerType type) : base(type)
        { }

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            return (byte)'s';
        }

        public override void Push(string value)
        {
            Name = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DatasetID = value;
            else if (Cursor == 1)
                SubDatasetID = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 2;
        }
    }
}
