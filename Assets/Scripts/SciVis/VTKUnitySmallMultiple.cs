using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Sereno.Datasets;
using Sereno.SciVis;
using System.Threading.Tasks;
using Unity.Burst;

namespace Sereno.SciVis
{
    /// <summary>
    /// VTK Small Multiple datasets. Computes data to use for 3D volumetric rendering
    /// </summary>
    public class VTKUnitySmallMultiple : ISubDatasetCallback, ISubvolumeSelection
    {
        /// <summary>
        /// The internal state of this small multiple
        /// </summary>
        private SubDataset m_subDataset;
        
        /// <summary>
        /// The dimensions in use
        /// </summary>
        private Vector3Int m_dimensions;

        /// <summary>
        /// The actual spacing between each cell (captured one)
        /// </summary>
        private Vector3    m_spacing;

        /// <summary>
        /// The 3D texture color RGBA4444 byte Array 
        /// </summary>
        private short[] m_textureColor = null;

        /// <summary>
        /// The VTKStructuredPoints associated with this small multiple
        /// </summary>
        private VTKStructuredPoints m_descPts;

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
        /// The application data provider
        /// </summary>
        private IDataProvider m_dataProvider = null;

        /// <summary>
        /// Was there any selection applied yet? true == no, false == otherwise
        /// </summary>
        private bool m_noSelection = true;

        /// <summary>
        /// The mask to apply to the object
        /// </summary>
        private bool[] m_mask = null;

        /// <summary>
        /// Initialize the small multiple
        /// </summary>
        /// <param name="parser">The VTK Parser</param>
        /// <param name="subDataset">The SubDataset bound to this VTK view</param>
        /// <param name="dimensions">The dimensions in use</param>
        /// <param name="dataProvider">The application data provider (a.k.a, Main)</param>
        /// <returns></returns>
        public unsafe bool Init(VTKParser parser, SubDataset subDataset, Vector3Int dimensions, IDataProvider dataProvider)
        {
            subDataset.AddListener(this);
            m_subDataset = subDataset;

            m_dataProvider = dataProvider;

            //Copy the variables
            m_dimensions = dimensions;
            m_subDataset = subDataset;

            VTKStructuredPoints descPts = parser.GetStructuredPointsDescriptor();
            m_descPts = descPts;

            float  maxAxis   = (float)Math.Max(descPts.Spacing[0]*m_dimensions[0],
                                               Math.Max(descPts.Spacing[1]*m_dimensions[1],
                                                        descPts.Spacing[2]*m_dimensions[2]));

            m_spacing = new Vector3();
            for(int i = 0; i < 3; i++)
                m_spacing[i] = (float)(descPts.Spacing[i]/maxAxis);

            m_mask = new bool[m_dimensions[0] * m_dimensions[1] * m_dimensions[2]];
            for(int i = 0; i < m_dimensions[0] * m_dimensions[1] * m_dimensions[2]; i++)
                m_mask[i] = true;

            UpdateTF();

            return true;
        }

        /// <summary>
        /// Update the computation based on the transfer function
        /// </summary>
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
                    lock (m_subDataset)
                        tf = (TransferFunction)m_subDataset.TransferFunction.Clone();

                    Debug.Log("Updating TF...");

                    lock (this)
                    {
                        m_textureColor = ComputeTFColor(tf);
                    }
                }
            });
        }

        [BurstCompile(CompileSynchronously = true)]
        private short[] ComputeTFColor(TransferFunction tf)
        {
            VTKDataset vtk = (VTKDataset)m_subDataset.Parent;
            int hasGradient = (tf.HasGradient() ? 1 : 0);

            if (vtk.IsLoaded == false || tf == null || tf.GetDimension()- hasGradient > vtk.PointFieldDescs.Count ||
               (m_subDataset.OwnerID != -1 && m_subDataset.OwnerID != m_dataProvider.GetHeadsetID())) //Not a public subdataset
                return null;

            else
            {
                unsafe
                {
                    int[] indices = new int[vtk.PointFieldDescs.Count];
                    for (int i = 0; i < indices.Length; i++)
                        indices[i] = i;

                    Datasets.Gradient gradient = vtk.GetGradient(indices);
                    short[] colors = new short[m_dimensions.x * m_dimensions.y * m_dimensions.z]; //short because RGBA4444 == 2 bytes -> short

                    List<PointFieldDescriptor> ptDescs = m_subDataset.Parent.PointFieldDescs;
                    Parallel.For(0, m_dimensions.z,
                        k =>
                        {
                            float[] partialRes = new float[indices.Length + hasGradient];

                            fixed (short* pcolors = colors)
                            {
                                UInt64 ind = (UInt64)(k * m_dimensions.x * m_dimensions.y);
                                for (int j = 0; j < m_dimensions.y; j++)
                                {
                                    //Pre compute the indice up to J--K coordinate
                                    UInt64 readIntJK = (UInt64)(m_descPts.Size[0] * (j * m_descPts.Size[1] / m_dimensions.y) +
                                                                m_descPts.Size[0] * m_descPts.Size[1] * (k * m_descPts.Size[2] / m_dimensions.z));

                                    for (int i = 0; i < m_dimensions.x; i++)
                                    {
                                        UInt64 readInd = (UInt64)(i * m_descPts.Size[0] / m_dimensions.x) + readIntJK;

                                        if (vtk.MaskValue != null && ((byte*)(vtk.MaskValue.Value))[readInd] == 0 ||
                                            m_mask[ind] == false)
                                            pcolors[ind] = 0;
                                        else
                                        {
                                            //Determine transfer function coordinates
                                            for(int l = 0; l < indices.Length; l++)
                                            {
                                                int ids = indices[l];
                                                if (ptDescs[ids].NbValuesPerTuple == 1)
                                                    partialRes[ids] = (ptDescs[ids].Value.ReadAsFloat(readInd) - ptDescs[ids].MinVal) / (ptDescs[ids].MaxVal - ptDescs[ids].MinVal);
                                                else
                                                    partialRes[ids] = (ptDescs[ids].ReadMagnitude(readInd) - ptDescs[ids].MinVal) / (ptDescs[ids].MaxVal - ptDescs[ids].MinVal);
                                            }

                                            if(tf.HasGradient())
                                            {
                                                if (gradient != null)
                                                    partialRes[partialRes.Length - 1] = gradient.Values[readInd]; //In case we need the gradient
                                                else
                                                    partialRes[partialRes.Length - 1] = 0.0f;
                                            }

                                            float t = tf.ComputeColor(partialRes);
                                            float a = tf.ComputeAlpha(partialRes);
                                            
                                            Color c = SciVisColor.GenColor(tf.ColorMode, t);
                                            short r = (short)(16 * c.r);
                                            if (r > 15)
                                                r = 15;
                                            short g = (short)(16 * c.g);
                                            if (g > 15)
                                                g = 15;
                                            short b = (short)(16 * c.b);
                                            if (b > 15)
                                                b = 15;
                                            short _a = (short)(16 * a);
                                            if (_a > 15)
                                                _a = 15;
                                            pcolors[ind] = (short)((r << 12) + (g << 8) + //RGBA4444 color format
                                                                   (b << 4) + _a);
                                        }
                                        ind += 1;
                                    }
                                }
                            }
                        });
                    return colors;
                }
            }
            return null;
        }
        
        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion){}

        public void OnPositionChange(SubDataset dataset, float[] position){}

        public void OnScaleChange(SubDataset dataset, float[] scale){}

        public void OnLockOwnerIDChange(SubDataset dataset, int ownerID){}

        public void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {
            UpdateTF();
        }

        public void OnNameChange(SubDataset dataset, String name){}

        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
            UpdateTF();
        }

        public void OnAddAnnotation(SubDataset dataset, Annotation annot){}

        public void OnClearAnnotations(SubDataset dataset){}

        [BurstCompile(CompileSynchronously = true)]
        public virtual void OnSelection(NewSelectionMeshData meshData, Matrix4x4 MeshToLocalMatrix)
        {
            const int CUBE_SIZE_X = 16;
            const int CUBE_SIZE_Y = 16;
            const int CUBE_SIZE_Z = 16;

            int[] CUBE_SIZE = new int[3] { CUBE_SIZE_X, CUBE_SIZE_Y, CUBE_SIZE_Z };

            int nbPoints = m_dimensions.x * m_dimensions.y * m_dimensions.z;

            if (meshData.Triangles.Count % 3 != 0)
            {
                Debug.LogWarning("The number of triangles is not a multiple of 3. Abort");
                return;
            }

            if (m_noSelection)
            {
                m_noSelection = false;

                //Remove every point
                Parallel.For(0, nbPoints,
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
            RasteredCube[] rasteredSpace = new RasteredCube[CUBE_SIZE_X * CUBE_SIZE_Y * CUBE_SIZE_Z];
            //rasteredSpace.Capacity = CUBE_SIZE_X * CUBE_SIZE_Y * CUBE_SIZE_Z; //Set the capacity of this list
            for (int i = 0; i < CUBE_SIZE_X * CUBE_SIZE_Y * CUBE_SIZE_Z; i++)
                rasteredSpace[i] = new RasteredCube();

            //Go through all the triangles. For each triangle, determine in which cells it is
            for (int i = 0; i < meshData.Triangles.Count / 3; i++)
            {
                //Get the bounding box of the triangle
                float[] minPos = new float[3];
                float[] maxPos = new float[3];
                for (int j = 0; j < 3; j++)
                {
                    minPos[j] = Math.Min(points[meshData.Triangles[3 * i + 0]][j], Math.Min(points[meshData.Triangles[3 * i + 1]][j], points[meshData.Triangles[3 * i + 2]][j]));
                    maxPos[j] = Math.Max(points[meshData.Triangles[3 * i + 0]][j], Math.Max(points[meshData.Triangles[3 * i + 1]][j], points[meshData.Triangles[3 * i + 2]][j]));

                    //Set them as indices in our 3D restered space
                    minPos[j] = CUBE_SIZE[j] * ((minPos[j] + 0.5f));
                    maxPos[j] = CUBE_SIZE[j] * ((maxPos[j] + 0.5f));

                    minPos[j] = Math.Max(0.0f, minPos[j]);
                    maxPos[j] = Math.Max(-1.0f, maxPos[j]); //-1 allows to "block" the for loops
                }

                //Cross the bounding box and our 3D rastered space
                for (int kk = (int)minPos[2]; kk <= (int)maxPos[2] && kk < CUBE_SIZE_Z; kk++)
                    for (int jj = (int)minPos[1]; jj <= (int)maxPos[1] && jj < CUBE_SIZE_Y; jj++)
                        for (int ii = (int)minPos[0]; ii <= (int)maxPos[0] && ii < CUBE_SIZE_X; ii++)
                            rasteredSpace[ii + CUBE_SIZE_X * jj + CUBE_SIZE_Y * CUBE_SIZE_X * kk].TriangleIDs.Add(i);
            }

            //Update the variable "MaxNbTriangles", useful to know the most efficient way to cast a ray
            for (int k = 0; k < CUBE_SIZE_Z; k++)
            {
                for (int j = 0; j < CUBE_SIZE_Y; j++)
                {
                    int nbTriangles = 0;
                    for (int i = 0; i < CUBE_SIZE_X; i++)
                    {
                        nbTriangles += rasteredSpace[i + CUBE_SIZE_X * j + CUBE_SIZE_Y * CUBE_SIZE_X * k].TriangleIDs.Count;
                        rasteredSpace[i + CUBE_SIZE_X * j + CUBE_SIZE_Y * CUBE_SIZE_X * k].MaxNbTriangle = nbTriangles;
                    }
                }
            }

            //Go through all the points of the dataset and check if it is inside or outside the Mesh
            Parallel.For(0, m_dimensions.z,
            z =>
            {
                for(int y = 0; y < m_dimensions.y; y++)
                { 
                    for(int x = 0; x < m_dimensions.x; x++)
                    { 
                        Vector3 pos = new Vector3(((float)x)/m_dimensions.x - 0.5f, ((float)y)/m_dimensions.y - 0.5f, ((float)z)/m_dimensions.z - 0.5f);
                        Vector3 rayDir = new Vector3(1.0f, 0.0f, 0.0f);
                        int nbIntersection = 0;
                        List<int> triangleIDAlready = new List<int>();

                        //The particule X position in the rastered space
                        int particuleX = (int)Math.Min(CUBE_SIZE_X - 1, CUBE_SIZE_X * (pos[0] + 0.5f));
                        int particuleY = (int)Math.Min(CUBE_SIZE_Y - 1, CUBE_SIZE_Y * (pos[1] + 0.5f));
                        int particuleZ = (int)Math.Min(CUBE_SIZE_Z - 1, CUBE_SIZE_Z * (pos[2] + 0.5f));

                        particuleX = Math.Max(particuleX, 0);
                        particuleY = Math.Max(particuleY, 0);
                        particuleZ = Math.Max(particuleZ, 0);

                        //Go through all the cubes
                        Vector3[] triangle = new Vector3[3];

                        Action<int> rayCastAction =
                        (j) =>
                        {
                            RasteredCube cube = rasteredSpace[j + particuleY * CUBE_SIZE_X + particuleZ * CUBE_SIZE_X * CUBE_SIZE_Y];

                            foreach (int triangleID in cube.TriangleIDs)
                            {
                                for (int i = 0; i < 3; i++)
                                    triangle[i] = points[meshData.Triangles[3 * triangleID + i]];

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

                        //Select the most efficient direction (i.e., less test)
                        if(2 * rasteredSpace[particuleX    + particuleY * CUBE_SIZE_Y + particuleZ * CUBE_SIZE_X * CUBE_SIZE_Y].MaxNbTriangle -
                               rasteredSpace[CUBE_SIZE_X-1 + particuleY * CUBE_SIZE_Y + particuleZ * CUBE_SIZE_X * CUBE_SIZE_Y].MaxNbTriangle > 0)
                        {
                            for (int j = particuleX; j < CUBE_SIZE_X; j++)
                                rayCastAction(j);
                        }
                        else
                        {
                            rayDir = new Vector3(-1.0f, 0.0f, 0.0f);
                            for (int j = particuleX; j >= 0; j--)
                                rayCastAction(j);
                        }

                        int ind = x + y * m_dimensions.x + z * m_dimensions.x * m_dimensions.y;
                        
                        //Apply the boolean operation
                        switch (meshData.BooleanOP)
                        {
                            case BooleanSelectionOperation.UNION:
                                m_mask[ind] = m_mask[ind] || (nbIntersection % 2 == 1);
                                break;
                            case BooleanSelectionOperation.INTER:
                                m_mask[ind] = m_mask[ind] && (nbIntersection % 2 == 1);
                                break;
                            case BooleanSelectionOperation.MINUS:
                                m_mask[ind] = m_mask[ind] && !(nbIntersection % 2 == 1);
                                break;
                            default:
                                break;
                        }
                    }
                }
            });

            //Update the transfer function at the end
            UpdateTF();
        }

        public void OnToggleMapVisibility(SubDataset dataset, bool visibility)
        {}

        /// <summary>
        /// The 3D Texure RGBA4444 byte array computed via the given transfer function.
        /// </summary>
        /// <returns></returns>
        public short[] TextureColor {get => m_textureColor; set { lock (this) { m_textureColor = value; } } }

        /// <summary>
        /// The SubDataset representing this Small Multiple
        /// </summary>
        public SubDataset SubDataset
        {
            get => m_subDataset;
            set
            {
                if(m_subDataset != null)
                    m_subDataset.RemoveListener(this);
                m_subDataset = value;
                m_subDataset.AddListener(this);
            }
        }

        /// <summary>
        /// The three axis dimensions
        /// </summary>
        /// <returns>The Three axis dimensions</returns>
        public Vector3Int Dimensions {get => m_dimensions;}

        /// <summary>
        /// The Point descriptors of the structured grid used
        /// </summary>
        /// <returns>A VTKStructuredPoints describing the grid</returns>
        public VTKStructuredPoints DescPts {get => m_descPts;}
    }
}