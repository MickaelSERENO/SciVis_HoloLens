using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message to confirm the current selection
    /// </summary>
	public class ConfirmSelectionMessage : ServerMessage
	{

        public ConfirmSelectionMessage(ServerType type) : base(type)
        {}
		
        public override byte GetCurrentType()
        {
            return (byte)'0';
        }
		
        public override Int32 GetMaxCursor()
        {
            return -1;
        }
	}
}