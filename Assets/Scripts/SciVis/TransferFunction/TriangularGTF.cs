using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno.SciVis
{
    /// <summary>
    /// Triangular Gaussian Transfer Function.
    /// </summary>
    public class TriangularGTF : TransferFunction
    {
        /// <summary>
        /// The Center of this TF
        /// </summary>
        private float[] m_center;

        /// <summary>
        /// The Scale to apply along each value
        /// </summary>
        private float[] m_scale;

        /// <summary>
        /// Maximum alpha component
        /// </summary>
        private float   m_alphaMax;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scale">The scale to apply along each dimensions</param>
        /// <param name="center">The center to apply along each dimensions</param>
        /// <param name="alphaMax">The maximum alpha value</param>
        public TriangularGTF(float[] scale, float[] center, float alphaMax = 1.0f)
        {
            m_scale    = scale;
            m_center   = center;
            m_alphaMax = alphaMax;
        }

        /// <summary>
        /// Compute the alpha channel based on the scalar values and the gradient magnitude
        /// </summary>
        /// <param name="values">Array of size at least == 2 . Last value correspond to the gradient magnitude</param>
        /// <returns>The Triangular Gaussian Transfer Function alpha component</returns>
        public override float ComputeAlpha(float[] values)
        {
            float grad = values[values.Length-1];
            if(grad == 0)
                return 0;

            float r0 = 1.0f/grad;
            float r1Mag = 0;

            for(uint i = 0; i < values.Length-1; i++)
            {
                float r1 = r0*m_scale[i]*(values[i] - m_center[i]);
                r1Mag += r1*r1;
            }

            return (float)Math.Min(m_alphaMax*Math.Exp(-r1Mag), 1.0f);
        }

        /// <summary>
        /// Compute the t parameter used for Colors
        /// </summary>
        /// <param name="values">The values to take account of</param>
        /// <returns>length(values)/(values.length) with values a vector where gradient is discarded (i.e, we take into account indices from 0 to values.length - 2) </returns>
        public override float ComputeColor(float[] values)
        {
            if(values.Length == 1)
                return values[0];

            float mag = 0;
            for(int i = 0; i < values.Length-1; i++)
                mag += values[i]*values[i];
            mag = (float)(Math.Sqrt(mag/(values.Length-1)));
            return mag;
        }

        public override uint GetDimension() { return (uint)(m_scale.Length+1); }
        
        /// <summary>
        /// The center of the GTF
        /// </summary>
        public float[] Center { get => m_center; set => m_center = value; }

        /// <summary>
        /// The scale of the GTF
        /// </summary>
        public float[] Scale { get => m_scale; set => m_scale = value; }

        /// <summary>
        /// The alpha max component
        /// </summary>
        public float AlphaMax { get => m_alphaMax; set => m_alphaMax = value; }
    }
}