using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Status of a collabroator Headset
    /// </summary>
    public class HeadsetStatus
    {
        /// <summary>
        /// The X, Y and Z 3D position
        /// </summary>
        public float[] Position = new float[3];

        /// <summary>
        /// The W, X, Y and Z 3D Quaternion rotation
        /// </summary>
        public float[] Rotation = new float[4];

        /// <summary>
        /// The headset ID
        /// </summary>
        public int ID;

        /// <summary>
        /// The headset Color
        /// </summary>
        public int Color;
    }

    /// <summary>
    /// Handles message for updating all the internal states of the known headsets (this excluded)
    /// </summary>
    public class HeadsetsStatusMessage : ServerMessage
    {
        /// <summary>
        /// The headsets status received
        /// </summary>
        private HeadsetStatus[] m_status;

        public HeadsetsStatusMessage(ServerType type) : base(type)
        {
        }

        public override void Push(float value)
        {
            int   headset = (Cursor-1)/9;
            Int32 id      = (Cursor-1) % 9;

            if((Cursor - 1) % 9 < 5) //Position
                m_status[headset].Position[id - 2] = value;
            else if((Cursor - 1) % 9 < 9) //Rotation
                m_status[headset].Rotation[id - 5] = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            //Number of headsets
            if(Cursor == 0)
                m_status = new HeadsetStatus[value];
            else
            {
                Int32 id      = (Cursor-1)%9;
                int   headset = (Cursor-1)/9;

                if(id == 0)
                    m_status[headset].ID = value;
                else if(id == 1)
                    m_status[headset].Color = value;
            }
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if(Cursor == 0) //Number of headsets
                return (byte)'I';
            else
            {
                Int32 id = (Cursor-1) % 9;

                if(id < 2) //Color / ID
                    return (byte)'I';

                if(id < 5) //Position
                    return (byte)'f';

                else if(id < 9) //Rotation
                    return (byte)'f';
            }
            return 0;
        }

        public override Int32 GetMaxCursor() { return 0+(m_status != null ? m_status.Length*9 : 0); }

        /// <summary>
        /// The headsets status received
        /// </summary>
        public HeadsetStatus[] HeadsetsStatus {get => m_status;}
    }
}
