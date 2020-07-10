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
        /// The mask array we compute during selection
        /// </summary>
        private bool[] m_mask;

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
                new VertexAttributeDescriptor(VertexAttribute.Color,    VertexAttributeFormat.Float32, 4, 1),
            };
            m_mesh.SetVertexBufferParams((int)dataset.NbPoints, layout);

            m_mask = new bool[m_dataset.NbPoints];
            for (int i = 0; i < m_dataset.NbPoints; i++)
                m_mask[i] = false;
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

                    float[] colors = ComputeTFColor(tf);
                    lock (this)
                    {
                        m_colors = colors;
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

                                Color c = SciVisColor.GenColor(tf.ColorMode, t);
                                pcolors[4*i + 0] = c.r;
                                pcolors[4*i + 1] = c.g;
                                pcolors[4*i + 2] = c.b;
                                pcolors[4*i + 3] = (m_mask[i] ? 1.0f : 0.0f);
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


        public override void OnSelection(NewSelectionMeshData meshData, Matrix4x4 MeshToLocalMatrix)
        {
            const int CUBE_SIZE_X = 16;
            const int CUBE_SIZE_Y = 16;
            const int CUBE_SIZE_Z = 16;

            int[] CUBE_SIZE = new int[3] { CUBE_SIZE_X, CUBE_SIZE_Y, CUBE_SIZE_Z };

            if (meshData.Triangles.Count % 3 != 0)
            {
                Debug.LogWarning("The number of triangles is not a multiple of 3. Abort");
                return;
            }

            //First, transform every point using the provided matrix
            List<Vector3> points = new List<Vector3>(meshData.Points);
            for(int i = 0; i < points.Count; i++)
                points[i] = MeshToLocalMatrix.MultiplyPoint(points[i]);

            //Second, initialize our raster our space. Each cell contains the list of triangles it contains. Indice(x, y, z) = z*CUBE_SIZE_X*CUBE_SIZE_Y + y*CUBE_SIZE_X + x
            List<List<int>> rasteredSpace = new List<List<int>>();
            rasteredSpace.Capacity = CUBE_SIZE_X * CUBE_SIZE_Y * CUBE_SIZE_Z; //Set the capacity of this list
            for(int k = 0; k < 16; k++)
                for(int j = 0; j < 16; j++)
                    for(int i = 0; i < 16; i++)
                        rasteredSpace.Add(new List<int>());

            //Go through all the triangles. For each triangle, determine in which cells it is
            for(int i = 0; i < meshData.Triangles.Count; i+=3)
            {
                //Get the bounding box of the triangle
                float[] minPos = new float[3];
                float[] maxPos = new float[3];
                for(int j = 0; j < 3; j++)
                {
                    minPos[j] = Math.Min(points[meshData.Triangles[i]][j], Math.Min(points[meshData.Triangles[i + 1]][j], points[meshData.Triangles[i + 2]][j]));
                    maxPos[j] = Math.Max(points[meshData.Triangles[i]][j], Math.Max(points[meshData.Triangles[i + 1]][j], points[meshData.Triangles[i + 2]][j]));

                    //Set them as indices in our 3D restered space
                    minPos[j] = Math.Min(15, CUBE_SIZE[j] * ((minPos[j] - m_dataset.MinPos[j]) / (m_dataset.MaxPos[j] - m_dataset.MinPos[j])));
                    maxPos[j] = Math.Min(15, CUBE_SIZE[j] * ((maxPos[j] - m_dataset.MinPos[j]) / (m_dataset.MaxPos[j] - m_dataset.MinPos[j])));
                }

                //Cross the bounding box and our 3D rastered space
                for(int kk = (int)minPos[2]; kk <= (int)maxPos[2]; kk++)
                    for(int jj = (int)minPos[1]; jj <= (int)maxPos[1]; jj++)
                        for(int ii = (int)minPos[0]; ii <= (int)maxPos[0]; ii++)
                            rasteredSpace[ii + CUBE_SIZE_X * jj + CUBE_SIZE_Y* CUBE_SIZE_X* kk].Add(i);
            }

            //Go through all the points of the dataset and check if it is inside or outside the Mesh
            Parallel.For(0, m_dataset.NbPoints,
                (k, loopState) =>
                {
                    List<float> tComputed = new List<float>(); //All the valid t already computed. Useful for not checking multiple times the same triangles

                    //The particule X position in the rastered space
                    int particuleX = (int)Math.Min(15, CUBE_SIZE_X * (m_dataset.Position[3*k + 0] - m_dataset.MinPos[0]) / (m_dataset.MaxPos[0] - m_dataset.MinPos[0]));
                    int particuleY = (int)Math.Min(15, CUBE_SIZE_Y * (m_dataset.Position[3*k + 1] - m_dataset.MinPos[1]) / (m_dataset.MaxPos[1] - m_dataset.MinPos[1]));
                    int particuleZ = (int)Math.Min(15, CUBE_SIZE_Z * (m_dataset.Position[3*k + 2] - m_dataset.MinPos[2]) / (m_dataset.MaxPos[2] - m_dataset.MinPos[2]));

                    //Go through all the cubes
                    Vector3[] triangle = new Vector3[3];

                    for(int j = particuleX; j < CUBE_SIZE_X; j++)
                    {
                        List<int> cube = rasteredSpace[j + particuleY* CUBE_SIZE_X + particuleZ*CUBE_SIZE_X*CUBE_SIZE_Y];

                        foreach(int triangleID in cube)
                        {
                            for(int i = 0; i < 3; i++)
                                triangle[i] = points[meshData.Triangles[3*triangleID + i]];

                            //Ray -- triangle intersection along the positive x axis
                            float t;
                            if(RayIntersection.RayTriangleIntersection(new Vector3(m_dataset.Position[3*k + 0], m_dataset.Position[3*k + 1], m_dataset.Position[3*k + 2]),
                                                                       new Vector3(1.0f, 0.0f, 0.0f), triangle, out t))
                            {
                                //Discard this t
                                if (tComputed.Contains(t))
                                    continue;
                                tComputed.Add(t);
                            }
                        }
                    }

                    if(tComputed.Count%2 == 1)
                    {
                        Debug.Log("Particule inside the mesh!!!");
                        m_mask[k] = true;
                    }
                    else
                    {
                        Debug.Log("Particule outside the mesh!!!");
                        m_mask[k] = false;
                    }
                });

            //Update the transfer function at the end
            UpdateTF();
        }
    }
}
