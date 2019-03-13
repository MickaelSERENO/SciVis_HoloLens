using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Status of a collabroator Headset
    /// </summary>
    public class HeadsetStatus
    {
        /// <summary>
        /// The ID of the headset
        /// </summary>/
        public Int32 ID;
        /// <summary>
        /// The X, Y and Z 3D position
        /// </summary>
        public float[] Position = new float[3];

        /// <summary>
        /// The W, X, Y and Z 3D Quaternion rotation
        /// </summary>
        public float[] Rotation = new float[4];

        /// <summary>
        /// The color representing this collaborator
        /// </summary>
        public Int32   Color    = 0;
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
            int headset = (Cursor-1)/8;
            if((Cursor - 1) % 8 < 4) //Position
                m_status[headset].Position[(Cursor - 1) % 8 - 1] = value;
            else if((Cursor - 1) % 8 < 8) //Rotation
                m_status[headset].Rotation[(Cursor - 1) % 8 - 4] = value;
        }

        public override void Push(Int32 value)
        {
            //Number of headsets
            if(Cursor == 0)
            {
                m_status = new HeadsetStatus[value];
                return;
            }

            int headset = (Cursor-1)/8;
            if((Cursor-1)%8 == 0) //ID
                m_status[headset].ID = value;
            else //Color
                m_status[headset].Color = value;
        }

        public override byte GetCurrentType()
        {
            if(Cursor == 0) //Number of headsets
                return (byte)'I';

            else if((Cursor - 1) % 8 < 4) //Position
                return (byte)'f';

            else if((Cursor - 1) % 8 < 8) //Rotation
                return (byte)'f';

            return (byte)'I'; //Color or ID
        }

        /// <summary>
        /// The headsets status received
        /// </summary>
        public HeadsetStatus[] HeadsetsStatus {get => m_status;}
    }
}
