using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Datasets
{
    public class SubDatasetMetaData
    {
        /// <summary>
        /// Value for subdatasets in public state
        /// </summary>
        public const int VISIBILITY_PUBLIC  = 1;

        /// <summary>
        /// Value for subdatasets in private state
        /// </summary>
        public const int VISIBILITY_PRIVATE = 0;

        /// <summary>
        /// The public subdataset state
        /// </summary>
        private SubDataset m_publicSD;

        /// <summary>
        /// The private subdataset state
        /// </summary>
        private SubDataset m_privateSD;

        /// <summary>
        /// The subdataset visibility
        /// </summary>
        private int        m_visibility = VISIBILITY_PUBLIC;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sd">The public subdataset state</param>
        public SubDatasetMetaData(SubDataset sd)
        {
            m_publicSD = sd;
            m_privateSD = new SubDataset(sd);
        }

        /// <summary>
        /// Get the public sub dataset state
        /// </summary>
        public SubDataset SubDatasetPublicState { get => m_publicSD; }

        /// <summary>
        /// Get the private sub dataset state
        /// </summary>
        public SubDataset SubDatasetPrivateState { get => m_privateSD; }

        /// <summary>
        /// Get the current subdataset state based on the visibility
        /// </summary>
        public SubDataset CurrentSubDataset { get => Visibility == VISIBILITY_PUBLIC ? m_publicSD : m_privateSD; }

        /// <summary>
        /// The visibility of the subdataset (see VISIBILITY_PUBLIC and VISIBILITY_PRIVATE)
        /// </summary>
        public int Visibility { get => m_visibility; set => m_visibility = value; }
    }
}
