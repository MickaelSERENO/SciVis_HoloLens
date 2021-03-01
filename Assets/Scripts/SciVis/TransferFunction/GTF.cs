using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

namespace Sereno.SciVis
{
    /// <summary>
    /// Class representing a Gaussian Transfer Function
    /// Compue the Alpha based on a Gaussian function.
    /// </summary>
    public class GTF : TransferFunction
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
        public GTF(float[] center, float[] scale, float alphaMax=0.5f)
        {
            m_scale    = scale;
            m_center   = center;
            m_alphaMax = alphaMax;
        }

        [BurstCompile(CompileSynchronously = true)]
        public override float ComputeAlpha(float[] values)
        {
            if(values.Length < GetDimension())
                return -1;
            
            float rMag = 0;
            for(uint i = 0; i < GetDimension(); i++)
            {
                if(m_scale[i] != 0.0f)
                {
                    float r = (values[i] - m_center[i])/m_scale[i];
                    rMag += r * r;
                }
            }

            float val = (float)(m_alphaMax * Math.Exp(-rMag));
            if (val > 1.0f)
                return 1.0f;
            return val;
        }

        [BurstCompile(CompileSynchronously = true)]
        public override Color ComputeColor(float[] values)
        {
            float mag = 0;
            for(int i = 0; i < m_scale.Length; i++)
                mag += values[i]*values[i];
            mag = (float)(Math.Sqrt(mag) / m_scale.Length);
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
            GTF g = new GTF((float[])m_center.Clone(), (float[])m_scale.Clone(), m_alphaMax);
            g.ColorMode = ColorMode;
            g.Timestep  = Timestep;

            return g;
        }

        public override uint GetDimension() { return (uint)(m_scale.Length); }
        
        /// <summary>
        /// The center of the GTF. center.Length should equal to scale.Length
        /// </summary>
        public float[] Center { get => m_center; set => m_center = value; }

        /// <summary>
        /// The scale of the GTF. center.Length should equal to scale.Length
        /// </summary>
        public float[] Scale  { get => m_scale;  set => m_scale = value; }

        /// <summary>
        /// The alpha max component
        /// </summary>
        public float AlphaMax { get => m_alphaMax; set => m_alphaMax = value; }
    }
}