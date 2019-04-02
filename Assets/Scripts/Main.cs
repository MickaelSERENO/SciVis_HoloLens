#define TEST

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
        NONE,
        EXPORT,
        IMPORT
    }

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
        /// The client color
        /// </summary>
        private Color32 m_clientColor;

        /// <summary>
        /// The root anchor game object
        /// </summary>
        private GameObject m_rootAnchorGO = null;

        /// <summary>
        /// Information received from the communication thread in order to update the IP text values being displayed
        /// </summary>
        private IPTextValue m_textValues = new IPTextValue();

        /// <summary>
        /// The anchor communication the server asked
        /// </summary>
        private AnchorCommunication m_anchorCommunication = AnchorCommunication.NONE;

        /// <summary>
        /// List of the other headset status
        /// </summary>
        private List<HeadsetStatus> m_headsetStatus = new List<HeadsetStatus>();

        /// <summary>
        /// List of GameObject representing the headset colors
        /// </summary>
        private List<GameObject> m_headsetColors = new List<GameObject>();

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

        /// <summary>
        /// Default text displayed for the IP address
        /// </summary>
        const String DEFAULT_IP_ADDRESS_TEXT = "Server not found";

        void Start()
        {
            //Default text helpful to bind headset to tablet
            m_textValues.UpdateTexts = true;
            m_textValues.EnableTexts = true;

            //Configure the main camera. Depth texture is used during raycasting
            MainCamera.depthTextureMode = DepthTextureMode.Depth;

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
            addVTKMsg.PtFieldValueIndices = new int[] { 1 };
            OnAddVTKDataset(null, addVTKMsg);
#endif
        }

        // Update is called once per frame
        void Update()
        {
            lock(this)
            {
                if(m_anchorCommunication == AnchorCommunication.EXPORT)
                {
                    if(m_rootAnchorGO != null)
                        Destroy(m_rootAnchorGO);

                    m_rootAnchorGO = new GameObject();
                    m_rootAnchorGO.AddComponent<UnityEngine.XR.WSA.WorldAnchor>();

                    WorldAnchorTransferBatch transferBatch = new WorldAnchorTransferBatch();
                    transferBatch.AddWorldAnchor("rootAnchor", m_rootAnchorGO.GetComponent<UnityEngine.XR.WSA.WorldAnchor>());
                    WorldAnchorTransferBatch.ExportAsync(transferBatch, OnExportDataAvailable, OnExportComplete);
                }
            }
            lock(m_textValues)
            {
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
                    gameObject.transform.parent = transform;
                    gameObject.Init(sm);
                }
            }

            //Load the headset status
            lock(m_headsetStatus)
            {
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
                                                                             m_headsetStatus[i].Position[1],
                                                                             m_headsetStatus[i].Position[2]);

                    m_headsetColors[i].GetComponent<MeshRenderer>().material.color = new Color(((m_headsetStatus[i].Color >> 24) & 0xff)/255.0f,
                                                                                               ((m_headsetStatus[i].Color >> 16) & 0xff)/255.0f,
                                                                                               ((m_headsetStatus[i].Color >> 8)  & 0xff)/255.0f);
                }
            }

            //Send camera status
            HeadsetUpdateData headsetData = new HeadsetUpdateData();
            headsetData.Position = new float[3]{MainCamera.transform.position[0],
                                                MainCamera.transform.position[1],
                                                MainCamera.transform.position[2]};

            headsetData.Rotation = new float[4]{MainCamera.transform.rotation[3],
                                                MainCamera.transform.rotation[0],
                                                MainCamera.transform.rotation[1],
                                                MainCamera.transform.rotation[2]};

            if(m_client.IsConnected())
                m_client.SendHeadsetUpdateData(headsetData);
        }

         
        public void OnDestroy()
        {
            m_client.Close();
        }

        /* Message Buffer callbacks */
#region
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
                    for(int i = 0; i < ptValues.Length; i++)
                    {
                        VTKUnitySmallMultiple sm = grid.CreatePointFieldSmallMultiple(i);
                        lock(m_vtkSMLoaded)
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
                lock(m_textValues)
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
            lock(m_headsetStatus)
            {
                m_headsetStatus.Clear();
                m_headsetStatus.AddRange(msg.HeadsetsStatus);
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
            lock(m_textValues)
            {
                m_textValues.EnableTexts = true;
                m_textValues.UpdateTexts = true;
                if(status == ConnectionStatus.CONNECTED)
                    m_textValues.IPStr = IPAddress.Parse(((IPEndPoint)s.LocalEndPoint).Address.ToString()).ToString();
                else
                    m_textValues.IPStr = "";
            }
        }

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
        /// <param name="completionReason"></param>
        private void OnExportComplete(SerializationCompletionReason completionReason)
        {
            m_client.SendAnchoringDataStatus(completionReason == SerializationCompletionReason.Succeeded);
        }
    }
}