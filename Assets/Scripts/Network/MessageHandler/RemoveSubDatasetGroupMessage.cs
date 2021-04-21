using System;

namespace Sereno.Network.MessageHandler
{
    public class RemoveSubDatasetGroupMessage : ServerMessage
    {
        public RemoveSubDatasetGroupMessage(ServerType type) : base(type)
        {}

        public int SDG_ID;

        public override void Push(int value)
        {
            if (Cursor == 0)
            SDG_ID = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor == 0)
                return (byte)'I';
            return 0;
        }

        public override Int32 GetMaxCursor() { return 0; }
    }
}