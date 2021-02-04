using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Messages to register a log annotation position component
    /// </summary>
    public class AddLogAnnotationPositionMessage : ServerMessage
    {
        /// <summary>
        /// The server-defined ID of the parent log annotation container
        /// </summary>
        public Int32   AnnotID;

        /// <summary>
        /// The server-defined ID of this annotation component data INSIDE AnnotID
        /// </summary>
        public Int32   CompID;

        /// <summary>
        /// The indexes to use
        /// </summary>
        public Int32[] Indexes = new Int32[3];

        public AddLogAnnotationPositionMessage(ServerType type) : base(type)
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
                AnnotID = value;
            else if(Cursor == 1)
                CompID = value;
            else if(Cursor <= 4)
                Indexes[Cursor-2] = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 4;
        }
    }
}