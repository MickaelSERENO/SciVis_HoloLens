#define TEST

#if ENABLE_WINMD_SUPPORT
using Windows.Perception.Spatial;
using System.Runtime.InteropServices;
#endif

using Sereno.Datasets;
using Sereno.Datasets.Annotation;
using Sereno.Network;
using Sereno.Network.MessageHandler;
using Sereno.Pointing;
using Sereno.SciVis;
using Sereno.Unity.HandDetector;
using Sereno.Enumerates;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.WSA.Sharing;
using System.IO;
using System.Globalization;


namespace Sereno
{
    /// <summary>
    /// The different viewtype this application handles
    /// </summary>
    public enum ViewType
    {
        /// <summary>
        /// AR View
        /// </summary>
        AR = 0,
        
        /// <summary>
        /// 2D projection place on space
        /// </summary>
        TWO_DIMENSION = 1,

        /// <summary>
        /// Both rendering mode
        /// </summary>
        BOTH = 2,
    }

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
    /// Class representing the tablet selection data
    /// </summary>
    public class TabletSelectionData
    {
        /// <summary>
        /// Is the tablet matrix updated?
        /// </summary>
        public bool IsMatrixUpdated = false;

        /// <summary>
        /// The current tablet scaling. 
        /// Since the tablet is in orthographic view, this corresponds to the orthographic parameters
        /// </summary>
        public Vector3 Scaling = new Vector3(1,1,1);

        /// <summary>
        /// The current tablet position
        /// </summary>
        public Vector3 Position = new Vector3(0,0,0);

        /// <summary>
        /// The current tablet rotation
        /// </summary>
        public Quaternion Rotation = Quaternion.identity;

        /// <summary>
        /// tablet positions for current selection
        /// </summary>
        public List<Vector3> PositionList = new List<Vector3>();

        /// <summary>
        /// tablet rotations for current selection
        /// </summary>
        public List<Quaternion> RotationList = new List<Quaternion>();

        /// <summary>
        /// The IDs position/rotation (in the array list) of when to create a new selection mesh
        /// </summary>
        public List<NewSelectionMeshData> NewSelectionMeshIDs = new List<NewSelectionMeshData>();
        
        /// <summary>
        /// The current new selection mesh ID to use
        /// </summary>
        public NewSelectionMeshData CurrentNewSelectionMeshIDs = null;

        /// <summary>
        /// The List of all the selection meshes created for the current selection
        /// </summary>
        public List<GameObject> SelectionMeshes = new List<GameObject>();

        /// <summary>
        /// The list of the lasso points. First and Last points are stitched together
        /// </summary>
        public List<Vector2> LassoPoints = new List<Vector2>();

        /// <summary>
        /// The graphical object representing this tablet
        /// </summary>
        public GameObject GraphicalObject;

        /// <summary>
        /// The graphical lasso
        /// </summary>
        public LineRenderer GraphicalLasso;

        /// <summary>
        /// What is the capture ID of the current selection? Each time m_currentCaptureID == 0, we capture the position to create the mesh
        /// </summary>
        public int CurrentCaptureID = 0;

        /// <summary>
        /// Should we update the lasso?
        /// </summary>
        public bool UpdateLasso = false;
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

    /// <summary>
    /// The stored HeadsetData. TODO implement the way to handle multiple "in-selection" tablets
    /// </summary>
    public class HeadsetData
    {
        /// <summary>
        /// The data associated to this headset
        /// </summary>
        public HeadsetStatus Status;
    }

    public class Main : MonoBehaviour, IMessageBufferCallback, IDataProvider
    {
        /// <summary>
        /// Default text displayed for the IP address
        /// </summary>
        const String DEFAULT_IP_ADDRESS_TEXT = "Server not found at";

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
        /// The application properties
        /// </summary>
        private Properties m_appProperties = null;

        /// <summary>
        /// The matrix localToWorld of this GameObject
        /// </summary>
        private Matrix4x4 m_localToWorldMatrix = Matrix4x4.identity;

        /// <summary>
        /// The Dataset currently parsed. The key represents the datase ID
        /// </summary>
        private Dictionary<Int32, Dataset> m_datasets = new Dictionary<Int32, Dataset>();

        private Dictionary<Int32, LogAnnotationContainer> m_logAnnotations = new Dictionary<Int32, LogAnnotationContainer>();

        /// <summary>
        /// The VTK Structured Grid information shared by all SubDataset for a given VTK dataset
        /// </summary>
        private Dictionary<Dataset, VTKUnityStructuredGrid> m_vtkStructuredGrid = new Dictionary<Dataset, VTKUnityStructuredGrid>();

        /// <summary>
        /// Dataset loaded that needed to be visualized (in construction GameObject. Needed to be read in the main thread.
        /// </summary>
        private Queue<VTKDataset> m_vtkDatasetsLoaded = new Queue<VTKDataset>();

        /// <summary>
        /// Cloud Point SubDataset to visualiy load
        /// </summary>
        private Queue<SubDataset> m_cloudPointSubDatasetToLoad = new Queue<SubDataset>(); 

        /// <summary>
        /// SubDataset to visualy load.
        /// </summary>
        private Queue<SubDataset> m_vtkSubDatasetToLoad = new Queue<SubDataset>();
        
        /// <summary>
        /// SubDataset to remove.
        /// </summary>
        private Queue<SubDataset> m_subDatasetToRemove = new Queue<SubDataset>();

        /// <summary>
        /// Datasets to remove.
        /// </summary>
        private Queue<Dataset> m_datasetToRemove = new Queue<Dataset>();

        /// <summary>
        /// All the dataset game objects
        /// </summary>
        private List<DefaultSubDatasetGameObject> m_datasetGameObjects = new List<DefaultSubDatasetGameObject>();

        /// <summary>
        /// All the registered subdataset groups
        /// </summary>
        private Dictionary<Int32, SubDatasetGroup> m_sdGroups = new Dictionary<int, SubDatasetGroup>();

        /// <summary>
        /// All the game objects created for SubDatasetSubjectiveStackedGroup data objects
        /// </summary>
        private Dictionary<SubDatasetSubjectiveStackedGroup, SubjectiveGroupGameObject> m_subjGroupsGOs = new Dictionary<SubDatasetSubjectiveStackedGroup, SubjectiveGroupGameObject>();

        /// <summary>
        /// All the SubDatasetSubjectiveStackedGroup to graphically load
        /// </summary>
        private Queue<SubDatasetSubjectiveStackedGroup> m_subjViewToLoad = new Queue<SubDatasetSubjectiveStackedGroup>();

        /// <summary>
        /// All the SubDatasetSubjectiveStackedGroup to graphically unload
        /// </summary>
        private Queue<SubDatasetSubjectiveStackedGroup> m_subjViewToRemove = new Queue<SubDatasetSubjectiveStackedGroup>();
               
        /// <summary>
        /// The Server Client. 
        /// </summary>
        private VFVClient m_client = null;

        /// <summary>
        /// The client headset ID
        /// </summary>
        private int m_headsetID = -1;

        /// <summary>
        /// The tablet ID bound to this headset
        /// </summary>
        private int m_tabletID = -1;

        /// <summary>
        /// The current action
        /// </summary>
        private HeadsetCurrentAction m_currentAction = HeadsetCurrentAction.NOTHING;

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
        /// When should the random text be disabled? -1 == never
        /// </summary>
        private Int64 m_disableRandomTextTimestamp = -1;

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
        private List<HeadsetData> m_headsetStatus = new List<HeadsetData>();

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
        /// The current pointing IT GameObject
        /// </summary>
        private GameObject m_currentPointingITGO = null;

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
        private SubDataset m_sdInAnnotation = null;

        /// <summary>
        /// The SubDataset waiting for an annotation selection
        /// </summary>
        private SubDataset m_sdWaitingAnnotation = null;

        /// <summary>
        /// The pointing interaction technique in wait for annotation being loaded
        /// </summary>
        private PointingIT m_waitingPointingID = PointingIT.NONE;

        /// <summary>
        /// Should we update the pointing ID
        /// </summary>
        private bool m_updatePointingID = false;

        /// <summary>
        /// The Tablet selection internal data (matrix, in selection, etc.)
        /// </summary>
        private TabletSelectionData m_tabletSelectionData = new TabletSelectionData();

        /// <summary>
        /// The current view type 
        /// </summary>
        private ViewType m_viewType = ViewType.AR;

        /// <summary>
        /// Is the current view type updated? (useful for asynchronous messages)
        /// </summary>
        private bool m_viewTypeUpdated = true;
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
        /// Prefab of CloudPointGameObject correctly configured
        /// </summary>
        public CloudPointGameObject CloudPointGameObjectPrefab;

        /// <summary>
        /// Prefab of Subjective Groups correctely configured
        /// </summary>
        public SubjectiveGroupGameObject SubjectiveGroupGameObjectPrefab;

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
        /// The AR Collaborator pointing IT prefab
        /// </summary>
        public ARCollabPointingIT ARCollabPointingITPrefab;

        /// <summary>
        /// The tablet prefab
        /// </summary>
        public GameObject TabletPrefab;
        
        /// <summary>
        /// The selection mesh prefab (full object)
        /// </summary>
        public FullSelectionMeshGameObject FullSelectionMeshPrefab;

        public OutlineSelectionMeshGameObject OutlineSelectionMeshPrefab;

        /// <summary>
        /// The tablet virtual camera to use
        /// </summary>
        public Camera TabletVirtualCamera;

        /// <summary>
        /// A bird view camera of the scene in the camera mode
        /// </summary>
        public Camera TabletBirdViewCamera;

        /// <summary>
        /// The tablet virtual plane that display, in space, what the tablet sees
        /// </summary>
        public GameObject TabletVirtualPlane;

        /// <summary>
        /// How many tablet's position should be ignored before capturing?
        /// </summary>
        public int NBTabletPositionIgnored = 2;
        #endregion
        
        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            m_appProperties = Properties.ParseProperties();

            //Default text helpful to bind headset to tablet
            m_textValues.UpdateIPTexts = true;
            m_textValues.EnableIPTexts = true;

            m_textValues.UpdateRandomText = true;
            m_textValues.EnableRandomText = false;

            //Configure the main camera. Depth texture is used during raycasting
            Camera.main.depthTextureMode = DepthTextureMode.Depth;

            //Start the network communication
            m_client = new VFVClient(this, m_appProperties.Network.IP, m_appProperties.Network.Port);
            m_client.AddListener(new ClientStatusCallback(OnConnectionStatus));
            m_client.Connect();

            //Start the hand detector
            m_hdProvider.Smoothness = 0.75f;
            //m_hdProvider.InitializeHandDetector();

            //Initialize our selection techniques
            GoGoGameObject.Init(m_hdProvider);
            GoGoGameObject.transform.parent = null;
            GoGoGameObject.transform.position = new Vector3(0, 0, 0);
            GoGoGameObject.transform.rotation = Quaternion.identity;
            GoGoGameObject.gameObject.SetActive(false);

            //Initialize the tablet selection representation
            m_tabletSelectionData.GraphicalObject = Instantiate(TabletPrefab);
            m_tabletSelectionData.GraphicalObject.transform.parent = this.transform;
            m_tabletSelectionData.GraphicalObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            m_tabletSelectionData.GraphicalObject.transform.localRotation = Quaternion.identity;
            m_tabletSelectionData.GraphicalLasso = m_tabletSelectionData.GraphicalObject.GetComponent<LineRenderer>();
            m_tabletSelectionData.GraphicalLasso.startWidth = m_tabletSelectionData.GraphicalLasso.endWidth = 0.001f; //width == 5mm
            m_tabletSelectionData.GraphicalObject.SetActive(false);

            //Initialize the bird view camera
            TabletBirdViewCamera.transform.localPosition = new Vector3(1, 1, -1);
            TabletBirdViewCamera.transform.LookAt(Vector3.zero);    
        
            CurrentPointingIT = PointingIT.NONE;

#if TEST
            Task t = new Task( () =>
            {
                AddCloudDatasetMessage addCloudMsg = new AddCloudDatasetMessage(ServerType.GET_ADD_CLOUD_POINT_DATASET);
                addCloudMsg.Path   = "3.cp";
                addCloudMsg.DataID = 0;
                OnAddCloudPointDataset(null, addCloudMsg);
                
                /*AddVTKDatasetMessage addVTKMsg = new AddVTKDatasetMessage(ServerType.GET_ADD_VTK_DATASET);
                addVTKMsg.DataID = 0;
                addVTKMsg.NbCellFieldValueIndices = 0;
                addVTKMsg.NbPtFieldValueIndices = 1;
                addVTKMsg.Path = "history2019-12-10.vtk";
                addVTKMsg.PtFieldValueIndices = new int[] { 1 };
                OnAddVTKDataset(null, addVTKMsg);*/
                
                AddSubDatasetMessage addSDMsg = new AddSubDatasetMessage(ServerType.GET_ADD_SUBDATASET);
                addSDMsg.DatasetID = 0;
                addSDMsg.SubDatasetID = 0;
                addSDMsg.Name = m_datasets[0].PointFieldDescs[0].Name;
                OnAddSubDataset(null, addSDMsg);
                /*addSDMsg.SubDatasetID = 1;
                addSDMsg.Name = $"{m_datasets[0].PointFieldDescs[0].Name}_bis";
                OnAddSubDataset(null, addSDMsg);
                addSDMsg.SubDatasetID = 2;
                addSDMsg.Name = $"{m_datasets[0].PointFieldDescs[0].Name}_bis2";
                OnAddSubDataset(null, addSDMsg);*/

                //m_datasets[0].SubDatasets[0].MinDepthClipping = 0.25f;
                //m_datasets[0].SubDatasets[0].MaxDepthClipping = 0.70f;
                //m_datasets[0].SubDatasets[1].DepthClipping = 1.0f;

                /*MoveDatasetMessage moveVTKMsg = new MoveDatasetMessage(ServerType.GET_ON_MOVE_DATASET);
                moveVTKMsg.DataID = 0;
                moveVTKMsg.SubDataID = 0;
                moveVTKMsg.Position = new float[3] { -0.30f, -0.75f, 1.5f };
                moveVTKMsg.HeadsetID = -1;
                OnMoveDataset(null, moveVTKMsg);

                ScaleDatasetMessage scaleMsg = new ScaleDatasetMessage(ServerType.GET_ON_SCALE_DATASET);
                scaleMsg.DataID = 0;
                scaleMsg.SubDataID = 0;
                scaleMsg.HeadsetID = -1;
                scaleMsg.Scale = new float[3] { 0.4f, 0.4f, 0.4f };
                OnScaleDataset(null, scaleMsg);*/

                //Create a SubjectiveView group
                /*AddSubDatasetSubjectiveGroupMessage subjGroupMsg = new AddSubDatasetSubjectiveGroupMessage(ServerType.ADD_SUBJECTIVE_VIEW_GROUP);
                subjGroupMsg.SDG_ID = 0;
                subjGroupMsg.BaseDatasetID    = 0;
                subjGroupMsg.BaseSubDatasetID = 0;
                subjGroupMsg.SubjectiveViewType = (int)SubDatasetGroupType.STACKED_LINKED;
                OnAddSubDatasetSubjectiveGroup(null, subjGroupMsg);

                AddSubDatasetToSubjectiveStackedGroupMessage addSDToSubjMsg = new AddSubDatasetToSubjectiveStackedGroupMessage(ServerType.ADD_SD_TO_SV_STACKED_LINKED_GROUP);
                addSDToSubjMsg.DatasetID = 0;
                addSDToSubjMsg.StackedID = 1;
                addSDToSubjMsg.LinkedID  = 2;
                addSDToSubjMsg.SDG_ID    = 0;
                OnAddSubDatasetToSubjectiveStackedGroup(null, addSDToSubjMsg);*/

                /*Thread.Sleep(15000);
                RemoveSubDatasetMessage removeSDMsg = new RemoveSubDatasetMessage(ServerType.GET_DEL_SUBDATASET);
                removeSDMsg.DataID = 0;
                removeSDMsg.SubDataID = 0;
                OnRemoveSubDataset(null, removeSDMsg);*/

                //Annotations
                /*AddLogAnnotationMessage logAnnot = new AddLogAnnotationMessage(ServerType.GET_ADD_LOG_ANNOTATION);
                logAnnot.LogID     = 0;
                logAnnot.HasHeader = true;
                logAnnot.FileName  = "log_history_2019_12_10.csv";
                logAnnot.TimeIdx   = 0;
                OnAddLogAnnotation(null, logAnnot);

                AddLogAnnotationPositionMessage logPos = new AddLogAnnotationPositionMessage(ServerType.GET_ADD_LOG_ANNOTATION_POSITION);
                logPos.AnnotID = 0;
                logPos.CompID  = 0;
                OnAddLogAnnotationPosition(null, logPos);

                LinkLogAnnotationPositionSubDatasetMessage linkPos = new LinkLogAnnotationPositionSubDatasetMessage(ServerType.GET_LINK_LOG_ANNOT_POS_SD);
                linkPos.DataID    = 0;
                linkPos.SubDataID = 0;
                linkPos.AnnotID   = 0;
                linkPos.CompID    = 0;
                linkPos.DrawableID = 0;
                OnLinkLogAnnotationPositionSubDataset(null, linkPos);

                SubDataset sd = GetSubDataset(0, 0);
                if (sd == null)
                    return;
                LogAnnotationPositionInstance annot = sd.LogAnnotationPositions.FirstOrDefault(i => i.InstanceID == 0);
                if (annot == null)
                    return;
                annot.UseTime = true;

                SetLogAnnotationPositionIndexesMessage idxMsg = new SetLogAnnotationPositionIndexesMessage(ServerType.GET_SET_LOG_ANNOTATION_POSITION_INDEXES);
                idxMsg.AnnotID = 0;
                idxMsg.CompID  = 0;
                idxMsg.Indexes = new int[] { 1, 2, -1 };
                OnSetLogAnnotationPositionIndexes(null, idxMsg);

                SetDrawableAnnotationPositionColorMessage colorMsg = new SetDrawableAnnotationPositionColorMessage(ServerType.GET_SET_DRAWABLE_ANNOTATION_POSITION_COLOR);
                colorMsg.DatasetID = 0;
                colorMsg.SubDatasetID = 0;
                colorMsg.DrawableID = 0;
                colorMsg.Color = 0xffff0000;
                OnSetDrawableAnnotationPositionColor(null, colorMsg);

                SetDrawableAnnotationPositionIdxMessage idxPosMsg = new SetDrawableAnnotationPositionIdxMessage(ServerType.GET_SET_DRAWABLE_ANNOTATION_POSITION_IDX);
                idxPosMsg.DatasetID = 0;
                idxPosMsg.SubDatasetID = 0;
                idxPosMsg.DrawableID = 0;
                idxPosMsg.Indices = new int[] { 6 };
                OnSetDrawableAnnotationPositionIdx(null, idxPosMsg);*/

                /*TFSubDatasetMessage tfMsgSD2 = new TFSubDatasetMessage(ServerType.GET_TF_DATASET);
                tfMsgSD2.ColorType = ColorMode.WARM_COLD_CIELAB;
                tfMsgSD2.DataID = 0;
                tfMsgSD2.SubDataID = 2;
                tfMsgSD2.TFID = TFType.TF_TRIANGULAR_GTF;
                tfMsgSD2.GTFData = new TFSubDatasetMessage.GTF();
                tfMsgSD2.GTFData.Props = new TFSubDatasetMessage.GTFProp[1];
                tfMsgSD2.GTFData.Props[0] = new TFSubDatasetMessage.GTFProp() { Center = 0.5f, PID = (int)m_datasets[0].PointFieldDescs[0].ID, Scale = 0.5f };
                tfMsgSD2.Timestep = 0.0f;
                tfMsgSD2.HeadsetID = -1;
                OnTFDataset(null, tfMsgSD2);*/

                //Simulate a time animation
                /*TFSubDatasetMessage tfMsgSD1 = new TFSubDatasetMessage(ServerType.GET_TF_DATASET);
                tfMsgSD1.ColorType = ColorMode.WARM_COLD_CIELAB;
                tfMsgSD1.DataID = 0;
                tfMsgSD1.SubDataID = 0;
                tfMsgSD1.TFID = TFType.TF_GTF;
                tfMsgSD1.GTFData = new TFSubDatasetMessage.GTF();
                tfMsgSD1.GTFData.Props = new TFSubDatasetMessage.GTFProp[1];
                tfMsgSD1.GTFData.Props[0] = new TFSubDatasetMessage.GTFProp() { Center = 0.5f, PID = (int)m_datasets[0].PointFieldDescs[0].ID, Scale = 0.75f };
                tfMsgSD1.MinClipping = 0.00f;
                tfMsgSD1.MaxClipping = 1.0f;
                tfMsgSD1.Timestep = 0.0f;
                tfMsgSD1.HeadsetID = -1;
                OnTFDataset(null, tfMsgSD1);*/
                
                //Simulate a lasso input
                /*CurrentActionMessage curAction = new CurrentActionMessage(ServerType.GET_CURRENT_ACTION);
                curAction.CurrentAction = (int)HeadsetCurrentAction.LASSO;
                OnCurrentAction(null, curAction);

                TabletScaleMessage tabletScale = new TabletScaleMessage(ServerType.GET_TABLET_SCALE);
                tabletScale.height = 1920;
                tabletScale.width  = 1920;
                tabletScale.posx   = 0;
                tabletScale.posy   = 0;
                tabletScale.scale  = 0.15f / 1920;
                OnTabletScale(null, tabletScale);
                
                LassoMessage lasso = new LassoMessage(ServerType.GET_LASSO);
                lasso.size = 128*3;
                lasso.data = new List<float>();
                for(int i = 0; i < 128; i++)
                {
                    lasso.data.Add((float)(0.5*Math.Cos(i/64.0f * Math.PI)));
                    lasso.data.Add((float)(0.5*Math.Sin(i/64.0f * Math.PI)));
                    lasso.data.Add(0.0f);
                }
                OnLasso(null, lasso);
                               
                //Simulate a movement
                curAction.CurrentAction = (int)HeadsetCurrentAction.SELECTING;
                OnCurrentAction(null, curAction);

                //First entry
                AddNewSelectionInputMessage addNewSelection = new AddNewSelectionInputMessage(ServerType.GET_ADD_NEW_SELECTION_INPUT);
                addNewSelection.BooleanOperation = 0;
                OnAddNewSelectionInput(null, addNewSelection);

                LocationMessage loc = new LocationMessage(ServerType.GET_TABLET_LOCATION);
                loc.position = new float[3] { 0.0f, 0.3f, 0.0f };
                loc.rotation = new float[4] { 0.0f, 0.0f, 0.0f, 1.0f };
                OnLocation(null, loc);

                Thread.Sleep(100);
                loc = new LocationMessage(ServerType.GET_TABLET_LOCATION);
                loc.position = new float[3] { 0.2f, 0.5f, 0.2f };
                loc.rotation = new float[4] { 0.0f, 0.0f, 0.0f, 1.0f };
                OnLocation(null, loc);
                
                Thread.Sleep(100);
                loc = new LocationMessage(ServerType.GET_TABLET_LOCATION);
                loc.rotation = new float[4] { 0.0f, 0.0f, 0.0f, 1.0f };
                loc.position = new float[3] { 0.3f, -0.4f, 0.3f };
                OnLocation(null, loc);*/

                /*Thread.Sleep(500);
                curAction.CurrentAction = (int)HeadsetCurrentAction.REVIEWING_SELECTION;
                OnCurrentAction(null, curAction);

                //The dataset needs to be loaded for that
                OnConfirmSelection(null, new ConfirmSelectionMessage(ServerType.GET_CONFIRM_SELECTION) {DataID = 0, SubDataID = 0} );*/
                
               /* while (true)
                {
                    if (m_datasets[0].PointFieldDescs[0].Value == null || tfMsgSD1.Timestep >= m_datasets[0].PointFieldDescs[0].Value.Count)
                        tfMsgSD1.Timestep = 0.0f;
                    OnTFDataset(null, tfMsgSD1);
                    tfMsgSD1.Timestep += 0.25f;
                    Thread.Sleep(3000);
                }*/
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
            foreach(DefaultSubDatasetGameObject go in m_datasetGameObjects)
            {
                if(go.SubDatasetState == sd)
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
                        IPHeaderText.text = $"{DEFAULT_IP_ADDRESS_TEXT}\n{m_client.ServerAddress}:{m_client.ServerPort}";
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
            if(m_textValues.UpdateRandomText)
            {
                //Enable/Disable the IP Text
                RandomText.enabled = m_textValues.EnableRandomText;

                //If we should enable the text, set the text value
                if(m_textValues.EnableRandomText)
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
                foreach(var go in m_datasetGameObjects)
                    Destroy(go.gameObject);
                m_datasetGameObjects.Clear();
                m_vtkStructuredGrid.Clear();

                CurrentPointingIT = PointingIT.NONE;
            }

            //Load what the server thread loaded regarding vtk datasets
            while(m_vtkSubDatasetToLoad.Count > 0)
            {
                SubDataset sd = m_vtkSubDatasetToLoad.Dequeue();

                //Structure grid dataset
                if(m_vtkStructuredGrid.ContainsKey(sd.Parent))
                {
                    VTKUnitySmallMultipleGameObject gameObject = Instantiate(VTKSMGameObject);
                    VTKUnitySmallMultiple sm = m_vtkStructuredGrid[sd.Parent].CreatePointFieldSmallMultiple(sd);

                    gameObject.transform.parent = transform;
                    gameObject.Init(sm, this);
                    m_datasetGameObjects.Add(gameObject);
                }
            }

            while(m_cloudPointSubDatasetToLoad.Count > 0)
            {
                SubDataset sd = m_cloudPointSubDatasetToLoad.Dequeue();

                CloudPointGameObject gameObject = Instantiate(CloudPointGameObjectPrefab);

                gameObject.transform.parent = transform;
                gameObject.Init((CloudPointDataset)sd.Parent, sd, this);
                m_datasetGameObjects.Add(gameObject);
            }
        }

        /// <summary>
        /// Handles the datasets to remove
        /// </summary>
        private void HandleDatasetsToRemove()
        {
            //Lambda to remove subdatasets
            Action<SubDataset> removeSubDatasetFunc = (SubDataset sd) =>
            {
                DefaultSubDatasetGameObject go = GetSDGameObject(sd);
                if (go != null)
                {
                    m_datasetGameObjects.Remove(go);
                    if (go == m_datasetInAnnotation)
                        CurrentPointingIT = PointingIT.NONE;

                    if(m_vtkStructuredGrid.ContainsKey(sd.Parent))
                    {
                        m_vtkStructuredGrid[sd.Parent].RemoveSmallMultipleFromSubDataset(sd);
                    }
                    GameObject.Destroy(go.gameObject);
                    Debug.Log("Deleting a subdataset game object");
                }
            };

            while(m_datasetToRemove.Count > 0)
            {
                Dataset d = m_datasetToRemove.Dequeue();
                foreach(SubDataset sd in d.SubDatasets)
                    removeSubDatasetFunc(sd);

                m_vtkStructuredGrid.Remove(d);
                m_datasets.Remove(d.ID);
            }

            while(m_subDatasetToRemove.Count > 0)
            {
                SubDataset sd = m_subDatasetToRemove.Dequeue();
                removeSubDatasetFunc(sd);
            }
        }

        private void HandleSubDatasetGroupsToRemove()
        {
            while (m_subjViewToRemove.Count > 0)
            {
                SubDatasetSubjectiveStackedGroup svg = m_subjViewToRemove.Dequeue();
                if (!m_subjGroupsGOs.ContainsKey(svg))
                    continue;
                SubjectiveGroupGameObject go = m_subjGroupsGOs[svg];
                Destroy(go);
                m_subjGroupsGOs.Remove(svg);
            }
        }

        private void HandleSubDatasetGroupsLoaded()
        {
            while (m_subjViewToLoad.Count > 0)
            {
                SubDatasetSubjectiveStackedGroup svg = m_subjViewToLoad.Dequeue();
                SubjectiveGroupGameObject go = Instantiate(SubjectiveGroupGameObjectPrefab);
                go.transform.parent = transform;
                go.Init(svg);
                m_subjGroupsGOs.Add(svg, go);
            }
        }

        private void HandlePointingID()
        {
            if(m_updatePointingID)
            {
                if(m_sdWaitingAnnotation != null)
                {
                    DefaultSubDatasetGameObject sdGo = GetSDGameObject(m_sdWaitingAnnotation);

                    if(sdGo != null)
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
            for(int i = m_headsetStatus.Count; i < m_headsetGameObjects.Count; i++)
            {
                m_headsetGameObjects[i].Glyph.SetActive(false);
                m_headsetGameObjects[i].ARPointingGO.PointingIT = PointingIT.NONE;
            }

            //Change the color, shape and the position/rotation of each character's glyph
            for(int i = 0; i < m_headsetStatus.Count; i++)
            {
                //Update the glyph position / rotation
                //First update the general headset transform gameobject
                m_headsetGameObjects[i].Headset.transform.localRotation = new Quaternion(m_headsetStatus[i].Status.Rotation[1],
                                                                                         m_headsetStatus[i].Status.Rotation[2],
                                                                                         m_headsetStatus[i].Status.Rotation[3],
                                                                                         m_headsetStatus[i].Status.Rotation[0]);

                m_headsetGameObjects[i].Headset.transform.localPosition = new Vector3(m_headsetStatus[i].Status.Position[0],
                                                                                      m_headsetStatus[i].Status.Position[1],
                                                                                      m_headsetStatus[i].Status.Position[2]);

                //Handle the special case of the glyph
                m_headsetGameObjects[i].Glyph.transform.localPosition  = Vector3.forward*(-HEADSET_SIZE/2.0f) + //Middle of the head
                                                                         Vector3.up     *(HEADSET_TOP);        //Top of the head

                //Update the glyph color
                m_headsetGameObjects[i].Glyph.GetComponent<MeshRenderer>().material.color = new Color(((byte)(m_headsetStatus[i].Status.Color >> 16) & 0xff)/255.0f,
                                                                                                      ((byte)(m_headsetStatus[i].Status.Color >> 8)  & 0xff)/255.0f,
                                                                                                      ((byte)(m_headsetStatus[i].Status.Color >> 0)  & 0xff)/255.0f);

                m_headsetGameObjects[i].Glyph.SetActive(true);

                //Update the glyph shape
                MeshFilter mf = m_headsetGameObjects[i].Glyph.GetComponent<MeshFilter>();
                switch(m_headsetStatus[i].Status.CurrentAction)
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

                if(m_headsetStatus[i].Status.ID != m_headsetID)
                {
                    //Update the pointing interaction technique
                    m_headsetGameObjects[i].ARPointingGO.PointingIT           = m_headsetStatus[i].Status.PointingIT;
                    m_headsetGameObjects[i].ARPointingGO.SubDatasetGameObject = GetSDGameObject(GetSubDataset(m_headsetStatus[i].Status.PointingDatasetID, m_headsetStatus[i].Status.PointingSubDatasetID));
                    m_headsetGameObjects[i].ARPointingGO.TargetPosition       = new Vector3(m_headsetStatus[i].Status.PointingLocalSDPosition[0], m_headsetStatus[i].Status.PointingLocalSDPosition[1], m_headsetStatus[i].Status.PointingLocalSDPosition[2]);

                    Vector3 pointingHeadsetStartPosition = new Vector3(m_headsetStatus[i].Status.PointingHeadsetStartPosition[0], m_headsetStatus[i].Status.PointingHeadsetStartPosition[1], m_headsetStatus[i].Status.PointingHeadsetStartPosition[2]);
                    pointingHeadsetStartPosition = this.transform.localToWorldMatrix.MultiplyPoint(pointingHeadsetStartPosition);

                    Quaternion headsetOrientation = new Quaternion(m_headsetStatus[i].Status.PointingHeadsetStartOrientation[1],
                                                                   m_headsetStatus[i].Status.PointingHeadsetStartOrientation[2],
                                                                   m_headsetStatus[i].Status.PointingHeadsetStartOrientation[3],
                                                                   m_headsetStatus[i].Status.PointingHeadsetStartOrientation[0]);
                    headsetOrientation = headsetOrientation * this.transform.rotation; //Convert the headset orientation in world space.
                    m_headsetGameObjects[i].ARPointingGO.HeadsetStartPosition    = pointingHeadsetStartPosition;
                    m_headsetGameObjects[i].ARPointingGO.HeadsetStartOrientation = headsetOrientation;
                }
            }
        }

        private void HandleTablet2DView()
        {
            if(m_viewTypeUpdated)
            {
                switch(m_viewType)
                {
                    case ViewType.AR:
                        Camera.main.cullingMask |= ((1 << 8) + (1 << 9)); //Enable VolumeRendering and tablet selection Mask
                        //Disable 2D camera
                        TabletVirtualCamera.gameObject.SetActive(false);
                        TabletBirdViewCamera.gameObject.SetActive(false);
                        TabletVirtualPlane.gameObject.SetActive(false);
                        break;

                    case ViewType.TWO_DIMENSION:
                        Camera.main.cullingMask &= ~((1 << 8) + (1 << 9)); //Disable VolumeRendering and tablet selection Mask
                        //Enable 2D camera
                        TabletVirtualCamera.gameObject.SetActive(true);
                        TabletBirdViewCamera.gameObject.SetActive(true);
                        TabletVirtualPlane.gameObject.SetActive(true);
                        break;

                    case ViewType.BOTH:
                        Camera.main.cullingMask |= ((1 << 8) + (1 << 9)); //Enable VolumeRendering and tablet selection Mask
                        //Enable 2D camera
                        TabletVirtualCamera.gameObject.SetActive(true);
                        TabletBirdViewCamera.gameObject.SetActive(true);
                        TabletVirtualPlane.gameObject.SetActive(true);
                        break;
                }
                m_viewTypeUpdated = false;
            }

            if(m_viewType == ViewType.BOTH || m_viewType == ViewType.TWO_DIMENSION)
            {
                //Do not change the scaling since we do not know how to translate that for perspective projections
                TabletVirtualCamera.transform.localPosition = m_tabletSelectionData.Position;
                TabletVirtualCamera.transform.localRotation = m_tabletSelectionData.Rotation;
            }
        }

        /// <summary>
        /// Close the current TabletSelectionMesh
        /// </summary>
        private void CloseTabletCurrentSelectionMesh()
        {
            //Nothing to do here
            if (m_tabletSelectionData.CurrentNewSelectionMeshIDs == null || m_tabletSelectionData.CurrentNewSelectionMeshIDs.IsClosed)
                return;

            m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData?.CloseVolume();
        }

        /// <summary>
        /// Tells the orientation of a triangle
        /// </summary>
        /// <param name="p1">the first point of the triangle</param>
        /// <param name="p2">the second point of the triangle</param>
        /// <param name="p3">the third point of the triangle</param>
        /// <returns>-1 if clockwise, 1 if counterclock wise, 0 if colinear</returns>
        private int Orientation(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float val = (p2.y - p1.y) * (p3.x - p2.x) -
                        (p2.x - p1.x) * (p3.y - p2.y);

            if (val == 0)
                return 0;  // colinear 
            return (val > 0) ? -1 : 1; // clock or counterclock wise 
        }

        /// <summary>
        /// Handle the tablet selection data
        /// </summary>
        private void HandleTabletSelection()
        {
            //Update the tablet's position
            m_tabletSelectionData.GraphicalObject.transform.localPosition = m_tabletSelectionData.Position;
            m_tabletSelectionData.GraphicalObject.transform.localScale = m_tabletSelectionData.Scaling;
            m_tabletSelectionData.GraphicalObject.transform.localRotation = m_tabletSelectionData.Rotation;

            if (m_currentAction == HeadsetCurrentAction.SELECTING || m_currentAction == HeadsetCurrentAction.REVIEWING_SELECTION || m_currentAction == HeadsetCurrentAction.LASSO)
            {
                //Show the tablet
                m_tabletSelectionData.GraphicalObject.SetActive(true);

                if (m_currentAction == HeadsetCurrentAction.LASSO)
                {
                    //Show only the tablet
                    foreach (GameObject go in m_tabletSelectionData.SelectionMeshes)
                        go.SetActive(false);
                    m_tabletSelectionData.GraphicalLasso.positionCount = 0; //Remove the "old lasso"

                    //Delete all the selection meshes data
                    foreach (GameObject go in m_tabletSelectionData.SelectionMeshes)
                        Destroy(go);
                    m_tabletSelectionData.SelectionMeshes.Clear();
                }

                if (m_tabletSelectionData.UpdateLasso)
                {
                    //Set the lasso
                    Vector3[] lasso = new Vector3[m_tabletSelectionData.LassoPoints.Count + 1];
                    for (int i = 0; i < m_tabletSelectionData.LassoPoints.Count; i++)
                        lasso[i] = new Vector3(m_tabletSelectionData.LassoPoints[i].x, 0.0f, m_tabletSelectionData.LassoPoints[i].y);
                    lasso[lasso.Length - 1] = lasso[0]; //Cycle

                    m_tabletSelectionData.GraphicalLasso.positionCount = lasso.Length;
                    m_tabletSelectionData.GraphicalLasso.SetPositions(lasso);

                    m_tabletSelectionData.UpdateLasso = false;
                }

                if (m_currentAction == HeadsetCurrentAction.SELECTING || m_currentAction == HeadsetCurrentAction.REVIEWING_SELECTION)
                {
                    foreach (NewSelectionMeshData meshData in m_tabletSelectionData.NewSelectionMeshIDs)
                    {
                        //Recreate a Selection Mesh
                        if (meshData.MeshGameObject == null)
                        {
                            if (meshData.VisualizationMode == VolumetricSelectionVisualizationMode.FULL)
                            {
                                FullSelectionMeshGameObject go = Instantiate(FullSelectionMeshPrefab);
                                go.gameObject.SetActive(true);
                                go.transform.parent = this.transform;
                                go.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                                go.transform.localRotation = Quaternion.identity;
                                go.Init((FullSelectionMeshData)meshData.MeshData);

                                meshData.MeshGameObject = go.gameObject;

                                m_tabletSelectionData.SelectionMeshes.Add(go.gameObject);
                            }

                            else if (meshData.VisualizationMode == VolumetricSelectionVisualizationMode.FULL_OUTLINE || meshData.VisualizationMode == VolumetricSelectionVisualizationMode.WEAK_OUTLINE)
                            {
                                OutlineSelectionMeshGameObject go = Instantiate(OutlineSelectionMeshPrefab);
                                go.gameObject.SetActive(true);
                                go.transform.parent = this.transform;
                                go.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                                go.transform.localRotation = Quaternion.identity;
                                go.Init((OutlineSelectionMeshData)meshData.MeshData);

                                meshData.MeshGameObject = go.gameObject;

                                m_tabletSelectionData.SelectionMeshes.Add(go.gameObject);
                            }
                        }
                    }
                }
            }

            else
            {
                m_tabletSelectionData.GraphicalObject.SetActive(false);

                //Delete all the selection meshes data as they are not needed anymore
                foreach (GameObject go in m_tabletSelectionData.SelectionMeshes)
                    Destroy(go);
                m_tabletSelectionData.SelectionMeshes.Clear();
                m_tabletSelectionData.GraphicalLasso.positionCount = 0;
            }
        }


        /// <summary>
        /// Get the relative position from the anchor
        /// </summary>
        /// <param name="position">The position to evaluate</param>
        /// <returns>the x, y and z component of the position regarding the root anchor game object</returns>
        private float[] GetRelativePositionToAnchor(Vector3 position)
        {
            Vector3 relPos = transform.worldToLocalMatrix.MultiplyPoint(position); //This work because WE are a child of the root anchor and we have an identity local matrix
            return new float[3] { relPos.x, relPos.y, relPos.z };
        }

        /// <summary>
        /// Send this headset status
        /// </summary>
        private void HandleHeadsetStatusSending()
        {
            //Send camera status. No need to send status if there is no root anchor (i.e, synchronization)
            if(m_client != null && m_client.IsConnected() && m_client.HeadsetConnectionSent)
            {
                HeadsetUpdateData headsetData = new HeadsetUpdateData();

                Quaternion rel;

                if(m_rootAnchorGO != null)
                {
                    headsetData.Position = GetRelativePositionToAnchor(Camera.main.transform.position);
                    rel = Quaternion.Inverse(m_rootAnchorGO.transform.localRotation) * Camera.main.transform.rotation;
                }
                else
                {
                    headsetData.Position = new float[3];
                    for(int i = 0; i < 3; i++)
                        headsetData.Position[i] = Camera.main.transform.position[i];
                    rel = Camera.main.transform.rotation;
                }


                //The relative orientation
                headsetData.Rotation = new float[4] { rel[3], rel[0], rel[1], rel[2] };

                //The current pointing data
                headsetData.PointingIT = CurrentPointingIT;
                if(CurrentPointingIT != PointingIT.NONE && m_sdInAnnotation != null && m_currentPointingIT != null)
                {
                    SubDataset curSD = m_sdInAnnotation;
                    headsetData.PointingDatasetID = curSD.Parent.ID;
                    headsetData.PointingSubDatasetID = curSD.Parent.SubDatasets.FindIndex(s => s == curSD);
                    headsetData.PointingInPublic = true; //TODO modify it when needed

                    for(int i = 0; i < 3; i++)
                        headsetData.PointingLocalSDPosition[i] = m_currentPointingIT.TargetPosition[i];

                    headsetData.PointingHeadsetStartPosition = GetRelativePositionToAnchor(m_currentPointingIT.HeadsetStartPosition);
                    Quaternion relHeadsetStartOrientation = Quaternion.Inverse(m_rootAnchorGO.transform.localRotation) * m_currentPointingIT.HeadsetStartOrientation;
                    headsetData.PointingHeadsetStartOrientation = new float[] { relHeadsetStartOrientation[3], relHeadsetStartOrientation[0],
                                                                                relHeadsetStartOrientation[1], relHeadsetStartOrientation[2] };
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

        // Update is called once per frame
        void LateUpdate()
        {
            foreach (Camera cam in Camera.allCameras)
            {
                if (cam.depthTextureMode == DepthTextureMode.None)
                    cam.depthTextureMode = DepthTextureMode.Depth;
            }
            HandleSpatialCoordinateSystem();

            lock(this)
            {
                m_localToWorldMatrix = transform.localToWorldMatrix;
                HandleDatasetsToRemove();
                HandleDatasetsLoaded();
                HandleSubDatasetGroupsToRemove();
                HandleSubDatasetGroupsLoaded();
                HandlePointingID();
                HandleAnchor();
                HandleIPTxt();
                HandleRandomText();
                HandleHeadsetStatusLoaded();
                HandleHeadsetStatusSending();
                HandleTabletSelection();
                HandleTablet2DView();

                m_targetedGameObject = null;

                //Update intersection between each datasets rendered and the selected pointing interaction technique
                if(m_currentPointingIT != null && m_currentPointingIT.TargetPositionIsValid)
                {
                    Vector3 targetPos = m_currentPointingIT.TargetPosition;
                    if(targetPos.x >= -0.5f && targetPos.x <= 0.5f &&
                       targetPos.y >= -0.5f && targetPos.y <= 0.5f &&
                       targetPos.z >= -0.5f && targetPos.z <= 0.5f)
                        m_targetedGameObject = m_currentPointingIT.CurrentSubDataset;
                }


                m_connectionLost = false;
            }
        }

        public void OnDestroy()
        {
            if(m_client != null)
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
            VTKParser  parser   = new VTKParser($"{Application.streamingAssetsPath}/Datasets/{msg.Path}");
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
            VTKDataset dataset = new VTKDataset(msg.DataID, msg.Path, parser, ptValues, cellValues);

            //Search for the other timesteps
            List<String> suffixes = new List<String>();
            foreach(String path in Directory.GetFiles($"{Application.streamingAssetsPath}/Datasets/"))
            {
                String fileName = Path.GetFileName(path);
                if(fileName.StartsWith(msg.Path+".") && fileName.Length > msg.Path.Length+1)
                {
                    String suffix = fileName.Substring(msg.Path.Length + 1);
                    bool isDigits = true;
                    foreach (char c in suffix)
                        if (c < '0' || c > '9')
                        {
                            isDigits = false;
                            break;
                        }
                    if(isDigits)
                        suffixes.Add(suffix);
                }
            }
            suffixes.Sort();
            foreach(String s in suffixes)
            {
                Debug.Log($"Parsing {Application.streamingAssetsPath}/Datasets/{msg.Path}.{s}...");
                VTKParser  parsert = new VTKParser($"{Application.streamingAssetsPath}/Datasets/{msg.Path}.{s}");
                if(parsert.Parse())
                    dataset.AddTimestep(parsert);
                else
                    Debug.Log($"Issue at parsing {Application.streamingAssetsPath}/Datasets/{msg.Path}.{s}");
            }

            //Set properties
            dataset.DatasetProperties = m_appProperties.DatasetPropertiesArray.FirstOrDefault(prop => dataset.Name == prop.Name);
            
            //Load the values in an asynchronous way
            dataset.LoadValues().ContinueWith((status) =>
            {
                //Update the transfer functions (again, asynchronously)
                foreach (SubDataset sd in dataset.SubDatasets)
                {
                    Debug.Log("Updating SD after loading dataset");
                    TransferFunction tf = sd.TransferFunction;
                    sd.TransferFunction = null; //We need to disactivate it in order to "launch" the event. Indeed, is old == new, no event is launched
                    sd.TransferFunction = tf;
                }
            });

            lock (this)
            {
                m_datasets.Add(dataset.ID, dataset);
                
                //Create the associate visualization
                if(parser.GetDatasetType() == VTKDatasetType.VTK_STRUCTURED_POINTS)
                {
                    unsafe
                    {
                        VTKUnityStructuredGrid grid = new VTKUnityStructuredGrid(dataset, DesiredVTKDensity, this);
                        m_vtkStructuredGrid.Add(dataset, grid);
                    }
                }
                foreach(SubDataset sd in dataset.SubDatasets)
                    m_vtkSubDatasetToLoad.Enqueue(sd);
            }
        }

        public void OnAddCloudPointDataset(MessageBuffer messageBuffer, AddCloudDatasetMessage msg)
        {
            CloudPointDataset dataset = new CloudPointDataset(msg.DataID, msg.Path, msg.Path);
            dataset.DatasetProperties = m_appProperties.DatasetPropertiesArray.FirstOrDefault(prop => dataset.Path == prop.Name);

            //Load the values in an asynchronous way
            dataset.LoadValues().ContinueWith((status) =>
            {
                //Update the transfer functions (again, asynchronously)
                foreach(SubDataset sd in dataset.SubDatasets)
                {
                    Debug.Log("Updating SD after loading dataset");
                    lock (sd)
                        sd.TransferFunction = sd.TransferFunction;
                }
            });

            //Store the dataset
            lock(this)
            {
                m_datasets.Add(dataset.ID, dataset);
                foreach(SubDataset sd in dataset.SubDatasets)
                    m_cloudPointSubDatasetToLoad.Enqueue(sd);
            }
        }

        /// <summary>
        /// Get the SubDataset targeted by the dataset ID, subdataset ID and public/private status
        /// </summary>
        /// <param name="datasetID"></param>
        /// <param name="subDatasetID"></param>
        /// <returns>The SubDataset found with the correct IDs, null otherwise</returns>
        private SubDataset GetSubDataset(int datasetID, int subDatasetID)
        {
            if(datasetID < 0 || subDatasetID < 0)
                return null;

            if(m_datasets.Count <= datasetID)
                return null;

            return m_datasets[datasetID].GetSubDataset(subDatasetID);
        }

        public void OnRotateDataset(MessageBuffer messageBuffer, RotateDatasetMessage msg)
        {
            Debug.Log("Received rotation event");
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
            if (sd == null)
                return;
            sd.Rotation = msg.Quaternion;
        }

        public void OnMoveDataset(MessageBuffer messageBuffer, MoveDatasetMessage msg)
        {
            Debug.Log($"Received movement event : {msg.Position[0]}, {msg.Position[1]}, {msg.Position[2]}");
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
            if (sd != null)
                sd.Position = msg.Position;
        }

        public void OnScaleDataset(MessageBuffer messageBuffer, ScaleDatasetMessage msg)
        {
            Debug.Log($"Received Scale event : {msg.Scale[0]}, {msg.Scale[1]}, {msg.Scale[2]}");
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
            if (sd != null)
                sd.Scale = msg.Scale;
        }

        private TransferFunction TFMessageToTF(SubDataset sd, TFSubDatasetMessage msg)
        {
            TransferFunction tf = null;

            //Parse the transfer function
            switch (msg.TFID)
            {
                case TFType.TF_GTF:
                case TFType.TF_TRIANGULAR_GTF:
                {
                    //Fill centers and scales
                    float[] centers = new float[msg.GTFData.Props.Length];
                    float[] scales = new float[msg.GTFData.Props.Length];

                    foreach (TFSubDatasetMessage.GTFProp prop in msg.GTFData.Props)
                    {
                        int ind = sd.Parent.GetTFIndiceFromPropID(prop.PID);
                        if (ind != -1)
                        {
                            centers[ind] = prop.Center;
                            scales[ind] = prop.Scale;
                        }
                    }

                    //Generate the propert transfer function
                    if (msg.TFID == TFType.TF_GTF)
                        tf = new GTF(centers, scales);
                    else
                        tf = new TriangularGTF(centers, scales);

                    break;
                }
                case TFType.TF_MERGE:
                {
                    tf = new MergeTF(TFMessageToTF(sd, msg.MergeTFData.tf1), TFMessageToTF(sd, msg.MergeTFData.tf2), msg.MergeTFData.t);
                    break;
                }
            }

            if (tf != null)
            {
                tf.ColorMode = msg.ColorType;
                tf.Timestep = msg.Timestep;
                tf.MinClipping = msg.MinClipping;
                tf.MaxClipping = msg.MaxClipping;
            }
            return tf;
        }

        public void OnTFDataset(MessageBuffer messageBuffer, TFSubDatasetMessage msg)
        {
            lock (this)
            {
                Debug.Log("Received a Transfer Function event");

                SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
                if (sd == null)
                    return;
                TransferFunction tf = TFMessageToTF(sd, msg);

                //Update the TF. Numerous thread will be separately launched to update the visual
                sd.TransferFunction = tf;
                Debug.Log("End Transfer Function event");
            }
        }

        public void OnHeadsetInit(MessageBuffer messageBuffer, HeadsetInitMessage msg)
        {
            Debug.Log($"Received init headset message. Color : {msg.Color:X}, tablet connected: {msg.TabletConnected}, first connected: {msg.IsFirstConnected}");

            lock(this)
            {
                //Remove the connection message
                if(msg.TabletConnected)
                {
                    m_textValues.UpdateIPTexts = true;
                    m_textValues.EnableIPTexts = false;
                }
                //Redisplay the connection message
                else
                {
                    IPAddress addr = m_client.GetIPAddress();
                    String s = DEFAULT_IP_ADDRESS_TEXT;
                    if(addr != null)
                        s = IPAddress.Parse(addr.ToString()).ToString();
                    m_textValues.IPStr = s;
                    m_textValues.UpdateIPTexts = true;
                    m_textValues.EnableIPTexts = true;
                }


                m_clientColor = new Color32((byte)((msg.Color >> 16) & 0xff),
                                            (byte)((msg.Color >> 8) & 0xff),
                                            (byte)(msg.Color & 0xff), 255);

                m_headsetID = msg.ID;

                m_tabletID = msg.TabletID;

                m_hdProvider.Handedness = (Handedness)msg.Handedness;

                //Send anchor dataset to the server. 
                //The server will store that and send that information to other headsets if needed
                if(msg.IsFirstConnected)
                    m_anchorCommunication = AnchorCommunication.EXPORT;
            }
        }

        public void OnHeadsetsStatus(MessageBuffer messageBuffer, HeadsetsStatusMessage msg)
        {
            lock(this)
            {
                //Remove useless headsetStatus
                int i = 0;
                while(i < m_headsetStatus.Count)
                {
                    //Remove this headset status if it does not exist anymore
                    if (!Array.Exists(msg.HeadsetsStatus, x => x.ID == m_headsetStatus[i].Status.ID))
                        m_headsetStatus.RemoveAt(i);
                    else
                        i++;
                }

                //Add the remaining headset status
                foreach(HeadsetStatus hs in msg.HeadsetsStatus)
                {
                    HeadsetData data = m_headsetStatus.Find(x => x.Status.ID == hs.ID);
                    if (data == null)
                        m_headsetStatus.Add(new HeadsetData() { Status = hs });
                    else
                        data.Status = hs;
                }
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

        public void OnSubDatasetModificationOwner(MessageBuffer messageBuffer, SubDatasetModificationOwnerMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);
            if(sd != null)
                sd.LockOwnerID = msg.HeadsetID;
        }

        public void OnSubDatasetOwner(MessageBuffer messageBuffer, SubDatasetOwnerMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);
            if(sd != null)
                sd.OwnerID = msg.HeadsetID;
        }

        public void OnStartAnnotation(MessageBuffer messageBuffer, StartAnnotationMessage msg)
        {
            lock(this)
            {
                //Search got the current DefaultSubDatasetGameObject being in annotation
                SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);

                if(sd != null)
                {
                    //If currently in an annotation this will cancel the previous one
                    m_waitingPointingID = msg.PointingID;
                    m_sdWaitingAnnotation = sd;
                    m_updatePointingID = true;
                }
            }
        }

        public void OnAnchorAnnotation(MessageBuffer messageBuffer, AnchorAnnotationMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);
            if(sd != null)
                sd.AddCanvasAnnotation(new CanvasAnnotation(msg.LocalPosition));
        }

        public void OnClearAnnotations(MessageBuffer messageBuffer, ClearAnnotationsMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);

            if(sd != null)
                sd.ClearCanvasAnnotations();
        }

        public void OnAddSubDataset(MessageBuffer messageBuffer, AddSubDatasetMessage msg)
        {
            Debug.Log("On AddSubDataset message");
            //Search for the parent dataset
            Dataset dataset;
            lock(this)
            {
                if(!m_datasets.ContainsKey(msg.DatasetID))
                    return;
                dataset = m_datasets[msg.DatasetID];
            }

            //If VTK type
            if(dataset.GetType() == typeof(VTKDataset))
            {
                VTKDataset vtk = dataset as VTKDataset;
                if(vtk.PtFieldValues.Count == 0)
                    return;

                //Create a new SubDataset
                lock(this)
                {
                    SubDataset sd = new SubDataset(vtk, msg.OwnerID, msg.Name);
                    sd.ID = msg.SubDatasetID;

                    //TGTF transfer function by default
                    float[] scale  = new float[sd.Parent.PointFieldDescs.Count];
                    float[] center = new float[sd.Parent.PointFieldDescs.Count];
                    for(int i = 0; i < sd.Parent.PointFieldDescs.Count; i++)
                    {
                        center[i] = 0.5f;
                        scale[i]  = 0.5f;
                    }
                    sd.TransferFunction = new GTF(center, scale);

                    vtk.AddSubDataset(sd, false);
                    m_vtkSubDatasetToLoad.Enqueue(sd);
                }
            }

            else if(dataset.GetType() == typeof(CloudPointDataset))
            {
                CloudPointDataset cp = dataset as CloudPointDataset;

                //Create a new SubDataset
                lock (this)
                {
                    SubDataset sd = new SubDataset(cp, msg.OwnerID, msg.Name);
                    sd.ID = msg.SubDatasetID;

                    //GTF transfer function by default
                    float[] scale = new float[sd.Parent.PointFieldDescs.Count];
                    float[] center = new float[sd.Parent.PointFieldDescs.Count];
                    for (int i = 0; i < sd.Parent.PointFieldDescs.Count; i++)
                    {
                        center[i] = 0.5f;
                        scale[i] = 0.5f;
                    }
                    sd.TransferFunction = new GTF(center, scale);

                    cp.AddSubDataset(sd, false);
                    m_cloudPointSubDatasetToLoad.Enqueue(sd);
                }
            }
        }

        public void OnRemoveSubDataset(MessageBuffer messageBuffer, RemoveSubDatasetMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
            if (sd != null)
            {
                if(sd.SubDatasetGroup != null)
                    sd.SubDatasetGroup.RemoveSubDataset(sd);

                lock (this)
                {
                    m_subDatasetToRemove.Enqueue(sd);
                }
            }
        }


        private void AddSelectionMeshPosition()
        {
            m_tabletSelectionData.PositionList.Add(m_tabletSelectionData.Position);
            m_tabletSelectionData.RotationList.Add(m_tabletSelectionData.Rotation);

            if (m_tabletSelectionData.CurrentNewSelectionMeshIDs == null)
                return;

            if (m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData != null)
            {
                const double MINIMAL_DISTANCE = 0.005;
                if (!m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData.HasInserted ||
                   (m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData.LastInsertedPosition - m_tabletSelectionData.Position).sqrMagnitude > MINIMAL_DISTANCE * MINIMAL_DISTANCE)
                    m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData.AddPosition(m_tabletSelectionData.Position, m_tabletSelectionData.Rotation);
            }
        }

        public void OnCurrentAction(MessageBuffer messageBuffer, CurrentActionMessage msg)
        {
            lock(this)
            {
                HeadsetCurrentAction curAction = (HeadsetCurrentAction)msg.CurrentAction;
                Debug.Log($"Current action: {curAction}");

                if(curAction == HeadsetCurrentAction.REVIEWING_SELECTION)
                {
                    //Take into account the last position of the last shape if needed
                    if(NBTabletPositionIgnored > 0 && m_tabletSelectionData.CurrentCaptureID != 0 && m_tabletSelectionData.LassoPoints.Count > 0)
                        AddSelectionMeshPosition();

                    CloseTabletCurrentSelectionMesh();
                }

                //If needed, we clean everything
                if((m_currentAction == HeadsetCurrentAction.SELECTING || m_currentAction == HeadsetCurrentAction.REVIEWING_SELECTION || m_currentAction == HeadsetCurrentAction.LASSO) &&
                    curAction       != HeadsetCurrentAction.SELECTING && curAction       != HeadsetCurrentAction.REVIEWING_SELECTION && curAction       != HeadsetCurrentAction.LASSO)
                    ClearSelectionData();

                m_currentAction = curAction;
            }
        }

        public void OnLocation(MessageBuffer messageBuffer, LocationMessage msg)
        {
            //Debug.Log("Tablet position: " + msg.position[0] + " " + msg.position[1] + " " + msg.position[2] + "; "
            //        + "Tablet rotation: " + msg.rotation[0] + " " + msg.rotation[1] + " " + msg.rotation[2] + " " + msg.rotation[3]);
            lock (this)
            {
                // update current location
                m_tabletSelectionData.Position.x = msg.position[0];
                m_tabletSelectionData.Position.y = msg.position[1];
                m_tabletSelectionData.Position.z = msg.position[2];

                m_tabletSelectionData.Rotation.x = msg.rotation[1];
                m_tabletSelectionData.Rotation.y = msg.rotation[2];
                m_tabletSelectionData.Rotation.z = msg.rotation[3];
                m_tabletSelectionData.Rotation.w = msg.rotation[0];

                // record the movement
                if (m_currentAction == HeadsetCurrentAction.SELECTING && m_tabletSelectionData.LassoPoints.Count > 0)
                {
                    if (m_tabletSelectionData.CurrentCaptureID == 0)
                        AddSelectionMeshPosition();
                    m_tabletSelectionData.CurrentCaptureID = (m_tabletSelectionData.CurrentCaptureID + 1) % (NBTabletPositionIgnored + 1);
                }
            }
        }
        public void OnTabletScale(MessageBuffer messageBuffer, TabletScaleMessage msg)
        {
            //Debug.Log("Scale received : " + msg.scale + " width : " + msg.width + " height : " + msg.height + " posx : " + msg.posx + " posy : " + msg.posy);
            lock (this)
            { 
                m_tabletSelectionData.Scaling.x = msg.scale * msg.width/2;
                m_tabletSelectionData.Scaling.y = msg.scale;
                m_tabletSelectionData.Scaling.z = msg.scale * msg.height/2;
            }
        }

        public void OnLasso(MessageBuffer messageBuffer, LassoMessage msg)
        {
            lock(this)
            {
                if(m_currentAction == HeadsetCurrentAction.LASSO || m_currentAction == HeadsetCurrentAction.SELECTING)
                {
                    m_tabletSelectionData.LassoPoints.Clear();
                    for (int i = 0; i < msg.size; i += 3)
                        m_tabletSelectionData.LassoPoints.Add(new Vector2(msg.data[i], msg.data[i + 1]));
                    m_tabletSelectionData.UpdateLasso = true;
                }
            }
        }

        public void OnConfirmSelection(MessageBuffer messageBuffer, ConfirmSelectionMessage msg)
        {
            lock(this)
            {
                CloseTabletCurrentSelectionMesh();
            }
        }

        public void OnAddNewSelectionInput(MessageBuffer messageBuffer, AddNewSelectionInputMessage msg)
        {
            lock (this)
            {
                //Take into account the last position
                if (NBTabletPositionIgnored > 0 && m_tabletSelectionData.CurrentCaptureID != 0 && m_tabletSelectionData.LassoPoints.Count > 0)
                    AddSelectionMeshPosition();
                m_tabletSelectionData.CurrentCaptureID = 0; //Re init this position

                //Close the current selection mesh, useful for the computation
                CloseTabletCurrentSelectionMesh();

                if ((BooleanSelectionOperation)msg.BooleanOperation != BooleanSelectionOperation.NONE)
                {
                    m_tabletSelectionData.CurrentNewSelectionMeshIDs = new NewSelectionMeshData() { BooleanOP = (BooleanSelectionOperation)msg.BooleanOperation, VisualizationMode = VolumetricSelectionVisualizationMode.FULL_OUTLINE };

                    switch (m_tabletSelectionData.CurrentNewSelectionMeshIDs.VisualizationMode)
                    {
                        case VolumetricSelectionVisualizationMode.FULL:
                            {
                                m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData = new FullSelectionMeshData();
                                break;
                            }
                        case VolumetricSelectionVisualizationMode.WEAK_OUTLINE:
                        case VolumetricSelectionVisualizationMode.FULL_OUTLINE:
                            {
                                m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData = new OutlineSelectionMeshData(m_tabletSelectionData.CurrentNewSelectionMeshIDs.VisualizationMode == VolumetricSelectionVisualizationMode.FULL_OUTLINE);
                                break;
                            }

                        default:
                            {
                                break;
                            }
                    }


                    if (m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData != null)
                    {
                        m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData.Lasso = new List<Vector2>(m_tabletSelectionData.LassoPoints);
                        m_tabletSelectionData.CurrentNewSelectionMeshIDs.MeshData.LassoScale = m_tabletSelectionData.Scaling;
                    }
                    m_tabletSelectionData.NewSelectionMeshIDs.Add(m_tabletSelectionData.CurrentNewSelectionMeshIDs);
                    AddSelectionMeshPosition();
                }
                else
                {
                    m_tabletSelectionData.CurrentNewSelectionMeshIDs = null;
                }
            }
        }
        public void OnToggleMapVisibility(MessageBuffer messageBuffer, ToggleMapVisibilityMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
            if(sd != null)   
                sd.IsMapVisible = msg.IsVisible;
        }
               
        public void OnResetVolumetricSelection(MessageBuffer messageBuffer, ResetVolumetricSelectionMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);

            if (sd != null)
                sd.ResetVolumetricMask(false);
        }

        public void OnSubDatasetVolumetricMask(MessageBuffer messageBuffer, SubDatasetVolumetricMaskMessage msg)
        {
            Debug.Log("Received a volumetric mask message");
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
            if (sd != null && sd.VolumetricMask.Length == msg.Mask.Length)
            {
                sd.SetVolumetricMask(msg.Mask, msg.IsEnabled);
            }
        }
        
        public void OnAddLogAnnotation(MessageBuffer messageBuffer, AddLogAnnotationMessage msg)
        {
            LogAnnotationContainer container = new LogAnnotationContainer($"{Application.streamingAssetsPath}/Logs/{msg.FileName}", msg.HasHeader);
            container.SetTimeIdx(msg.TimeIdx);
            m_logAnnotations.Add(msg.LogID, container);
        }

        public void OnAddLogAnnotationPosition(MessageBuffer message, AddLogAnnotationPositionMessage msg)
        {
            lock (this)
            {
                LogAnnotationContainer annot = m_logAnnotations[msg.AnnotID];
                if (annot != null)
                {
                    LogAnnotationPosition pos = annot.BuildAnnotationPositionView();
                    annot.ParseAnnotationPosition(pos);
                    pos.ID = msg.CompID;
                }
            }
        }

        public void OnSetLogAnnotationPositionIndexes(MessageBuffer message, SetLogAnnotationPositionIndexesMessage msg)
        {
            lock (this)
            {
                LogAnnotationContainer annot = m_logAnnotations[msg.AnnotID];
                if (annot != null)
                {
                    LogAnnotationPosition pos = annot.LogAnnotationPositions.Keys.FirstOrDefault((p) => p.ID == msg.CompID);
                    if (pos != null)
                        pos.SetXYZIndices(msg.Indexes[0], msg.Indexes[1], msg.Indexes[2]);
                }
            }
        }

        public void OnLinkLogAnnotationPositionSubDataset(MessageBuffer message, LinkLogAnnotationPositionSubDatasetMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DataID, msg.SubDataID);
            if (sd == null)
                return;

            LogAnnotationContainer annot = m_logAnnotations[msg.AnnotID];
            if (annot == null)
                return;

            LogAnnotationPosition pos = annot.LogAnnotationPositions.Keys.FirstOrDefault((p) => p.ID == msg.CompID);
            if (pos == null)
                return;

            sd.AddLogAnnotationPosition(new LogAnnotationPositionInstance(annot, pos, sd, msg.DrawableID));
        }

        public void OnSetSubDatasetClipping(MessageBuffer message, SetSubDatasetClipping msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);
            if (sd == null)
                return;

            sd.SetDepthClipping(msg.MinDepthClipping, msg.MaxDepthClipping);
        }

        public void OnSetDrawableAnnotationPositionColor(MessageBuffer message, SetDrawableAnnotationPositionColorMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);
            if (sd == null)
                return;

            LogAnnotationPositionInstance annot = sd.LogAnnotationPositions.FirstOrDefault(i => i.InstanceID == msg.DrawableID);
            if (annot == null)
                return;

            annot.Color = new Color32((byte)((msg.Color >> 16) & 0xff),
                                        (byte)((msg.Color >> 8) & 0xff),
                                        (byte)((msg.Color >> 0) & 0xff),
                                        (byte)((msg.Color >> 24) & 0xff));
        }

        public void OnSetDrawableAnnotationPositionIdx(MessageBuffer message, SetDrawableAnnotationPositionIdxMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);
            if (sd == null)
                return;

            LogAnnotationPositionInstance annot = sd.LogAnnotationPositions.FirstOrDefault(i => i.InstanceID == msg.DrawableID);
            if (annot == null)
                return;

            annot.MappedIndices = msg.Indices;
        }
        
        public void OnAddSubDatasetSubjectiveGroup(MessageBuffer message, AddSubDatasetSubjectiveGroupMessage msg)
        {
            SubDataset baseSD = GetSubDataset(msg.BaseDatasetID, msg.BaseSubDatasetID);
            if(baseSD == null)
                return;

            SubDatasetSubjectiveStackedGroup svGroup = new SubDatasetSubjectiveStackedGroup(m_headsetID, (SubDatasetGroupType)msg.SubjectiveViewType, msg.SDG_ID, baseSD);
            m_sdGroups.Add(svGroup.ID, svGroup);

            m_subjViewToLoad.Enqueue(svGroup);
        }

        public void OnAddSubDatasetToSubjectiveStackedGroup(MessageBuffer message, AddSubDatasetToSubjectiveStackedGroupMessage msg)
        {
            SubDataset stackedSD = null;
            SubDataset linkedSD  = null;
            if (msg.StackedID >= 0)
            {
                stackedSD = GetSubDataset(msg.DatasetID, msg.StackedID);
                if (stackedSD == null)
                    return;
            }

            if (msg.LinkedID >= 0)
            {
                linkedSD = GetSubDataset(msg.DatasetID, msg.LinkedID);
                if (linkedSD == null)
                    return;
            }

            SubDatasetGroup sdg = m_sdGroups[msg.SDG_ID];
            if (sdg == null || !SubDatasetGroup.IsSubjective(sdg))
                return;
            SubDatasetSubjectiveStackedGroup svGroup = (SubDatasetSubjectiveStackedGroup)sdg;
            svGroup.AddSubjectiveSubDataset(stackedSD, linkedSD);
        }

        public void OnSetSubjectiveStackedGroupParameters(MessageBuffer message, SubjectiveStackedGroupGlobalParametersMessage msg)
        {
            SubDatasetGroup sdg = m_sdGroups[msg.SDG_ID];
            if (sdg == null || !SubDatasetGroup.IsSubjective(sdg))
                return;
            SubDatasetSubjectiveStackedGroup svGroup = (SubDatasetSubjectiveStackedGroup)sdg;
            svGroup.Gap         = msg.Gap;
            svGroup.IsMerged    = msg.IsMerged;
            svGroup.StackMethod = (StackMethod)msg.StackedMethod;
        }

        public void OnRemoveSubDatasetGroup(MessageBuffer message, RemoveSubDatasetGroupMessage msg)
        {
            lock (this)
            {
                SubDatasetGroup sdg = m_sdGroups[msg.SDG_ID];
                if (sdg == null)
                    return;

                if (SubDatasetGroup.IsSubjective(sdg))
                    m_subjViewToRemove.Enqueue((SubDatasetSubjectiveStackedGroup)sdg);
            }
        }

        public void OnRenameSubDataset(MessageBuffer message, RenameSubDatasetMessage msg)
        {
            SubDataset sd = GetSubDataset(msg.DatasetID, msg.SubDatasetID);
            if(sd != null)
                sd.Name = msg.Name;
        }

        #endregion

        public Color GetHeadsetColor(int headsetID)
        {
            lock(this)
            {
                HeadsetData status = GetHeadsetData(headsetID);
                if(status != null)
                    return new Color(((byte)(status.Status.Color >> 16) & 0xff) / 255.0f,
                                     ((byte)(status.Status.Color >> 8) & 0xff) / 255.0f,
                                     ((byte)(status.Status.Color >> 0) & 0xff) / 255.0f);

                return new Color(0.0f, 0.0f, 1.0f);
            }
        }


        /// <summary>
        /// Get the headset data associated to the headset ID
        /// </summary>
        /// <param name="headsetID">the headset ID to look for</param>
        /// <returns>The headset data associated to the headset ID</returns>
        public HeadsetData GetHeadsetData(int headsetID)
        {
            return m_headsetStatus.Find(x => x.Status.ID == headsetID);
        }

        public int GetHeadsetID()
        {
            return m_headsetID;
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
                    case PointingIT.WIM_RAY:
                    case PointingIT.MANUAL:
                        Destroy(m_currentPointingITGO);
                        break;
                    case PointingIT.GOGO:
                        GoGoGameObject.gameObject.SetActive(false);
                        break;
                }

                m_currentPointingIT   = null;
                m_currentPointingITGO = null;

                m_enumPointingIT = value;

                //Set the m_currentPointingIT variable (and treat the new one)
                switch(m_enumPointingIT)
                {
                    case PointingIT.NONE:
                        break;

                    case PointingIT.GOGO: //Reuse the GoGoGameObject
                        m_currentPointingIT = GoGoGameObject;
                        GoGoGameObject.CurrentSubDataset = m_datasetInAnnotation;
                        GoGoGameObject.gameObject.SetActive(true);
                        m_currentPointingITGO = GoGoGameObject.gameObject;
                        break;

                    case PointingIT.WIM: //Create a new WIM with the correct copy
                    {
                        if(m_datasetGameObjects.Count > 0)
                        {
                            ARWIM go = Instantiate(ARWIMPrefab, null);
                            go.Init(m_hdProvider, m_datasetInAnnotation, new Vector3(0.25f, 0.25f, 0.25f));
                            go.gameObject.SetActive(true);
                            m_currentPointingIT = go;
                            m_currentPointingITGO = go.gameObject;
                        }
                        break;
                    }

                    case PointingIT.WIM_RAY:
                    {
                        if(m_datasetGameObjects.Count > 0)
                        {
                            ARWIMRay go = Instantiate(ARWIMRayPrefab, null);
                            go.Init(m_hdProvider, m_datasetInAnnotation, new Vector3(0.25f, 0.25f, 0.25f));
                            go.gameObject.SetActive(true);
                            m_currentPointingIT = go;
                            m_currentPointingITGO = go.gameObject;
                        }
                        break;
                    }

                    case PointingIT.MANUAL:
                        if(m_datasetGameObjects.Count > 0)
                        {
                            ARManual go = Instantiate(ARManualPrefab, null);
                            go.Init(m_hdProvider, m_datasetInAnnotation);
                            go.gameObject.SetActive(true);
                            m_currentPointingIT = go;
                            m_currentPointingITGO = go.gameObject;
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
            lock(this)
            {
                if(pointingIT.TargetPositionIsValid)
                {
                    m_client.SendAnchorAnnotation(m_sdInAnnotation, new float[3] { pointingIT.TargetPosition.x, pointingIT.TargetPosition.y, pointingIT.TargetPosition.z });
                    m_updatePointingID = true;
                    m_waitingPointingID = PointingIT.NONE;
                }
            }
        }
        
        /// <summary>
        /// Clear the selection non-graphical data
        /// </summary>
        private void ClearSelectionData()
        {
            m_tabletSelectionData.LassoPoints.Clear(); 
            m_tabletSelectionData.PositionList.Clear();
            m_tabletSelectionData.RotationList.Clear();
            m_tabletSelectionData.NewSelectionMeshIDs.Clear();
            m_tabletSelectionData.CurrentCaptureID  = 0;
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
                if(status == ConnectionStatus.CONNECTED)
                {
                    m_wasConnected = true;
                    txt = IPAddress.Parse(((IPEndPoint)s.LocalEndPoint).Address.ToString()).ToString();
                }

                //Clear everything
                else if(m_wasConnected)
                {
                    m_connectionLost = true;
                    m_wasConnected = false;

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
                    m_vtkSubDatasetToLoad.Clear();
                    foreach (var d in m_datasets)
                    {
                        foreach (var sd in d.Value.SubDatasets)
                            if (sd.SubDatasetGroup != null)
                                sd.SubDatasetGroup.RemoveSubDataset(sd);
                        m_datasetToRemove.Enqueue(d.Value);
                    }
                    foreach (var sdg in m_sdGroups)
                        if (SubDatasetGroup.IsSubjective(sdg.Value))
                            m_subjViewToRemove.Enqueue((SubDatasetSubjectiveStackedGroup)sdg.Value);
                    txt = "";

                    //Selection
                    ClearSelectionData();

                    //Annotation logs
                    m_logAnnotations.Clear();

                    m_currentAction = HeadsetCurrentAction.NOTHING; //Reset the current action
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