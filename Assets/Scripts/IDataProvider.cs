using Sereno.SciVis;
using UnityEngine;

namespace Sereno
{
    /// <summary>
    /// The Metadata provider mostly helping the workspace awareness
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// Get the color representing the headset ID "headsetID". headsetID can be -1 (no headset)
        /// </summary>
        /// <param name="headsetID">The headset ID. can be -1 (i.e., no headset ID)</param>
        /// <returns>The color representing the headset</returns>
        Color GetHeadsetColor(int headsetID);

        /// <summary>
        /// Get the targeted game object by the pointing interaction technique
        /// </summary>
        /// <returns>The targeted GameObject or NULL if none</returns>
        DefaultSubDatasetGameObject GetTargetedGameObject();
    }
}
