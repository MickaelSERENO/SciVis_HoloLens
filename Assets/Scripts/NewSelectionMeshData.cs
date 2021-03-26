using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    /// <summary>
    /// The different Boolean Operation a Selection Mesh can perform
    /// </summary>
    public enum BooleanSelectionOperation
    {
        NONE  = -1, //No operation
        UNION = 0,  //Perform a  Union
        MINUS = 1,  //Perform a  difference
        INTER = 2   //Perform an intersection
    }

    /// <summary>
    /// the different volumetric selection visualization mode (i.e., how to visualize the volume selecting the regions)
    /// </summary>
    public enum VolumetricSelectionVisualizationMode
    {
        FULL = 0, //Displays the whole volume

        FULL_OUTLINE = 1, //Displays outlines of the volumes + lassos

        WEAK_OUTLINE = 2, //Displays outlines of the lassos only
    }

    public abstract class SelectionMeshData
    {
        /// <summary>
        /// What are the lasso points?
        /// </summary>
        public List<Vector2> Lasso = new List<Vector2>();

        /// <summary>
        /// What is the current lasso scaling?
        /// </summary>
        public Vector3 LassoScale = new Vector3(1, 1, 1);
               
        /// <summary>
        /// Should we update the graphical mesh?
        /// </summary>
        protected bool m_shouldUpdate = false;

        /// <summary>
        /// Ask to close the volume
        /// </summary>
        public virtual void CloseVolume()
        {}

        /// <summary>
        /// Add a new position to update this mesh
        /// </summary>
        /// <param name="pos">The new mesh position</param>
        /// <param name="rot">The new mesh rotation</param>
        public virtual void AddPosition(Vector3 pos, Quaternion rot)
        {}



        public bool ShouldUpdate
        {
            get => m_shouldUpdate;
            set => m_shouldUpdate = value;
        }
    }

    public class FullSelectionMeshData : SelectionMeshData
    {
        /// <summary>
        /// The position ID to when this selection mesh starts
        /// </summary>
        private int m_positionID = 0;

        /// <summary>
        /// The list of points ID (to form triangles) associated to this mesh
        /// </summary>
        public List<int> Triangles = new List<int>();

        /// <summary>
        /// The list of points associated to this mesh
        /// </summary>
        public List<Vector3> Points = new List<Vector3>();
        
        public override void CloseVolume()
        {
            lock (this)
            {
                if (Triangles.Count == 0)
                    return;

                m_shouldUpdate = true;

                //Triangulate our shape
                int[] tri = Triangulator.Triangulate(Lasso);

                //Close the "closest face"
                foreach (int i in tri)
                    Triangles.Add(i);

                //Close the furthest shape
                foreach (int i in tri)
                    Triangles.Add(i + Points.Count - Lasso.Count);
            }
        }

        /// <summary>
        /// Add a new triangle to the current tablet selection mesh. Does not change "ShouldUpdate" as this can be called multiple times for ONE computation
        /// </summary>
        /// <param name="tri">The triangle to add</param>
        private void AddTriangle(int[] tri)
        {
            Triangles.Add(tri[0]);
            Triangles.Add(tri[1]);
            Triangles.Add(tri[2]);
        }

        public override void AddPosition(Vector3 pos, Quaternion rot)
        {
            lock (this)
            {
                if (m_positionID > 0)
                {
                    m_shouldUpdate = true;
                    int[] tri = new int[3];

                    // add vertices from next posiion and connect them to the previous ones with triangles
                    Points.Add(pos + rot * new Vector3(Lasso[0].x * LassoScale.x, 0.0f, Lasso[0].y * LassoScale.z));

                    for (int i = 1; i < Lasso.Count; i++)
                    {
                        Points.Add(pos + rot * new Vector3(Lasso[i].x * LassoScale.x, 0.0f, Lasso[i].y * LassoScale.z));

                        // side 1 triangle 1
                        tri[0] = Lasso.Count * m_positionID + i - 1;
                        tri[2] = Lasso.Count * m_positionID + i;
                        tri[1] = Lasso.Count * (m_positionID - 1) + i - 1;
                        AddTriangle(tri);

                        // side 1 triangle 2
                        tri[0] = Lasso.Count * m_positionID + i;
                        tri[2] = Lasso.Count * (m_positionID - 1) + i;
                        tri[1] = Lasso.Count * (m_positionID - 1) + i - 1;
                        AddTriangle(tri);
                    }


                    // side 1 triangle 1
                    tri[0] = Lasso.Count * (m_positionID + 1) - 1;
                    tri[2] = Lasso.Count * m_positionID;
                    tri[1] = Lasso.Count * m_positionID - 1;
                    AddTriangle(tri);

                    // side 1 triangle 2
                    tri[0] = Lasso.Count * m_positionID;
                    tri[2] = Lasso.Count * (m_positionID - 1);
                    tri[1] = Lasso.Count * m_positionID - 1;
                    AddTriangle(tri);
                }

                else
                {
                    for (int i = 0; i < Lasso.Count; i++)
                        Points.Add(pos + rot * new Vector3(Lasso[i].x * LassoScale.x, 0.0f, Lasso[i].y * LassoScale.z));
                }

                m_positionID++;
            }
        }
    }

    public class OutlineSelectionMeshData : SelectionMeshData
    {
        /// <summary>
        /// All the lasso points to display
        /// </summary>
        public List<List<Vector3>> LassoPoints = new List<List<Vector3>>();

        /// <summary>
        /// All the connection tubes to display
        /// </summary>
        public List<List<Vector3>> ConnectionPoints = new List<List<Vector3>>();

        private bool m_generateConnections;

        public OutlineSelectionMeshData(bool generateConnections = false)
        {
            m_generateConnections = generateConnections;
        }

        public override void AddPosition(Vector3 pos, Quaternion rot)
        {
            lock (this)
            {
                m_shouldUpdate = true;

                //Generate the lasso
                List<Vector3> SubLasso = new List<Vector3>((Lasso.Count+4)/5 + 1);
                for(int i = 0; i < (Lasso.Count+4) / 5; i++)
                    SubLasso.Add(pos + rot * new Vector3(Lasso[5*i].x * LassoScale.x, 0.0f, Lasso[5*i].y * LassoScale.z));
                SubLasso.Add(pos + rot * new Vector3(Lasso[0].x * LassoScale.x, 0.0f, Lasso[0].y * LassoScale.z));

                LassoPoints.Add(SubLasso);

                //Generate the connections
                if (m_generateConnections)
                {
                    if (ConnectionPoints.Count == 0)
                    {
                        ConnectionPoints.Capacity = (Lasso.Count+4)/5;
                        for (int i = 0; i < SubLasso.Count; i++)
                            ConnectionPoints.Add(new List<Vector3>());
                    }

                    for (int i = 0; i < ConnectionPoints.Count; i++)
                        ConnectionPoints[i].Add(SubLasso[i]);
                }
            }
        }
    }

    /// <summary>
    /// Class regrouping data received when new selection mesh needs to be created
    /// </summary>
    public class NewSelectionMeshData
    {
        /// <summary>
        /// The boolean operation to use
        /// </summary>
        public BooleanSelectionOperation BooleanOP = 0;

        /// <summary>
        /// Is this mesh closed?
        /// </summary>
        public bool IsClosed = false;

        /// <summary>
        /// The mesh being updated
        /// </summary>
        public SelectionMeshData MeshData = null;

        public GameObject MeshGameObject = null;

        /// <summary>
        /// What visualization mode should be used here?
        /// </summary>
        public VolumetricSelectionVisualizationMode VisualizationMode = VolumetricSelectionVisualizationMode.FULL;
    }
}
