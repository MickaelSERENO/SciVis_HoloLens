namespace Sereno.Datasets.Annotation
{
    /// <summary>
    /// Class representing a cavas annotation.
    /// </summary>
    public class CanvasAnnotation
    {
        /// <summary>
        /// The annotation position in the local subdataset space
        /// </summary>
        private float[] m_localPosition = new float[3];

        /// <summary>
        /// Constructor. Initialize an annotation
        /// </summary>
        /// <param name="localPosition">The annotation position in the SubDataset local space</param>
        public CanvasAnnotation(float[] localPosition)
        {
            LocalPosition = localPosition;
        }

        /// <summary>
        /// The annotation position in the local subdataset space
        /// </summary>
        public float[] LocalPosition
        {
            get { return m_localPosition; }
            set
            {
                for (int i = 0; i < 3; i++)
                    m_localPosition[i] = value[i];
            }
        }
    }
}
