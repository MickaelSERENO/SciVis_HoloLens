using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message for the tablet scale
    /// </summary>
	public class TabletScaleMessage : ServerMessage
	{
        /// <summary>
        /// The tablet's virtual scale
        /// </summary>
        public float scale;

        /// <summary>
        /// Size of the tablet's view window
        /// </summary>
        public float width;
        public float height;

        /// <summary>
        /// Position of the tablet's view window
        /// </summary>
        public float posx;
        public float posy;
		
        public TabletScaleMessage(ServerType type) : base(type)
        {}
		
        public override byte GetCurrentType()
        {
            return (byte)'f';
        }
		
        public override void Push(float value)
        {
			switch(Cursor)
			{
				case 0:
					scale = value;
					break;
				case 1:
					width = value;
					break;
				case 2:
					height = value;
					break;
				case 3:
					posx = value;
					break;
				case 4:
					posy = value;
					break;
			}
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 4;
        }
	}
}