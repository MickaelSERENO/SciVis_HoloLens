using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sereno
{
    public class Dataset
    {
        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        protected List<SubDataset> m_subDatasets = new List<SubDataset>();

        /// <summary>
        /// Constructor, does nothing
        /// </summary>
        public Dataset()
        { }

        /// <summary>
        /// Get the SubDataset at indice i.
        /// </summary>
        /// <param name="i">The indice of the SubDataset to retrieve</param>
        /// <returns>SubDatasets[i]</returns>
        public SubDataset GetSubDataset(int i) {return m_subDatasets[i];}

        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        public List<SubDataset> SubDatasets {get => m_subDatasets;}
    }
}