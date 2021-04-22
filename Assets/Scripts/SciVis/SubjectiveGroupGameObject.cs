using UnityEngine;
using System;
using System.Collections.Generic;
using Sereno.Datasets;
using static Sereno.Datasets.SubDatasetSubjectiveStackedGroup;
using static Sereno.Datasets.SubDatasetGroup;
using Sereno.Datasets.Annotation;

namespace Sereno.SciVis
{
    public class SubjectiveGroupGameObject : MonoBehaviour, ISubDatasetSubjectiveStackedGroupListener, 
                                                            ISubDatasetGroupListener, ISubDatasetCallback
    {
        private SubDatasetSubjectiveStackedGroup m_sdg = null;

        private Dictionary<SubDataset, GameObject> m_links  = new Dictionary<SubDataset, GameObject>();
        private Dictionary<SubDataset, GameObject> m_stacks = new Dictionary<SubDataset, GameObject>();
        private bool                               m_shouldUpdatePos         = false;
        private bool                               m_shouldAddConnections    = false;
        private bool                               m_shouldRemoveConnections = false;

        public GameObject StackedConnectionGameObject;
        public GameObject LinkedConnectionGameObject;

        public void Init(SubDatasetSubjectiveStackedGroup sdg)
        {
            m_sdg = sdg;

            m_sdg.AddListener((ISubDatasetGroupListener)this);
            m_sdg.AddListener((ISubDatasetSubjectiveStackedGroupListener)this);

            foreach(var s in sdg.SubjectiveViews)
            {
                if(s.Key != null)
                    AddStackConnection(s.Key);
                if(s.Value != null)
                    AddLinkConnection(s.Value);
            }
        }

        private void AddStackConnection(SubDataset stack)
        {
            //TODO
        }

        private void AddLinkConnection(SubDataset link)
        {
            //TODO
        }

        private void Update()
        {
            lock(this)
            {
                if(m_shouldAddConnections)
                {
                    foreach(var s in m_sdg.SubjectiveViews)
                    {
                        if(s.Key != null && !m_stacks.ContainsKey(s.Key))
                            AddStackConnection(s.Key);

                        if(s.Value != null && !m_links.ContainsKey(s.Value))
                            AddStackConnection(s.Value);
                    }
                    m_shouldAddConnections = false;
                }

                if(m_shouldRemoveConnections)
                {
                    foreach(var s in m_links.Keys)
                    {
                        if(!m_sdg.SubDatasets.Contains(s))
                        {
                            //TODO
                        }
                    }

                    foreach(var s in m_stacks.Keys)
                    {
                        if(!m_sdg.SubDatasets.Contains(s))
                        {
                            //TODO
                        }
                    }

                    m_shouldRemoveConnections = false;
                }

                if(m_shouldUpdatePos)
                {
                    m_shouldUpdatePos = false;
                }
            }
        }

        public void OnSetGap(SubDatasetSubjectiveStackedGroup group, float gap)
        {
            lock(this)
                m_shouldUpdatePos = true;
        }

        public void OnSetMerge(SubDatasetSubjectiveStackedGroup group, bool merge)
        {
            lock(this)
                m_shouldUpdatePos = true;
        }

        public void OnSetStackMethod(SubDatasetSubjectiveStackedGroup group, StackMethod method)
        {
            lock(this)
                m_shouldUpdatePos = true;
        }

        public void OnAddSubjectiveViews(SubDatasetSubjectiveStackedGroup group, KeyValuePair<SubDataset, SubDataset> subjViews)
        {
            lock(this)
                m_shouldAddConnections = true;
        }

        public void OnAddSubDataset(SubDatasetGroup sdg, SubDataset sd)
        {
            throw new NotImplementedException();
        }

        public void OnRemoveSubDataset(SubDatasetGroup sdg, SubDataset sd)
        {
            lock(this)
                m_shouldRemoveConnections = true;
        }

        public void OnUpdateSubDatasets(SubDatasetGroup sdg)
        {}

        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {}

        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion)
        {
            lock(this)
                m_shouldUpdatePos = true;
        }

        public void OnPositionChange(SubDataset dataset, float[] position)
        {
            lock(this)
                m_shouldUpdatePos = true;
        }

        public void OnScaleChange(SubDataset dataset, float[] scale)
        {
            lock(this)
                m_shouldUpdatePos = true;
        }

        public void OnLockOwnerIDChange(SubDataset dataset, int ownerID)
        {}

        public void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {}

        public void OnNameChange(SubDataset dataset, string name)
        {}

        public void OnAddCanvasAnnotation(SubDataset dataset, CanvasAnnotation annot)
        {}

        public void OnAddLogAnnotationPosition(SubDataset dataset, LogAnnotationPositionInstance annot)
        {}

        public void OnClearCanvasAnnotations(SubDataset dataset)
        {}

        public void OnToggleMapVisibility(SubDataset dataset, bool visibility)
        {}

        public void OnChangeVolumetricMask(SubDataset dataset)
        {}

        public void OnChangeDepthClipping(SubDataset dataset, float depth)
        {}

        public void OnSetSubDatasetGroup(SubDataset dataset, SubDatasetGroup sdg)
        {}
    }
}