using System;
using System.Collections.Generic;
using Sereno.SciVis;
using UnityEngine;

namespace Sereno.Datasets
{
    /// <summary>
    /// Callback interface when the SubDataset internal state changed
    /// </summary>
    public interface ISubDatasetCallback
    {
        /// <summary>
        /// Called when the transfer function attached changed. This method can be called regularly from e.g., the Network or UI Update thread. 
        /// It may be interesting to manage that call in a separated thread to not block the calling thread
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

        /// <summary>
        /// Called when a new annotation has been added
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="annot">The annotation local space</param>
        void OnAddAnnotation(SubDataset dataset, Annotation annot);

        /// <summary>
        /// Called when annotations are about to be cleaned
        /// </summary>
        /// <param name="dataset">The SubDataset being modified</param>
        void OnClearAnnotations(SubDataset dataset);
    }

    public class SubDataset
    {
        /// <summary>
        /// The Dataset owning this sub dataset
        /// </summary>
        protected Dataset   m_parent;

        /// <summary>
        /// The transfer function to use
        /// </summary>
        protected TransferFunction m_tf = null;

        /// <summary>
        /// The Owner ID
        /// </summary>
        protected int m_ownerID;

        /// <summary>
        /// List of created annotations
        /// </summary>
        protected List<Annotation> m_annotations = new List<Annotation>();
        
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
        /// The ID of this SubDataset
        /// </summary>
        protected int m_ID; 

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent">The Dataset parent</param>
        public SubDataset(Dataset parent)
        {
            m_parent   = parent;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy"></param>
        public SubDataset(SubDataset copy)
        {
            m_parent       = copy.m_parent;
            m_ownerID      = copy.m_ownerID;
            m_position     = (float[])m_position.Clone();
            m_rotation     = (float[])m_rotation.Clone();
            m_scale        = (float[])m_scale.Clone();
        }

        public void AddAnnotation(Annotation annot)
        {
            m_annotations.Add(annot);
            foreach (var l in m_listeners)
                l.OnAddAnnotation(this, annot);
        }

        public void ClearAnnotations()
        {
            foreach (var l in m_listeners)
                l.OnClearAnnotations(this);
            m_annotations.Clear();
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
        /// The Rotation Quaternion array. Rotation[0] == w, Rotation[1] = i, Rotation[2] = j, Rotation[3] = k
        /// </summary>
        /// <returns>The Rotation Quaternion array</returns>
        public float[] Rotation 
        {
            get => m_rotation; 
            set
            {
                for (int i = 0; i < Rotation.Length; i++) 
                    m_rotation[i] = value[i];
                
                foreach (var l in m_listeners)
                    l.OnRotationChange(this, m_rotation);
            }
        }

        public float[] GraphicalRotation
        {
            get
            {
                float[] quat = new float[4];
                
                //Apply -90° angle X rotation
                if (Parent.DatasetProperties.RotateX)
                {
                    float qW = (float)Math.Cos(-Math.PI / 4.0);
                    float qX = (float)Math.Sin(-Math.PI / 4.0);

                    quat[0] = m_rotation[0] * qW - m_rotation[1] * qX;
                    quat[1] = m_rotation[1] * qW + m_rotation[0] * qX;
                    quat[2] = m_rotation[2] * qW + m_rotation[3] * qX;
                    quat[3] = m_rotation[3] * qW - m_rotation[2] * qX;
                }
                else
                    for (int i = 0; i < 4; i++)
                        quat[i] = m_rotation[i];

                return quat;
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

        /// <summary>
        /// The list of annotation. Please, do not modify the list (but list's items can)
        /// </summary>
        public List<Annotation> Annotations
        {
            get => m_annotations;
        }

        /// <summary>
        /// The ID of this SubDataset. Please do not modify the ID if you are not the Dataset owning it.
        /// </summary>
        public int ID
        {
            get => m_ID;
            set => m_ID = value;
        }
    }
}