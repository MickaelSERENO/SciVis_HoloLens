using System;


namespace Sereno.Network.MessageHandler
{
    public class ResetVolumetricSelectionMessage : ServerMessage
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
        /// The headset ID sending this event (can be us)
        /// </summary>
        public Int32 HeadsetID;

        public ResetVolumetricSelectionMessage(ServerType type) : base(type)
        { }

        public override byte GetCurrentType()
        {
            if (Cursor <= 2)
                return (byte)'I';
            return (byte)0;
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DataID = value;
            else if (Cursor == 1)
                SubDataID = value;
            else if (Cursor == 2)
                HeadsetID = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 2;
        }
    }
}
