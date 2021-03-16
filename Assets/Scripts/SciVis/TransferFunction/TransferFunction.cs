using System;
using UnityEngine;

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
        /// The current timestep parameterized for this visualization
        /// </summary>
        private float m_timestep = 0;

        /// <summary>
        /// The current minimum clipping value to use for this visualization in the value domain (between 0.0f and 1.0f). Default: 0.0f
        /// </summary>
        private float m_minClipping = 0.0f;

        /// <summary>
        /// The current maximum clipping value to use for this visualization in the value domain (between 0.0f and 1.0f). Default: 1.0f
        /// </summary>
        private float m_maxClipping = 1.0f;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mode">The ColorMode to apply</param>
        public TransferFunction(ColorMode mode = ColorMode.RAINBOW)
        {
            m_mode = mode;
        }

        /// <summary>
        /// Is this transfer function using the gradient as the latest dimension?
        /// </summary>
        /// <returns>true if yes, false otherwise</returns>
        public virtual bool HasGradient() { return false; }

        /// <summary>
        /// Get the dimension of this Transfer function
        /// </summary>
        /// <returns>The dimension of the TF</returns>
        public abstract uint GetDimension();

        /// <summary>
        /// Compute the Color t parameter
        /// </summary>
        /// <param name="values">The values to compute the color. Must be between 0.0f and 1.0f. Length: at minimum the length of GetDimension()</param>
        /// <returns>The resulting color</returns>
        public abstract Color ComputeColor(float[] values);

        /// <summary>
        /// Compute the alpha t parameter
        /// </summary>
        /// <param name="values">The values to compute the color. Must be between 0.0f and 1.0f</param>
        /// <returns>The alpha t parameter. Negative values when errors occure (e.g array too large)</returns>
        public abstract float ComputeAlpha(float[] values);

        /// <summary>
        /// Clone this object
        /// </summary>
        /// <returns></returns>
        public abstract TransferFunction Clone();
        
        /// <summary>
        /// The ColorMode bound to this Transfer Function
        /// </summary>
        public ColorMode ColorMode { get => m_mode; set => m_mode = value; }

        /// <summary>
        /// The current timestep parameterized for this visualization. Its value must be positive
        /// </summary>
        public float Timestep {get => m_timestep; set => m_timestep = value;}

        /// <summary>
        /// The current minimum clipping value to use for this visualization in the value domain (between 0.0f and 1.0f). Default: 0.0f
        /// </summary>
        public float MinClipping { get => m_minClipping; set => m_minClipping = value;}

        /// <summary>
        /// The current maximum clipping value to use for this visualization in the value domain (between 0.0f and 1.0f). Default: 1.0f
        /// </summary>
        public float MaxClipping { get => m_maxClipping; set => m_maxClipping = value; }
    }
}