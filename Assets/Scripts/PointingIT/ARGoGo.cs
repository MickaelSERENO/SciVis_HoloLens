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

        public event OnSelection OnSelection;

        public void Init(HandDetectorProvider hd)
        {
            m_hdProvider = hd;
            m_gestureRecognizer = new GestureRecognizer();
            m_gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.Hold | GestureSettings.ManipulationTranslate);
            m_gestureRecognizer.Tapped += OnTap;
            m_gestureRecognizer.StartCapturingGestures();
        }

        // Start is called before the first frame update
        void Start()
        {}

        // Update is called once per frame
        void Update()
        {
#if ENABLE_WINMD_SUPPORT
            if (m_hdProvider != null)
            {
                lock (m_hdProvider)
                {
                    //Update the gameobjects
                    RayObject.SetActive(false);
                    List<HandDetected> validHDs = m_hdProvider.GetHandsNotOnBody(0.10f);

                    if (validHDs.Count > 0)
                    {
                        m_isHandDetected = true;
                        HandDetected hd = m_hdProvider.GetFarthestHand(validHDs);

                        RayObject.SetActive(true);

                        //Pointing
                        Vector3 anchorPoint = Camera.main.transform.localPosition + new Vector3(0, -0.20f, 0);
                        Vector3 pointDir = (hd.Position - anchorPoint).normalized;

                        if (m_pointingDir.x == 0 && m_pointingDir.y == 0 && m_pointingDir.z == 0)
                        {
                            m_pointingDir = pointDir;
                            RayObject.transform.up = m_pointingDir;
                        }
                        else
                        {
                            m_pointingDir = (1.0f - 0.8f) * pointDir + 0.8f * m_pointingDir; //Apply a strong "smoothing"
                            RayObject.transform.up = m_pointingDir;
                        }
                        RayObject.transform.localPosition = anchorPoint + RayObject.transform.up * RayObject.transform.localScale.y;
                        PosObject.transform.localPosition = anchorPoint + m_pointingDir * (20.0f * Math.Max((anchorPoint - hd.Position).magnitude, 0.3f) - 6.0f);

                        m_handPosition  = hd.Position;
                        if (m_currentSubDataset)
                            m_targetPosition = m_currentSubDataset.transform.worldToLocalMatrix.MultiplyPoint3x4(PosObject.transform.localPosition);
                    }
                }
            }
#else
            if(m_currentSubDataset != null)
            {
                m_targetPosition = m_currentSubDataset.transform.worldToLocalMatrix.MultiplyPoint3x4(PosObject.transform.localPosition);
                m_isHandDetected = RayObject.activeInHierarchy && PosObject.activeInHierarchy;
                m_pointingDir    = RayObject.transform.up;
            }
#endif
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
        }

        public bool TargetPositionIsValid
        {
            get { return m_isHandDetected; }
        }

        public DefaultSubDatasetGameObject CurrentSubDataset
        {
            get { return m_currentSubDataset; }
            set { m_currentSubDataset = value; }
        }
    }
}