using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    public class Main : MonoBehaviour
    {
        private List<Dataset>    m_datasets           = new List<Dataset>();
        private List<GameObject> m_datasetGameObjects = new List<GameObject>();

        public VTKUnityStructuredGrid VTKStructuredGrid;

        void Start()
        {
            VTKParser oceanParser   = new VTKParser($"{Application.streamingAssetsPath}/Agulhas_10_resampled.vtk");
            oceanParser.Parse();
            VTKDataset oceanDataset = new VTKDataset(oceanParser, new VTKFieldValue[1]{oceanParser.GetPointFieldValueDescriptors()[0]}, new VTKFieldValue[0]);

            unsafe
            {
                VTKUnityStructuredGrid grid = Object.Instantiate(VTKStructuredGrid);
                grid.Init(oceanDataset);
                grid.CreatePointFieldSmallMultiple(0);
                m_datasetGameObjects.Add(grid.gameObject);
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}