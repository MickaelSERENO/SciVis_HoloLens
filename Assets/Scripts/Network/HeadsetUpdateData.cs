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
    }
}