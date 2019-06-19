using System.Collections.Generic;

namespace Sereno.Datasets
{
    /// <summary>
    /// Encapsulate a dataset to add more meta data
    /// </summary>
    public class DatasetMetaData
    {
        /// <summary>
        /// The bound dataset
        /// </summary>
        private Dataset m_dataset;

        /// <summary>
        /// List of private SubDataset states
        /// </summary>
        private List<SubDatasetMetaData> m_subDatasets = new List<SubDatasetMetaData>();
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="d">The dataset to be bound to</param>
        public DatasetMetaData(Dataset d)
        {
            m_dataset = d;
            foreach(SubDataset sd in d.SubDatasets)
                m_subDatasets.Add(new SubDatasetMetaData(sd));
        }

        /// <summary>
        /// The SubDataset private states
        /// </summary>
        public List<SubDatasetMetaData> SubDatasets { get => m_subDatasets; }

        /// <summary>
        /// Get the Bound dataset
        /// </summary>
        public Dataset Dataset { get => m_dataset; }
    }
}