using System;


namespace Sereno.SciVis
{
    /// <summary>
    /// The TransferFunction base class. Permits to compute Transfer Function 
    /// </summary>
    public abstract class TransferFunction
    {
        /// <summary>
        /// The Transfer Function ColorMode
        /// </summary>
        private ColorMode m_mode;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mode">The ColorMode to apply</param>
        public TransferFunction(ColorMode mode = ColorMode.RAINBOW)
        {
            m_mode = mode;
        }
     
        /// <summary>
        /// Get the dimension of this Transfer function
        /// </summary>
        /// <returns>The dimension of the TF</returns>
        public uint GetDimension() {return 2;}

        /// <summary>
        /// Compute the Color t parameter
        /// </summary>
        /// <param name="values">The values to compute the color. Must be between 0.0f and 1.0f</param>
        /// <returns>The color t parameter. Negative values when errors occure (e.g array too large)</returns>
        public abstract float ComputeColor(float[] values);

        /// <summary>
        /// Compute the alpha t parameter
        /// </summary>
        /// <param name="values">The values to compute the color. Must be between 0.0f and 1.0f</param>
        /// <returns>The alpha t parameter. Negative values when errors occure (e.g array too large)</returns>
        public abstract float ComputeAlpha(float[] values);

        /// <summary>
        /// The ColorMode bound to this Transfer Function
        /// </summary>
        public ColorMode ColorMode { get => m_mode; set => m_mode = value; }
    }
}