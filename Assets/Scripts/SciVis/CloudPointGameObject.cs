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
        private float[] m_colors = null;

        /// <summary>
        /// The Mesh corresponding to the data cloud
        /// </summary>
        private Mesh m_mesh;

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
                new VertexAttributeDescriptor(VertexAttribute.Color,    VertexAttributeFormat.Float32, 4, 1),
            };
            m_mesh.SetVertexBufferParams((int)dataset.NbPoints, layout);

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

                    lock (this)
                    {
                        m_colors = ComputeTFColor(tf);
                    }
                }
            });
        }

        [BurstCompile(CompileSynchronously = true)]
        private float[] ComputeTFColor(TransferFunction tf)
        {
            if (m_dataset.IsLoaded == false || tf == null || tf.GetDimension() > m_dataset.PointFieldDescs.Count + 1 ||
               (m_sd.OwnerID != -1 && m_sd.OwnerID != m_dataProvider.GetHeadsetID())) //Not a public subdataset
                return null;

            else
            {
                unsafe
                {
                    float[] colors = new float[4*m_dataset.NbPoints]; //RGBA colors;

                    List<PointFieldDescriptor> ptDescs = m_dataset.PointFieldDescs;
                    Parallel.For(0, m_dataset.NbPoints,
                        i =>
                        {
                            float[] partialRes = new float[ptDescs.Count + 1];
                            fixed (float* pcolors = colors)
                            {
                                //Determine transfer function coordinates
                                for (int l = 0; l < ptDescs.Count; l++)
                                {
                                    if (ptDescs[l].NbValuesPerTuple == 1)
                                        partialRes[l] = (ptDescs[l].Value.ReadAsFloat((ulong)i) - ptDescs[l].MinVal) / (ptDescs[l].MaxVal - ptDescs[l].MinVal);
                                    else
                                        partialRes[l] = (ptDescs[l].ReadMagnitude((ulong)i) - ptDescs[l].MinVal) / (ptDescs[l].MaxVal - ptDescs[l].MinVal);
                                }

                                if (m_dataset.Gradient != null)
                                    partialRes[partialRes.Length - 1] = m_dataset.Gradient[(ulong)i]; //In case we need the gradient
                                else
                                    partialRes[partialRes.Length - 1] = 0.0f;

                                float t = tf.ComputeColor(partialRes);
                                float a = tf.ComputeAlpha(partialRes);

                                Color c = SciVisColor.GenColor(tf.ColorMode, t);
                                pcolors[4*i + 0] = c.r;
                                pcolors[4*i + 1] = c.g;
                                pcolors[4*i + 2] = c.b;
                                pcolors[4*i + 3] = c.a;
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
                    Debug.Log("Updating the mesh...");

                    //Set position
                    if (!m_isPositionInit)
                    { 
                        m_mesh.SetVertexBufferData(m_dataset.Position, 0, 0, (int)m_dataset.NbPoints*3, 0);
                        m_isPositionInit = true;

                        int[] indices = new int[m_dataset.NbPoints];
                        for (int i = 0; i < m_dataset.NbPoints; i++)
                            indices[i] = i;
                        m_mesh.SetIndices(indices, MeshTopology.Points, 0);
                        m_mesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
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
    }
}
