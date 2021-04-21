using System;

namespace Sereno.Network.MessageHandler
{
    public class AddSubDatasetToSubjectiveStackedGroupMessage : ServerMessage
    {
        public int SDG_ID;
        public int DatasetID;
        public int StackedID;
        public int LinkedID;

        public AddSubDatasetToSubjectiveStackedGroupMessage(ServerType type) : base(type)
        {}

        public override void Push(int value)
        {
            if (Cursor == 0)
                SDG_ID = value;
            else if (Cursor == 1)
                DatasetID = value;
            else if (Cursor == 2)
                StackedID = value;
            else if (Cursor == 3)
                LinkedID = value;

            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor < 4)
                return (byte)'I';
            return 0;
        }

        public override Int32 GetMaxCursor()
        {
            return 3;
        }
    }
}
