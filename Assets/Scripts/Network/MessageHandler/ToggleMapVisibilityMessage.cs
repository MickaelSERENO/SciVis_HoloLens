using System;

namespace Sereno.Network.MessageHandler
{

    public class ToggleMapVisibilityMessage : ServerMessage
    {
        /// <summary>
        /// The DataID bound to this message
        /// </summary>
        public Int32 DataID;

        /// <summary>
        /// The SubDataID of the DataID which is being rotated
        /// </summary>
        public Int32 SubDataID;

        /// <summary>
        /// Is the Map visible for the given SubDataset?
        /// </summary>
        public bool IsVisible;

        public ToggleMapVisibilityMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            return (byte)'b';
        }

        public override void Push(byte value)
        {
            if(Cursor == 2)
                IsVisible = (value != 0);
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DataID = value;
            else if (Cursor == 1)
                SubDataID = value;

            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 2;
        }
    }
}