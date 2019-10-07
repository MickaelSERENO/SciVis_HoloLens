using System.Collections.Generic;
using System.Linq;

namespace Sereno.Datasets
{
    public class Dataset
    {
        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        protected List<SubDataset> m_subDatasets = new List<SubDataset>();

        private int m_id;

        private int m_curSDID = 0;

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
        /// <returns>The SubDataset corresponding to the ID, null if not found</returns>
        public SubDataset GetSubDataset(int i)
        {
            return m_subDatasets.FirstOrDefault(sd => sd.ID == i);
        }

        /// <summary>
        /// Add a SubDataset to the list
        /// </summary>
        /// <param name="sd">The SubDataset to add</param>
        /// <param name="updateID">Update the SubDataset ID or not</param>
        public void AddSubDataset(SubDataset sd, bool updateID=true)
        {
            if(m_subDatasets.Contains(sd))
                return;

            if (updateID)
            {
                sd.ID = m_curSDID;
                m_curSDID++;
            }
            m_subDatasets.Add(sd);
        }

        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        public List<SubDataset> SubDatasets {get => m_subDatasets;}

        public int ID {get => m_id;}
    }
}