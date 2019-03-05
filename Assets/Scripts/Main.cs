using System.Net.Security;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sereno.SciVis;
using Sereno.Network;
using Sereno.Datasets;
using Sereno.Network.MessageHandler;
using System;

namespace Sereno
{
    public class Main : MonoBehaviour, IMessageBufferCallback
    {
        /// <summary>
        /// The Dataset currently parsed. The key represents the datase ID
        /// </summary>
        private Dictionary<Int32, Dataset> m_datasets = new Dictionary<Int32, Dataset>();

        /// <summary>
        /// Dataset loaded that needed to be visualized (in construction GameObject. Needed to be read in the main thread.
        /// </summary>
        private Queue<VTKDataset> m_vtkDatasetsLoaded = new Queue<VTKDataset>();

        /// <summary>
        /// The VTK Small multiple being loaded from the Server thread. Useful to construct GameObjects in the main thread
        /// </summary>

        private Queue<VTKUnitySmallMultiple> m_vtkSMLoaded = new Queue<VTKUnitySmallMultiple>();

        /// <summary>
        /// The Server Client. 
        /// </summary>
        private VFVClient m_client;

        /// <summary>
        /// Prefab of VTKUnitySmallMultipleGameObject correctly configured
        /// </summary>
        public VTKUnitySmallMultipleGameObject VTKSMGameObject;

        /// <summary>
        /// The Main Camera to configure
        /// </summary>
        public Camera MainCamera;

        /// <summary>
        /// The desired density for VTK structured grid datasets
        /// </summary>
        public UInt32 DesiredVTKDensity;

        void Start()
        {
            //Start the network communication
            m_client = new VFVClient(this);
            m_client.Connect();

            //Configure the main camera. Depth texture is used during raycasting
            MainCamera.depthTextureMode = DepthTextureMode.Depth;

            //This if for test
            /*VTKParser oceanParser   = new VTKParser($"{Application.streamingAssetsPath}/Agulhas_10_resampled.vtk");
            oceanParser.Parse();
            VTKDataset oceanDataset = new VTKDataset(0, oceanParser, new VTKFieldValue[1]{oceanParser.GetPointFieldValueDescriptors()[2]}, new VTKFieldValue[0]);

            unsafe
            {
                VTKUnityStructuredGrid grid = Object.Instantiate(VTKStructuredGrid);
                grid.Init(oceanDataset);
                grid.CreatePointFieldSmallMultiple(0);
                m_datasetGameObjects.Add(grid.gameObject);
            }*/
        }

        // Update is called once per frame
        void Update()
        {
            //Load what the server thread loaded
            lock(m_vtkDatasetsLoaded)
            {
                while(m_vtkDatasetsLoaded.Count > 0)
                {
                    VTKDataset d = m_vtkDatasetsLoaded.Dequeue();
                    for(int i = 0; i < d.SubDatasets.Count; i++)
                        d.GetSubDataset(i).UpdateGraphics();
                    m_datasets.Add(d.ID, d);
                }
            }

            lock(m_vtkSMLoaded)
            {
                while(m_vtkSMLoaded.Count > 0)
                {
                    VTKUnitySmallMultiple sm = m_vtkSMLoaded.Dequeue();
                    var gameObject = Instantiate(VTKSMGameObject);
                    gameObject.Init(sm);
                }
            }

        }

        public void OnEmptyMessage(MessageBuffer messageBuffer, EmptyMessage msg)
        {
        }

        public void OnAddVTKDataset(MessageBuffer messageBuffer, AddVTKDatasetMessage msg)
        {
            Debug.Log($"Opening VTK Dataset ID {msg.DataID}, path {msg.Path}");

            //Open the parser
            VTKParser  parser   = new VTKParser($"{Application.streamingAssetsPath}/{msg.Path}");
            parser.Parse();

            var ptDescriptors   = parser.GetPointFieldValueDescriptors();
            var cellDescriptors = parser.GetCellFieldValueDescriptors();

            //Reconstruct the array of FieldValue of interest
            VTKFieldValue[] ptValues   = new VTKFieldValue[msg.NbPtFieldValueIndices];
            VTKFieldValue[] cellValues = new VTKFieldValue[msg.NbCellFieldValueIndices];

            for(int i = 0; i < ptValues.Length; i++)
                ptValues[i] = ptDescriptors[msg.PtFieldValueIndices[i]];
            for(int i = 0; i < cellValues.Length; i++)
                cellValues[i] = cellDescriptors[msg.PtFieldValueIndices[i]];

            //Create the Dataset
            VTKDataset dataset = new VTKDataset(msg.DataID, parser, ptValues, cellValues);
            lock(m_vtkDatasetsLoaded)
                m_vtkDatasetsLoaded.Enqueue(dataset);

            //Create the associate visualization
            //if(parser.GetDatasetType() == VTKDatasetType.VTK_STRUCTURED_GRID)
            {
                unsafe
                {
                    VTKUnityStructuredGrid grid = new VTKUnityStructuredGrid(dataset, DesiredVTKDensity);
                    VTKUnitySmallMultiple sm    = grid.CreatePointFieldSmallMultiple(0);

                    lock(m_vtkSMLoaded)
                        m_vtkSMLoaded.Enqueue(sm);
                }
            }
        }

        public void OnRotateDataset(MessageBuffer messageBuffer, RotateDatasetMessage msg)
        {
            Debug.Log("Received rotation event");
            lock(m_datasets[msg.DataID].SubDatasets[msg.SubDataID])
                m_datasets[msg.DataID].SubDatasets[msg.SubDataID].Rotation = msg.Quaternion;
        }
    }
}