using System;
using System.Collections.Generic;
using Sereno.Datasets.Annotation;
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
        /// Called when a new canvas annotation has been added
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="annot">The annotation local space</param>
        void OnAddCanvasAnnotation(SubDataset dataset, CanvasAnnotation annot);

        /// <summary>
        /// Called when a new log annotation position component has been added
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="annot">The annotation</param>
        void OnAddLogAnnotationPosition(SubDataset dataset, LogAnnotationPositionInstance annot);

        /// <summary>
        /// Called when annotations are about to be cleaned
        /// </summary>
        /// <param name="dataset">The SubDataset being modified</param>
        void OnClearCanvasAnnotations(SubDataset dataset);

        /// <summary>
        /// Called when the map visibility for this situated subdataset has been changed
        /// </summary>
        /// <param name="dataset">The dataset being modified</param>
        /// <param name="visibility">The new visibility</param>
        void OnToggleMapVisibility(SubDataset dataset, bool visibility);

        /// <summary>
        /// Called when the volumetric mask of this SubDataset has changed
        /// </summary>
        /// <param name="dataset">The SubDataset calling this method</param>
        void OnChangeVolumetricMask(SubDataset dataset);

        /// <summary>
        /// Called when the depth clipping value of this SubDataset has changed
        /// </summary>
        /// <param name="dataset">The SubDataset calling this method</param>
        /// <param name="depth">The new depth clipping value</param>
        void OnChangeDepthClipping(SubDataset dataset, float depth);
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
        protected List<CanvasAnnotation> m_canvasAnnots = new List<CanvasAnnotation>();

        /// <summary>
        /// List of create annotations to visualize positions
        /// </summary>
        protected List<LogAnnotationPositionInstance> m_logAnnotPositions = new List<LogAnnotationPositionInstance>();

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
        /// The depth clipping plane factor, between 0.0f and 1.0f
        /// </summary>
        private float m_depthClip = 1.0f;

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
        /// Is the map visible?
        /// </summary>
        protected bool m_mapVisibility = true;

        /// <summary>
        /// The volumetric mask. Each bit contains one boolean value
        /// </summary>
        protected byte[] m_volumetricMask = null;

        /// <summary>
        /// Is the volumetric mask enabled?
        /// </summary>
        protected bool m_enableVolumetricMask = false;

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

            m_volumetricMask = new byte[(parent.GetNbSpatialValues()+7) / 8];
	        ResetVolumetricMask(false, false);
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

        /// <summary>
        /// Register a new canvas annotation. The event 'OnAddCanvasAnnotation' shall be fired
        /// </summary>
        /// <param name="annot">The annotation to register</param>
        public void AddCanvasAnnotation(CanvasAnnotation annot)
        {
            m_canvasAnnots.Add(annot);
            foreach (var l in m_listeners)
                l.OnAddCanvasAnnotation(this, annot);
        }


        /// <summary>
        /// Register a new LogAnnotationPositionInstance. The event 'OnAddLogAnnotationPosition' shall be fired
        /// </summary>
        /// <param name="pos">The annotation to register</param>
        public void AddLogAnnotationPosition(LogAnnotationPositionInstance pos)
        {
            m_logAnnotPositions.Add(pos);
            foreach(var l in m_listeners)
                l.OnAddLogAnnotationPosition(this, pos);
        }

        /// <summary>
        /// Clear (remove) all canvas annotations
        /// </summary>
        public void ClearCanvasAnnotations()
        {
            foreach (var l in m_listeners)
                l.OnClearCanvasAnnotations(this);
            m_canvasAnnots.Clear();
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
        /// Get the volumetric mask at x
        /// </summary>
        /// <param name="x">The x position to evaluate</param>
        /// <returns>VolumetricMask[x / 8] & (0x01 << (x % 8)) converted to a boolean value</returns>
        public bool GetVolumetricMaskAt(int x)
        {
            return (m_volumetricMask[x / 8] & (0x01 << (x % 8))) == 0 ? false : true;
        }

        /// <summary>
        /// Reset the volumetric mask to a default value
        /// </summary>
        /// <param name="value">The new mask value to apply</param>
        /// <param name="enable">Should we enable  this volumetric mask?</param>
        public void ResetVolumetricMask(bool value, bool enable = true)
        {
            byte val = (byte)(value ? 0xff : 0x00);
            for (int i = 0; i < (m_parent.GetNbSpatialValues()+7)/8; i++)
                m_volumetricMask[i] = val;

            m_enableVolumetricMask = enable;

            foreach (var l in m_listeners)
                l.OnChangeVolumetricMask(this);
        }

        /// <summary>
        /// Set the volumetric Mask
        /// </summary>
        /// <param name="val">The volumetric mask new values. Size must be equal to this.VolumetricMask.Length</param>
        /// <param name="enable">Should we enable the volumetric mask?</param>
        public void SetVolumetricMask(byte[] val, bool enable = true)
        {
            m_volumetricMask       = val;
            m_enableVolumetricMask = enable;

            foreach (var l in m_listeners)
                l.OnChangeVolumetricMask(this);
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

                //Update annotations relying on time
                if(m_tf != null)
                    foreach(var comp in m_logAnnotPositions)
                        comp.CurrentTime = m_tf.Timestep;

                //Fire the event
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

        public float DepthClipping
        {
            get => m_depthClip;
            set
            {
                if(m_depthClip != value)
                {
                    m_depthClip = value;
                    foreach (var l in m_listeners)
                        l.OnChangeDepthClipping(this, value);
                }
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
        /// The list of canvas annotations. Please, do not modify the list (but list's items can)
        /// </summary>
        public List<CanvasAnnotation> CanvasAnnotations
        {
            get => m_canvasAnnots;
        }

        /// <summary>
        /// The list of log annotation position. Please, do not modify the list (but list's items can)
        /// </summary>
        public List<LogAnnotationPositionInstance> LogAnnotationPositions
        {
            get => m_logAnnotPositions;
        }

        /// <summary>
        /// The ID of this SubDataset. Please do not modify the ID if you are not the Dataset owning it.
        /// </summary>
        public int ID
        {
            get => m_ID;
            set => m_ID = value;
        }

        /// <summary>
        /// The Volumetric Mask this SubDataset has registered.
        /// Pay attention that each value are contained inside a bit and not a byte.
        /// 
        /// Array size: (Parent.GetNbSpatialValues()+7)/8
        /// </summary>
        public byte[] VolumetricMask
        {
            get => m_volumetricMask;
            set
            {
                SetVolumetricMask(value, m_enableVolumetricMask);
            }
        }

        /// <summary>
        /// Is the associated volumetric mask enabled or not?
        /// </summary>
        public bool EnableVolumetricMask
        {
            get => m_enableVolumetricMask;
            set
            {
                if(m_enableVolumetricMask != value)
                {
                    SetVolumetricMask(VolumetricMask, value);
                }
            }
        }

        /// <summary>
        /// Compute the graphical matrix that should be used for this SubDataset. The computation is performed at each call: better save the result than calling this property repeatedly
        /// </summary>
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

        /// <summary>
        /// It the map placed at the bottom visible?
        /// </summary>
        public bool IsMapVisible
        {
            get => m_mapVisibility;
            set 
            {
                m_mapVisibility = value;
                foreach (var l in m_listeners)
                    l.OnToggleMapVisibility(this, value);
            }
        }
    }
}
