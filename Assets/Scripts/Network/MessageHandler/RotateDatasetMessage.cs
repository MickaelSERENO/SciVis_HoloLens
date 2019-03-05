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

        public RotateDatasetMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if(Cursor <= 1)
                return (byte)'I';
            return (byte)'f';
        }

        public override void Push(float value)
        {
            Quaternion[Cursor-2] = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if(Cursor == 0)
                DataID = value;
            else
                SubDataID = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 5;
        }
    }
}