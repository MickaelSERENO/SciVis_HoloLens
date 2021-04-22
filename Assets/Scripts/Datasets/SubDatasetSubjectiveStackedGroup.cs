using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno.Datasets
{
    public class SubDatasetSubjectiveStackedGroup : SubDatasetGroup
    {
        public interface ISubDatasetSubjectiveStackedGroupListener
        {
            void OnSetGap(SubDatasetSubjectiveStackedGroup group, float gap);
            void OnSetMerge(SubDatasetSubjectiveStackedGroup group, bool merge);
            void OnSetStackMethod(SubDatasetSubjectiveStackedGroup group, StackMethod method);
            void OnAddSubjectiveViews(SubDatasetSubjectiveStackedGroup group, KeyValuePair<SubDataset, SubDataset> subjViews);
        }

        private List<ISubDatasetSubjectiveStackedGroupListener> m_listeners = new List<ISubDatasetSubjectiveStackedGroupListener>();

        private SubDataset m_base      = null;
        private StackMethod m_stack    = StackMethod.STACK_VERTICAL;
        private float       m_gap      = 0.0f; 
        private bool        m_isMerged = false;

        private List<KeyValuePair<SubDataset, SubDataset>> m_subjViews = new List<KeyValuePair<SubDataset, SubDataset>>();

        public SubDatasetSubjectiveStackedGroup(SubDatasetGroupType type, Int32 id, SubDataset baseSD) : base(type, id)
        {
            m_base = baseSD;
            AddSubDataset(m_base);
        }

        public void AddListener(ISubDatasetSubjectiveStackedGroupListener l)
        {
            if(m_listeners.Contains(l))
                return;
            m_listeners.Add(l);
        }

        public void RemoveListener(ISubDatasetSubjectiveStackedGroupListener l)
        {
            if(!m_listeners.Contains(l))
                return;
            m_listeners.Remove(l);
        }

        public override bool RemoveSubDataset(SubDataset sd)
        {
            if(sd == null)
                return false;

            //If remove the base --> remove everything from this group
            if(sd == m_base)
            {
                SubDataset _base = m_base;
                m_base = null;
                base.RemoveSubDataset(_base);
                while(m_subjViews.Count > 0)
                {
                    SubDataset sd1 = m_subjViews[m_subjViews.Count-1].Key; //Save and remove BEFORE calling function because of domino effect between SubDataset and SubDatasetGroup
                    SubDataset sd2 = m_subjViews[m_subjViews.Count-1].Value;
                    m_subjViews.RemoveAt(m_subjViews.Count-1);
                    if(sd1 != null)
                        base.RemoveSubDataset(sd1);
                    if(sd2 != null)
                        base.RemoveSubDataset(sd2);
                }
                return true;
            }

            //Otherwise, test if we are removing a subjective view
            for(int i = 0; i < m_subjViews.Count; i++)
            {
                //If yes, remove both the linked and the stacked subjective views
                if(m_subjViews[i].Value == sd || m_subjViews[i].Key == sd)
                {
                    SubDataset sd1 = m_subjViews[i].Key; //Save and remove BEFORE calling function because of domino effect between SubDataset and SubDatasetGroup
                    SubDataset sd2 = m_subjViews[i].Value;
                    m_subjViews.RemoveAt(i);

                    if(sd1 != null)
                        base.RemoveSubDataset(sd1);
                    if(sd2 != null)
                        base.RemoveSubDataset(sd2);
                    return true;
                }
            }

            return false;
        }

        public bool AddSubjectiveSubDataset(SubDataset sdStacked, SubDataset sdLinked)
        {
            if(sdStacked == null && sdLinked == null)
                return false;

            //Test if those are already registered
            foreach(var it in SubDatasets)
                if(it == sdStacked || it == sdLinked)
                    return false;

            if(sdStacked != null)
                AddSubDataset(sdStacked);
            if(sdLinked != null)
                AddSubDataset(sdLinked);

            m_subjViews.Add(new KeyValuePair<SubDataset, SubDataset>(sdStacked, sdLinked));

            foreach(var l in m_listeners)
                l.OnAddSubjectiveViews(this, m_subjViews[m_subjViews.Count-1]);
            return true;
        }

        public override void UpdateSubDatasets()
        {
            if(m_base == null)
                return;

            float[] scale = m_base.Scale;
            float[] pos   = m_base.Position;
            float[] rot   = m_base.Rotation;

            float size = scale[0]*scale[0] + scale[1]*scale[1] + scale[2]*scale[2];
            size = (float)Math.Sqrt(size);

            foreach(var it in m_subjViews)
            {
                if(it.Key != null)
                {
                    it.Key.Scale    = scale;
                    it.Key.Rotation = rot;
                }
            }

            if(m_stack == StackMethod.STACK_VERTICAL)
            {
                Int32 i = 0;
                foreach(var it in m_subjViews)
                {
                    if(it.Key != null)
                    {
                        it.Key.Position = new float[]{pos[0], pos[1]+(i+1)*(m_gap+size), pos[2]};
                        if(!m_isMerged)
                            i++;
                    }
                }
            }

            else if(m_stack == StackMethod.STACK_HORIZONTAL)
            {
                Int32 i = 0;
                foreach(var it in m_subjViews)
                {
                    if(it.Key != null)
                    {
                        it.Key.Position = new float[]{pos[0]+(i+1)*(m_gap+size), pos[1], pos[2]};
                        if(!m_isMerged)
                            i++;
                    }
                }
            }        
        }

        public SubDataset Base
        {
            get => m_base;
        }

        public List<KeyValuePair<SubDataset, SubDataset>> SubjectiveViews
        {
            get => m_subjViews;
        }

        public float Gap
        {
            get => m_gap;
            set
            {
                if(m_gap != value)
                {
                    m_gap = value;
                    foreach(var l in m_listeners)
                        l.OnSetGap(this, m_gap);
                }
            }
        }

        public bool IsMerged
        {
            get => m_isMerged;
            set
            {
                if(m_isMerged != value)
                {
                    m_isMerged = value;
                    foreach(var l in m_listeners)
                        l.OnSetMerge(this, m_isMerged);
                }
            }
        }

        public StackMethod StackMethod
        {
            get => m_stack;
            set
            {
                if(m_stack != value)
                {
                    m_stack = value;
                    foreach(var l in m_listeners)
                        l.OnSetStackMethod(this, m_stack);
                }
            }
        }
    }
}