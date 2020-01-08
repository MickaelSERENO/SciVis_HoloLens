using System;

namespace Sereno.Network.MessageHandler
{
    public class AddSubDatasetMessage : ServerMessage
    {
        /// <summary>
        /// The Dataset ID of this SubDataset
        /// </summary>
        public Int32 DatasetID;

        /// <summary>
        /// The pre-defined SubDataset ID to use
        /// </summary>
        public Int32 SubDatasetID;

        /// <summary>
        /// The ID of the owner. -1 == public SubDataset
        /// </summary>
        public Int32 OwnerID;

        /// <summary>
        /// The name of the SubDataset
        /// </summary>
        public String Name;

        public AddSubDatasetMessage(ServerType type) : base(type)
        { }

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            else if (Cursor == 3)
                return (byte)'I';
            else if(Cursor == 2)
                return (byte)'s';
            return 0;
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
            else if (Cursor == 3)
                OwnerID = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 3;
        }
    }
}
