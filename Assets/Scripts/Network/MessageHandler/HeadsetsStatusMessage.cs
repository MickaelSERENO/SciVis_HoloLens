using Sereno.Pointing;
using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Enumeration of headset current action
    /// </summary>
    public enum HeadsetCurrentAction
    {
        NOTHING   = 0,
        MOVING    = 1,
        SCALING   = 2,
        ROTATING  = 3,
        SKETCHING = 4
    }

    /// <summary>
    /// Status of a collabroator Headset
    /// </summary>
    public class HeadsetStatus : HeadsetUpdateData
    {
        /// <summary>
        /// The headset ID
        /// </summary>
        public int ID;

        /// <summary>
        /// The headset Color
        /// </summary>
        public int Color;

        /// <summary>
        /// The headset current action
        /// </summary>
        public HeadsetCurrentAction CurrentAction = HeadsetCurrentAction.NOTHING;
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
            int   headset = (Cursor-1)/24;
            Int32 id      = (Cursor-1)%24;

            if (id < 6) //Position
                m_status[headset].Position[id - 3] = value;
            else if (id < 10) //Rotation
                m_status[headset].Rotation[id - 6] = value;
            else if (id < 17) //Pointing local's position
                m_status[headset].PointingLocalSDPosition[id - 14] = value;
            else if (id < 20) //Start position of the headset when the IT started
                m_status[headset].PointingHeadsetStartPosition[id - 17] = value;
            else if (id < 24) //Start orientation of the headset when the IT started
                m_status[headset].PointingHeadsetStartOrientation[id - 20] = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            //Number of headsets
            if(Cursor == 0)
            {
                m_status = new HeadsetStatus[value];
                for(int i = 0; i < value; i++)
                    m_status[i] = new HeadsetStatus();
            }
            else
            {
                Int32 id = (Cursor-1)%24;
                int headset = (Cursor-1)/24;

                if (id == 0)
                    m_status[headset].ID = value;
                else if (id == 1)
                    m_status[headset].Color = value;
                else if (id == 2)
                    m_status[headset].CurrentAction = (HeadsetCurrentAction)value;
                else if (id == 10)
                    m_status[headset].PointingIT = (PointingIT)value;
                else if (id == 11)
                    m_status[headset].PointingDatasetID = value;
                else if (id == 12)
                    m_status[headset].PointingSubDatasetID = value;
            }
            base.Push(value);
        }

        public override void Push(byte value)
        {
            Int32 id = (Cursor - 1) % 24;
            int headset = (Cursor - 1) / 24;

            if (id == 13)
                m_status[headset].PointingInPublic = (value != 0);

            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if(Cursor == 0) //Number of headsets
                return (byte)'I';
            else
            {
                Int32 id = (Cursor-1) % 24;

                if (id < 3) //Color / ID / Current Action
                    return (byte)'I';

                else if (id < 6) //Position
                    return (byte)'f';

                else if (id < 10) //Rotation
                    return (byte)'f';

                else if (id < 13) //Pointing information (integer ones)
                    return (byte)'I';

                else if (id == 13) //Pointing in public space?
                    return (byte)'b';

                else if (id < 17) //Pointing floating local SD position
                    return (byte)'f';

                else if (id < 20) //Pointing position of the headset when the pointing started
                    return (byte)'f';

                else if (id < 24) //Pointing orientation of the headset when the pointing started
                    return (byte)'f';
            }
            return 0;
        }

        public override Int32 GetMaxCursor() { return 0+(m_status != null ? m_status.Length*24 : 0); }

        /// <summary>
        /// The headsets status received
        /// </summary>
        public HeadsetStatus[] HeadsetsStatus { get => m_status; set => m_status = value; }
    }
}
