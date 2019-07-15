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
        /// The handedness defined by the server or the tablet connected
        /// </summary>
        private Int32 m_handedness;

        /// <summary>
        /// The ID of the connected tablet.
        /// </summary>
        private Int32 m_tabletID;

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
            else if(Cursor == 5)
                m_isFirst = value;
            base.Push(value);
        }

        public override void Push(int value)
        {
            if (Cursor == 0)
                m_id = value;
            else if (Cursor == 1)
                m_color = value;
            else if (Cursor == 3)
                m_handedness = value;
            else if (Cursor == 4)
                m_tabletID = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if(Cursor == 0 || Cursor == 1 || Cursor == 3 || Cursor == 4)
                return (byte)'I';
            else if(Cursor == 2 || Cursor == 5)
                return (byte)'b';
            return 0;
        }

        public override Int32 GetMaxCursor() {return 5;}


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

        /// <summary>
        /// The handedness ID
        /// </summary>
        public Int32 Handedness { get => m_handedness; set => m_handedness = value; }

        /// <summary>
        /// Get the tablet ID bound to this Headset. Is usable only if TabletConnected == true. The TabletID defines a role in the application.
        /// </summary>
        public Int32 TabletID { get => m_tabletID; set => m_tabletID = value; }
    }
}