using System;

namespace Sereno.Network.MessageHandler
{
    public class LinkLogAnnotationPositionSubDatasetMessage : ServerMessage
    {
        /// <summary>
        /// The DataID of the SubDataset's parent
        /// </summary>
        public Int32   DataID;

        /// <summary>
        /// The SubDataID of the DataID which is concerned with the linkage
        /// </summary>
        public Int32   SubDataID;

        /// <summary>
        /// The server-defined ID of the parent log annotation container
        /// </summary>
        public Int32   AnnotID;

        /// <summary>
        /// The server-defined ID of this annotation component data INSIDE AnnotID
        /// </summary>
        public Int32   CompID;

        public LinkLogAnnotationPositionSubDatasetMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if(Cursor <= 3)
                return (byte)'I';
            return 0;
        }

        public override void Push(Int32 value)
        {
            if(Cursor == 0)
                DataID = value;
            else if(Cursor == 1)
                SubDataID = value;
            else if(Cursor == 2)
                AnnotID = value;
            else if(Cursor == 3)
                CompID = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 3;
        }
    }
}