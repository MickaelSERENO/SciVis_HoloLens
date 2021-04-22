using System;
using System.Collections;
using System.Collections.Generic;

namespace Sereno.Datasets
{
    /// <summary>
    /// abstract class for SubDatasetGroup
    /// </summary>
    public abstract class SubDatasetGroup
    {
        /// <summary>
        /// Is a given SubDatasetGroup representing a subjective view group?
        /// </summary>
        /// <param name="sdg">The SubDataset group to evaluate</param>
        /// <returns>true if yes, false otherwise</returns>
        public static bool IsSubjective(SubDatasetGroup sdg)
        {
             return sdg.Type == SubDatasetGroupType.LINKED  ||
                    sdg.Type == SubDatasetGroupType.STACKED ||
                    sdg.Type == SubDatasetGroupType.STACKED_LINKED;
        }

        /// <summary>
        /// Interface to handle events from SubDatasetGroup objects
        /// </summary>
        public interface ISubDatasetGroupListener
        {
            /// <summary>
            /// Event called when a subdataset has been added to this group
            /// </summary>
            /// <param name="sdg">the group calling this method</param>
            /// <param name="sd">the subdataset being added</param>
            void OnAddSubDataset(SubDatasetGroup sdg, SubDataset sd);
            
            /// <summary>
            /// Event called when a subdataset has been removed to this group
            /// </summary>
            /// <param name="sdg">the group calling this method</param>
            /// <param name="sd">the subdataset being removed</param>
            void OnRemoveSubDataset(SubDatasetGroup sdg, SubDataset sd);

            /// <summary>
            /// Event called when subdatasets belonging to a group have been updated
            /// </summary>
            /// <param name="sdg">the group calling this method</param>
            void OnUpdateSubDatasets(SubDatasetGroup sdg);
        }

        /// <summary>
        /// All the registered subdatasets
        /// </summary>
        private List<SubDataset>    m_subDatasets = new List<SubDataset>();

        /// <summary>
        /// All the registered listeners to call on events
        /// </summary>
        private List<ISubDatasetGroupListener> m_listeners = new List<ISubDatasetGroupListener>();

        /// <summary>
        /// The type of this SubDatasetGroup
        /// </summary>
        private SubDatasetGroupType m_type;
        
        /// <summary>
        /// The ID of this group as defined by the server
        /// </summary>
        private Int32               m_id;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">The type as defined by the server of this SubDatasetGroup</param>
        /// <param name="id">The ID as defined by the server of this SubDatasetGroup</param>
        public SubDatasetGroup(SubDatasetGroupType type, Int32 id)
        {
            m_type = type;
            m_id   = id;
        }

        public void AddListener(ISubDatasetGroupListener l)
        {
            if(m_listeners.Contains(l))
                return;
            m_listeners.Add(l);
        }

        public void RemoveListener(ISubDatasetGroupListener l)
        {
            if(!m_listeners.Contains(l))
                return;
            m_listeners.Remove(l);
        }

        /// <summary>
        /// Removes an already registered subdataset
        /// </summary>
        /// <param name="sd"></param>
        /// <returns>true if we could remove this subdataset, false otherwise</returns>
        public virtual bool RemoveSubDataset(SubDataset sd)
        {
            int sdIdx = m_subDatasets.FindIndex(it => it == sd);
            if(sdIdx >= 0)
            {
                m_subDatasets.RemoveAt(sdIdx);
                sd.SubDatasetGroup = null;

                foreach(var l in m_listeners)
                    l.OnRemoveSubDataset(this, sd);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a subdataset to this group
        /// </summary>
        /// <param name="sd">The new SubDataset to add to this group</param>
        /// <returns>true if the adding was a success, false otherwise</returns>
        public virtual bool AddSubDataset(SubDataset sd)
        {
            int sdIdx = m_subDatasets.FindIndex(it => it == sd);
            if(sdIdx < 0)
            {
                m_subDatasets.Add(sd);
                sd.SubDatasetGroup = this;

                foreach(var l in m_listeners)
                    l.OnAddSubDataset(this, sd);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update the status of every registered SubDataset
        /// </summary>
        public abstract void UpdateSubDatasets();

        public SubDatasetGroupType Type
        {
            get => m_type;
        }

        public Int32 ID
        {
            get => m_id;
        }

        public List<SubDataset> SubDatasets {get => m_subDatasets;}
    }
}