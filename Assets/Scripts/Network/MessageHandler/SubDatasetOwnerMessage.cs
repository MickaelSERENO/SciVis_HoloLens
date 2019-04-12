using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class SubDatasetOwnerMessage : ServerMessage
    {
        /// <summary>
        /// The SubDataset ID
        /// </summary>
        private int m_sdID      = 0;

        /// <summary>
        /// The Dataset ID
        /// </summary>
        private int m_datasetID = 0;

        /// <summary>
        /// The Headset ID owning this subdataset
        /// </summary>
        private int m_headsetID = 0;

        public SubDatasetOwnerMessage(ServerType type) : base(type)
        {}

        public override void Push(int value)
        {
            if(Cursor == 0)
                m_datasetID = value;
            else if(Cursor == 1)
                m_sdID = value;
            else if(Cursor == 2)
                m_headsetID = value;
            base.Push(value);
        }

        public override int GetMaxCursor()
        {
            return 2;
        }

        public override byte GetCurrentType()
        {
            if(Cursor <= 2)
                return (byte)'I';
            return 0;
        }

        /// <summary>
        /// The Dataset ID
        /// </summary>
        public int DatasetID    { get => m_datasetID; set => m_datasetID = value; }

        /// <summary>
        /// The SubDataset ID
        /// </summary>
        public int SubDatasetID { get => m_sdID;      set => m_sdID = value; }

        /// <summary>
        /// The Headset ID owning this subdataset
        /// </summary>
        public int HeadsetID    { get => m_headsetID; set => m_headsetID = value; }
    }
}
