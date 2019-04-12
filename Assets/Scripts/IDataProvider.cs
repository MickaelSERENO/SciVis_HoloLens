using UnityEngine;

namespace Sereno
{
    public interface IDataProvider
    {
        /// <summary>
        /// Get the color representing the headset ID "headsetID". headsetID can be -1 (no headset)
        /// </summary>
        /// <param name="headsetID">The headset ID. can be -1 (i.e., no headset ID)</param>
        /// <returns>The color representing the headset</returns>
        Color GetHeadsetColor(int headsetID);
    }
}
