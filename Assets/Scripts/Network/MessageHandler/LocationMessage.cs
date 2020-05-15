using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message for the tablet's location
    /// </summary>
	public class LocationMessage : ServerMessage
	{
        /// <summary>
        /// The position vector
        /// </summary>
        public float[] position = new float[3];
		
        /// <summary>
        /// The rotation vector
        /// </summary>
        public float[] rotation = new float[4];
		
        public LocationMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            return (byte)'f';
        }
		
        public override void Push(float value)
        {
			if(Cursor < 3)
            	position[Cursor] = value;
			else
				rotation[Cursor-3] = value;
            base.Push(value);
        }
		
        public override Int32 GetMaxCursor()
        {
            return 6;
        }
	}
}