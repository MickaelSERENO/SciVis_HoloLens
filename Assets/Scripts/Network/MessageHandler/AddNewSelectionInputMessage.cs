using System;

namespace Sereno.Network.MessageHandler
{
    public class AddNewSelectionInputMessage : ServerMessage
    {
        public Int32 BooleanOperation;
        public bool  Constrained;

        public AddNewSelectionInputMessage(ServerType type) : base(type)
        {}

        public override void Push(Int32 Value)
        {
            BooleanOperation = Value;
            base.Push(Value);
        }

        public override void Push(byte value)
        {
            Constrained = (value!=0);
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor == 0)
                return (byte)'I';
            else if (Cursor == 1)
                return (byte)'b';
            return 0;
        }
        
        public override Int32 GetMaxCursor()
        {
            return 1;
        }
    }
}