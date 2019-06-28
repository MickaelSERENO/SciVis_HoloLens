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
    public class ARWIM : MonoBehaviour, IPointingIT
    {
        /// <summary>
        /// The HandDetector provider
        /// </summary>
        private HandDetectorProvider m_hdProvider = null;
        
        /// <summary>
        /// The WorldInMiniature GameObject.
        /// </summary>
        private DefaultSubDatasetGameObject m_wim = null;

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
        /// The game object representing the hand position
        /// </summary>
        public GameObject HandPositionGO;
        
        /// <summary>
        /// The OnSelection event, event called when a selection has been performed
        /// The selection can be OUTSIDE the dataset, a verification in the event function must be performed then
        /// </summary>
        public event OnSelection OnSelection;

        /// <summary>
        /// Initialize the interaction technique
        /// </summary>
        /// <param name="hdProvider">The HandProvider.</param>
        /// <param name="go">The Original SubDataset.</param>
        /// <param name="wimScale">The replicas scale</param>
        public void Init(HandDetectorProvider hdProvider, DefaultSubDatasetGameObject go, Vector3 wimScale)
        {
            //Hand provider
            m_hdProvider = hdProvider;

            m_original = go;

            //Handle the transform tree
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            transform.localPosition = Camera.main.transform.position + new Vector3(0, -0.15f, 0) + 0.40f * cameraForward;
            transform.localRotation = Quaternion.identity;
            transform.localScale    = wimScale;

            m_wim = go.CreateMiniature();
            m_wim.transform.SetParent(transform, false);

            HandPositionGO.transform.SetParent(null, false);

            //Handle gestures
            m_gestureRecognizer = new GestureRecognizer();
            m_gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);
            m_gestureRecognizer.Tapped += OnTap;
            m_gestureRecognizer.StartCapturingGestures();
        }

        // Start is called before the first frame update
        void Start()
        {

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
                    List<HandDetected> validHDs = m_hdProvider.GetHandsNotOnBody(0.10f);

                    if (validHDs.Count > 0)
                    {
                        m_isHandDetected = true;
                        HandDetected hd = m_hdProvider.GetFarthestHand(validHDs);

                        //Update the position
                        Vector3 cameraLeft = -Camera.main.transform.right;
                        cameraLeft.y = 0;
                        cameraLeft.Normalize();
                        Vector3 cameraForward = Camera.main.transform.forward;
                        cameraForward.y = 0;
                        cameraForward.Normalize();

                        m_handPosition = hd.Position;
                        
                        //Adjust to fingers
                        m_handPosition += 0.06f*cameraLeft + 0.03f*Vector3.up + 0.11f*cameraForward;

                        HandPositionGO.transform.position = m_handPosition;
                        m_targetPosition = m_wim.transform.worldToLocalMatrix.MultiplyPoint3x4(m_handPosition);
                    }
                }
            }
#else
            m_targetPosition = m_wim.transform.worldToLocalMatrix.MultiplyPoint3x4(HandPositionGO.transform.position);
            m_isHandDetected = HandPositionGO.activeInHierarchy;
#endif
        }

        private void OnTap(TappedEventArgs args)
        {
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
        }

        public bool TargetPositionIsValid
        {
            get { return m_isHandDetected; }
        }

        public DefaultSubDatasetGameObject CurrentSubDataset
        {
            get { return m_original; }
        }
    }
}