using System.Net.Security;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sereno.SciVis;
using Sereno.Network;
using Sereno.Datasets;
using Sereno.Network.MessageHandler;
using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine.XR.WSA.Sharing;


namespace Sereno
{
    public class IPTextValue
    {
        /// <summary>
        /// The IP string value
        /// </summary>
        public String IPStr = "";

        /// <summary>
        /// Should we enable the IPText?
        /// </summary>
        public bool   EnableTexts = false;

        /// <summary>
        /// Should we update the text values?
        /// </summary>
        public bool   UpdateTexts = false;
    }

    /// <summary>
    /// Results of the anchor creation communication
    /// </summary>
    public enum AnchorCommunication
    {
        NONE,   //Nothing to do
        EXPORT, //Export the anchor
        IMPORT  //Import the anchor
    }

    public class Main : MonoBehaviour, IMessageBufferCallback
    {
        /// <summary>
        /// Default text displayed for the IP address
        /// </summary>
        const String DEFAULT_IP_ADDRESS_TEXT = "Server not found";

        /// <summary>
        /// Maximum number of retry for importing anchor data
        /// </summary>
        const int MAX_IMPORT_ANCHOR_DATA_RETRY = 3;

        /* Private attributes*/
        #region 
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
        /// All the dataset game objects
        /// </summary>
        private Queue<MonoBehaviour> m_datasetGameObjects = new Queue<MonoBehaviour>();

        /// <summary>
        /// The Server Client. 
        /// </summary>
        private VFVClient m_client;

        /// <summary>
        /// The client color
        /// </summary>
        private Color32 m_clientColor;

        /// <summary>
        /// The root anchor game object
        /// </summary>
        private GameObject m_rootAnchorGO = null;

        /// <summary>
        /// The import data queue to import in the main thread
        /// </summary>
        private Queue<byte[]> m_anchorImportSegments = new Queue<byte[]>();

        /// <summary>
        /// The import anchor size to import
        /// </summary>
        private UInt32 m_importAnchorSize = 0;

        /// <summary>
        /// Information received from the communication thread in order to update the IP text values being displayed
        /// </summary>
        private IPTextValue m_textValues = new IPTextValue();

        /// <summary>
        /// The anchor communication the server asked
        /// </summary>
        private AnchorCommunication m_anchorCommunication = AnchorCommunication.NONE;

        /// <summary>
        /// The current transfer batch
        /// </summary>
        private WorldAnchorTransferBatch m_transferBatch = null;

        /// <summary>
        /// List of the other headset status
        /// </summary>
        private List<HeadsetStatus> m_headsetStatus = new List<HeadsetStatus>();

        /// <summary>
        /// List of GameObject representing the headset colors
        /// </summary>
        private List<GameObject> m_headsetColors = new List<GameObject>();


        /// <summary>
        /// The final import anchor data once created. Need to be kept because of the number of retry.
        /// </summary>
        private byte[] m_importAnchorData      = null;

        /// <summary>
        /// Actual number of remaining retrying to import the anchor data
        /// </summary>
        private int    m_importAnchorDataRetry = MAX_IMPORT_ANCHOR_DATA_RETRY;
#endregion

        /* Public attributes*/
#region
        /// <summary>
        /// Prefab of VTKUnitySmallMultipleGameObject correctly configured
        /// </summary>
        public VTKUnitySmallMultipleGameObject VTKSMGameObject;

        /// <summary>
        /// The desired density for VTK structured grid datasets
        /// </summary>
        public UInt32 DesiredVTKDensity;

        /// <summary>
        /// The IP header text being displayed
        /// </summary>
        public UnityEngine.UI.Text IPHeaderText;

        /// <summary>
        /// The IP value text being displayed
        /// </summary>
        public UnityEngine.UI.Text IPValueText;

        /// <summary>
        /// The minicube displayed above the user coding their color
        /// </summary>
        public GameObject UserEntityColor;
#endregion

        void Start()
        {
            //Default text helpful to bind headset to tablet
            m_textValues.UpdateTexts = true;
            m_textValues.EnableTexts = true;

            //Configure the main camera. Depth texture is used during raycasting
            Camera.main.depthTextureMode = DepthTextureMode.Depth;

            //Start the network communication
            m_client = new VFVClient(this);
            m_client.AddListener(new ClientStatusCallback(OnConnectionStatus));
            m_client.Connect();
#if TEST
            AddVTKDatasetMessage addVTKMsg = new AddVTKDatasetMessage(ServerType.GET_ADD_VTK_DATASET);
            addVTKMsg.DataID = 0;
            addVTKMsg.NbCellFieldValueIndices = 0;
            addVTKMsg.NbPtFieldValueIndices   = 1;
            addVTKMsg.Path = "Agulhas_10_resampled.vtk";
            addVTKMsg.PtFieldValueIndices = new int[] { 0 };
            OnAddVTKDataset(null, addVTKMsg);
#endif
        }

        /// <summary>
        /// Handle the anchor data (export / import)
        /// </summary>
        private void HandleAnchor()
        {
            lock(this)
            {
                //Export the anchor coordinate system
                if(m_anchorCommunication == AnchorCommunication.EXPORT)
                {
#if !UNITY_EDITOR
                    RecreateRootAnchorGO();

                    m_transferBatch = new WorldAnchorTransferBatch();
                    m_transferBatch.AddWorldAnchor("rootAnchor", m_rootAnchorGO.GetComponent<UnityEngine.XR.WSA.WorldAnchor>());

                    WorldAnchorTransferBatch.ExportAsync(m_transferBatch, OnExportDataAvailable, OnExportComplete);
#endif
                    m_anchorCommunication = AnchorCommunication.NONE;
                }

                //Import the incoming data segment
                else if(m_anchorCommunication == AnchorCommunication.IMPORT)
                {
                    Debug.Log("Finish importing data");
#if !UNITY_EDITOR
                    if(m_importAnchorData == null)
                    {
                        m_importAnchorData = new byte[m_importAnchorSize];
                        long i = 0;
                        while(m_anchorImportSegments.Count > 0)
                        {
                            byte[] seg = m_anchorImportSegments.Dequeue();
                            seg.CopyTo(m_importAnchorData, i);
                            i+=seg.Length;
                        }
                    }

                    if(m_importAnchorDataRetry == 0)
                    {
                        m_importAnchorDataRetry = MAX_IMPORT_ANCHOR_DATA_RETRY;
                        m_importAnchorData = null;
                        m_anchorImportSegments.Clear();
                    }
                    else 
                    {
                        m_importAnchorDataRetry--;
                        WorldAnchorTransferBatch.ImportAsync(m_importAnchorData, OnImportComplete);
                    }
#else
                    m_anchorImportSegments.Clear();
#endif
                }
            }
        }

        /// <summary>
        /// Handles the server status texts
        /// </summary>
        private void HandleIPTxt()
        {
            //Update the displayed text requiring networking attention
            if(m_textValues.UpdateTexts)
            {
                //Enable/Disable the IP Text
                IPHeaderText.enabled = m_textValues.EnableTexts;
                IPValueText.enabled  = m_textValues.EnableTexts;

                //If we should enable the text, set the text value
                if(m_textValues.EnableTexts)
                {
                    if(m_textValues.IPStr.Length > 0)
                    {
                        IPHeaderText.text = "Headset IP adress:";
                        IPValueText.text  = m_textValues.IPStr;
                    }
                    else
                    {
                        IPHeaderText.text = DEFAULT_IP_ADDRESS_TEXT;
                        IPValueText.text  = "";
                    }
                }
                m_textValues.UpdateTexts = false;
            }
        }

        /// <summary>
        /// Handles the datasets loaded
        /// </summary>
        private void HandleDatasetsLoaded()
        {
            //Load what the server thread loaded regarding vtk datasets
            while(m_vtkDatasetsLoaded.Count > 0)
            {
                VTKDataset d = m_vtkDatasetsLoaded.Dequeue();
                for(int i = 0; i < d.SubDatasets.Count; i++)
                    d.GetSubDataset(i).UpdateGraphics();
                m_datasets.Add(d.ID, d);
            }
            
            while(m_vtkSMLoaded.Count > 0)
            {
                VTKUnitySmallMultiple sm = m_vtkSMLoaded.Dequeue();
                var gameObject = Instantiate(VTKSMGameObject);
                gameObject.transform.parent = transform;
                gameObject.Init(sm);

                m_datasetGameObjects.Enqueue(gameObject);
            }
        }

        /// <summary>
        /// Handle the headset status loaded
        /// </summary>
        private void HandleHeadsetStatusLoaded()
        {
            //Load the headset status
            //Create enough GameObjects
            while(m_headsetStatus.Count > m_headsetColors.Count)
            {
                GameObject go = Instantiate(UserEntityColor);
                go.transform.parent = this.transform;
                m_headsetColors.Add(go);
            }

            //hide the useless ones
            for(int i = m_headsetStatus.Count; i < m_headsetColors.Count; i++)
                m_headsetColors[i].SetActive(false);

            //Change the color and the position/rotation
            for(int i = 0; i < m_headsetStatus.Count; i++)
            {
                m_headsetColors[i].transform.localRotation = new Quaternion(m_headsetStatus[i].Rotation[1],
                                                                            m_headsetStatus[i].Rotation[2],
                                                                            m_headsetStatus[i].Rotation[3],
                                                                            m_headsetStatus[i].Rotation[0]);

                m_headsetColors[i].transform.localPosition = new Vector3(m_headsetStatus[i].Position[0],
                                                                            m_headsetStatus[i].Position[1]+0.15f,
                                                                            m_headsetStatus[i].Position[2]);

                m_headsetColors[i].GetComponent<MeshRenderer>().material.color = new Color(((m_headsetStatus[i].Color >> 24) & 0xff)/255.0f,
                                                                                            ((m_headsetStatus[i].Color >> 16) & 0xff)/255.0f,
                                                                                            ((m_headsetStatus[i].Color >> 8)  & 0xff)/255.0f);
            }
        }

        /// <summary>
        /// Send this headset status
        /// </summary>
        private void HandleHeadsetStatusSending()
        {
            //Send camera status
            if(m_rootAnchorGO != null && m_client.IsConnected() && m_client.HeadsetConnectionSent)
            {

                HeadsetUpdateData headsetData = new HeadsetUpdateData();

                headsetData.Position = new float[3]{Camera.main.transform.localPosition[0] - m_rootAnchorGO.transform.localPosition[0],
                                                    Camera.main.transform.localPosition[1] - m_rootAnchorGO.transform.localPosition[1],
                                                    Camera.main.transform.localPosition[2] - m_rootAnchorGO.transform.localPosition[2]};

                Quaternion rel = Quaternion.Inverse(Camera.main.transform.localRotation) * m_rootAnchorGO.transform.localRotation;
                headsetData.Rotation = new float[4]{rel[3], rel[0], rel[1], rel[2]};

                m_client.SendHeadsetUpdateData(headsetData);
            }
        }

        // Update is called once per frame
        void Update()
        {
            lock(this)
            {
                HandleAnchor();
                HandleIPTxt();
                HandleDatasetsLoaded();
                HandleHeadsetStatusLoaded();
                HandleHeadsetStatusSending();
            }
        }

         
        public void OnDestroy()
        {
            m_client.Close();
        }

        /* Message Buffer callbacks */
#region
        public void OnEmptyMessage(MessageBuffer messageBuffer, EmptyMessage msg)
        {
            if(msg.Type == ServerType.GET_ANCHOR_EOF)
                lock(this)
                    m_anchorCommunication = AnchorCommunication.IMPORT;
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
            lock(this)
                m_vtkDatasetsLoaded.Enqueue(dataset);

            //Create the associate visualization
            //if(parser.GetDatasetType() == VTKDatasetType.VTK_STRUCTURED_GRID)
            {
                unsafe
                {
                    VTKUnityStructuredGrid grid = new VTKUnityStructuredGrid(dataset, DesiredVTKDensity);
                    for(int i = 0; i < ptValues.Length; i++)
                    {
                        VTKUnitySmallMultiple sm = grid.CreatePointFieldSmallMultiple(i);
                        lock(this)
                            m_vtkSMLoaded.Enqueue(sm);
                    }
                }
            }
        }

        public void OnRotateDataset(MessageBuffer messageBuffer, RotateDatasetMessage msg)
        {
            Debug.Log("Received rotation event");
            lock(m_datasets[msg.DataID].SubDatasets[msg.SubDataID])
                m_datasets[msg.DataID].SubDatasets[msg.SubDataID].Rotation = msg.Quaternion;
        }

        public void OnMoveDataset(MessageBuffer messageBuffer, MoveDatasetMessage msg)
        {
            Debug.Log($"Received movement event : {msg.Position[0]}, {msg.Position[1]}, {msg.Position[2]}");
            lock(m_datasets[msg.DataID].SubDatasets[msg.SubDataID])
                m_datasets[msg.DataID].SubDatasets[msg.SubDataID].Position = msg.Position;
        }

        public void OnHeadsetInit(MessageBuffer messageBuffer, HeadsetInitMessage msg)
        {
            Debug.Log($"Received init headset message. Color : {msg.Color:X}, tablet connected: {msg.TabletConnected}, first connected: {msg.IsFirstConnected}");
            if(msg.TabletConnected)
            {
                lock(this)
                {
                    m_textValues.UpdateTexts = true;
                    m_textValues.EnableTexts = false;
                }
            }
            m_clientColor = new Color32((byte)((msg.Color >> 16) & 0xff),
                                        (byte)((msg.Color >> 8 ) & 0xff),
                                        (byte)(msg.Color & 0xff), 255);

            //Send anchor dataset to the server. 
            //The server will store that and send that information to other headsets if needed
            if(msg.IsFirstConnected)
            {
                lock(this)
                    m_anchorCommunication = AnchorCommunication.EXPORT;
            }
        }

        public void OnHeadsetsStatus(MessageBuffer messageBuffer, HeadsetsStatusMessage msg)
        {
            lock(this)
            {
                m_headsetStatus.Clear();
                m_headsetStatus.AddRange(msg.HeadsetsStatus);
            }
        }

        public void OnDefaultByteArray(MessageBuffer messageBuffer, DefaultByteArray msg)
        {
            if(msg.Type == ServerType.GET_ANCHOR_SEGMENT)
            {
                Debug.Log("importing anchor data...");
                lock(this)
                {
                    m_anchorImportSegments.Enqueue(msg.Data);
                    m_importAnchorSize += (UInt32)msg.Data.Length;
                }
            }
        }
#endregion

        /// <summary>
        /// Handles changement in the connection status
        /// </summary>
        /// <param name="s">The socket being used. It can be closed after this call</param>
        /// <param name="status">The new status to take account of</param>
        private void OnConnectionStatus(Socket s, ConnectionStatus status)
        {
            lock(this)
            {
                m_textValues.EnableTexts = true;
                String txt = "";
                if(status == ConnectionStatus.CONNECTED)
                    txt = IPAddress.Parse(((IPEndPoint)s.LocalEndPoint).Address.ToString()).ToString();
                
                //Clear everything
                else
                {
                    //Restore anchor data
                    if(m_transferBatch != null)
                    {
                        m_transferBatch.Dispose();
                        m_transferBatch = null;
                        m_anchorImportSegments.Clear();
                        m_importAnchorSize = 0;
                    }

                    //Headset status
                    m_headsetStatus.Clear();

                    //Datasets
                    m_vtkSMLoaded.Clear();
                    while(m_datasetGameObjects.Count > 0)
                    {
                        var gameObject = m_datasetGameObjects.Dequeue();
                        Destroy(gameObject);
                    }
                    m_vtkDatasetsLoaded.Clear();
                    m_datasets.Clear();
                    
                    txt = "";
                }
                if(txt != m_textValues.IPStr)
                {
                    m_textValues.IPStr = txt;
                    m_textValues.UpdateTexts = true;
                }
            }
        }

        private void RecreateRootAnchorGO()
        {
            //Initial the Anchor game object
            if(m_rootAnchorGO != null)
            {
                transform.parent = null;
                Destroy(m_rootAnchorGO);
            }

            //Create the GameObject and attach the transfer batch
            m_rootAnchorGO = new GameObject();
            m_rootAnchorGO.AddComponent<UnityEngine.XR.WSA.WorldAnchor>();

            transform.parent = m_rootAnchorGO.transform;
            transform.localPosition = new Vector3(0, 0, 0);
            transform.localRotation = Quaternion.identity;
            transform.localScale    = new Vector3(1, 1, 1);
        }

        /* Anchoring import/export callbacks */
#region 
        /// <summary>
        /// Method called each time anchor data is available (segment of data)
        /// </summary>
        /// <param name="data"></param>
        private void OnExportDataAvailable(byte[] data)
        {
            m_client.SendAnchoringDataSegment(data);
        }

        /// <summary>
        /// Method called once the export data are all correctly exported
        /// </summary>
        /// <param name="completionReason">the status of the export</param>
        private void OnExportComplete(SerializationCompletionReason completionReason)
        {
            m_client.SendAnchoringDataStatus(completionReason == SerializationCompletionReason.Succeeded);
            lock(this)
            {
                m_transferBatch.Dispose();
                m_transferBatch = null;
            }
        }

        /// <summary>
        /// Method called once the import data are all correctly imported
        /// </summary>
        /// <param name="completionReason">The status of the import</param>
        /// <param name="deserializedTransferBatch">The import deserialized data</param>
        private void OnImportComplete(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch)
        {
            //Handle fail status
            if(completionReason != SerializationCompletionReason.Succeeded)
            {
                Debug.LogWarning("Error, could not import anchoring data... " + completionReason);
                return;
            }

            Debug.Log("Anchor import succeed");

            RecreateRootAnchorGO();

            //Lock the object
            deserializedTransferBatch.LockObject("rootAnchor", m_rootAnchorGO);
            m_anchorCommunication = AnchorCommunication.NONE;
            m_importAnchorData      = null;
            m_importAnchorDataRetry = MAX_IMPORT_ANCHOR_DATA_RETRY;

            m_anchorImportSegments.Clear();
        }
    }
#endregion
}