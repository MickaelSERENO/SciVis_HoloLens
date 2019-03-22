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

        /// <summary>
        /// The headset ID saved by the server
        /// </summary>
        private Int32 m_id;

        /// <summary>
        /// Is the tablet connected?
        /// </summary>
        private bool m_tabletConnected;

        /// <summary>
        /// Is this headset the first connected headset?
        /// </summary>
        private byte m_isFirst = 0;

        public HeadsetInitMessage(ServerType type) : base(type)
        {}

        public override void Push(byte value)
        {
            if(Cursor == 2)
                m_tabletConnected = (value != 0);
            else if(Cursor == 3)
                m_isFirst = value;
            base.Push(value);
        }

        public override void Push(int value)
        {
            if(Cursor == 0)
                m_id = value;
            else if(Cursor == 1)
                m_color = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if(Cursor == 0 || Cursor == 1)
                return (byte)'I';
            else if(Cursor == 2 || Cursor == 3)
                return (byte)'b';
            return 0;
        }

        public override Int32 GetMaxCursor() {return 3;}


        /// <summary>
        /// The RGB color representing this headset. 
        /// R = (color >> 16) & 0xff;
        /// G = (color >> 8)  & 0xff;
        /// B = color & 0xff;
        /// </summary>
        public Int32 Color {get => m_color; set => m_color = value;}

        /// <summary>
        /// The headset ID
        /// </summary>
        public Int32 ID {get => m_id; set => m_id = value;}

        /// <summary>
        /// Is the tablet connected to this headset?
        /// </summary>
        public bool TabletConnected { get => m_tabletConnected; set => m_tabletConnected = value; }

        /// <summary>
        /// Is this Headset the first headset connected?
        /// </summary>
        public bool IsFirstConnected {get => m_isFirst != 0; set => m_isFirst = (byte)(value ? 1 : 0);}
    }
}