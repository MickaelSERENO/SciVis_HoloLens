using Sereno.Pointing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class NextTrialMessage : ServerMessage
    {
        /// <summary>
        /// The Tablet ID that should perform the task
        /// </summary>
        private int m_tabletID = -1;

        /// <summary>
        /// The trial ID
        /// </summary>
        private int m_trialID = -1;

        /// <summary>
        /// The study ID (1 or 2)
        /// </summary>
        private int m_studyID = -1;

        /// <summary>
        /// The pointing interaction technique interface to use in this trial
        /// </summary>
        private PointingIT m_pointingIT;

        /// <summary>
        /// The target annotation position (where should the annotation be anchored to?)
        /// </summary>
        private float[] m_annotPos = new float[3] { 0, 0, 0 };

        public NextTrialMessage(ServerType type) : base(type)
        {}

        public override void Push(int value)
        {
            if (Cursor == 0)
                m_tabletID = value;
            else if (Cursor == 1)
                m_trialID = value;
            else if (Cursor == 2)
                m_studyID = value;
            else if (Cursor == 3)
                m_pointingIT = (PointingIT)value;
            base.Push(value);
        }

        public override void Push(float value)
        {
            if (Cursor <= 6)
                m_annotPos[Cursor - 4] = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor <= 3)
                return (byte)'I';
            else if (Cursor <= 6)
                return (byte)'f';
            return 0;
        }

        public override int GetMaxCursor()
        {
            return 6;
        }

        /// <summary>
        /// The Tablet ID that should perform the task
        /// </summary>
        public int TabletID
        {
            get => m_tabletID;
            set => m_tabletID = value;
        }

        /// <summary>
        /// The trial ID
        /// </summary>
        public int TrialID
        {
            get => m_trialID;
            set => m_trialID = value;
        }

        /// <summary>
        /// The study ID
        /// </summary>
        public int StudyID
        {
            get => m_studyID;
            set => m_studyID = value;
        }

        /// <summary>
        /// The target annotation position (where should the annotation be anchored to?)
        /// </summary>
        public float[] TargetPosition
        {
            get => m_annotPos;
            set => m_annotPos = value;
        }

        /// <summary>
        /// The pointing interaction technique to use in this trial
        /// </summary>
        public PointingIT PointingIT
        {
            get => m_pointingIT;
            set => m_pointingIT = value;
        }
    }
}
