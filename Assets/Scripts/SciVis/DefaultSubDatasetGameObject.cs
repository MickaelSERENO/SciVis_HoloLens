using Sereno.Datasets;
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
        /// The Color to apply when the GameObject is being targeted
        /// </summary>
        public Color ColorOnTargetPointingIT = new Color(1.0f, 1.0f, 1.0f);

        /// <summary>
        /// the annotation prefab to use
        /// </summary>
        public AnnotationGameObject AnnotationPrefab = null;

        /// <summary>
        /// The Miniature prefab
        /// </summary>
        public DefaultSubDatasetGameObject DefaultMiniaturePrefab;

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
        /// List of annotations game object created
        /// </summary>
        protected List<AnnotationGameObject> m_annotationGOs = new List<AnnotationGameObject>();

        /// <summary>
        /// List of annotation where a GameObject is needed to be created
        /// </summary>
        private List<Annotation> m_annotationGOsInRemoving = new List<Annotation>();

        /// <summary>
        /// List of annotation where a GameObject is needed to be created
        /// </summary>
        private List<Annotation> m_annotationGOsInCreation = new List<Annotation>();

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
        }

        protected void LinkToSM()
        {
            //Update position / rotation / scaling
            lock(m_sd)
            {
                OnRotationChange(m_sd, m_sd.Rotation);
                OnPositionChange(m_sd, m_sd.Position);
                OnScaleChange(m_sd, m_sd.Scale);
                m_sd.AddListener(this);
                m_sdChanged = true;
            }
        }
        
        public virtual void OnColorRangeChange(SubDataset dataset, float min, float max)
        {
        }

        public virtual void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {
            Debug.Log($"New owner : {ownerID}");
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
                m_newQ = new Quaternion(rotationQuaternion[1],
                                        rotationQuaternion[2],
                                        rotationQuaternion[3],
                                        rotationQuaternion[0]);
                m_updateQ = true;
            }
        }

        public void OnScaleChange(SubDataset dataset, float[] scale)
        {
            lock (this)
            {
                m_newS = new Vector3(scale[0], scale[1], scale[2]);
                m_updateS = true;
            }
        }

        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
        }

        public void OnAddAnnotation(SubDataset dataset, Annotation annot)
        {
            m_annotationGOsInCreation.Add(annot);
        }

        public void OnClearAnnotations(SubDataset dataset)
        {
            foreach (Annotation annot in dataset.Annotations)
                m_annotationGOsInRemoving.Add(annot);
        }

        public void SetSubDatasetState(SubDataset sd)
        {
            lock (m_sd)
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
                foreach (var annot in m_annotationGOs)
                    Destroy(annot);
            }
        }

        public virtual void LateUpdate()
        {
            //Update the 3D transform of this game object
            lock (this)
            {
                if (m_dataProvider.GetTargetedGameObject() == this)
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
                
                if (!m_isMiniature)
                {
                    if (m_updateP)
                        transform.localPosition = m_newP;
                    m_updateP = false;

                    if (m_updateQ)
                        transform.localRotation = m_newQ;
                    m_updateQ = false;

                    if (m_updateS)
                        transform.localScale = m_newS;
                    m_updateS = false;
                                       
                    if (m_updateOutlineColor && !m_isTargeted)
                    {
                        foreach (var comp in m_outline.transform.GetComponentsInChildren<MeshRenderer>())
                            comp.material.color = m_outlineColor;
                        m_updateOutlineColor = false;
                    }
                    
                    if(m_sdChanged)
                    {
                        //Destroy every annotation GO
                        foreach (AnnotationGameObject go in m_annotationGOs)
                            Destroy(go);
                        m_annotationGOs.Clear();

                        //Create every Annotations
                        foreach(Annotation annot in m_sd.Annotations)
                            CreateAnnotationGO(annot);

                        m_sdChanged = false;
                        m_annotationGOsInCreation.Clear();
                    }
                    else
                    {
                        //Create annotation game object received asynchronously
                        foreach (Annotation annot in m_annotationGOsInCreation)
                            CreateAnnotationGO(annot);
                        m_annotationGOsInCreation.Clear();

                        //Delete annotation game object received asynchronously
                        foreach (Annotation annot in m_annotationGOsInRemoving)
                        {
                            for (int i = 0; i < m_annotationGOs.Count; i++)
                            {
                                if(m_annotationGOs[i].Annotation == annot)
                                {
                                    Destroy(m_annotationGOs[i]);
                                    m_annotationGOs.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }
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
        /// Create an annotation GameObject
        /// </summary>
        /// <param name="annot">The annotation model data</param>
        private void CreateAnnotationGO(Annotation annot)
        {
            bool found = false;
            foreach(AnnotationGameObject go in m_annotationGOs)
            {
                if(go.Annotation == annot)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                AnnotationGameObject go = Instantiate(AnnotationPrefab);
                go.transform.SetParent(transform, false);
                go.Init(annot);
                m_annotationGOs.Add(go);
            }
        }

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
    }
}
