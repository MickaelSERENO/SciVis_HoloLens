using System.Collections.Generic;

namespace Sereno.Datasets
{
    public class Dataset
    {
        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        protected List<SubDataset> m_subDatasets = new List<SubDataset>();

        private int m_id;

        /// <summary>
        /// Constructor, does nothing
        /// </summary>
        /// <param name="id">The ID of the Dataset</param>
        public Dataset(int id)
        { 
            m_id = id;
        }

        /// <summary>
        /// Get the SubDataset at indice i.
        /// </summary>
        /// <param name="i">The indice of the SubDataset to retrieve</param>
        /// <returns>SubDatasets[i]</returns>
        public SubDataset GetSubDataset(int i) {return m_subDatasets[i];}

        /// <summary>
        /// Get the SubDataset ID
        /// </summary>
        /// <param name="publicSD">The public SubDataset to compare. The parent must matches</param>
        /// <returns>-1 if not found, the id (i.e, SubDatasets[id] == sd) if found</returns>
        public int GetSubDatasetID(SubDataset publicSD)
        {
            for (int i = 0; i < m_subDatasets.Count; i++)
                if (m_subDatasets[i] == publicSD)
                    return i;
            return -1;
        }

        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        public List<SubDataset> SubDatasets {get => m_subDatasets;}

        public int ID {get => m_id;}
    }
}