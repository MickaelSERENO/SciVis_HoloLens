using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    /// <summary>
    /// 2D Transfer Function Texture. Bind a TF and a Texure
    /// </summary>
    public class TFTexture
    {
        /// <summary>
        /// The color mode to apply
        /// </summary>
        public ColorMode m_mode;

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
        public TFTexture(TransferFunction tf, Vector2Int textureDim, ColorMode mode)
        {
            m_mode    = mode;
            m_tf      = tf;

            m_texture    = new Texture2D(textureDim.x, textureDim.y, TextureFormat.RGBA32, false);
            m_texture.wrapModeU = TextureWrapMode.Clamp;
            m_texture.wrapModeV = TextureWrapMode.Clamp;
        }

        /// <summary>
        /// Compute the Texture pixels
        /// </summary>
        /// <param name="values">Array of values to send to the Transfer Function. Size : texelSize.x*texelSize.y</param>
        /// <param name="padding">The padding in the array between each values</param>
        /// <returns>Return true on success, false on failure</returns>
        public bool ComputeTexture(float[] values, uint padding)
        {
            if(values.Length/padding != m_texture.width*m_texture.height)
                return false;

            float[] v  = new float[padding];
            byte[] col = new byte[4*values.Length/padding];
            for(int i = 0; i < col.Length/4; i++)
            {
                Array.Copy(values, padding*i, v, 0, padding);
                Color iCol = SciVisColor.GenColor(m_mode, m_tf.ComputeColor(v));
                col[4*i]   = (byte)(iCol.r*255);
                col[4*i+1] = (byte)(iCol.g*255);
                col[4*i+2] = (byte)(iCol.b*255);
                col[4*i+3] = (byte)(m_tf.ComputeAlpha(v)*255);
            }

            m_texture.LoadRawTextureData(col);
            m_texture.Apply();
            return true;
        }

        /// <summary>
        /// Get the Texture2D created
        /// </summary>
        public Texture2D Texture {get => m_texture;}
        /// <sum>        /// The ColorMode in use. When set, use "ComputeTexture" to update the Texture
        /// </summary>
        public ColorMode ColorMode {get => m_mode; set => m_mode = value;}

        /// <summary>
        /// Get the TransferFunction
        /// </summary>
        public TransferFunction TF {get => m_tf;}
    }
}