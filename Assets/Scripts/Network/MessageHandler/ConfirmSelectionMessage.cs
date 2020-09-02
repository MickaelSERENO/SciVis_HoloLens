using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message to confirm the current selection
    /// </summary>
	public class ConfirmSelectionMessage : ServerMessage
	{
        /// <summary>
        /// The DataID bound to this message
        /// </summary>
        public Int32 DataID;

        /// <summary>
        /// The SubDataID of the DataID which is being rotated
        /// </summary>
        public Int32 SubDataID;

        public ConfirmSelectionMessage(ServerType type) : base(type)
        {}

        public override void Push(int value)
        {
            if(Cursor == 0)
                DataID = value;
            else if(Cursor == 1)
                SubDataID = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            return (byte)'I';
        }
		
        public override Int32 GetMaxCursor()
        {
            return 1;
        }
	}
}