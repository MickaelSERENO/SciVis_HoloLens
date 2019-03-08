using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message for rotation of datasets
    /// </summary>
    public class MoveDatasetMessage : ServerMessage
    {
        /// <summary>
        /// The 3D position vector
        /// </summary>
        public float[] Position = new float[3];

        /// <summary>
        /// The DataID bound to this message
        /// </summary>
        public Int32   DataID;

        /// <summary>
        /// The SubDataID of the DataID which is being rotated
        /// </summary>
        public Int32   SubDataID;

        public MoveDatasetMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if(Cursor <= 1)
                return (byte)'I';
            return (byte)'f';
        }

        public override void Push(float value)
        {
            Position[Cursor-2] = value;
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
            return 4;
        }
    }
}