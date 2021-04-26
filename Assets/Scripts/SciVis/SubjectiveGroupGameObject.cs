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
                                                            ISubDatasetGroupListener
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
            m_sdg.AddListener((ISubDatasetSubjectiveStackedGroupListener)this);
            m_sdg.AddListener((ISubDatasetGroupListener)this);

            foreach (var s in sdg.SubjectiveViews)
            {
                if(s.Key != null)
                    AddStackConnection(s.Key);
                if(s.Value != null)
                    AddLinkConnection(s.Value);
            }

            m_shouldUpdatePos = true;
        }

        private void AddStackConnection(SubDataset stack)
        {
            if (stack == null)
                return;

            GameObject go = Instantiate(StackedConnectionGameObject, transform, true);
            m_stacks.Add(stack, go);
        }

        private void AddLinkConnection(SubDataset link)
        {
            if (link == null)
                return;

            GameObject go = Instantiate(LinkedConnectionGameObject, transform, true);
            m_links.Add(link, go);
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
                            AddLinkConnection(s.Value);
                    }
                    m_shouldAddConnections = false;
                    m_shouldUpdatePos      = true;
                }

                if(m_shouldRemoveConnections)
                {
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        foreach (var s in m_links.Keys)
                        {
                            if (!m_sdg.SubDatasets.Contains(s))
                            {
                                Destroy(m_links[s]);
                                m_links.Remove(s);
                                found = true;
                                break;
                            }
                        }
                    }

                    found = true;
                    while (found)
                    {
                        found = false;
                        foreach (var s in m_stacks.Keys)
                        {
                            if (!m_sdg.SubDatasets.Contains(s))
                            {
                                Destroy(m_stacks[s]);
                                m_stacks.Remove(s);
                                found = true;
                                break;
                            }
                        }
                    }

                    m_shouldRemoveConnections = false;
                    m_shouldUpdatePos = true;
                }

                if(m_shouldUpdatePos)
                {
                    if (m_sdg.StackMethod == StackMethod.STACK_VERTICAL)
                    {
                        foreach (var it in m_stacks)
                        {
                            it.Value.transform.localScale = new Vector3(it.Key.Scale[0], m_sdg.YVerticalGap, it.Key.Scale[2]);

                            it.Value.transform.position = new Vector3(it.Key.Position[0],
                                                                      it.Key.Position[1] - (it.Key.Scale[1] + it.Value.transform.localScale[1])/2.0f,
                                                                      it.Key.Position[2]);
                        }
                    }
                    else
                    {
                        foreach (var it in m_stacks)
                            it.Value.SetActive(false);
                    }

                    foreach(var it in m_links)
                    {
                        SubDataset stack = m_sdg.SubjectiveViews.Find(s => s.Value == it.Key).Key;
                        if (stack == null)
                            continue;

                        Vector3 anchorPoint = new Vector3(it.Key.Position[0] + it.Key.Scale[0]/2.0f,
                                                          it.Key.Position[1] + it.Key.Scale[1]/2.0f,
                                                          it.Key.Position[2] + it.Key.Scale[2]/2.0f);

                        Vector3 targetPos = new Vector3(stack.Position[0] - stack.Scale[0]/2.0f,
                                                        stack.Position[1] - stack.Scale[1]/2.0f,
                                                        stack.Position[2] - stack.Scale[2]/2.0f);

                        Vector3 rayVec = targetPos - anchorPoint;
                        rayVec = rayVec.normalized;

                        Vector3 scale = it.Value.transform.localScale;
                        scale.x = 0.02f;
                        scale.z = 0.02f;
                        scale.y = (targetPos - anchorPoint).magnitude / 2.0f;

                        it.Value.transform.localScale = scale;
                        it.Value.transform.up = rayVec;
                        it.Value.transform.localPosition = anchorPoint + rayVec * it.Value.transform.localScale.y;
                    }

                    m_shouldUpdatePos = false;
                }
            }
        }

        public void OnSetGap(SubDatasetSubjectiveStackedGroup group, float gap)
        {}

        public void OnSetMerge(SubDatasetSubjectiveStackedGroup group, bool merge)
        {}

        public void OnSetStackMethod(SubDatasetSubjectiveStackedGroup group, StackMethod method)
        {}

        public void OnAddSubjectiveViews(SubDatasetSubjectiveStackedGroup group, KeyValuePair<SubDataset, SubDataset> subjViews)
        {
            lock(this)
                m_shouldAddConnections = true;
        }

        public void OnAddSubDataset(SubDatasetGroup sdg, SubDataset sd)
        {}

        public void OnRemoveSubDataset(SubDatasetGroup sdg, SubDataset sd)
        {
            lock(this)
                m_shouldRemoveConnections = true;
        }

        public void OnUpdateSubDatasets(SubDatasetGroup sdg)
        {
            lock (this)
                m_shouldUpdatePos = true;
        }
    }
}