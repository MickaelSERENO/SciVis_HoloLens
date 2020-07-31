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
    /// Class regrouping data received when new selection mesh needs to be created
    /// </summary>
    public class NewSelectionMeshData
    {
        /// <summary>
        /// The position ID to when this selection mesh starts
        /// </summary>
        public int PositionID = 0;

        /// <summary>
        /// The boolean operation to use
        /// </summary>
        public BooleanSelectionOperation BooleanOP = 0;

        /// <summary>
        /// Is this mesh closed?
        /// </summary>
        public bool IsClosed = false;

        /// <summary>
        /// The list of points ID (to form triangles) associated to this mesh
        /// </summary>
        public List<int> Triangles = new List<int>();

        /// <summary>
        /// The list of points associated to this mesh
        /// </summary>
        public List<Vector3> Points = new List<Vector3>();

        /// <summary>
        /// The associated graphical Mesh
        /// </summary>
        public Mesh Mesh = null;

        /// <summary>
        /// Should this graphical object be updated?
        /// </summary>
        public bool ShouldUpdate = false;
    }
}
