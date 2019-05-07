using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message for rotation of datasets
    /// </summary>
    public class RotateDatasetMessage : ServerMessage
    {
        /// <summary>
        /// The rotation quaternion.
        /// 0 -> w, 1 -> i, 2 -> j, 3 -> k
        /// </summary>
        public float[] Quaternion = new float[4];

        /// <summary>
        /// The DataID bound to this message
        /// </summary>
        public Int32   DataID;

        /// <summary>
        /// The SubDataID of the DataID which is being rotated
        /// </summary>
        public Int32   SubDataID;

        /// <summary>
        /// The headset ID performing this movement (can be us). -1 == server
        /// </summary>
        public Int32 HeadsetID;

        /// <summary>
        /// Is the translation done into the public workspace?
        /// </summary>
        public byte InPublic;

        public RotateDatasetMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if(Cursor <= 2)
                return (byte)'I';
            else if(Cursor == 3)
                return (byte)'b';
            return (byte)'f';
        }

        public override void Push(float value)
        {
            Quaternion[Cursor-4] = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if(Cursor == 0)
                DataID = value;
            else if(Cursor == 1)
                SubDataID = value;
            else if(Cursor == 2)
                HeadsetID = value;

            base.Push(value);
        }

        public override void Push(byte value)
        {
            InPublic = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 7;
        }
    }
}