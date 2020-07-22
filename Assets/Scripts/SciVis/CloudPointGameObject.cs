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
        /// Was there an attempt of selection? True == no attemp, false otherwise
        /// </summary>
        private bool m_noSelection = true;

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
            
            m_mask = new bool[m_dataset.NbPoints];
            for (int i = 0; i < m_dataset.NbPoints; i++)
                m_mask[i] = true;
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
            if (m_dataset.IsLoaded == false || tf == null || tf.GetDimension() > m_dataset.PointFieldDescs.Count + 1 ||
               (m_sd.OwnerID != -1 && m_sd.OwnerID != m_dataProvider.GetHeadsetID())) //Not a public subdataset
                return null;

            else
            {
                unsafe
                {
                    byte[] colors = new byte[4*m_dataset.NbPoints]; //RGBA colors;

                    List<PointFieldDescriptor> ptDescs = m_dataset.PointFieldDescs;
                    Parallel.For(0, m_dataset.NbPoints,
                        i =>
                        {
                            float[] partialRes = new float[ptDescs.Count + 1];
                            fixed (byte* pcolors = colors)
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
                                pcolors[4*i + 0] = (byte)(c.r*255);
                                pcolors[4*i + 1] = (byte)(c.g*255);
                                pcolors[4*i + 2] = (byte)(c.b*255);
                                pcolors[4*i + 3] = (byte)(m_mask[i] ? 255 : 0);
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

        [BurstCompile(CompileSynchronously = true)]
        public override void OnSelection(NewSelectionMeshData meshData, Matrix4x4 MeshToLocalMatrix)
        {
            Debug.Log($"Nb points for selection: {m_dataset.NbPoints}");
            const int CUBE_SIZE_X = 16;
            const int CUBE_SIZE_Y = 16;
            const int CUBE_SIZE_Z = 16;

            int[] CUBE_SIZE = new int[3] { CUBE_SIZE_X, CUBE_SIZE_Y, CUBE_SIZE_Z };

            if (meshData.Triangles.Count % 3 != 0)
            {
                Debug.LogWarning("The number of triangles is not a multiple of 3. Abort");
                return;
            }

            if(m_noSelection)
            {
                m_noSelection = false;

                //Remove every point
                Parallel.For(0, m_dataset.NbPoints,
                k =>
                {
                    m_mask[k] = false;
                });
            }
            
            //First, transform every point using the provided matrix
            Vector3[] points = new Vector3[meshData.Points.Count];
            Parallel.For(0, meshData.Points.Count,
            i =>
            {
                points[i] = MeshToLocalMatrix.MultiplyPoint(meshData.Points[i]);
            });

            //Second, initialize our raster 3D space. Each cell contains the list of triangles it contains. Indice(x, y, z) = z*CUBE_SIZE_X*CUBE_SIZE_Y + y*CUBE_SIZE_X + x
            RasteredCube[] rasteredSpace = new RasteredCube[CUBE_SIZE_X*CUBE_SIZE_Y*CUBE_SIZE_Z];
            //rasteredSpace.Capacity = CUBE_SIZE_X * CUBE_SIZE_Y * CUBE_SIZE_Z; //Set the capacity of this list
            for(int i = 0; i <  CUBE_SIZE_X * CUBE_SIZE_Y * CUBE_SIZE_Z; i++)
                rasteredSpace[i] = new RasteredCube();

            //Go through all the triangles. For each triangle, determine in which cells it is
            for(int i = 0; i < meshData.Triangles.Count/3; i++)
            {
                //Get the bounding box of the triangle
                float[] minPos = new float[3];
                float[] maxPos = new float[3];
                for(int j = 0; j < 3; j++)
                {
                    minPos[j] = Math.Min(points[meshData.Triangles[3*i+0]][j], Math.Min(points[meshData.Triangles[3*i+1]][j], points[meshData.Triangles[3*i+2]][j]));
                    maxPos[j] = Math.Max(points[meshData.Triangles[3*i+0]][j], Math.Max(points[meshData.Triangles[3*i+1]][j], points[meshData.Triangles[3*i+2]][j]));

                    //Set them as indices in our 3D restered space
                    minPos[j] = CUBE_SIZE[j] * ((minPos[j] - m_dataset.MinPos[j]) / (m_dataset.MaxPos[j] - m_dataset.MinPos[j]));
                    maxPos[j] = CUBE_SIZE[j] * ((maxPos[j] - m_dataset.MinPos[j]) / (m_dataset.MaxPos[j] - m_dataset.MinPos[j]));

                    minPos[j] = Math.Max(0.0f,  minPos[j]);
                    maxPos[j] = Math.Max(-1.0f, maxPos[j]);
                }

                //Cross the bounding box and our 3D rastered space
                for(int kk = (int)minPos[2]; kk <= (int)maxPos[2] && kk < CUBE_SIZE_Z; kk++)
                    for(int jj = (int)minPos[1]; jj <= (int)maxPos[1] && jj < CUBE_SIZE_Y; jj++)
                        for(int ii = (int)minPos[0]; ii <= (int)maxPos[0] && ii < CUBE_SIZE_X; ii++)
                            rasteredSpace[ii + CUBE_SIZE_X*jj + CUBE_SIZE_Y*CUBE_SIZE_X*kk].TriangleIDs.Add(i);
            }

            //Update the variable "MaxNbTriangles", useful to know the most efficient way to cast a ray
            for(int k = 0; k < CUBE_SIZE_Z; k++)
            {
                for(int j = 0; j < CUBE_SIZE_Y; j++)
                {
                    int nbTriangles = 0;
                    for(int i = 0; i < CUBE_SIZE_X; i++)
                    {
                        nbTriangles += rasteredSpace[i + CUBE_SIZE_X*j + CUBE_SIZE_Y*CUBE_SIZE_X*k].TriangleIDs.Count;
                        rasteredSpace[i + CUBE_SIZE_X*j + CUBE_SIZE_Y*CUBE_SIZE_X*k].MaxNbTriangle = nbTriangles;
                    }
                }
            }

            //Go through all the points of the dataset and check if it is inside or outside the Mesh
            Parallel.For(0, m_dataset.NbPoints,
            k =>
            {
                Vector3 pos    = new Vector3(m_dataset.Position[3*k + 0], m_dataset.Position[3*k + 1], m_dataset.Position[3*k + 2]);
                Vector3 rayDir = new Vector3(1.0f, 0.0f, 0.0f);
                int nbIntersection = 0;
                List<int> triangleIDAlready = new List<int>();

                //The particule X position in the rastered space
                int particuleX = (int)Math.Min(CUBE_SIZE_X-1, CUBE_SIZE_X * ((pos[0] - m_dataset.MinPos[0]) / (m_dataset.MaxPos[0] - m_dataset.MinPos[0])));
                int particuleY = (int)Math.Min(CUBE_SIZE_Y-1, CUBE_SIZE_Y * ((pos[1] - m_dataset.MinPos[1]) / (m_dataset.MaxPos[1] - m_dataset.MinPos[1])));
                int particuleZ = (int)Math.Min(CUBE_SIZE_Z-1, CUBE_SIZE_Z * ((pos[2] - m_dataset.MinPos[2]) / (m_dataset.MaxPos[2] - m_dataset.MinPos[2])));

                particuleX = Math.Max(particuleX, 0);
                particuleY = Math.Max(particuleY, 0);
                particuleZ = Math.Max(particuleZ, 0);

                //Go through all the cubes
                Vector3[] triangle = new Vector3[3];

                Action<int> rayCastAction = 
                (j) => 
                {
                    RasteredCube cube = rasteredSpace[j + particuleY*CUBE_SIZE_X + particuleZ*CUBE_SIZE_X*CUBE_SIZE_Y];

                    foreach(int triangleID in cube.TriangleIDs)
                    {
                        for (int i = 0; i < 3; i++)
                            triangle[i] = points[meshData.Triangles[3*triangleID + i]];

                        //Ray -- triangle intersection along the positive x axis
                        float t;
                        if (RayIntersection.RayTriangleIntersection(pos, rayDir, triangle, out t))
                        {
                            //Do not check multiple times the same triangle
                            if (triangleIDAlready.Contains(triangleID))
                                continue;
                            nbIntersection++;
                            triangleIDAlready.Add(triangleID);
                        }
                    }
                };

                //Select the most efficient way
                if(2*rasteredSpace[particuleX    + particuleY*CUBE_SIZE_Y + particuleZ*CUBE_SIZE_X*CUBE_SIZE_Y].MaxNbTriangle - 
                     rasteredSpace[CUBE_SIZE_X-1 + particuleY*CUBE_SIZE_Y + particuleZ*CUBE_SIZE_X*CUBE_SIZE_Y].MaxNbTriangle > 0)
                { 
                    for(int j = particuleX; j < CUBE_SIZE_X; j++)
                        rayCastAction(j);
                }
                else
                {
                    rayDir = new Vector3(-1.0f, 0.0f, 0.0f);
                    for (int j = particuleX; j >= 0; j--)
                        rayCastAction(j);
                }

                if (nbIntersection%2 == 1)
                {
                    m_mask[k] = true;
                }
            });

            //Update the transfer function at the end
            UpdateTF();
        }
    }
}