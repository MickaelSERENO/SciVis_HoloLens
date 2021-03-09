using System;

namespace Sereno.Network.MessageHandler
{
    public class SetDrawableAnnotationPositionColorMessage : ServerMessage
    {
        public Int32  DatasetID;
        public Int32  SubDatasetID;
        public Int32  DrawableID;
        public UInt32 Color;

        public SetDrawableAnnotationPositionColorMessage(ServerType type) : base(type)
        {}
        
        public override byte GetCurrentType()
        {
            if(Cursor <= 4)
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
                Color = (uint)value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 3;
        }
    }
}