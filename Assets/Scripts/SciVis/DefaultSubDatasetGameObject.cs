using Sereno.Datasets;
using Sereno.Datasets.Annotation;
using Sereno.DataVis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sereno.SciVis
{
    public class DefaultSubDatasetGameObject : MonoBehaviour, IChangeInternalState, ISubDatasetCallback
    {
        /// <summary>
        /// The Outline gameobject
        /// </summary>
        public GameObject Outline = null;

        /// <summary>
        /// The Name UI GameObject to display the dataset name
        /// </summary>
        public TextMesh NameUI = null;

        /// <summary>
        /// The NameUI pivot used to always put the name "on top" of the gameobject
        /// </summary>
        public GameObject NameUIPivot = null;

        /// <summary>
        /// The Color to apply when the GameObject is being targeted
        /// </summary>
        public Color ColorOnTargetPointingIT = new Color(1.0f, 1.0f, 1.0f);

        /// <summary>
        /// the canvas annotation prefab to use
        /// </summary>
        public CanvasAnnotationGameObject CanvasAnnotationPrefab = null;

        /// <summary>
        /// the log annotation position prefab to use
        /// </summary>
        public LogAnnotationPositionGameObject LogAnnotationPositionPrefab = null;

        /// <summary>
        /// The Miniature prefab
        /// </summary>
        public DefaultSubDatasetGameObject DefaultMiniaturePrefab;

        /// <summary>
        /// The map texture to display if the dataset requires it
        /// </summary>
        public MapTextureGameObject MapTextureGameObject;

        /// <summary>
        /// The SubDataset bound
        /// </summary>
        protected SubDataset m_sd = null;

        /// <summary>
        /// Object providing the needed data
        /// </summary>
        protected IDataProvider m_dataProvider = null;

        /// <summary>
        /// The outline gameobject created from "Outline"
        /// </summary>
        protected GameObject m_outline;

        /// <summary>
        /// The new outline color to apply
        /// </summary>
        protected Color m_outlineColor;
        
        /// <summary>
        /// Should we update the outline color?
        /// </summary>
        protected bool m_updateOutlineColor = false;

        /// <summary>
        /// The new Quaternion received from the SmallMultiple.
        /// </summary>
        protected Quaternion m_newQ;

        /// <summary>
        /// The new position received from the SmallMultiple.
        /// </summary>
        protected Vector3 m_newP;

        /// <summary>
        /// The new scale received from the SmallMultiple
        /// </summary>
        protected Vector3 m_newS;

        /// <summary>
        /// The new name received from the SmallMultiple
        /// </summary>
        protected String m_newName;

        /// <summary>
        /// Should we update the rotation quaternion?
        /// </summary>
        protected bool m_updateQ = false;

        /// <summary>
        /// Should we update the 3D position?
        /// </summary>
        protected bool m_updateP = false;

        /// <summary>
        /// Should we update the 3D scaling?
        /// </summary>
        protected bool m_updateS = false;

        /// <summary>
        /// Should we update the displayed name?
        /// </summary>
        protected bool m_updateName = false;

        /// <summary>
        /// Is this GameObject targeted?
        /// </summary>
        protected bool m_isTargeted = false;
        
        /// <summary>
        /// Is this GameObject a miniature
        /// </summary>
        protected bool m_isMiniature = false;

        /// <summary>
        /// Has the SubDataset state changed?
        /// </summary>
        protected bool m_sdChanged = false;

        /// <summary>
        /// List of canvas annotations game object created
        /// </summary>
        protected List<CanvasAnnotationGameObject> m_canvasAnnotationGOs = new List<CanvasAnnotationGameObject>();

        /// <summary>
        /// The matrix local to World of this GameObject
        /// </summary>
        private Matrix4x4 m_localToWorldMatrix = Matrix4x4.identity;

        /// <summary>
        /// List of canvas annotation where a GameObject is needed to be created
        /// </summary>
        private List<CanvasAnnotation> m_canvasAnnotationGOsInRemoving = new List<CanvasAnnotation>();

        /// <summary>
        /// List of canvas annotation where a GameObject is needed to be created
        /// </summary>
        private List<CanvasAnnotation> m_canvasAnnotationGOsInCreation = new List<CanvasAnnotation>();

        
        /// <summary>
        /// List of log annotation positions component game objects created
        /// </summary>
        protected List<LogAnnotationPositionGameObject> m_annotationPositionsGOs = new List<LogAnnotationPositionGameObject>();

        /// <summary>
        /// List of log annotation positions components where a GameObject is needed to be created
        /// </summary>
        private List<LogAnnotationPositionInstance> m_annotationPositionsGOsInRemoving = new List<LogAnnotationPositionInstance>();

        /// <summary>
        /// List of log annotation positions components where a GameObject is needed to be created
        /// </summary>
        private List<LogAnnotationPositionInstance> m_annotationPositionsGOsInCreation = new List<LogAnnotationPositionInstance>();

        /// <summary>
        /// The instantiate map texture gameobject
        /// </summary>
        private MapTextureGameObject m_mapTextureGO = null;

        /// <summary>
        /// Initialize the visualization. Call this method only once please.
        /// </summary>
        /// <param name="sd">The sub dataset to use</param>
        public void Init(SubDataset sd, IDataProvider provider, bool isMiniature = false)
        {
            m_dataProvider = provider;
            m_sd = sd;
            m_isMiniature = isMiniature;

            //Add external 3D objects
            m_outline = Outline;
            m_outline.transform.SetParent(transform);
            m_outline.transform.localPosition = new Vector3(0, 0, 0);
            m_outline.transform.localScale = new Vector3(1, 1, 1);
            m_outline.transform.localRotation = Quaternion.identity;

            LinkToSM();

            m_outlineColor = m_dataProvider.GetHeadsetColor(-1);

            //Load the map beneath this game object
            if(m_sd.Parent.DatasetProperties != null)
            { 
                Properties.DatasetMapProperties mapProp = m_sd.Parent.DatasetProperties.MapProperties;
                if(mapProp != null)
                {
                    Texture2D tex = Resources.Load<Texture2D>($"Textures/{mapProp.TexturePath}");
                    m_mapTextureGO = Instantiate(MapTextureGameObject, this.transform);
                    m_mapTextureGO.transform.localPosition = new Vector3(0.0f, 0.0f, -0.5f);
                    m_mapTextureGO.transform.localRotation = Quaternion.identity;
                    m_mapTextureGO.ApplyTexture(tex, new Vector2(mapProp.Tiling[0], mapProp.Tiling[1]), new Vector2(mapProp.Offset[0], mapProp.Offset[1]));
                }
            }
        }

        protected void LinkToSM()
        {
            //Update position / rotation / scaling
            lock(this)
            {
                OnRotationChange(m_sd, m_sd.Rotation);
                OnPositionChange(m_sd, m_sd.Position);
                OnScaleChange(m_sd, m_sd.Scale);
                OnNameChange(m_sd, m_sd.Name);
                m_sd.AddListener(this);
                m_sdChanged = true;
            }
        }
        
        public virtual void OnLockOwnerIDChange(SubDataset dataset, int ownerID)
        {
            lock (this)
            {
                if (ownerID == -1)
                {
                    m_updateOutlineColor = true;
                    m_outlineColor = Color.blue;
                }
                else
                {
                    if (m_dataProvider != null)
                    {
                        m_updateOutlineColor = true;
                        m_outlineColor = m_dataProvider.GetHeadsetColor(ownerID);
                    }
                }
            }
        }

        public virtual void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {}
        
        public void OnPositionChange(SubDataset dataset, float[] position)
        {
            lock (this)
            {
                m_updateP = true;
                m_newP = new Vector3(position[0], position[1], position[2]);
            }
        }

        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion)
        {
            lock (this)
            {
                float[] q = dataset.GraphicalRotation;
                m_newQ = new Quaternion(q[1],
                                        q[2],
                                        q[3],
                                        q[0]).normalized;
                m_updateQ = true;
            }
        }

        public void OnScaleChange(SubDataset dataset, float[] scale)
        {
            lock (this)
            {
                scale = dataset.GraphicalScaling;
                m_newS = new Vector3(scale[0], scale[1], scale[2]);
                m_updateS = true;
            }
        }

        public void OnNameChange(SubDataset dataset, String name)
        {
            lock(this)
            {
                m_newName    = name;
                m_updateName = true;
            }
        }

        public virtual void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
        }

        public void OnAddCanvasAnnotation(SubDataset dataset, CanvasAnnotation annot)
        {
            lock(this)
            {
                m_canvasAnnotationGOsInCreation.Add(annot);
            }
        }

        public void OnClearCanvasAnnotations(SubDataset dataset)
        {
            lock(this)
            {
                foreach (CanvasAnnotation annot in dataset.CanvasAnnotations)
                    m_canvasAnnotationGOsInRemoving.Add(annot);
            }
        }

        public void SetSubDatasetState(SubDataset sd)
        {
            lock(this)
            {
                m_sd.RemoveListener(this);
                m_sd = sd;
                LinkToSM();
            }
        }

        public virtual void OnDestroy()
        {
            Destroy(m_outline);
            if(!m_isMiniature)
            {
                foreach (var annot in m_canvasAnnotationGOs)
                    Destroy(annot.gameObject);
            }
        }

        public virtual void LateUpdate()
        {
            //Update the 3D transform of this game object
            lock(this)
            {
                //Not initialized yet
                if (m_sd == null)
                    return;

                if (m_dataProvider != null && m_dataProvider.GetTargetedGameObject() == this)
                {
                    if (!m_isTargeted)
                    {
                        foreach (var comp in m_outline.transform.GetComponentsInChildren<MeshRenderer>())
                            comp.material.color = ColorOnTargetPointingIT;
                        m_updateOutlineColor = true;
                        m_isTargeted = true;
                    }
                }
                else
                    m_isTargeted = false;

                if(m_mapTextureGO != null)
                    m_mapTextureGO.gameObject.SetActive(m_sd.IsMapVisible);
                
                if (!m_isMiniature)
                {
                    if (m_updateP)
                        transform.localPosition = m_newP;
                    m_updateP = false;

                    if (m_updateQ)
                    {
                        transform.localRotation = m_newQ;
                        NameUIPivot.transform.localRotation = Quaternion.Inverse(m_newQ) * new Quaternion(0.0f, m_sd.Rotation[2], 0.0f, m_sd.Rotation[0]).normalized;
                    }
                    m_updateQ = false;

                    if (m_updateS)
                    {
                        transform.localScale = m_newS;
                        if (m_newS.x != 0 && m_newS.y != 0 && m_newS.z != 0)
                            NameUI.transform.localScale = new Vector3(1.0f/m_newS.x, 1.0f/m_newS.y, 1.0f/m_newS.z);
                    }
                    m_updateS = false;

                    if(m_updateName)
                        NameUI.text = m_newName;
                    m_updateName = false;
                                       
                    if (m_updateOutlineColor && !m_isTargeted)
                    {
                        foreach (var comp in m_outline.transform.GetComponentsInChildren<MeshRenderer>())
                            comp.material.color = m_outlineColor;
                        m_updateOutlineColor = false;
                    }
                    
                    if(m_sdChanged)
                    {
                        //Destroy every annotation GO
                        foreach (CanvasAnnotationGameObject go in m_canvasAnnotationGOs)
                            Destroy(go.gameObject);
                        m_canvasAnnotationGOs.Clear();
                        foreach(LogAnnotationPositionGameObject go in m_annotationPositionsGOs)
                            Destroy(go.gameObject);
                        m_annotationPositionsGOs.Clear();

                        //Create every Annotations
                        foreach(CanvasAnnotation annot in m_sd.CanvasAnnotations)
                            CreateCanvasAnnotationGO(annot);
                        foreach(LogAnnotationPositionInstance annot in m_sd.LogAnnotationPositions)
                            CreateLogAnnotationPositionGO(annot);

                        m_sdChanged = false;
                        m_canvasAnnotationGOsInCreation.Clear();
                    }
                    else
                    {
                        //Create annotation game object received asynchronously
                        foreach (CanvasAnnotation annot in m_canvasAnnotationGOsInCreation)
                            CreateCanvasAnnotationGO(annot);
                        m_canvasAnnotationGOsInCreation.Clear();
                        foreach(LogAnnotationPositionInstance annot in m_annotationPositionsGOsInCreation)
                            CreateLogAnnotationPositionGO(annot);

                        //Delete annotation game object received asynchronously
                        foreach (CanvasAnnotation annot in m_canvasAnnotationGOsInRemoving)
                        {
                            for (int i = 0; i < m_canvasAnnotationGOs.Count; i++)
                            {
                                if(m_canvasAnnotationGOs[i].Annotation == annot)
                                {
                                    Destroy(m_canvasAnnotationGOs[i].gameObject);
                                    m_canvasAnnotationGOs.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        m_canvasAnnotationGOsInRemoving.Clear();

                        foreach(LogAnnotationPositionInstance annot in m_annotationPositionsGOsInRemoving)
                        {
                            for(int i = 0; i < m_annotationPositionsGOs.Count; i++)
                            {
                                if(m_annotationPositionsGOs[i].Component == annot)
                                {
                                    Destroy(m_annotationPositionsGOs[i].gameObject);
                                    m_annotationPositionsGOs.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        m_annotationPositionsGOsInRemoving.Clear();

                    }
                }

                m_localToWorldMatrix = transform.localToWorldMatrix;
            }
        }


        /// <summary>
        /// Create a new miniature. Updates on the subdataset states will also change this miniature. However linking to another SubDataset will break the link between these objects!
        /// </summary>
        /// <returns>The created GameObject</returns>
        public virtual DefaultSubDatasetGameObject CreateMiniature()
        {
            DefaultSubDatasetGameObject defaultSDGO = Instantiate(DefaultMiniaturePrefab);
            defaultSDGO.Init(m_sd, m_dataProvider, true);
            
            return defaultSDGO;
        }

        /// <summary>
        /// Create a canvas annotation GameObject
        /// </summary>
        /// <param name="annot">The annotation model data</param>
        private void CreateCanvasAnnotationGO(CanvasAnnotation annot)
        {
            bool found = false;
            foreach(CanvasAnnotationGameObject go in m_canvasAnnotationGOs)
            {
                if(go.Annotation == annot)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                CanvasAnnotationGameObject go = Instantiate(CanvasAnnotationPrefab);
                go.transform.SetParent(transform, false);
                go.Init(annot);
                m_canvasAnnotationGOs.Add(go);
            }
        }

        /// <summary>
        /// Create a log annotation position GameObject
        /// </summary>
        /// <param name="annot">The annotation model data</param>
        private void CreateLogAnnotationPositionGO(LogAnnotationPositionInstance annot)
        {
            bool found = false;
            foreach(LogAnnotationPositionGameObject go in m_annotationPositionsGOs)
            {
                if(go.Component == annot)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                LogAnnotationPositionGameObject go = Instantiate(LogAnnotationPositionPrefab);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0, 0, 0);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = new Vector3(1, 1, 1);
                go.Init(annot);
                m_annotationPositionsGOs.Add(go);
            }
        }

        public virtual void OnToggleMapVisibility(SubDataset dataset, bool visibility)
        {}

        public virtual void OnChangeVolumetricMask(SubDataset dataset)
        {}

        public void OnAddLogAnnotationPosition(SubDataset dataset, LogAnnotationPositionInstance annot)
        {
            lock(this)
            {
                m_annotationPositionsGOsInCreation.Add(annot);
            }
        }

        public virtual void OnChangeDepthClipping(SubDataset dataset, float depth)
        {}

        /// <summary>
        /// Is this GameObject targeted?
        /// </summary>
        public bool IsTargeted
        {
            get { return m_isTargeted; }
        }

        /// <summary>
        /// The SubDataset current state being used
        /// </summary>
        public SubDataset SubDatasetState
        {
            get { return m_sd; }
            set { SetSubDatasetState(value); }
        }

        /// <summary>
        /// The local to world matrix saved for parallel purposes.
        /// </summary>
        public Matrix4x4 LocalToWorldMatrix
        {
            get {return m_localToWorldMatrix;}
        }

        /// <summary>
        /// Should the transform be updated?
        /// </summary>
        public bool UpdateTransform
        {
            get => m_updateP || m_updateQ || m_updateS;
        }
    }
}
