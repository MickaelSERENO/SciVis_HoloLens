﻿using System;
using System.Collections;
using System.Collections.Generic;
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

        public override float ComputeAlpha(float[] values)
        {
            if(values.Length < m_scale.Length || values.Length < m_center.Length)
                return -1;
            
            float rMag = 0;
            for(uint i = 0; i < GetDimension(); i++)
            {
                if(m_scale[i] != 0.0f)
                {
                    float r = (values[i] - m_center[i]) / m_scale[i];
                    rMag += r * r;
                }
            }

            return (float)(Math.Min(m_alphaMax*Math.Exp(-rMag), 1.0f));
        }

        /// <summary>
        /// Compute the t parameter used for Colors
        /// </summary>
        /// <param name="values">The values to take account of</param>
        /// <returns>values[0]</returns>
        public override float ComputeColor(float[] values)
        {
            float mag = 0;
            for (int i = 0; i < m_scale.Length; i++)
                mag += values[i] * values[i];
            mag = (float)(Math.Sqrt(mag / m_scale.Length));
            return mag;
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