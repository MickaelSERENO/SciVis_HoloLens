using Sereno.Pointing;
using System;

namespace Sereno.Network
{
    public class HeadsetUpdateData
    {
        /// <summary>
        /// The X, Y and Z headset 3D position
        /// </summary>
        public float[] Position = new float[3];

        /// <summary>
        /// The W, X, Y and Z Rotation Quaternion value
        /// </summary>
        public float[] Rotation = new float[4];

        /// <summary>
        /// The pointing interaction technique in use
        /// </summary>
        public PointingIT PointingIT = PointingIT.NONE;

        /// <summary>
        /// The DatasetID linked to the pointing technique
        /// </summary>
        public Int32 PointingDatasetID = -1;

        /// <summary>
        /// The SubDataset ID linked to the pointing technique
        /// </summary>
        public Int32 PointingSubDatasetID = -1;

        /// <summary>
        /// Is the public pointing done in the public space?
        /// </summary>
        public bool PointingInPublic = true;

        /// <summary>
        /// The position of the pointing technique in the subdataset local space
        /// </summary>
        public float[] PointingLocalSDPosition = new float[3];

        /// <summary>
        /// The position headset when the pointing technique started
        /// </summary>
        public float[] PointingHeadsetStartPosition = new float[3];
    }
}