using System;

namespace Sereno.Network.MessageHandler
{
    public class AddLogAnnotationMessage : ServerMessage
    {
        /// <summary>
        /// The server-defined ID of this annotation data
        /// </summary>
        public int    LogID;

        /// <summary>
        /// The filename to open
        /// </summary>
        public String FileName;

        /// <summary>
        /// Has this data an embedded header?
        /// </summary>
        public bool   HasHeader;

        /// <summary>
        /// What is the column ID of the time data?
        /// </summary>
        public int    TimeIdx;
        
        public AddLogAnnotationMessage(ServerType type) : base(type)
        {}

        public override void Push(String value)
        {
            FileName = value;
            base.Push(value);
        }

        public override void Push(byte value)
        {
            if(Cursor == 2)
                HasHeader = (value != 0);
            base.Push(value);
        }

        public override void Push(int value)
        {
            if(Cursor == 0)
                LogID = value;
            else if(Cursor == 3)
                TimeIdx = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if(Cursor == 0 || Cursor == 3)
                return (byte)'I';
            else if(Cursor == 1)
                return (byte)'s';
            else if(Cursor == 2)
                return (byte)'b';
            return 0;
        }
       
        public override Int32 GetMaxCursor()
        {
            return 3;
        }
    }
}