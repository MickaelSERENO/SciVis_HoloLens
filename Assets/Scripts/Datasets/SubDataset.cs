using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sereno.SciVis;

namespace Sereno.Datasets
{
    /// <summary>
    /// Callback interface when the SubDataset internal state changed
    /// </summary>
    public interface ISubDatasetCallback
    {
        /// <summary>
        /// Called when the color range changed
        /// </summary>
        /// <param name="dataset">The subdataset being modified</param>
        /// <param name="min">The minimum clamping color value of the sub dataset</param>
        /// <param name="max">The maximum clamping color value of the sub dataset</param>
        /// <param name="mode">The new color mode to apply</param>
        void OnColorRangeChange(SubDataset dataset, float min, float max, ColorMode mode);

        /// <summary>
        /// Called when the rotation quaternion changed
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="rotationQuaternion">The new rotation to apply. See dataset.Rotation</param>
        void OnRotationChange(SubDataset dataset, float[] rotationQuaternion);

        /// <summary>
        /// Called when the position quaternion changed
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="position">The new position to apply. See dataset.Position</param>
        void OnPositionChange(SubDataset dataset, float[] position);
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

        /// <summary>
        /// The Vector3 position
        /// </summary>
        private float[] m_position = new float[3]{0.0f, 0.0f, 0.0f};

        /// <summary>
        /// The listeners to call when the internal state changed
        /// </summary>
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

        /// <summary>
        /// Add a callback listener
        /// </summary>
        /// <param name="clbk">The callback listener to call</param>
        public void AddListener(ISubDatasetCallback clbk)
        {
            m_listeners.Add(clbk);
        }

        /// <summary>
        /// Remove a callback listener
        /// </summary>
        /// <param name="clbk">The callback listener to not call anymore</param>
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
                foreach(var l in m_listeners)
                    l.OnRotationChange(this, m_rotation);
            }
        }
        /// <summary>
        /// The Rotation Quaternion array. Rotation[0] == w, Rotation[1] = i, Rotation[2] = j, Rotation[3] = k
        /// </summary>
        /// <returns>The Rotation Quaternion array</returns>
        public float[] Position
        {
            get => m_position; 
            set
            {
                for(int i = 0; i < Position.Length; i++) 
                    m_position[i] = value[i];
                foreach(var l in m_listeners)
                    l.OnPositionChange(this, m_position);
            }
        }
    }
}