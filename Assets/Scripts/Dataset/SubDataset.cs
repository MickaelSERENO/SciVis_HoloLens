using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    public class SubDataset
    {
        /// <summary>
        /// The Dataset owning this sub dataset
        /// </summary>
        protected Dataset   m_parent;

        /// <summary>
        /// The minimum clamp to apply for this subdataset
        /// </summary>
        protected float     m_minClamp  = 0.0f;

        /// <summary>
        /// The maximum clamp to apply for this subdataset
        /// </summary>
        protected float     m_maxClamp  = 1.0f;

        /// <summary>
        /// The ColorMode to apply for this subdataset
        /// </summary>
        protected ColorMode m_colorMode = ColorMode.RAINBOW;

        /// <summary>
        /// The minimum amplitude of this dataset
        /// </summary>
        protected float m_minAmplitude = 0.0f;

        /// <summary>
        /// The maximum amplitude of this dataset
        /// </summary>
        protected float m_maxAmplitude = 0.0f;

        /// <summary>
        /// The transfer function to use
        /// </summary>
        protected TFTexture m_tfTexture;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent">The Dataset parent</param>
        public SubDataset(Dataset parent)
        {
            m_parent = parent;
        }

        /// <summary>
        /// Set the color of this SubDataset
        /// </summary>
        /// <param name="min">The minimum clamping (values below that should be clamped)</param>
        /// <param name="max">The maximum clamping (values above that should be clamped)</param>
        /// <param name="mode">The ColorMode to apply</param>
        void SetColor(float min, float max, ColorMode mode)
        {
            m_minClamp  = min;
            m_maxClamp  = max;
            m_colorMode = mode;
        }

        /// <summary>
        /// The Dataset parent
        /// </summary>
        public Dataset Parent {get => m_parent;}

        /// <summary>
        /// The minimum clamping applied (values below that should be clamped)
        /// </summary>
        public float MinClamp {get => m_minClamp; set => SetColor(value, m_maxClamp, m_colorMode);}

        /// <summary>
        /// The maximum clamping applied (values above that should be clamped)</param>
        /// </summary>
        public float MaxClamp { get => m_maxClamp; set => SetColor(m_minClamp, value, m_colorMode); }

        /// <summary>
        /// The ColorMode applied
        /// </summary>
        public ColorMode ColorMode { get => m_colorMode; set => SetColor(m_minClamp, m_minClamp, value); }

        /// <summary>
        /// The minimum amplitude found in this SubDataset
        /// </summary>
        public float MinAmplitude {get => m_minAmplitude; set => m_minAmplitude = value;}

        /// <summary>
        /// The maximum ampliude found in this SubDataset
        /// </summary>
        public float MaxAmplitude { get => m_maxAmplitude; set => m_maxAmplitude = value; }

        /// <summary>
        /// The Transfer function bound to this object
        /// </summary>
        public TFTexture TFTexture {get => m_tfTexture; set => m_tfTexture = value;}
    }
}