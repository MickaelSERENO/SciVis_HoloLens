using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.XR.WSA.Input;
using Sereno.SciVis;

#if ENABLE_WINMD_SUPPORT
using Windows.Perception.Spatial;
#endif

using Sereno.Unity.HandDetector;

namespace Sereno.Pointing
{ 
    public class ARGoGo : MonoBehaviour, IPointingIT
    {
        /// <summary>
        /// The current pointing direction
        /// </summary>
        private Vector3 m_pointingDir = new Vector3(0, 0, 0);

        /// <summary>
        /// The hand current position (last update)
        /// </summary>
        private Vector3 m_handPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The hand target position (local dataset space)
        /// </summary>
        private Vector3 m_targetPosition = new Vector3(0, 0, 0);
        
        /// <summary>
        /// Was the hand detected?
        /// </summary>
        private bool m_isHandDetected = false;

        /// <summary>
        /// The HandDetector provider
        /// </summary>
        private HandDetectorProvider m_hdProvider = null;

        /// <summary>
        /// The headset start position when the interaction technique was created
        /// </summary>
        private Vector3 m_headsetStartPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The GameObject representing the ray
        /// </summary>
        public GameObject RayObject = null;

        /// <summary>
        /// The GameObject permitting to anchor a position
        /// </summary>
        public GameObject PosObject = null;

        /// <summary>
        /// The Gesture recognizer.
        /// </summary>
        private GestureRecognizer m_gestureRecognizer = null;

        /// <summary>
        /// The Current subdataset being targeted
        /// </summary>
        private DefaultSubDatasetGameObject m_currentSubDataset = null;

        /// <summary>
        /// The headset transform. By default it will be the main camera
        /// </summary>
        private Transform m_headsetTransform = null;

        public event OnSelection OnSelection;

        private void Awake()
        {
            if(m_headsetTransform == null)
                m_headsetTransform = Camera.main.transform;
        }

        public void Init(HandDetectorProvider hd)
        {
            m_hdProvider = hd;
            m_gestureRecognizer = new GestureRecognizer();
            m_gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.Hold | GestureSettings.ManipulationTranslate);
            m_gestureRecognizer.Tapped += OnTap;
            m_gestureRecognizer.StartCapturingGestures();

            m_headsetStartPosition = m_headsetTransform.transform.position;
        }

        void Start()
        {}

        void Update()
        {
#if ENABLE_WINMD_SUPPORT
            if (m_hdProvider != null)
            {
                lock (m_hdProvider)
                {
                    List<HandDetected> validHDs = m_hdProvider.GetHandsNotOnBody(0.10f);

                    if (validHDs.Count > 0)
                    {
                        m_isHandDetected = true;
                        HandDetected hd = m_hdProvider.GetFarthestHand(validHDs);

                        RayObject.SetActive(true);

                        //Pointing
                        Vector3 anchorPoint = Camera.main.transform.position + new Vector3(0, -0.20f, 0);
                        Vector3 pointDir = (hd.Position - anchorPoint).normalized;

                        if (m_pointingDir.x == 0 && m_pointingDir.y == 0 && m_pointingDir.z == 0)
                            m_pointingDir = pointDir;
                        else
                            m_pointingDir = (1.0f - 0.8f) * pointDir + 0.8f * m_pointingDir; //Apply a strong "smoothing"

                        m_handPosition   = hd.Position;
                        m_targetPosition = anchorPoint + m_pointingDir * (20.0f * Math.Max((anchorPoint - hd.Position).magnitude, 0.3f) - 6.0f);
                        m_targetPosition = m_currentSubDataset.transform.worldToLocalMatrix.MultiplyPoint3x4(m_targetPosition);
                    }
                }
            }
#endif
            //We do this because it permits to use the same code for both the local user and the remote collaborators embodiement
            if (m_currentSubDataset)
            {
                Vector3 anchorPoint = m_headsetTransform.position + new Vector3(0, -0.20f, 0);
                Vector3 targetPos = m_currentSubDataset.transform.localToWorldMatrix.MultiplyPoint3x4(m_targetPosition);

                Vector3 rayVec = targetPos - anchorPoint;
                rayVec = rayVec.normalized;

                RayObject.transform.up = rayVec;
                RayObject.transform.localPosition = anchorPoint + RayObject.transform.up * RayObject.transform.localScale.y;
                PosObject.transform.localPosition = targetPos;
            }
        }

        /// <summary>
        /// Method called when the device has recognized a tap event
        /// </summary>
        /// <param name="args"></param>
        private void OnTap(TappedEventArgs args)
        {
            OnSelection?.Invoke(this);
        }

        /// <summary>
        /// The current hand position
        /// </summary>
        public Vector3 CurrentHandPosition
        {
            get { return m_handPosition; }
        }

        /// <summary>
        /// The current hand pointing direction
        /// </summary>
        public Vector3 CurrentPointingDirection
        {
            get { return m_pointingDir; }
        }
        
        /// <summary>
        /// Is the hand detected?
        /// </summary>
        public bool IsHandDetected
        {
            get { return m_isHandDetected; }
        }

        /// <summary>
        /// The HandDetector provider
        /// </summary>
        public HandDetectorProvider HDProvider
        {
            get { return m_hdProvider; }
        }
        
        public Vector3 TargetPosition
        {
            get { return m_targetPosition; }
            set { m_targetPosition = value; }
        }

        public bool TargetPositionIsValid
        {
            get
            {
                return m_isHandDetected && m_targetPosition.x <= 0.5f && m_targetPosition.x >= -0.5f &&
                                           m_targetPosition.y <= 0.5f && m_targetPosition.y >= -0.5f &&
                                           m_targetPosition.z <= 0.5f && m_targetPosition.z >= -0.5f;
            }
        }

        public DefaultSubDatasetGameObject CurrentSubDataset
        {
            get { return m_currentSubDataset; }
            set { m_currentSubDataset = value; }
        }

        public Transform HeadsetTransform
        {
            get { return m_headsetTransform; }
            set { m_headsetTransform = value; }
        }

        public Vector3 HeadsetStartPosition { get => m_headsetStartPosition; set => m_headsetStartPosition = value; }
    }
}