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

        /// <summary>
        /// Array of the parsed Dataset Properties
        /// </summary>
        public DatasetProperties[] DatasetPropertiesArray = new Properties.DatasetProperties[0];

        public static Properties ParseProperties()
        {
            //Read properties file
            String propsTxt = File.ReadAllText($"{Application.streamingAssetsPath}/properties.json");
            Properties root = JsonUtility.FromJson<Properties>(propsTxt);

            return root;
        }
    }
}
