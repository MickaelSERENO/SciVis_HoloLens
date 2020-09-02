using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Sereno.Datasets
{
    /// <summary>
    /// Delegate function called by a Dataset when loading its values
    /// </summary>
    /// <param name="d">The Dataset calling this method</param>
    /// <param name="status">The loading status</param>
    public delegate void LoadDatasetCallback(Dataset d, int status);

    public class Gradient
    {
        /// <summary>
        /// The gradient values
        /// </summary>
        private float[] m_values;

        /// <summary>
        /// The property indices matching this Gradient (multi dimensional or one-dimensional)
        /// </summary>
        private int[] m_indices;

        /// <summary>
        /// The maximum gradient computed
        /// </summary>
        private float m_maxGrad;

        public Gradient(int[] indices, float[] values, float maxGrad)
        {
            m_indices = indices;
            m_values  = values;
            m_maxGrad = maxGrad;
        }

        /// <summary>
        /// Get the multi dimensionnal gradient raw values. 
        /// </summary>
        public float[] Values { get => m_values; }

        /// <summary>
        /// Get the maximum gradient magnitude computed.
        /// </summary>
        public float MaxGrad { get => m_maxGrad; }

        /// <summary>
        /// Get the indices matching this gradient
        /// </summary>
        public int[] Indices { get => m_indices; }
    }

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
        protected List<Gradient> m_grads = new List<Gradient>();

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
        /// Compute the gradient from indices "ids"
        /// </summary>
        /// <returns>A new Gradient Object</returns>
        protected virtual Gradient ComputeGradient(int[] ids)
        {
            return null;
        }

        /// <summary>
        /// Get the Gradient values associated to a particular indices
        /// </summary>
        /// <param name="indices">The dataset's property indices to get the gradient from. 
        /// If not found, the gradient is computed from the beginning</param>
        /// <returns>The multi-dimensional Gradient associated to the indices "indices"</returns>
        public Gradient GetGradient(int[] indices)
        {
            int[] ids = (int[])indices.Clone();
            Array.Sort(ids);

            Gradient grad = m_grads.Find(x => x.Indices.SequenceEqual(ids));
            if(grad == null)
            {
                grad = ComputeGradient(ids);
                if (grad != null)
                    m_grads.Add(grad);
            }
            return grad;
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