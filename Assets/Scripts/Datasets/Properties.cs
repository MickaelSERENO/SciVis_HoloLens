using System;
using System.IO;
using UnityEngine;

namespace Sereno.Datasets
{
    /// <summary>
    /// Class parsing properties of this project
    /// </summary>
    [System.Serializable]
    public class Properties
    {

        [Serializable]
        public class DatasetMapProperties
        {
            /// <summary>
            /// The Texture path to look for in Resources/Textures folder (e.g., texture.png == Resources/Textures/texture.png)
            /// </summary>
            public String TexturePath = "";

            /// <summary>
            /// The tilling texture property
            /// </summary>
            public float[] Tiling = new float[] { 1.0f, 1.0f };

            /// <summary>
            /// The offset texture property
            /// </summary>
            public float[] Offset = new float[] { 0.0f, 0.0f };
        }

        /// <summary>
        /// Dataset properties structure
        /// </summary>
        [Serializable]
        public class DatasetProperties
        {
            /// <summary>
            /// The dataset name
            /// </summary>
            public String Name    = "";

            /// <summary>
            /// Should we rotate the dataset about -90° around the x Axis?
            /// </summary>
            public bool   RotateX = false;

            /// <summary>
            /// The map properties to display for a geographical dataset
            /// </summary>
            public DatasetMapProperties MapProperties = null;
        }

        /// <summary>
        /// Network properties structure
        /// </summary>
        [Serializable]
        public class NetworkProperties
        {
            public String IP   = "";
            public uint   Port = 0;
        }

        /// <summary>
        /// Array of the parsed Dataset Properties
        /// </summary>
        public DatasetProperties[] DatasetPropertiesArray = new Properties.DatasetProperties[0];

        /// <summary>
        /// The network properties
        /// </summary>
        public NetworkProperties Network = new NetworkProperties();

        public static Properties ParseProperties()
        {
            //Read properties file
            //Copy it first to the local user properties if it does not exist
#if !UNITY_EDITOR
            String jsonPath = $"{Application.persistentDataPath}/properties.json";
            if (!File.Exists(jsonPath))
                File.Copy($"{Application.streamingAssetsPath}/properties.json", jsonPath);
#else
            String jsonPath = $"{Application.streamingAssetsPath}/properties.json";
#endif
            String propsTxt = File.ReadAllText(jsonPath);
            Properties root = JsonUtility.FromJson<Properties>(propsTxt);

            return root;
        }
    }
}
