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
        NONE    = -1, //No interaction technique
        GOGO    = 0,  //Manual (\ie, the user has to move) interaction technique
        WIM     = 1,  //World in miniature interaction technique
        WIM_RAY = 2,  //The WIM enhanced with a ray pointing from the WIM and the distant dataset
        MANUAL  = 3,  //The GOGO technique interaction technique
    }

    public delegate void OnSelection(IPointingIT pointingIT);

    public interface IPointingIT
    {
        /// <summary>
        /// The target position in the dataset local space
        /// </summary>
        Vector3 TargetPosition { get; set; }

        /// <summary>
        /// Is the target position a valid value?
        /// </summary>
        bool TargetPositionIsValid { get; }

        /// <summary>
        /// What is the current sub dataset this pointing technique is interacting with?
        /// </summary>
        DefaultSubDatasetGameObject CurrentSubDataset { get; }
        
        /// <summary>
        /// The headset start position when the interaction technique was created
        /// </summary>
        Vector3 HeadsetStartPosition { get; set; }

        /// <summary>
        /// The Headset transform object. By default it is the MainCamera
        /// </summary>
        Transform HeadsetTransform { get; set; }

        /// <summary>
        /// The OnSelection event handler
        /// </summary>
        event OnSelection OnSelection;
    }
}