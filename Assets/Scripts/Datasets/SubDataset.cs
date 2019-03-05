using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sereno.SciVis;

namespace Sereno.Datasets
{
    public interface ISubDatasetCallback
    {
        void OnColorRangeChange(SubDataset dataset, float min, float max, ColorMode mode);
    }

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
        /// The Quaternion rotation
        /// 0 -> W, 1 -> i, 2 -> j, 3 -> k
        /// </summary>
        private float[] m_rotation = new float[4]{1.0f, 0.0f, 0.0f, 0.0f};

        private bool m_rotationUpdated = false;

        protected List<ISubDatasetCallback> m_listeners = new List<ISubDatasetCallback>();

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
        public void SetColor(float min, float max, ColorMode mode)
        {
            m_minClamp  = min;
            m_maxClamp  = max;
            m_colorMode = mode;

            foreach(var l in m_listeners)
                l.OnColorRangeChange(this, min, max, mode);
        }

        public void AddListener(ISubDatasetCallback clbk)
        {
            m_listeners.Add(clbk);
        }

        public void RemoveListener(ISubDatasetCallback clbk)
        {
            m_listeners.Remove(clbk);
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

        /// <summary>
        /// The Rotation Quaternion array. Rotation[0] == w, Rotation[1] = i, Rotation[2] = j, Rotation[3] = k
        /// </summary>
        /// <returns>The Rotation Quaternion array</returns>
        public float[] Rotation 
        {
            get => m_rotation; 
            set
            {
                for(int i = 0; i < Rotation.Length; i++) 
                    m_rotation[i] = value[i];
                m_rotationUpdated = true;
            }
        }

        /// <summary>
        /// Set or get if the rotation has been updated. Put at false when the new rotation has been read carrefully
        /// </summary>
        public bool RotationUpdated
        {
            get => m_rotationUpdated;
            set => m_rotationUpdated = value;
        }
    }
}