using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
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
        public TriangularGTF(float[] center, float[] scale, float alphaMax = 0.5f)
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
        [BurstCompile(CompileSynchronously = true)]
        public override float ComputeAlpha(float[] values)
        {
            float grad = values[values.Length-1];
            if(grad == 0)
                return 0;

            float r0 = 1.0f/grad;
            float r1Mag = 0;

            for(uint i = 0; i < GetDimension()-1; i++)
            {
                if(m_scale[i] != 0)
                {
                    float r1 = r0 * (values[i] - m_center[i]) / m_scale[i];
                    r1Mag += r1 * r1;
                }
            }

            float val = (float)(m_alphaMax * Math.Exp(-r1Mag));
            if (val > 1.0f)
                return 1.0f;
            return val;
        }

        /// <summary>
        /// Compute the t parameter used for Colors
        /// </summary>
        /// <param name="values">The values to take account of</param>
        /// <returns>The color corresponding to the color mode and length(values)/(values.length) with values a vector where gradient is discarded (i.e, we take into account indices from 0 to values.length - 2) </returns>
        [BurstCompile(CompileSynchronously = true)]
        public override Color ComputeColor(float[] values)
        {
            if(values.Length < m_scale.Length)
                return new Color(0,0,0,0);

            float mag = 0;
            for(int i = 0; i < m_scale.Length; i++)
                mag += values[i]*values[i];
            mag = (float)(Math.Sqrt(mag)/m_scale.Length);
            if (mag < MinClipping)
                mag = 0;
            else if (mag > MaxClipping)
                mag = 1;
            else
                mag = (mag - MinClipping) / (MaxClipping - MinClipping);
            return SciVisColor.GenColor(ColorMode, mag);
        }

        public override TransferFunction Clone()
        {
            TriangularGTF g = new TriangularGTF((float[])m_center.Clone(), (float[])m_scale.Clone(), m_alphaMax);
            g.ColorMode = ColorMode;
            g.Timestep  = Timestep;
            g.MinClipping = MinClipping;
            g.MaxClipping = MaxClipping;

            return g;
        }

        public override uint GetDimension() { return (uint)(m_scale.Length+1); }

        public override bool HasGradient() { return true; }
        
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