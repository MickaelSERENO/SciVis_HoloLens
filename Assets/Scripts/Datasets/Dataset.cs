using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sereno.Datasets
{
    /// <summary>
    /// Delegate function called by a Dataset when loading its values
    /// </summary>
    /// <param name="d">The Dataset calling this method</param>
    /// <param name="status">The loading status</param>
    public delegate void LoadDatasetCallback(Dataset d, int status);

    public abstract class Dataset
    {
        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        protected List<SubDataset> m_subDatasets = new List<SubDataset>();

        /// <summary>
        /// List of loaded PointFieldDescriptor
        /// </summary>
        protected List<PointFieldDescriptor> m_ptFieldDescs = new List<PointFieldDescriptor>();

        /// <summary>
        /// The Dataset ID 
        /// </summary>
        private int m_id;

        /// <summary>
        /// What is the current SubDataset ID in use?
        /// </summary>
        private int m_curSDID = 0;

        /// <summary>
        /// The Gradient loaded.
        /// </summary>
        protected float[] m_grads = null;

        /// <summary>
        /// The maximum gradient magnitude computed
        /// </summary>
        protected float m_maxGrad = float.MaxValue;

        /// <summary>
        /// Are the values loaded?
        /// </summary>
        protected bool m_isLoaded = false;

        /// <summary>
        /// The Dataset name (usually its Path)
        /// </summary>
        protected String m_name = "";

        /// <summary>
        /// The parsed Dataset properties
        /// </summary>
        protected Properties.DatasetProperties m_props;

        /// <summary>
        /// Constructor, does nothing
        /// </summary>
        /// <param name="id">The ID of the Dataset</param>
        /// <param name="name">The Dataset's name</param>
        public Dataset(int id, String name)
        {
            m_id = id;
            m_name = name;
            m_props = new Properties.DatasetProperties
            {
                Name = m_name
            };
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
        /// Get the TFIndice to use from a property ID
        /// </summary>
        /// <param name="propID">The property ID to convert</param>
        /// <returns>-1 if propID is not found, the TF Indice (i.e., the indice in PointFieldDescs) otherwise</returns>
        public int GetTFIndiceFromPropID(int propID)
        {
            return m_ptFieldDescs.FindIndex((PointFieldDescriptor desc) => { return desc.ID == propID; });
        }

        /// <summary>
        /// Load the Dataset internal values. This is done asynchronously
        /// </summary>
        /// <return>The asynchronous task being executed. TResult: the status of the loading</return>
        public abstract Task<int> LoadValues();

        /// <summary>
        /// List of SubDatasets contained in this Dataset
        /// </summary>
        public List<SubDataset> SubDatasets {get => m_subDatasets;}

        /// <summary>
        /// List of loaded PointFieldDescriptor. If IsLoaded == false, this array is not yet finished to be computed
        /// </summary>
        public List<PointFieldDescriptor> PointFieldDescs { get => m_ptFieldDescs; }

        /// <summary>
        /// Get the multi dimensionnal gradient raw values. 
        /// If IsLoaded == false, this property returns null;
        /// </summary>
        public float[] Gradient { get => m_grads; }

        /// <summary>
        /// Get the maximum gradient magnitude computed. If IsLoaded == false, this property returns float.MaxValue;
        /// </summary>
        public float MaxGrad { get => m_maxGrad; }

        /// <summary>
        /// Is the Dataset Loaded?
        /// </summary>
        public bool IsLoaded { get => m_isLoaded; }

        /// <summary>
        /// The Dataset ID
        /// </summary>
        public int ID {get => m_id;}

        /// <summary>
        /// The Dataset name (usually its Path)
        /// </summary>
        public String Name { get => m_name; }

        /// <summary>
        /// The parsed Dataset Properties
        /// </summary>
        public Properties.DatasetProperties DatasetProperties { get => m_props; set => m_props = value; }
    }
}