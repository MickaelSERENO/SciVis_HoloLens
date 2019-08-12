using Sereno.Datasets;
using Sereno.SciVis;
using Sereno.Unity.HandDetector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.WSA.Input;

namespace Sereno.Pointing
{
    /// <summary>
    /// Class handling WIM interaction
    /// </summary>
    public class ARManual : MonoBehaviour, IPointingIT
    {
        /// <summary>
        /// The HandDetector provider
        /// </summary>
        private HandDetectorProvider m_hdProvider = null;
        
        /// <summary>
        /// The original SubDataset GameObject
        /// </summary>
        private DefaultSubDatasetGameObject m_original = null;

        /// <summary>
        /// The Gesture recognizer
        /// </summary>
        private GestureRecognizer m_gestureRecognizer = null;

        /// <summary>
        /// The hand current position (last update)
        /// </summary>
        private Vector3 m_handPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The target position
        /// </summary>
        private Vector3 m_targetPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// Was the hand detected?
        /// </summary>
        private bool m_isHandDetected = false;

        /// <summary>
        /// The headset start position when the interaction technique was created
        /// </summary>
        private Vector3 m_headsetStartPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The headset orientation when the interaction technique was created
        /// </summary>
        protected Quaternion m_headsetStartOrientation = Quaternion.identity;

        /// <summary>
        /// The headset transform representing the headset
        /// </summary>
        protected Transform m_headsetTransform;

        /// <summary>
        /// The game object representing the hand position
        /// </summary>
        public GameObject HandPositionGO;
        
        /// <summary>
        /// The OnSelection event, event called when a selection has been performed
        /// The selection can be OUTSIDE the dataset, a verification in the event function must be performed then
        /// </summary>
        public event OnSelection OnSelection;

        private void Awake()
        {
            if (m_headsetTransform == null)
                m_headsetTransform = Camera.main.transform;
        }

        /// <summary>
        /// Initialize the interaction technique
        /// </summary>
        /// <param name="hdProvider">The HandProvider.</param>
        /// <param name="go">The Original SubDataset.</param>
        /// <param name="headsetTransform">The transform representing the head. If null, we take Camera.main.transform gameobject</param>
        public void Init(HandDetectorProvider hdProvider, DefaultSubDatasetGameObject go, Transform headsetTransform=null)
        {
            //Hand provider
            m_hdProvider = hdProvider;

            m_original = go;

            if (headsetTransform == null)
                headsetTransform = Camera.main.transform;

            //Handle the transform tree
            Vector3 cameraForward = headsetTransform.forward;
            cameraForward.y = 0;
            transform.localPosition = headsetTransform.position + new Vector3(0, -0.15f, 0) + 0.45f * cameraForward;
            transform.localRotation = Quaternion.identity;

            HandPositionGO.transform.SetParent(null, false);

            //Handle gestures
            m_gestureRecognizer = new GestureRecognizer();
            m_gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);
            m_gestureRecognizer.Tapped += OnTap;
            m_gestureRecognizer.StartCapturingGestures();

            m_headsetTransform = headsetTransform;

            HeadsetStartOrientation = headsetTransform.rotation;
            HeadsetStartPosition    = headsetTransform.position;
        }

        void OnDestroy()
        {
            Destroy(HandPositionGO);
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        protected virtual void OnEnable()
        {
            HandPositionGO.SetActive(true);
        }

        protected virtual void OnDisable()
        {
            HandPositionGO.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
#if ENABLE_WINMD_SUPPORT
            if (m_hdProvider != null)
            {
                lock (m_hdProvider)
                {
                    //Get the valid hands
                    List<HandDetected> validHDs = m_hdProvider.GetHandsNotOnBody(0.15f);

                    if (validHDs.Count > 0)
                    {
                        m_isHandDetected = true;
                        HandDetected hd = m_hdProvider.GetOptimalHand(validHDs);

                       m_handPosition = hd.Position;

                        //Adjust to fingers
                        m_handPosition  += PointingFunctions.GetFingerOffset(m_headsetTransform, m_hdProvider.Handedness);
                        m_targetPosition = m_original.transform.worldToLocalMatrix.MultiplyPoint3x4(m_handPosition);
                    }
                }
            }
#endif
            //We do this because it permits to use the same code for both the local user and the remote collaborators embodiement
            if (m_original != null)
                HandPositionGO.transform.position = m_original.transform.localToWorldMatrix.MultiplyPoint3x4(m_targetPosition);
        }

        private void OnTap(TappedEventArgs args)
        {
            if (!gameObject.activeInHierarchy)
                return;
            if(!m_isHandDetected)
                args.sourcePose.TryGetPosition(out m_handPosition);
            OnSelection?.Invoke(this);
        }

        /// <summary>
        /// The HandDetector provider
        /// </summary>
        public HandDetectorProvider HDProvider
        {
            get { return m_hdProvider; }
        }

        /// <summary>
        /// Is the hand detected?
        /// </summary>
        public bool IsHandDetected
        {
            get { return m_isHandDetected; }
        }

        /// <summary>
        /// The corresponding Hand Position
        /// </summary>
        public Vector3 HandPosition
        {
            get { return m_handPosition; }
        }

        public Vector3 TargetPosition
        {
            get { return m_targetPosition; }
            set { m_targetPosition = value; }
        }

        public bool TargetPositionIsValid
        {
            get { return m_isHandDetected && m_targetPosition.x <= 0.5f && m_targetPosition.x >= -0.5f &&
                                             m_targetPosition.y <= 0.5f && m_targetPosition.y >= -0.5f &&
                                             m_targetPosition.z <= 0.5f && m_targetPosition.z >= -0.5f; }
        }

        public DefaultSubDatasetGameObject CurrentSubDataset
        {
            get { return m_original; }
        }

        public Transform HeadsetTransform
        {
            get { return m_headsetTransform; }
            set { m_headsetTransform = value; }
        }

        public Vector3 HeadsetStartPosition
        {
            get => m_headsetStartPosition;
            set => m_headsetStartPosition = value;
        }

        public Quaternion HeadsetStartOrientation
        {
            get => m_headsetStartOrientation;
            set => m_headsetStartOrientation = value;
        }
    }
}