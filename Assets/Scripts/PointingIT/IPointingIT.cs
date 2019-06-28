using Sereno.SciVis;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno.Pointing
{
    /// <summary>
    /// The pointing interaction technique in use
    /// </summary>
    public enum PointingIT
    {
        NONE   = -1, //No interaction technique
        GOGO   = 0,  //The GOGO technique interaction technique
        WIM    = 1,  //World in miniature interaction technique
        MANUAL = 2   //Manual (\ie, the user has to move) interaction technique
    }

    public delegate void OnSelection(IPointingIT pointingIT);

    public interface IPointingIT
    {
        /// <summary>
        /// The target position in the dataset local space
        /// </summary>
        Vector3 TargetPosition { get; }

        /// <summary>
        /// Is the target position a valid value?
        /// </summary>
        bool TargetPositionIsValid { get; }

        /// <summary>
        /// What is the current sub dataset this pointing technique is interacting with?
        /// </summary>
        DefaultSubDatasetGameObject CurrentSubDataset { get; }
        
        /// <summary>
        /// The OnSelection event handler
        /// </summary>
        event OnSelection OnSelection;
    }
}