using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message containing information regarding the new volumetric mask to apply on a specific subdataset
    /// </summary>
    public class SubDatasetVolumetricMaskMessage : ServerMessage
    {
        /// <summary>
        /// The DatasetID owning this subdataset
        /// </summary>
        public int DataID;

        /// <summary>
        /// The SubDataset ID
        /// </summary>
        public int SubDataID;

        /// <summary>
        /// The new Mask. Pay attention, here we consider only 'bits' and not 'bytes' for mask values, i.e., Mask[0] contains 8 booleans values
        /// </summary>
        public byte[] Mask;

        /// <summary>
        /// Is the volumetric mask enabled?
        /// </summary>
        public bool IsEnabled = false;

        public SubDatasetVolumetricMaskMessage(ServerType type) : base(type)
        {
        }

        public override void Push(byte[] value)
        {
            if (Cursor == 2)
                Mask = value;
            base.Push(value);
        }

        public override void Push(int value)
        {
            if (Cursor == 0)
                DataID = value;
            else if (Cursor == 1)
                SubDataID = value;
            base.Push(value);
        }

        public override void Push(byte value)
        {
            if (Cursor == 3)
                IsEnabled = (value != 0);
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            else if (Cursor == 2)
                return (byte)'a';
            else if (Cursor == 3)
                return (byte)'b';
            return 0;
        }

        public override int GetMaxCursor()
        {
            return 3;
        }
    }
}
