using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message received when initializing this headset
    /// </summary>
    public class HeadsetInitMessage : ServerMessage
    {
        /// <summary>
        /// The RGB color representing this headset. 
        /// R = (color >> 16) & 0xff;
        /// G = (color >> 8)  & 0xff;
        /// B = color & 0xff;       
        /// </summary>
        private Int32 m_color = 0x000000;

        public HeadsetInitMessage(ServerType type) : base(type)
        {}

        public override void Push(int value)
        {
            if(Cursor == 0)
                m_color = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if(Cursor == 0)
                return (byte)'I';
            return 0;
        }

        public override Int32 GetMaxCursor() {return 0;}


        /// <summary>
        /// The RGB color representing this headset. 
        /// R = (color >> 16) & 0xff;
        /// G = (color >> 8)  & 0xff;
        /// B = color & 0xff;
        /// </summary>
        public Int32 Color {get => m_color; set => m_color = value;}
    }
}