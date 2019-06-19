using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

#if ENABLE_WINMD_SUPPORT
using Windows.Perception.Spatial;
#endif

using Sereno.Unity.HandDetector;

namespace Sereno
{ 
    public class ARGoGo : MonoBehaviour
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
        /// The hand current quaternion rotation (last update)
        /// </summary>
        private Quaternion m_handRotation = Quaternion.identity;

        /// <summary>
        /// The hand target position
        /// </summary>
        private Vector3 m_targetPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// Was the hand detected?
        /// </summary>
        private bool m_isHandDetected = false;

#if ENABLE_WINMD_SUPPORT
        /// <summary>
        /// The root spatial coordinate system created by Unity
        /// </summary>
        private SpatialCoordinateSystem m_spatialCoordinateSystem = null;
#endif

        /// <summary>
        /// The HandDetector provider
        /// </summary>
        private HandDetectorProvider m_hdProvider = new HandDetectorProvider();

        /// <summary>
        /// The GameObject representing the ray
        /// </summary>
        public GameObject RayObject = null;

        /// <summary>
        /// The GameObject permitting to anchor a position
        /// </summary>
        public GameObject PosObject = null;

        // Start is called before the first frame update
        void Start()
        {
            m_hdProvider.Smoothness = 0.75f;
            m_hdProvider.InitializeHandDetector();
        }

        // Update is called once per frame
        void Update()
        {
#if ENABLE_WINMD_SUPPORT
            if(m_spatialCoordinateSystem == null)
            {
                //Get the Spatial Coordinate System pointer
                IntPtr spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
                m_spatialCoordinateSystem = Marshal.GetObjectForIUnknown(spatialCoordinateSystemPtr) as SpatialCoordinateSystem;
                m_hdProvider.SetSpatialCoordinateSystem(m_spatialCoordinateSystem);
            }

            lock(m_hdProvider)
            {
                //Update the gameobjects
                RayObject.SetActive(false);
                m_handDetected = false;
                foreach (HandDetected hd in m_hdProvider.HandsDetected)
                {
                    if(hd.IsValid)
                    {
                        Vector3 distBody   = (hd.Position - Camera.main.transform.position);
                        Vector2 distBody2D = new Vector2(distBody.x, distBody.z);
                        if (distBody2D.magnitude > 0.1) //Discard detection that may be the chest "on the body"
                        {
                            RayObject.SetActive(true);

                            //Pointing
                            Vector3 anchorPoint = Camera.main.transform.position + new Vector3(0, -0.25f, 0);
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
                                
                            //Select a position!
                            PosObject.transform.localPosition = anchorPoint + m_pointingDir * (20.0f * Math.Max((anchorPoint - hd.Position).magnitude, 0.3f) - 6.0f);

                            //Update cached hand values
                            m_isHandDetected = true;
                            m_handPosition   = hd.Position;
                            m_handRotation   = RayObject.transform.localRotation;
                            m_targetPosition = PosObject.transform.localPosition;
                            break;
                        }
                    }
                }

                //Reset the pointing
                if(RayObject.activeSelf == false)
                {
                    m_pointingDir = new Vector3(0, 0, 0);
                }
            }
#endif
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
        /// The Current Quaternion hand rotation
        /// </summary>
        public Quaternion CurrentHandRotation
        {
            get { return m_handRotation; }
        }

        /// <summary>
        /// The target position
        /// </summary>
        public Vector3 TargetPosition
        {
            get { return m_targetPosition; }
        }

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
    }
}