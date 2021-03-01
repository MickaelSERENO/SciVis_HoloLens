using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class SetSubDatasetClipping : ServerMessage
    {
        public int   DatasetID;
        public int   SubDatasetID;
        public float DepthClipping;

        public SetSubDatasetClipping(ServerType type) : base(type){}

        public override void Push(float value)
        {
            if (Cursor == 2)
                DepthClipping = value;
            base.Push(value);
        }

        public override void Push(int value)
        {
            if (Cursor == 0)
                DatasetID = value;
            else if (Cursor == 1)
                SubDatasetID = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            return (byte)'f'; ;
        }

        public override int GetMaxCursor()
        {
            return 2;
        }
    }
}
