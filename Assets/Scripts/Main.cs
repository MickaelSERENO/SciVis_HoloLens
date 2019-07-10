//#define TEST
#define CHI2020

#if ENABLE_WINMD_SUPPORT
using Windows.Perception.Spatial;
#endif

using Sereno.Datasets;
using Sereno.Network;
using Sereno.Network.MessageHandler;
using Sereno.Pointing;
using Sereno.SciVis;
using Sereno.Unity.HandDetector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
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
        public bool   EnableIPTexts = false;

        /// <summary>
        /// Should we update the text values?
        /// </summary>
        public bool   UpdateIPTexts = false;

        /// <summary>
        /// The Random string value
        /// </summary>
        public String RandomStr = "";

        /// <summary>
        /// Should we enable the Random Text?
        /// </summary>
        public bool EnableRandomText = false;

        /// <summary>
        /// Should we update the random text values?
        /// </summary>
        public bool UpdateRandomText = false;
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

    /// <summary>
    /// Class storing GameObjects per Headset
    /// </summary>
    public class HeadsetGameObjects
    {
        /// <summary>
        /// The Headset GameObject
        /// </summary>
        public GameObject Headset;

        /// <summary>
        /// The floating glyph game object
        /// </summary>
        public GameObject Glyph;

        /// <summary>
        /// The AR workspace awareness pointing game object
        /// </summary>
        public ARCollabPointingIT ARPointingGO;
    }

    public class Main : MonoBehaviour, IMessageBufferCallback, IDataProvider
    {
        /// <summary>
        /// Default text displayed for the IP address
        /// </summary>
        const String DEFAULT_IP_ADDRESS_TEXT = "Server not found";

#if CHI2020
        /// <summary>
        /// The duration time the next trial message should appear in milliseconds
        /// </summary>
        const Int64 NEXT_TRIAL_MESSAGE_DURATION_TIME = 3*1000;
#endif

        /// <summary>
        /// Maximum number of retry for importing anchor data
        /// </summary>
        const int MAX_IMPORT_ANCHOR_DATA_RETRY = 5;

        /// <summary>
        /// The headset size
        /// </summary>
        const float HEADSET_SIZE = 0.20f;

        /// <summary>
        /// Offset to apply for glyphs to be put on top of the head
        /// </summary>
        const float HEADSET_TOP = 0.15f;

        /* Private attributes*/
#region 
        /// <summary>
        /// The Dataset currently parsed. The key represents the datase ID
        /// </summary>
        private Dictionary<Int32, DatasetMetaData> m_datasets = new Dictionary<Int32, DatasetMetaData>();

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
        private List<DefaultSubDatasetGameObject> m_datasetGameObjects = new List<DefaultSubDatasetGameObject>();

        /// <summary>
        /// Dictionary linking subdataset to objects which we can change the internal status
        /// </summary>
        private Dictionary<SubDataset, IChangeInternalState> m_changeInternalStates = new Dictionary<SubDataset, IChangeInternalState>();

        /// <summary>
        /// The Server Client. 
        /// </summary>
        private VFVClient m_client;

        /// <summary>
        /// The client headset ID
        /// </summary>
        private int m_headsetID = -1;

        /// <summary>
        /// The tablet ID bound to this headset
        /// </summary>
        private int m_tabletID = -1;

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
        private List<HeadsetGameObjects> m_headsetGameObjects = new List<HeadsetGameObjects>();
        
        /// <summary>
        /// The final import anchor data once created. Need to be kept because of the number of retry.
        /// </summary>
        private byte[] m_importAnchorData      = null;

        /// <summary>
        /// Actual number of remaining retrying to import the anchor data
        /// </summary>
        private int    m_importAnchorDataRetry = MAX_IMPORT_ANCHOR_DATA_RETRY;

        /// <summary>
        /// The selected pointing interaction technique
        /// </summary>
        private PointingIT m_enumPointingIT = PointingIT.NONE;

        /// <summary>
        /// What is the dataset being annotated?
        /// </summary>
        private DefaultSubDatasetGameObject m_datasetInAnnotation = null;

        /// <summary>
        /// The HandDetector provider
        /// </summary>
        private HandDetectorProvider m_hdProvider = new HandDetectorProvider();
        
#if ENABLE_WINMD_SUPPORT
        /// <summary>
        /// The root spatial coordinate system created by Unity
        /// </summary>
        private SpatialCoordinateSystem m_spatialCoordinateSystem = null;
#endif

        /// <summary>
        /// The current pointing interaction technique in use.
        /// </summary>
        private IPointingIT m_currentPointingIT = null;

        /// <summary>
        /// The game object being target by the pointing interaction technique
        /// </summary>
        private DefaultSubDatasetGameObject m_targetedGameObject = null;

        /// <summary>
        /// Was the client connected once?
        /// </summary>
        private bool m_wasConnected = false;

        /// <summary>
        /// Is the connection lost?
        /// </summary>
        private bool m_connectionLost = false;

        /// <summary>
        /// The SubDataset in annotation
        /// </summary>
        private SubDatasetMetaData m_sdInAnnotation = null;

        /// <summary>
        /// The SubDataset waiting for an annotation selection
        /// </summary>
        private SubDatasetMetaData m_sdWaitingAnnotation = null;

        /// <summary>
        /// The pointing interaction technique in wait for annotation being loaded
        /// </summary>
        private PointingIT m_waitingPointingID = PointingIT.NONE;

        /// <summary>
        /// Should we update the pointing ID
        /// </summary>
        private bool m_updatePointingID = false;

#if CHI2020
        /// <summary>
        /// When should the random text be disabled? -1 == never
        /// </summary>
        private Int64 m_disableRandomTextTimestamp = -1;

        /// <summary>
        /// What is the current trial data ?
        /// </summary>
        private NextTrialMessage m_currentTrialMessage = null;

        /// <summary>
        /// Should we update the rendering pipeline due to new CHI2020 data?
        /// </summary>
        private bool m_updateCHI2020Data = false;

#endif
        #endregion

        /* Public attributes*/
        #region
        /// <summary>
        /// The Default SubDataset Game object. Only display the bounding box of the subdataset
        /// </summary>
        public DefaultSubDatasetGameObject DefaultSubDatasetGO;

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
        /// Any text that has to be displayed
        /// </summary>
        public UnityEngine.UI.Text RandomText;

        /// <summary>
        /// The minicube displayed above the user coding their color
        /// </summary>
        public GameObject UserEntityColor;

        /// <summary>
        /// The "Nothing" glyph displayed above characters
        /// </summary>
        public Mesh NothingGlyph;

        /// <summary>
        /// The "Translate" glyph displayed above characters
        /// </summary>
        public Mesh TranslateGlyph;

        /// <summary>
        /// The "Scale" glyph displayed above characters
        /// </summary>
        public Mesh ScaleGlyph;

        /// <summary>
        /// The "Rotate" glyph displayed above characters
        /// </summary>
        public Mesh RotateGlyph;

        /// <summary>
        /// The Go-Go GameObject
        /// </summary>
        public ARGoGo GoGoGameObject;

        /// <summary>
        /// The ARWIM Prefab
        /// </summary>
        public ARWIM ARWIMPrefab;

        /// <summary>
        /// The ARWIMRay Prefab object
        /// </summary>
        public ARWIMRay ARWIMRayPrefab;

        /// <summary>
        /// The ARManual Prefab
        /// </summary>
        public ARManual ARManualPrefab;

        /// <summary>
        /// a stupid cube prefab
        /// </summary>
        public GameObject CubePrefab;

        /// <summary>
        /// The AR Collaborator pointing IT prefab
        /// </summary>
        public ARCollabPointingIT ARCollabPointingITPrefab;

#if CHI2020
        /// <summary>
        /// The Target Annotation Game Object that the user sees
        /// </summary>
        public GameObject TargetAnnotationGO = null;
#endif
#endregion

        void Start()
        {
            //Default text helpful to bind headset to tablet
            m_textValues.UpdateIPTexts = true;
            m_textValues.EnableIPTexts = true;

            m_textValues.UpdateRandomText = true;
            m_textValues.EnableRandomText = false;

            //Configure the main camera. Depth texture is used during raycasting
            Camera.main.depthTextureMode = DepthTextureMode.Depth;

            //Start the network communication
            m_client = new VFVClient(this);
            m_client.AddListener(new ClientStatusCallback(OnConnectionStatus));
            m_client.Connect();

            //Start the hand detector
            m_hdProvider.Smoothness = 0.70f;
            m_hdProvider.InitializeHandDetector();

            //Initialize our selection techniques
            GoGoGameObject.Init(m_hdProvider);
            GoGoGameObject.transform.parent = null;
            GoGoGameObject.transform.position = new Vector3(0, 0, 0);
            GoGoGameObject.transform.rotation = Quaternion.identity;
            GoGoGameObject.gameObject.SetActive(false);

            CurrentPointingIT = PointingIT.NONE;

            //By default, false
            TargetAnnotationGO.SetActive(false);
#if TEST
            Task t = new Task( () =>
            {
                AddVTKDatasetMessage addVTKMsg = new AddVTKDatasetMessage(ServerType.GET_ADD_VTK_DATASET);
                addVTKMsg.DataID = 0;
                addVTKMsg.NbCellFieldValueIndices = 0;
                addVTKMsg.NbPtFieldValueIndices = 1;
                addVTKMsg.Path = "Agulhas_10_resampled.vtk";
                addVTKMsg.PtFieldValueIndices = new int[] { 0 };
                OnAddVTKDataset(null, addVTKMsg);

                MoveDatasetMessage moveVTKMsg = new MoveDatasetMessage(ServerType.GET_ON_MOVE_DATASET);
                moveVTKMsg.DataID = 0;
                moveVTKMsg.SubDataID = 0;
                moveVTKMsg.Position = new float[3] { -1, -1, -1 };
                moveVTKMsg.InPublic = 1;
                moveVTKMsg.HeadsetID = -1;
                OnMoveDataset(null, moveVTKMsg);

                /*ScaleDatasetMessage scaleMsg = new ScaleDatasetMessage(ServerType.GET_ON_SCALE_DATASET);
                scaleMsg.DataID = 0;
                scaleMsg.SubDataID = 0;
                scaleMsg.HeadsetID = -1;
                scaleMsg.Scale = new float[3] { 0.25f, 0.25f, 0.25f };
                scaleMsg.InPublic = 1;
                OnScaleDataset(null, scaleMsg);

                StartAnnotationMessage annotMsg = new StartAnnotationMessage(ServerType.GET_START_ANNOTATION);
                annotMsg.DatasetID = 0;
                annotMsg.SubDatasetID = 0;
                annotMsg.PointingID = PointingIT.WIM_RAY;
                annotMsg.InPublic = 1;
                OnStartAnnotation(null, annotMsg);*/

                //Headset Status
                HeadsetsStatusMessage headsetStatusMsg = new HeadsetsStatusMessage(ServerType.GET_HEADSETS_STATUS);
                headsetStatusMsg.HeadsetsStatus = new HeadsetStatus[1];
                HeadsetStatus headsetStatus = new HeadsetStatus();
                headsetStatus.Color = (int)0x00ff0000;
                headsetStatus.ID = 2;
                headsetStatus.Position = new float[3] { 1, 1, 1 };
                headsetStatus.Rotation = new float[4] { 1, 0, 0, 0 };
                headsetStatus.PointingIT = PointingIT.GOGO;
                headsetStatus.PointingDatasetID    = 0;
                headsetStatus.PointingSubDatasetID = 0;
                headsetStatus.PointingInPublic = true;
                headsetStatus.PointingLocalSDPosition = new float[3] { 0, 0, 0 };
                headsetStatus.PointingHeadsetStartPosition = new float[3] { 1, 1, 1 };
                headsetStatusMsg.HeadsetsStatus[0] = headsetStatus;
                OnHeadsetsStatus(null, headsetStatusMsg);

                /*AnchorAnnotationMessage anchorAnnot = new AnchorAnnotationMessage(ServerType.GET_ANCHOR_ANNOTATION);
                anchorAnnot.AnnotationID = 0;
                anchorAnnot.DatasetID = 0;
                anchorAnnot.SubDatasetID = 0;
                anchorAnnot.HeadsetID = -1;
                anchorAnnot.LocalPosition = new float[3] { 0.5f, 0.5f, 0.5f };
                anchorAnnot.InPublic = 1;
                OnAnchorAnnotation(null, anchorAnnot);*/
            }
            );
            t.Start();
#endif
        }

        /// <summary>
        /// Get the SubDataset GameObject affiliated to a SubDataset
        /// </summary>
        /// <param name="sd">The SubDataset affiliated to a GameObject</param>
        /// <returns>null if no game object is found, the DefaultSubDatasetGameObject otherwise</returns>
        private DefaultSubDatasetGameObject GetSDGameObject(SubDataset sd)
        {
            foreach (DefaultSubDatasetGameObject go in m_datasetGameObjects)
            {
                if (go.SubDatasetState == sd)
                {
                    return go;
                }
            }
            return null;
        }

        /// <summary>
        /// Handle the anchor data (export / import)
        /// </summary>
        private void HandleAnchor()
        {
#if !UNITY_EDITOR
            //Export the anchor coordinate system
            if (m_anchorCommunication == AnchorCommunication.EXPORT)
            {
                RecreateRootAnchorGO();

                m_transferBatch = new WorldAnchorTransferBatch();
                m_transferBatch.AddWorldAnchor("rootAnchor", m_rootAnchorGO.GetComponent<UnityEngine.XR.WSA.WorldAnchor>());

                WorldAnchorTransferBatch.ExportAsync(m_transferBatch, OnExportDataAvailable, OnExportComplete);

                m_anchorCommunication = AnchorCommunication.NONE;
            }

            //Import the incoming data segment
            else if(m_anchorCommunication == AnchorCommunication.IMPORT)
            {
                Debug.Log("Finish importing data");
                m_anchorCommunication   = AnchorCommunication.NONE;

                m_importAnchorData = new byte[m_importAnchorSize];
                long i = 0;
                while(m_anchorImportSegments.Count > 0)
                {
                    byte[] seg = m_anchorImportSegments.Dequeue();
                    seg.CopyTo(m_importAnchorData, i);
                    i+=seg.Length;
                }
                
                WorldAnchorTransferBatch.ImportAsync(m_importAnchorData, OnImportComplete);
                m_anchorImportSegments.Clear();
            }
#endif
        }

        /// <summary>
        /// Handles the server status texts
        /// </summary>
        private void HandleIPTxt()
        {
            //Update the displayed text requiring networking attention
            if(m_textValues.UpdateIPTexts)
            {
                //Enable/Disable the IP Text
                IPHeaderText.enabled = m_textValues.EnableIPTexts;
                IPValueText.enabled  = m_textValues.EnableIPTexts;

                //If we should enable the text, set the text value
                if(m_textValues.EnableIPTexts)
                {
                    if(m_textValues.IPStr.Length > 0)
                    {
                        IPHeaderText.text = "Headset IP address:";
                        IPValueText.text  = m_textValues.IPStr;
                    }
                    else
                    {
                        IPHeaderText.text = DEFAULT_IP_ADDRESS_TEXT;
                        IPValueText.text  = "";
                    }
                }
                m_textValues.UpdateIPTexts = false;
            }
        }

        /// <summary>
        /// Handle the random text to display
        /// </summary>
        private void HandleRandomText()
        {
            //Disable the random text if needed
            if(m_disableRandomTextTimestamp < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() && m_disableRandomTextTimestamp > 0)
            {
                m_textValues.EnableRandomText = false;
                m_textValues.UpdateRandomText = true;
                m_disableRandomTextTimestamp = -1;
            }
            
            //Update the displayed text requiring networking attention
            if (m_textValues.UpdateRandomText)
            {
                //Enable/Disable the IP Text
                RandomText.enabled = m_textValues.EnableRandomText;

                //If we should enable the text, set the text value
                if (m_textValues.EnableRandomText)
                {
                    RandomText.text = m_textValues.RandomStr;
                }
                m_textValues.UpdateRandomText = false;
            }
        }

        /// <summary>
        /// Handles the datasets loaded
        /// </summary>
        private void HandleDatasetsLoaded()
        {
            if(m_connectionLost)
            {
                foreach (var go in m_datasetGameObjects)
                    Destroy(go.gameObject);
                m_datasetGameObjects.Clear();
                CurrentPointingIT = PointingIT.NONE;
            }

            //Load what the server thread loaded regarding vtk datasets
#if CHI2020
            while (m_vtkDatasetsLoaded.Count > 0)
            {
                VTKDataset d = m_vtkDatasetsLoaded.Dequeue();
                foreach (SubDataset sd in d.SubDatasets)
                {
                    DefaultSubDatasetGameObject gameObject = Instantiate(DefaultSubDatasetGO);
                    gameObject.transform.parent = transform;
                    gameObject.Init(sd, this);
                    m_changeInternalStates.Add(sd, gameObject);
                    m_datasetGameObjects.Add(gameObject);
                }
            }
#else
            while(m_vtkSMLoaded.Count > 0)
            {
                VTKUnitySmallMultiple sm = m_vtkSMLoaded.Dequeue();
                TriangularGTF gtf = new TriangularGTF(new float[] { 0.5f, 0.5f }, new float[] { 0.5f, 0.5f }, 1.0f);
                sm.VTKSubDataset.TransferFunction = gtf;
                VTKUnitySmallMultipleGameObject gameObject = Instantiate(VTKSMGameObject);
                gameObject.transform.parent = transform;
                gameObject.Init(sm, this);
                m_changeInternalStates.Add(sm.VTKSubDataset, gameObject);

                m_datasetGameObjects.Add(gameObject);
            }
#endif
        }

        private void HandlePointingID()
        {
            if (m_updatePointingID)
            {
                if (m_sdWaitingAnnotation != null)
                {
                    DefaultSubDatasetGameObject sdGo = GetSDGameObject(m_sdWaitingAnnotation.CurrentSubDataset);

                    if (sdGo != null)
                    {
                        m_datasetInAnnotation = sdGo;
                        CurrentPointingIT     = m_waitingPointingID;
                        m_sdInAnnotation      = m_sdWaitingAnnotation;

                        m_sdWaitingAnnotation = null;
                        m_waitingPointingID   = PointingIT.NONE;
                    }
                }

                else
                {
                    m_datasetInAnnotation = null;
                    CurrentPointingIT     = m_waitingPointingID;
                    m_sdInAnnotation      = null;
                    m_waitingPointingID   = PointingIT.NONE;
                }

                m_updatePointingID = false;
            }
        }

        /// <summary>
        /// Handle the headset status loaded
        /// </summary>
        private void HandleHeadsetStatusLoaded()
        {
            //Load the headset status
            //Create enough GameObjects
            while(m_headsetStatus.Count > m_headsetGameObjects.Count)
            {
                GameObject headset = new GameObject();
                headset.transform.SetParent(this.transform, false);

                GameObject glyph = Instantiate(UserEntityColor);
                glyph.transform.SetParent(headset.transform, false);

                ARCollabPointingIT arCollabGO = Instantiate(ARCollabPointingITPrefab);
                arCollabGO.transform.SetParent(null, false);

                HeadsetGameObjects go = new HeadsetGameObjects() { Headset = headset, Glyph = glyph, ARPointingGO = arCollabGO };
                
                //Affiliate correctly the pointing tehcnique and the glyph
                go.ARPointingGO.HeadsetTransform = go.Headset.transform;

                m_headsetGameObjects.Add(go);
            }

            //hide the useless ones
            for (int i = m_headsetStatus.Count; i < m_headsetGameObjects.Count; i++)
            {
                m_headsetGameObjects[i].Glyph.SetActive(false);
                m_headsetGameObjects[i].ARPointingGO.PointingIT = PointingIT.NONE;
            }

            //Change the color, shape and the position/rotation of each character's glyph
            for(int i = 0; i < m_headsetStatus.Count; i++)
            {
                //Update the glyph position / rotation
                m_headsetGameObjects[i].Headset.transform.localRotation = new Quaternion(m_headsetStatus[i].Rotation[1],
                                                                                       m_headsetStatus[i].Rotation[2],
                                                                                       m_headsetStatus[i].Rotation[3],
                                                                                       m_headsetStatus[i].Rotation[0]);

                m_headsetGameObjects[i].Headset.transform.localPosition = new Vector3(m_headsetStatus[i].Position[0],
                                                                                      m_headsetStatus[i].Position[1],
                                                                                      m_headsetStatus[i].Position[2]);

                m_headsetGameObjects[i].Glyph.transform.localPosition  = m_headsetGameObjects[i].Headset.transform.forward*(-HEADSET_SIZE/2.0f) + //Middle of the head
                                                                         m_headsetGameObjects[i].Headset.transform.up*(HEADSET_TOP);  //Top of the head

                //Update the glyph color
                m_headsetGameObjects[i].Glyph.GetComponent<MeshRenderer>().material.color = new Color(((byte)(m_headsetStatus[i].Color >> 16) & 0xff)/255.0f,
                                                                                                      ((byte)(m_headsetStatus[i].Color >> 8)  & 0xff)/255.0f,
                                                                                                      ((byte)(m_headsetStatus[i].Color >> 0)  & 0xff)/255.0f);

                m_headsetGameObjects[i].Glyph.SetActive(true);

                //Update the glyph shape
                MeshFilter mf = m_headsetGameObjects[i].Glyph.GetComponent<MeshFilter>();
                switch(m_headsetStatus[i].CurrentAction)
                {
                    case HeadsetCurrentAction.MOVING:
                        mf.mesh = TranslateGlyph;
                        break;
                    case HeadsetCurrentAction.ROTATING:
                        mf.mesh = RotateGlyph;
                        break;
                    case HeadsetCurrentAction.SCALING:
                        mf.mesh = ScaleGlyph;
                        break;
                    default:
                        mf.mesh = NothingGlyph;
                        break;
                }

                if (m_headsetStatus[i].ID != m_headsetID)
                {
                    //Update the pointing interaction technique
                    m_headsetGameObjects[i].ARPointingGO.PointingIT = m_headsetStatus[i].PointingIT;
                    m_headsetGameObjects[i].ARPointingGO.SubDatasetGameObject = (m_headsetStatus[i].PointingInPublic ? GetSDGameObject(GetSubDataset(m_headsetStatus[i].PointingDatasetID, m_headsetStatus[i].PointingSubDatasetID, true)) : null);
                    m_headsetGameObjects[i].ARPointingGO.TargetPosition = new Vector3(m_headsetStatus[i].PointingLocalSDPosition[0], m_headsetStatus[i].PointingLocalSDPosition[1], m_headsetStatus[i].PointingLocalSDPosition[2]);
                    m_headsetGameObjects[i].ARPointingGO.HeadsetStartPosition = new Vector3(m_headsetStatus[i].PointingHeadsetStartPosition[0], m_headsetStatus[i].PointingHeadsetStartPosition[1], m_headsetStatus[i].PointingHeadsetStartPosition[2]);
                }
            }
        }

        /// <summary>
        /// Get the relative position from the anchor
        /// </summary>
        /// <param name="position">The position to evaluate</param>
        /// <returns>the x, y and z component of the position regarding the root anchor game object</returns>
        private float[] GetRelativePositionToAnchor(Vector3 position)
        {
            //The relative position (taking acount the headset's orientation, i.e., new coordinate system)
            Vector3 relPos = position - m_rootAnchorGO.transform.position;

            return new float[3] {Vector3.Dot(relPos, m_rootAnchorGO.transform.right),
                                 Vector3.Dot(relPos, m_rootAnchorGO.transform.up),
                                 Vector3.Dot(relPos, m_rootAnchorGO.transform.forward)};
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

                headsetData.Position = GetRelativePositionToAnchor(Camera.main.transform.position);
                
                //The relative orientation
                Quaternion rel = Quaternion.Inverse(m_rootAnchorGO.transform.localRotation) * Camera.main.transform.localRotation;
                headsetData.Rotation = new float[4]{rel[3], rel[0], rel[1], rel[2]};

                //The current pointing data
                headsetData.PointingIT = CurrentPointingIT;
                if(CurrentPointingIT != PointingIT.NONE && m_sdInAnnotation != null && m_currentPointingIT != null)
                {
                    SubDataset curSD = m_sdInAnnotation.SubDatasetPublicState;
                    headsetData.PointingDatasetID    = curSD.Parent.ID;
                    headsetData.PointingSubDatasetID = curSD.Parent.SubDatasets.FindIndex(s => s == curSD);
                    headsetData.PointingInPublic     = (curSD == m_sdInAnnotation.SubDatasetPublicState);

                    for (int i = 0; i < 3; i++)
                        headsetData.PointingLocalSDPosition[i] = m_currentPointingIT.TargetPosition[i];

                    headsetData.PointingHeadsetStartPosition = GetRelativePositionToAnchor(m_currentPointingIT.HeadsetStartPosition);
                }

                m_client.SendHeadsetUpdateData(headsetData);
            }
        }

        private void HandleSpatialCoordinateSystem()
        {
#if ENABLE_WINMD_SUPPORT
            if (m_spatialCoordinateSystem == null)
            {
                //Get the Spatial Coordinate System pointer
                IntPtr spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
                m_spatialCoordinateSystem = Marshal.GetObjectForIUnknown(spatialCoordinateSystemPtr) as SpatialCoordinateSystem;
                m_hdProvider.SetSpatialCoordinateSystem(m_spatialCoordinateSystem);
            }
#endif
        }

#if CHI2020
        private void HandleCHI2020()
        {
            if(m_updateCHI2020Data && m_currentTrialMessage != null)
            {
                DefaultSubDatasetGameObject sdGameObject = GetSDGameObject(GetSubDataset(0, 0, true));
                if (!sdGameObject)
                    return;

                if (m_currentTrialMessage.StudyID == 1 && m_currentTrialMessage.TabletID != m_tabletID ||
                    m_currentTrialMessage.StudyID == 2 && m_currentTrialMessage.TabletID == m_tabletID)
                {
                    TargetAnnotationGO.transform.SetParent(sdGameObject.transform, false);
                    TargetAnnotationGO.transform.localPosition = new Vector3(m_currentTrialMessage.TargetPosition[0], m_currentTrialMessage.TargetPosition[1], m_currentTrialMessage.TargetPosition[2]);
                    TargetAnnotationGO.SetActive(true);
                }

                else
                    TargetAnnotationGO.SetActive(false);
            }
            else
                TargetAnnotationGO.SetActive(false);
        }
#endif

        // Update is called once per frame
        void Update()
        {
            HandleSpatialCoordinateSystem();
            lock(this)
            {
                HandleDatasetsLoaded();
                HandlePointingID();
                HandleAnchor();
                HandleIPTxt();
                HandleRandomText();
                HandleHeadsetStatusLoaded();
                HandleHeadsetStatusSending();
#if CHI2020
                HandleCHI2020();
#endif
            }
        }

        void LateUpdate()
        {
            lock(this)
            {
                m_targetedGameObject = null;

                //Update intersection between each datasets rendered and the selected pointing interaction technique
                if (m_currentPointingIT != null && m_currentPointingIT.TargetPositionIsValid)
                {
                    Vector3 targetPos = m_currentPointingIT.TargetPosition;
                    if (targetPos.x >= -0.5f && targetPos.x <= 0.5f &&
                       targetPos.y >= -0.5f && targetPos.y <= 0.5f &&
                       targetPos.z >= -0.5f && targetPos.z <= 0.5f)
                        m_targetedGameObject = m_currentPointingIT.CurrentSubDataset;
                }
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
            lock (this)
            {
                m_vtkDatasetsLoaded.Enqueue(dataset);
                m_datasets.Add(dataset.ID, new DatasetMetaData(dataset));
            }
#if !CHI2020
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
#endif
        }

        /// <summary>
        /// Get the SubDataset targeted by the dataset ID, subdataset ID and public/private status
        /// </summary>
        /// <param name="datasetID"></param>
        /// <param name="subDatasetID"></param>
        /// <param name="inPublic"></param>
        /// <returns>The SubDataset found with the correct IDs, null otherwise</returns>
        private SubDataset GetSubDataset(int datasetID, int subDatasetID, bool inPublic)
        {
            if (datasetID < 0 || subDatasetID < 0)
                return null;

            if (m_datasets.Count <= datasetID)
                return null;

            if(m_datasets[datasetID].SubDatasets.Count <= subDatasetID)
                return null;

            return inPublic ? m_datasets[datasetID].SubDatasets[subDatasetID].SubDatasetPublicState : m_datasets[datasetID].SubDatasets[subDatasetID].SubDatasetPrivateState;
        }

        public void OnRotateDataset(MessageBuffer messageBuffer, RotateDatasetMessage msg)
        {
            Debug.Log("Received rotation event");
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID, msg.InPublic != 0);
            lock (sd)
                sd.Rotation = msg.Quaternion;
        }

        public void OnMoveDataset(MessageBuffer messageBuffer, MoveDatasetMessage msg)
        {
            Debug.Log($"Received movement event : {msg.Position[0]}, {msg.Position[1]}, {msg.Position[2]}");
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID, msg.InPublic != 0);
            if (sd != null)
            {
                lock (sd)
                    sd.Position = msg.Position;
            }
        }

        public void OnScaleDataset(MessageBuffer messageBuffer, ScaleDatasetMessage msg)
        {
            Debug.Log($"Received Scale event : {msg.Scale[0]}, {msg.Scale[1]}, {msg.Scale[2]}");
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID, msg.InPublic != 0);
            if (sd != null)
            {
                lock (sd)
                    sd.Scale = msg.Scale;
            }
        }
        
        public void OnSetVisibilityDataset(MessageBuffer messageBuffer, VisibilityMessage msg)
        {
#if !CHI2020
            lock(this)
            {
                SubDatasetMetaData metaData = m_datasets[msg.DataID].SubDatasets[msg.SubDataID];
                SetVisibilityDataset(metaData, msg.Visibility);
            }
#endif
        }

        public void SetVisibilityDataset(SubDatasetMetaData metaData, int visibility)
        {
            if (metaData != null)
            {
                metaData.Visibility = visibility;
                m_changeInternalStates[metaData.SubDatasetPublicState].SetSubDatasetState(metaData.CurrentSubDataset);
            }
        }

        public void OnHeadsetInit(MessageBuffer messageBuffer, HeadsetInitMessage msg)
        {
            Debug.Log($"Received init headset message. Color : {msg.Color:X}, tablet connected: {msg.TabletConnected}, first connected: {msg.IsFirstConnected}");

            //Remove the connection message
            if(msg.TabletConnected)
            {
                lock(this)
                {
                    m_textValues.UpdateIPTexts = true;
                    m_textValues.EnableIPTexts = false;
                }
            }
            //Redisplay the connection message
            else
            {
                lock(this)
                {
                    IPAddress addr = m_client.GetIPAddress();
                    String s = DEFAULT_IP_ADDRESS_TEXT;
                    if(addr != null)
                        s = IPAddress.Parse(addr.ToString()).ToString();
                    m_textValues.IPStr = s;
                    m_textValues.UpdateIPTexts = true;
                    m_textValues.EnableIPTexts = true;
                }
            }

            m_clientColor = new Color32((byte)((msg.Color >> 16) & 0xff),
                                        (byte)((msg.Color >> 8 ) & 0xff),
                                        (byte)(msg.Color & 0xff), 255);

            m_headsetID = msg.ID;

            m_tabletID = msg.TabletID;

            //Send anchor dataset to the server. 
            //The server will store that and send that information to other headsets if needed
            if(msg.IsFirstConnected)
            {
                lock(this)
                    m_anchorCommunication = AnchorCommunication.EXPORT;
            }

            m_updateCHI2020Data = true;
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
#if !UNITY_EDITOR
            if(msg.Type == ServerType.GET_ANCHOR_SEGMENT)
            {
                Debug.Log("importing anchor data...");
                lock(this)
                {
                    m_anchorImportSegments.Enqueue(msg.Data);
                    m_importAnchorSize += (UInt32)msg.Data.Length;
                }
            }
#endif
        }

        public void OnSubDatasetOwner(MessageBuffer messageBuffer, SubDatasetOwnerMessage msg)
        {
            lock(this)
            {
                m_datasets[msg.DatasetID].Dataset.SubDatasets[msg.SubDatasetID].OwnerID = msg.HeadsetID;
            }
        }

        public void OnStartAnnotation(MessageBuffer messageBuffer, StartAnnotationMessage msg)
        {
            lock(this)
            {
                //Search got the current DefaultSubDatasetGameObject being in annotation
                SubDatasetMetaData sd = m_datasets[msg.DatasetID].SubDatasets[msg.SubDatasetID];

                //Check visibility
                int msgVisibility = (msg.InPublic == 1 ? SubDatasetMetaData.VISIBILITY_PUBLIC : SubDatasetMetaData.VISIBILITY_PRIVATE);
                if(sd.Visibility != msgVisibility)
                    SetVisibilityDataset(sd, msgVisibility);

                //If currently in an annotation this will cancel the previous one
                m_waitingPointingID = msg.PointingID;
                m_sdWaitingAnnotation = sd;
                m_updatePointingID = true;
            }
        }

        public void OnAnchorAnnotation(MessageBuffer messageBuffer, AnchorAnnotationMessage msg)
        {
            lock(this)
            {
                SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID, msg.InPublic != 0);
                if(sd != null)
                    sd.AddAnnotation(new Annotation(msg.LocalPosition));
            }
        }
        
        public void OnClearAnnotations(MessageBuffer messageBuffer, ClearAnnotationsMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID, msg.InPublic != 0);

            if(sd != null)
                sd.ClearAnnotations();
        }

        public void OnNextTrial(MessageBuffer messageBuffer, NextTrialMessage msg)
        {
            lock(this)
            {
                if (msg.StudyID == 1 || msg.StudyID == 2)
                {
                    if (msg.TabletID == m_tabletID)
                    {
                        String s = "Selection Technique: ";
                        switch (msg.PointingIT)
                        {
                            case PointingIT.GOGO:
                                s += "GOGO";
                                break;
                            case PointingIT.MANUAL:
                                s += "Manual";
                                break;
                            case PointingIT.WIM:
                                s += "WIM";
                                break;
                            case PointingIT.WIM_RAY:
                                s += "WIM-RAY";
                                break;
                            default:
                                s += "NONE";
                                break;
                        }
                        m_textValues.RandomStr = s;
                        m_textValues.UpdateRandomText = true;
                        m_textValues.EnableRandomText = true;

                        m_disableRandomTextTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + NEXT_TRIAL_MESSAGE_DURATION_TIME;
                    }
                }

                m_currentTrialMessage = msg;
                m_updateCHI2020Data = true;
            }
        }

        #endregion

        public Color GetHeadsetColor(int headsetID)
        {
            lock(this)
            {
                if(headsetID == -1)
                    return new Color(0.0f, 0.0f, 1.0f);
                else
                {
                    foreach(HeadsetStatus status in m_headsetStatus)
                        if(status.ID == headsetID)
                            return new Color(((byte)(status.Color >> 16) & 0xff)/255.0f,
                                             ((byte)(status.Color >> 8)  & 0xff)/255.0f,
                                             ((byte)(status.Color >> 0)  & 0xff)/255.0f);

                    return new Color(0.0f, 0.0f, 1.0f);
                }
            }
        }

        public DefaultSubDatasetGameObject GetTargetedGameObject()
        {
            lock(this)
                return m_targetedGameObject;
        }

        /// <summary>
        /// The current pointing interaction technique in use
        /// </summary>
        public PointingIT CurrentPointingIT
        {
            get
            {
                return m_enumPointingIT;
            }

            set
            {
                //Unregister
                if(m_currentPointingIT != null)
                    m_currentPointingIT.OnSelection -= OnPointingSelection;

                //Destroy prefabs
                switch(m_enumPointingIT)
                {
                    case PointingIT.WIM:
                        Destroy(((ARWIM)m_currentPointingIT).gameObject);
                        break;
                    case PointingIT.WIM_RAY:
                        Destroy(((ARWIMRay)m_currentPointingIT).gameObject);
                        break;
                    case PointingIT.GOGO:
                        GoGoGameObject.gameObject.SetActive(false);
                        break;
                    case PointingIT.MANUAL:
                        Destroy(((ARManual)m_currentPointingIT).gameObject);
                        break;
                }

                m_currentPointingIT = null;

                m_enumPointingIT = value;

                //Set the m_currentPointingIT variable (and treat the new one)
                switch (m_enumPointingIT)
                {
                    case PointingIT.NONE:
                        break;

                    case PointingIT.GOGO: //Reuse the GoGoGameObject
                        m_currentPointingIT = GoGoGameObject;
                        GoGoGameObject.CurrentSubDataset = m_datasetInAnnotation;
                        GoGoGameObject.gameObject.SetActive(true);
                        break;

                    case PointingIT.WIM: //Create a new WIM with the correct copy
                    {
                        if(m_datasetGameObjects.Count > 0)
                        {
                            ARWIM go = Instantiate(ARWIMPrefab);
                            go.Init(m_hdProvider, m_datasetInAnnotation, new Vector3(0.30f, 0.30f, 0.30f));
                            go.gameObject.SetActive(true);
                            m_currentPointingIT = go;
                        }
                        break;
                    }

                    case PointingIT.WIM_RAY:
                    {
                        if(m_datasetGameObjects.Count > 0)
                        {
                            ARWIMRay go = Instantiate(ARWIMRayPrefab);
                            go.Init(m_hdProvider, m_datasetInAnnotation, new Vector3(0.30f, 0.30f, 0.30f));
                            go.gameObject.SetActive(true);
                            m_currentPointingIT = go;
                        }
                        break;
                    }

                    case PointingIT.MANUAL:
                        if (m_datasetGameObjects.Count > 0)
                        {
                            ARManual go = Instantiate(ARManualPrefab);
                            go.Init(m_hdProvider, m_datasetInAnnotation);
                            go.gameObject.SetActive(true);
                            m_currentPointingIT = go;
                        }
                        break;
                }

                //Register
                if(m_currentPointingIT != null)
                    m_currentPointingIT.OnSelection += OnPointingSelection;
            }
        }

        /// <summary>
        /// What to do when a new selection has been done using an interaction pointing technique?
        /// </summary>
        /// <param name="pointingIT">The pointing interaction technique calling this method</param>
        private void OnPointingSelection(IPointingIT pointingIT)
        {
            lock (this)
            {
                if (pointingIT.TargetPositionIsValid)
                {
                    m_client.SendAnchorAnnotation(m_sdInAnnotation, new float[3] { pointingIT.TargetPosition.x, pointingIT.TargetPosition.y, pointingIT.TargetPosition.z });
                    m_updatePointingID = true;
                    m_waitingPointingID = PointingIT.NONE;
                }
            }
        }

        /// <summary>
        /// Handles changement in the connection status
        /// </summary>
        /// <param name="s">The socket being used. It can be closed after this call</param>
        /// <param name="status">The new status to take account of</param>
        private void OnConnectionStatus(Socket s, ConnectionStatus status)
        {
            lock(this)
            {
                m_textValues.EnableIPTexts = true;
                String txt = "";
                if (status == ConnectionStatus.CONNECTED)
                {
                    m_wasConnected = true;
                    txt = IPAddress.Parse(((IPEndPoint)s.LocalEndPoint).Address.ToString()).ToString();
                }

                //Clear everything
                else if(m_wasConnected)
                {
                    m_connectionLost = true;

                    //Restore anchor data
                    if (m_transferBatch != null)
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

                    m_vtkDatasetsLoaded.Clear();
                    m_datasets.Clear();

                    txt = "";
                }
                if(txt != m_textValues.IPStr)
                {
                    m_textValues.IPStr = txt;
                    m_textValues.UpdateIPTexts = true;
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
                if(m_importAnchorDataRetry > 0)
                {
                    m_importAnchorDataRetry--;
                    WorldAnchorTransferBatch.ImportAsync(m_importAnchorData, OnImportComplete);
                }
                else
                {
                    m_importAnchorData      = null;
                    m_importAnchorDataRetry = MAX_IMPORT_ANCHOR_DATA_RETRY;
                }
                return;
            }

            Debug.Log("Anchor import succeed");

            RecreateRootAnchorGO();

            //Lock the object
            deserializedTransferBatch.LockObject("rootAnchor", m_rootAnchorGO);
            m_importAnchorData      = null;
            m_importAnchorDataRetry = MAX_IMPORT_ANCHOR_DATA_RETRY;

            m_anchorImportSegments.Clear();
        }

        #endregion
    }
}