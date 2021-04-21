using System;

namespace Sereno.Network.MessageHandler
{
    public class SubjectiveStackedGroupGlobalParametersMessage : ServerMessage
    {
        public int   SDG_ID;
        public int   StackedMethod;
        public float Gap;
        public bool  IsMerged;

        public SubjectiveStackedGroupGlobalParametersMessage(ServerType type) : base(type) {}

        public override void Push(int value)
        {
            if (Cursor == 0)
                SDG_ID = value;
            else if (Cursor == 1)
                StackedMethod = value;

            base.Push(value);
        }

        public override void Push(float value)
        {
            if (Cursor == 2)
                Gap = value;
            base.Push(value);
        }

        public override void Push(byte value)
        {
            if (Cursor == 3)
                IsMerged = (value != 0);
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor < 2)
                return (byte)'I';
            else if (Cursor == 2)
                return (byte)'f';
            else if (Cursor == 3)
                return (byte)'b';
            return 0;
        }
        
        public override Int32 GetMaxCursor()
        {
            return 3;
        }
    }
}