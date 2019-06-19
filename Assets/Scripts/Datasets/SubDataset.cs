using System.Collections.Generic;

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
        void OnColorRangeChange(SubDataset dataset, float min, float max);

        /// <summary>
        /// Called when the transfer function attached changed
        /// </summary>
        /// <param name="dataset">The subdataset being modified</param>
        /// <param name="tf">The new transfer function</param>
        void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf);

        /// <summary>
        /// Called when the rotation quaternion changed
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="rotationQuaternion">The new rotation to apply. See dataset.Rotation</param>
        void OnRotationChange(SubDataset dataset, float[] rotationQuaternion);

        /// <summary>
        /// Called when the 3D position changed
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="position">The new position to apply. See dataset.Position</param>
        void OnPositionChange(SubDataset dataset, float[] position);

        /// <summary>
        /// Called when the 3D scaling changed
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="scale">The new scale to apply. See dataset.Scale</param>
        void OnScaleChange(SubDataset dataset, float[] scale);

        /// <summary>
        /// Called when the owner changes
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="ownerID">The new owner ID</param>
        void OnOwnerIDChange(SubDataset dataset, int ownerID);
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
        protected TransferFunction m_tf = null;

        /// <summary>
        /// The Owner ID
        /// </summary>
        protected int       m_ownerID;
        
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
        /// The Scaling factors
        /// </summary>
        private float[] m_scale    = new float[3]{1.0f, 1.0f, 1.0f};

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
        /// Copy constructor
        /// </summary>
        /// <param name="copy"></param>
        public SubDataset(SubDataset copy)
        {
            m_parent       = copy.m_parent;
            m_minClamp     = copy.m_minClamp;
            m_minAmplitude = copy.m_minAmplitude;
            m_maxClamp     = copy.m_maxClamp;
            m_maxAmplitude = copy.m_maxAmplitude;
            m_ownerID      = copy.m_ownerID;
            m_position     = (float[])m_position.Clone();
            m_rotation     = (float[])m_rotation.Clone();
            m_scale        = (float[])m_scale.Clone();
        }

        /// <summary>
        /// Set the color of this SubDataset
        /// </summary>
        /// <param name="min">The minimum clamping (values below that should be clamped)</param>
        /// <param name="max">The maximum clamping (values above that should be clamped)</param>
        public void SetColorRange(float min, float max)
        {
            m_minClamp  = min;
            m_maxClamp  = max;

            foreach(var l in m_listeners)
                l.OnColorRangeChange(this, min, max);
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
        public float MinClamp {get => m_minClamp; set => SetColorRange(value, m_maxClamp);}

        /// <summary>
        /// The maximum clamping applied (values above that should be clamped)</param>
        /// </summary>
        public float MaxClamp { get => m_maxClamp; set => SetColorRange(m_minClamp, value); }

        /// <summary>
        /// The Transfer function bound to this SubDataset. Can be null (used then another algorithm, e.g, a grayscale algorithm)
        /// </summary>
        public TransferFunction TransferFunction
        {
            get => m_tf;
            set
            {
                m_tf = value;
                foreach(var l in m_listeners)
                    l.OnTransferFunctionChange(this, m_tf);
            }
        }

        /// <summary>
        /// The minimum amplitude found in this SubDataset
        /// </summary>
        public float MinAmplitude {get => m_minAmplitude; set => m_minAmplitude = value;}

        /// <summary>
        /// The maximum ampliude found in this SubDataset
        /// </summary>
        public float MaxAmplitude { get => m_maxAmplitude; set => m_maxAmplitude = value; }
        
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
        /// The 3D vector position (x, y, z)
        /// </summary>
        /// <returns>The 3D Position array</returns>
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

        /// <summary>
        /// The 3D scaling factors (x, y, z)
        /// </summary>
        /// <returns>The 3D Scaling array</returns>
        public float[] Scale
        {
            get => m_scale;
            set
            {
                for(int i = 0; i < Scale.Length; i++)
                    m_scale[i] = value[i];
                foreach(var l in m_listeners)
                    l.OnScaleChange(this, m_scale);
            }
        }

        public int OwnerID
        {
            get => m_ownerID;
            set
            {
                m_ownerID = value;
                foreach(var l in m_listeners)
                    l.OnOwnerIDChange(this, m_ownerID);
            }
        }
    }
}