using System;

namespace Sereno.Network.MessageHandler
{
    public class SetDrawableAnnotationPositionIdxMessage : ServerMessage
    {
        public Int32   DatasetID;
        public Int32   SubDatasetID;
        public Int32   DrawableID;
        public Int32[] Indices = new Int32[0];

        public SetDrawableAnnotationPositionIdxMessage(ServerType type) : base(type)
        {}
        
        public override byte GetCurrentType()
        {
            if(Cursor <= 3+Indices.Length)
                return (byte)'I';
            return 0;
        }

        public override void Push(Int32 value)
        {
            if(Cursor == 0)
                DatasetID = value;
            else if(Cursor == 1)
                SubDatasetID = value;
            else if(Cursor == 2)
                DrawableID = value;
            else if(Cursor == 3)
                Indices = new Int32[value];
            else if(Cursor < 4 + Indices.Length)
                Indices[Cursor-4] = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 3+Indices.Length;
        }
    }
}