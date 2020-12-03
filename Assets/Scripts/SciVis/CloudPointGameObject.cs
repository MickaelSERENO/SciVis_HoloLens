using Sereno.Datasets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sereno.SciVis
{
    /// <summary>
    /// Class used for the Selection algorithm (based on mesh)
    /// </summary>
    class RasteredCube
    {
        /// <summary>
        /// The list of triangle being part of this Cube (in a rastered space)
        /// </summary>
        public List<int> TriangleIDs = new List<int>();

        /// <summary>
        /// The maximum number of triangle a particular being on this cell might need to go through for the whole algorithm
        /// </summary>
        public int MaxNbTriangle = 0;

        public int this[int x] 
        {
            get { return TriangleIDs[x]; }
        }
    }

    public class CloudPointGameObject : DefaultSubDatasetGameObject
    {
        /// <summary>
        /// The update TF lock
        /// </summary>
        private System.Object m_updateTFLock = new System.Object();

        /// <summary>
        /// Is the lock to use to check wether is updateTFLock or not
        /// </summary>
        private System.Object m_isTFUpdateLock = new System.Object();

        /// <summary>
        /// Is a thread waiting to update the transfer function?
        /// </summary>
        private bool m_waitToUpdateTF = false;

        /// <summary>
        /// The dataset to use
        /// </summary>
        private CloudPointDataset m_dataset = null;

        /// <summary>
        /// The array colors computed
        /// </summary>
        private byte[] m_colors = null;
        
        /// <summary>
        /// The Mesh corresponding to the data cloud
        /// </summary>
        private Mesh m_mesh;

        /// <summary>
        /// Did we initialized the position data in our VBO?
        /// </summary>
        private bool m_isPositionInit = false;

        /// <summary>
        /// The cloud point material to use
        /// </summary>
        public Material MaterialCP;
        
        public void Init(CloudPointDataset dataset, SubDataset sd, IDataProvider provider, bool isMiniature = false) 
        {
            base.Init(sd, provider, isMiniature);
            m_dataset = dataset;

            //Init the mesh for better performance
            m_mesh = new Mesh();
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Color,    VertexAttributeFormat.UNorm8 , 4, 1),
            };
            m_mesh.SetVertexBufferParams((int)dataset.NbPoints, layout);

            if(m_dataset.NbPoints > 0xffff)
                m_mesh.indexFormat = IndexFormat.UInt32;
            
            UpdateTF();
        }

        public void UpdateTF()
        {
            Task.Factory.StartNew(() =>
            {
                //First, discard multiple call to this function (only two calls are available)
                lock (m_isTFUpdateLock)
                {
                    if (m_waitToUpdateTF)
                    {
                        Debug.Log("Already waiting for TF");
                        return;
                    }
                    else
                        m_waitToUpdateTF = true;
                }

                //The wait to update the TF
                lock (m_updateTFLock)
                {
                    //Another one can wait to update the TF if it wants (max: two parallel call, one pending, one executing)
                    lock (m_isTFUpdateLock)
                        m_waitToUpdateTF = false;

                    TransferFunction tf = null;
                    lock (m_sd)
                        tf = (TransferFunction)m_sd.TransferFunction.Clone();

                    Debug.Log("Updating TF...");

                    byte[] colors = ComputeTFColor(tf);
                    lock (this)
                    {
                        m_colors = colors;
                    }
                }
            });
        }

        [BurstCompile(CompileSynchronously = true)]
        private byte[] ComputeTFColor(TransferFunction tf)
        {
            int hasGradient = (tf.HasGradient() ? 1 : 0);

            if (m_dataset.IsLoaded == false || tf == null || tf.GetDimension() - hasGradient > m_dataset.PointFieldDescs.Count ||
               (m_sd.OwnerID != -1 && m_sd.OwnerID != m_dataProvider.GetHeadsetID())) //Not a public subdataset
                return null;

            else
            {
                unsafe
                {
                    int[] indices = new int[m_dataset.PointFieldDescs.Count];
                    for (int i = 0; i < indices.Length; i++)
                        indices[i] = i;

                    Datasets.Gradient gradient = m_dataset.GetGradient(indices);

                    byte[] colors = new byte[4*m_dataset.NbPoints]; //RGBA colors;

                    List<PointFieldDescriptor> ptDescs = m_dataset.PointFieldDescs;

                    Parallel.For(0, m_dataset.NbPoints,
                        i =>
                        {
                            fixed (byte* pcolors = colors)
                            {
                                float[] partialRes = new float[indices.Length + hasGradient];

                                if(m_sd.EnableVolumetricMask && !m_sd.GetVolumetricMaskAt((int)i))
                                {
                                    pcolors[4 * i + 3] = 0;
                                    return;
                                }

                                //Determine transfer function coordinates
                                for (int l = 0; l < ptDescs.Count; l++)
                                {
                                    int ids = indices[l];
                                    if (ptDescs[ids].NbValuesPerTuple == 1)
                                        partialRes[ids] = (ptDescs[ids].Value[0].ReadAsFloat((ulong)i) - ptDescs[ids].MinVal) / (ptDescs[ids].MaxVal - ptDescs[ids].MinVal);
                                    else
                                        partialRes[ids] = (ptDescs[ids].ReadMagnitude((ulong)i, 0) - ptDescs[ids].MinVal) / (ptDescs[ids].MaxVal - ptDescs[ids].MinVal);
                                }

                                if(tf.HasGradient())
                                {
                                    if(gradient != null)
                                        partialRes[partialRes.Length-1] = gradient.Values[0][(ulong)i]; //In case we need the gradient
                                    else
                                        partialRes[partialRes.Length-1] = 0.0f;
                                }

                                Color c = tf.ComputeColor(partialRes);

                                pcolors[4*i + 0] = (byte)(c.r*255);
                                pcolors[4*i + 1] = (byte)(c.g*255);
                                pcolors[4*i + 2] = (byte)(c.b*255);
                                pcolors[4*i + 3] = (byte)(255);
                            }
                        });
                    return colors;
                }
            }
        }

        public override void LateUpdate()
        {
            base.LateUpdate();
            lock(this)
            {
                if(m_colors != null)
                {
                    //Set position
                    if (!m_isPositionInit)
                    { 
                        m_mesh.SetVertexBufferData(m_dataset.Position, 0, 0, (int)m_dataset.NbPoints*3, 0);
                        m_isPositionInit = true;

                        int[] indices = new int[m_dataset.NbPoints];
                        for (int i = 0; i < m_dataset.NbPoints; i++)
                            indices[i] = i;

                        m_mesh.SetIndices(indices, MeshTopology.Points, 0);
                        m_mesh.bounds = new Bounds(new Vector3(0.0f, 0.0f, 0.0f), 
                                                   new Vector3(1.0f, 1.0f, 1.0f));
                    }

                    //Set color
                    m_mesh.SetVertexBufferData(m_colors, 0, 0, (int)m_dataset.NbPoints*4, 1);
                    m_colors = null;
                }
            }

            if (m_isPositionInit)
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, MaterialCP, gameObject.layer);
        }

        public override void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
            base.OnTransferFunctionChange(dataset, tf);
            UpdateTF();
        }

        public override void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {
            base.OnOwnerIDChange(dataset, ownerID);
            UpdateTF();
        }

        public override void OnChangeVolumetricMask(SubDataset dataset)
        {
            base.OnChangeVolumetricMask(dataset);
            UpdateTF();
        }
    }
}
