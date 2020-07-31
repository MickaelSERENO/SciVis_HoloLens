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
        /// Called when the modification owner changes
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="ownerID">The new modification owner ID</param>
        void OnLockOwnerIDChange(SubDataset dataset, int ownerID);

        /// <summary>
        /// Called when the headset owner ID changes.
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="ownerID">The new owner ID. -1 == public SubDataset</param>
        void OnOwnerIDChange(SubDataset dataset, int ownerID);

        /// <summary>
        /// Called when the SubDataset name changes
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="name">The dataset name</param>
        void OnNameChange(SubDataset dataset, String name);

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
        /// The Modification Owner ID
        /// </summary>
        protected int m_lockOwnerID;

        /// <summary>
        /// The Owner ID. -1 == public
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
        /// The SubDataset name
        /// </summary>
        private String m_name = "";

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
        /// <param name="ownerID">The HeadsetID onwing (private/public) this SubDataset. -1 == public SubDataset</param>
        /// <param name="name">The SubDataset name</param>
        public SubDataset(Dataset parent, int ownerID = -1, String name = "")
        {
            m_parent  = parent;
            m_name    = name;
            m_ownerID = ownerID;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy"></param>
        public SubDataset(SubDataset copy)
        {
            m_parent       = copy.m_parent;
            m_lockOwnerID  = copy.m_lockOwnerID;
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

        /// <summary>
        /// the rotation to apply by the graphical object only
        /// </summary>
        public float[] GraphicalRotation
        {
            get
            {
                float[] quat = new float[4];
                
                //Apply -90° angle X rotation
                if(Parent.DatasetProperties != null && Parent.DatasetProperties.RotateX)
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

        /// <summary>
        /// the scaling to apply by the graphical object only
        /// </summary>
        public float[] GraphicalScaling
        {
            get
            {
                float[] scale = (float[]) m_scale.Clone();

                //Apply -90° angle X rotation
                if(Parent.DatasetProperties != null && Parent.DatasetProperties.InverseX)
                    scale[0] *= -1;
                
                return scale;
            }
        }

        /// <summary>
        /// The Modification Owner ID
        /// </summary>
        public int LockOwnerID
        {
            get => m_lockOwnerID;
            set
            {
                m_lockOwnerID = value;
                foreach(var l in m_listeners)
                    l.OnLockOwnerIDChange(this, m_lockOwnerID);
            }
        }
        
        /// <summary>
        /// The Owner ID. -1 == public
        /// </summary>
        public int OwnerID
        {
            get => m_ownerID;
            set
            {
                m_ownerID = value;
                foreach (var l in m_listeners)
                    l.OnOwnerIDChange(this, m_ownerID);
            }
        }

        /// <summary>
        /// The SubDataset name
        /// </summary>
        public String Name
        {
            get => m_name;
            set
            {
                m_name = value;
                foreach (var l in m_listeners)
                    l.OnNameChange(this, value);
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

        public Matrix4x4 GraphicalMatrix
        {
            get
            {
                float[] graphicalScale = GraphicalScaling;
                Vector3 pos = new Vector3();
                Vector3 scale = new Vector3();
                for (int i = 0; i < 3; i++)
                { 
                    pos[i] = Position[i];
                    if (graphicalScale[i] != 0.0f)
                        scale[i] = 1.0f / graphicalScale[i];
                    else
                        scale[i] = 0.0f;
                }
                float[] graphicalRotation = GraphicalRotation;
                return Matrix4x4.TRS(pos, new Quaternion(graphicalRotation[1], graphicalRotation[2], graphicalRotation[3], graphicalRotation[0]).normalized, scale);
            }
        }
    }
}