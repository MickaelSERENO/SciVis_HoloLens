using System;

namespace Sereno.Network.MessageHandler
{
    public class AddNewSelectionInputMessage : ServerMessage
    {
        public Int32 BooleanOperation;

        public AddNewSelectionInputMessage(ServerType type) : base(type)
        {}

        public override void Push(Int32 Value)
        {
            BooleanOperation = Value;
            base.Push(Value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor == 0)
                return (byte)'I';
            return 0;
        }
        
        public override Int32 GetMaxCursor()
        {
            return 0;
        }
    }
}