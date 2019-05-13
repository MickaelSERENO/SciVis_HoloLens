using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message for rotation of datasets
    /// </summary>
    public class VisibilityMessage : ServerMessage
    {
        /// <summary>
        /// The DataID bound to this message
        /// </summary>
        public Int32   DataID;

        /// <summary>
        /// The SubDataID of the DataID which is being rotated
        /// </summary>
        public Int32   SubDataID;

        /// <summary>
        /// The visibility to apply
        /// </summary>
        public Int32 Visibility;

        public VisibilityMessage(ServerType type) : base(type)
        { }

        public override byte GetCurrentType()
        {
            return (byte)'I';
        }

        public override void Push(Int32 value)
        {
            if(Cursor == 0)
                DataID = value;
            else if(Cursor == 1)
                SubDataID = value;
            else if(Cursor == 2)
                Visibility = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 2;
        }
    }
}