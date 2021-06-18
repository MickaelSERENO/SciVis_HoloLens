using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class PostReviewRotationMessage : ServerMessage
    {
        public Int32 DatasetID;
        public Int32 SubDatasetID;
        public float[] Quaternion = new float[4] { 1.0f, 0.0f, 0.0f, 0.0f };

        public PostReviewRotationMessage(ServerType type) : base(type){}

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            return (byte)'f';
        }

        public override void Push(float value)
        {
            Quaternion[Cursor - 2] = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DatasetID = value;
            else if (Cursor == 1)
                SubDatasetID = value;

            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 5;
        }
    }
}
