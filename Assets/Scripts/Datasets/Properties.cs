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
        /// <summary>
        /// Dataset properties structure
        /// </summary>
        [Serializable]
        public class DatasetProperties
        {
            public String Name    = "";
            public bool   RotateX = false;
        }

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
