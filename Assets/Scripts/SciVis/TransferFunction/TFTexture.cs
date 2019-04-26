using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno.SciVis
{
    /// <summary>
    /// 2D Transfer Function Texture. Bind a TF and a Texure
    /// </summary>
    public class TFTexture
    {
        private Vector2Int m_dimensions;
        private byte[]     m_colors;

        /// <summary>
        /// The Texture2D created
        /// </summary>
        public Texture2D m_texture;

        /// <summary>
        /// The TransferFunction created
        /// </summary>
        public TransferFunction m_tf;
        
        /// <summary>
        /// Constructor. The texture is created but not initialized. Use ComputeTexture to do so
        /// </summary>
        /// <param name="tf">The Transfer Function to use</param>
        /// <param name="textureDim">The texture dimension wanted</param>
        /// <param name="mode">The ColorMode to apply</param>
        public TFTexture(TransferFunction tf, Vector2Int textureDim)
        {
            m_tf      = tf;
            m_dimensions = textureDim;
        }

        /// <summary>
        /// Compute the Texture pixels
        /// </summary>
        /// <param name="values">Array of values to send to the Transfer Function. Size : texelSize.x*texelSize.y</param>
        /// <param name="padding">The padding in the array between each values</param>
        /// <returns>Return true on success, false on failure</returns>
        public bool ComputeTexture(float[] values, uint padding)
        {
            if(values.Length/padding != m_dimensions.x*m_dimensions.y)
                return false;

            float[] v  = new float[padding];
            m_colors = new byte[4*values.Length/padding];
            for(int i = 0; i < m_colors.Length/4; i++)
            {
                Array.Copy(values, padding*i, v, 0, padding);
                Color iCol = SciVisColor.GenColor(m_tf.ColorMode, m_tf.ComputeColor(v));
                m_colors[4*i]   = (byte)(iCol.r*255);
                m_colors[4*i+1] = (byte)(iCol.g*255);
                m_colors[4*i+2] = (byte)(iCol.b*255);
                m_colors[4*i+3] = (byte)(m_tf.ComputeAlpha(v)*255);
            }
            return true;
        }

        public void UpdateTexture()
        {
            if(m_colors != null)
            {
                m_texture    = new Texture2D(m_dimensions.x, m_dimensions.y, TextureFormat.RGBA32, false);
                m_texture.wrapModeU = TextureWrapMode.Clamp;
                m_texture.wrapModeV = TextureWrapMode.Clamp;

                m_texture.LoadRawTextureData(m_colors);
                m_texture.Apply();
                m_colors = null;
            }
        }

        /// <summary>
        /// Get the Texture2D created
        /// </summary>
        public Texture2D Texture {get => m_texture;}

        /// <summary>
        /// Get the TransferFunction
        /// </summary>
        public TransferFunction TF {get => m_tf;}
    }
}