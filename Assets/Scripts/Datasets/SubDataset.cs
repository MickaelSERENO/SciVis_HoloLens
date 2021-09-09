using System;
using System.Collections.Generic;
using System.Linq;
using Sereno.Datasets.Annotation;
using Sereno.SciVis;
using UnityEngine;

namespace Sereno.Datasets
{
    public enum SubDatasetVisibility
    {
        VISIBLE = 0, //Visible
        PRIVATE = 1, //Visible only by its owner. Non-owner sees a questionmark
        GONE    = 2, //The associated game object is not visible at all.
    };

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
        /// <param name="minDepth">The new max depth clipping value</param>
        /// <param name="maxDepth">The new max depth clipping value</param>
        void OnChangeDepthClipping(SubDataset dataset, float minDepth, float maxDepth);

        /// <summary>
        /// Called when the subdataset group associated with this SubDataset has changed
        /// </summary>
        /// <param name="dataset">The SubDataset calling this method</param>
        /// <param name="sdg">The new SubDataset Group owning this SubDataset. null == no group owning this object</param>
        void OnSetSubDatasetGroup(SubDataset dataset, SubDatasetGroup sdg);

        /// <summary>
        /// Called when the subdataset visibility of this subdataset has changed
        /// </summary>
        /// <param name="dataset">The SubDataset calling this method</param>
        /// <param name="visibility">The new visibility value</param>
        void OnSetVisibility(SubDataset dataset, SubDatasetVisibility visibility);

        /// <summary>
        /// Called when the transfer function computation of this subdataset has changed
        /// </summary>
        /// <param name="dataset">The SubDataset calling this method</param>
        /// <param name="tfComputation">The new transfer function computation. A cast is required</param>
        void OnSetTFComputation(SubDataset dataset, object tfComputation);
    }

    public class SubDataset
    {
        /// <summary>
        /// The Dataset owning this sub dataset
        /// </summary>
        protected Dataset   m_parent;

        /// <summary>
        /// The subdataset group owning this sub dataset
        /// </summary>
        protected SubDatasetGroup m_sdg = null;

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
        /// The maxDepth clipping plane factor, between 0.0f and 1.0f
        /// </summary>
        private float m_maxDepthClip = 1.0f;

        /// <summary>
        /// The minDepth clipping plane factor, between 0.0f and 1.0f
        /// </summary>
        private float m_minDepthClip = 0.0f;

        /// <summary>
        /// The SubDataset name
        /// </summary>
        private String m_name = "";

        /// <summary>
        /// The visibility status of the subdataset
        /// </summary>
        private SubDatasetVisibility m_visibility = SubDatasetVisibility.VISIBLE;

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
        /// The transfer function computation result
        /// </summary>
        protected object m_tfComputation = null;

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
            lock (this)
            { 
                m_canvasAnnots.Add(annot);
                foreach (var l in m_listeners)
                    l.OnAddCanvasAnnotation(this, annot);
            }
        }


        /// <summary>
        /// Register a new LogAnnotationPositionInstance. The event 'OnAddLogAnnotationPosition' shall be fired
        /// </summary>
        /// <param name="pos">The annotation to register</param>
        public void AddLogAnnotationPosition(LogAnnotationPositionInstance pos)
        {
            lock (this)
            {
                m_logAnnotPositions.Add(pos);
                pos.CurrentTime = (m_tf == null) ? 0 : m_tf.Timestep;
                foreach (var l in m_listeners)
                    l.OnAddLogAnnotationPosition(this, pos);
            }
        }

        /// <summary>
        /// Clear (remove) all canvas annotations
        /// </summary>
        public void ClearCanvasAnnotations()
        {
            lock (this)
            {
                foreach (var l in m_listeners)
                    l.OnClearCanvasAnnotations(this);
                m_canvasAnnots.Clear();
            }
        }


        /// <summary>
        /// Add a callback listener
        /// </summary>
        /// <param name="clbk">The callback listener to call</param>
        public void AddListener(ISubDatasetCallback clbk)
        {
            lock (this)
                m_listeners.Add(clbk);
        }

        /// <summary>
        /// Remove a callback listener
        /// </summary>
        /// <param name="clbk">The callback listener to not call anymore</param>
        public void RemoveListener(ISubDatasetCallback clbk)
        {
            lock (this)
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
            lock (this)
            {
                byte val = (byte)(value ? 0xff : 0x00);
                for (int i = 0; i < (m_parent.GetNbSpatialValues() + 7) / 8; i++)
                    m_volumetricMask[i] = val;

                m_enableVolumetricMask = enable;

                foreach (var l in m_listeners)
                    l.OnChangeVolumetricMask(this);
            }
        }

        /// <summary>
        /// Set the volumetric Mask
        /// </summary>
        /// <param name="val">The volumetric mask new values. Size must be equal to this.VolumetricMask.Length</param>
        /// <param name="enable">Should we enable the volumetric mask?</param>
        public void SetVolumetricMask(byte[] val, bool enable = true)
        {
            lock (this)
            {
                m_volumetricMask = val;
                m_enableVolumetricMask = enable;

                foreach (var l in m_listeners)
                    l.OnChangeVolumetricMask(this);
            }
        }

        /// <summary>
        /// The Dataset parent
        /// </summary>
        public Dataset Parent {get => m_parent;}

        public object TFComputation
        {
            get => m_tfComputation;
            set
            {
                lock (this)
                {
                    if (m_tfComputation != value)
                    {
                        m_tfComputation = value;
                        foreach (ISubDatasetCallback l in m_listeners)
                            l.OnSetTFComputation(this, m_tfComputation);
                    }
                }
            }
        }

        /// <summary>
        /// The SubDatasetGroup owning this subdataset
        /// </summary>
        public SubDatasetGroup SubDatasetGroup 
        {
            get => m_sdg;
            set
            {
                lock (this)
                {
                    if (m_sdg == value)
                        return;
                    if (m_sdg != null)
                    {
                        SubDatasetGroup old = m_sdg;
                        m_sdg = null;
                        old.RemoveSubDataset(this);
                    }
                    m_sdg = value;
                    if (m_sdg != null && !m_sdg.SubDatasets.Contains(this))
                        if (!m_sdg.AddSubDataset(this))
                            m_sdg = null;

                    foreach (var l in m_listeners)
                        l.OnSetSubDatasetGroup(this, m_sdg);
                }
            }
        }

        /// <summary>
        /// The Transfer function bound to this SubDataset. Can be null (used then another algorithm, e.g, a grayscale algorithm)
        /// </summary>
        public TransferFunction TransferFunction
        {
            get => m_tf;
            set
            {
                lock (this)
                {
                    if (m_tf == value || (m_tf != null && m_tf.Equals(value)))
                        return;

                    m_tf = value;

                    //Update annotations relying on time
                    if (m_tf != null)
                        foreach (var comp in m_logAnnotPositions)
                            comp.CurrentTime = m_tf.Timestep;

                    //Fire the event
                    foreach (var l in m_listeners)
                        l.OnTransferFunctionChange(this, m_tf);
                }
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
                lock (this)
                {
                    if (m_rotation.SequenceEqual(value))
                        return;

                    for (int i = 0; i < Rotation.Length; i++)
                        m_rotation[i] = value[i];

                    foreach (var l in m_listeners)
                        l.OnRotationChange(this, m_rotation);
                }
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
                lock (this)
                {
                    if (m_position.SequenceEqual(value))
                        return;

                    for (int i = 0; i < Position.Length; i++)
                        m_position[i] = value[i];
                    foreach (var l in m_listeners)
                        l.OnPositionChange(this, m_position);
                }
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
                lock (this)
                {
                    if (m_scale.SequenceEqual(value))
                        return;
                    for (int i = 0; i < Scale.Length; i++)
                        m_scale[i] = value[i];

                    foreach (var l in m_listeners)
                        l.OnScaleChange(this, m_scale);
                }
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
        /// Set the depth clipping plane values. Values should be clamped between 0.0f and 1.0f
        /// </summary>
        /// <param name="minD">The minimum depth clipping value</param>
        /// <param name="maxD">The maximum depth clipping value</param>
        public void SetDepthClipping(float minD, float maxD)
        {
            if(minD != m_minDepthClip || maxD != m_maxDepthClip)
            {
                m_minDepthClip = minD;
                m_maxDepthClip = maxD;
                
                foreach (var l in m_listeners)
                    l.OnChangeDepthClipping(this, MinDepthClipping, MaxDepthClipping);
            }
        }

        /// <summary>
        /// The max depth clipping value, clamped between 0.0f and 1.0f
        /// </summary>
        /// <value></value>
        public float MaxDepthClipping
        {
            get => m_maxDepthClip;
            set
            {

                lock (this)
                {
                    if (m_maxDepthClip != value)
                    {
                        m_maxDepthClip = value;
                        foreach (var l in m_listeners)
                            l.OnChangeDepthClipping(this, MinDepthClipping, value);
                    }
                }
            }
        }

        
        /// <summary>
        /// The min depth clipping value, clamped between 0.0f and 1.0f
        /// </summary>
        /// <value></value>
        public float MinDepthClipping
        {
            get => m_minDepthClip;
            set
            {

                lock (this)
                {
                    if (m_minDepthClip != value)
                    {
                        m_minDepthClip = value;
                        foreach (var l in m_listeners)
                            l.OnChangeDepthClipping(this, value, MaxDepthClipping);
                    }
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
                lock (this)
                {
                    if (m_lockOwnerID == value)
                        return;
                    m_lockOwnerID = value;
                    foreach (var l in m_listeners)
                        l.OnLockOwnerIDChange(this, m_lockOwnerID);
                }
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
                lock (this)
                {
                    if (m_ownerID == value)
                        return;
                    m_ownerID = value;
                    foreach (var l in m_listeners)
                        l.OnOwnerIDChange(this, m_ownerID);
                }
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
                lock (this)
                {
                    if (m_name != null && m_name.Equals(value))
                        return;
                    m_name = value;
                    foreach (var l in m_listeners)
                        l.OnNameChange(this, value);
                }
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
            set
            {
                lock (this)
                    m_ID = value;
            }
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
                lock (this)
                {
                    if (m_mapVisibility == value)
                        return;
                    m_mapVisibility = value;
                    foreach (var l in m_listeners)
                        l.OnToggleMapVisibility(this, value);
                }
            }
        }

        /// <summary>
        /// The visibility status of this subdataset
        /// </summary>
        public SubDatasetVisibility Visibility
        {
            get => m_visibility;
            set
            {
                lock (this)
                {
                    if (m_visibility != value)
                    {
                        m_visibility = value;
                        foreach (var l in m_listeners)
                            l.OnSetVisibility(this, value);
                    }
                }
            }
        }
    }
}
