using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT
using Sereno.HandDetector;
using HandDetector_Native;
using Windows.Perception.Spatial;
#endif

namespace Sereno.Unity.HandDetector
{
    /// <summary>
    /// The Handedness enumerations
    /// </summary>
    public enum Handedness
    {
        NONE = -1,
        LEFT = 0,
        RIGHT = 1
    }

    public class HandDetectorProvider
#if ENABLE_WINMD_SUPPORT
        : IHDMediaSinkClbk
#endif
    {

#if ENABLE_WINMD_SUPPORT
        /// <summary>
        /// The root spatial coordinate system created by Unity
        /// </summary>
        private SpatialCoordinateSystem m_spatialCoordinateSystem = null;

        /// <summary>
        /// The hand detector object
        /// </summary>
        private Sereno.HandDetector.HandDetector m_handDetector = null;
#endif

        /// <summary>
        /// The default smoothness to apply when updating hands' position.
        /// </summary>
        public static readonly float HAND_Smoothness = 0.7f;

        /// <summary>
        /// List of hand detected
        /// </summary>
        private List<HandDetected> m_handsDetected = new List<HandDetected>();

        /// <summary>
        /// The current finger's position
        /// </summary>
        private Vector3 m_fingerPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The smoothness to apply when updating hands' position. Default: HAND_Smoothness
        /// </summary>
        private float m_smoothness = HAND_Smoothness;

        /// <summary>
        /// The handedness
        /// </summary>
        private Handedness m_handedness = Handedness.RIGHT;

        /// <summary>
        /// Initialize the hand detector algorithm and launch the hand tracking
        /// </summary>
        /// <returns>The asynchronous task created</returns>
#if ENABLE_WINMD_SUPPORT
        public async Task InitializeHandDetector()
#else
        public void InitializeHandDetector()
#endif
        {
#if ENABLE_WINMD_SUPPORT
            //Create the HandDetector object
            m_handDetector = await Sereno.HandDetector.HandDetector.CreateAsync(null);
            await m_handDetector.InitializeAsync(this);
#endif
        }

        /// <summary>
        /// Set the spatial coordinate system to convert the hands' position in. Only available in WINMD systems
        /// 
        /// Example:
        /// IntPtr spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
        /// m_spatialCoordinateSystem = Marshal.GetObjectForIUnknown(spatialCoordinateSystemPtr) as SpatialCoordinateSystem;
        /// m_hdProvider.SetSpatialCoordinateSystem(m_spatialCoordinateSystem);
        /// </summary>
        /// <param name="sys">The spatial coordinate system to convert the hands' position in</param>
#if ENABLE_WINMD_SUPPORT
        public void SetSpatialCoordinateSystem(SpatialCoordinateSystem sys)
        {
            lock(this)
                m_spatialCoordinateSystem = sys;
        }
#endif

#if ENABLE_WINMD_SUPPORT
        public void OnHandUpdate(CameraParameter cameraParam, SpatialCoordinateSystem CoordinateSystem, IList<Hand> hands)
        {
            lock (this)
            {
                if (m_spatialCoordinateSystem != null)
                {
                    //Start a new frame
                    foreach (HandDetected hand in m_handsDetected)
                        hand.NewDetection = true;

                    //For each detected hand
                    foreach (Hand hand in hands)
                    {
                        //Get the needed transformation matrices to convert hand in image space to camera and world space
                        System.Numerics.Matrix4x4? cameraToWorld = CoordinateSystem.TryGetTransformTo(m_spatialCoordinateSystem).Value;
                        System.Numerics.Matrix4x4 viewToCamera;
                        System.Numerics.Matrix4x4.Invert(cameraParam.CameraViewTransform, out viewToCamera);
                        if (cameraToWorld == null)
                            cameraToWorld = System.Numerics.Matrix4x4.Identity;

                        //Hand in camera space
                        System.Numerics.Vector4 handVecCamera = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(hand.PalmX, hand.PalmY, hand.PalmZ, 1.0f), viewToCamera);
                        Vector3 unityHandCamera = new Vector3(handVecCamera.X, handVecCamera.Y, handVecCamera.Z) / handVecCamera.W;

                        //Wrist in camera space
                        System.Numerics.Vector4 wristVecCamera = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(hand.WristX, hand.WristY, hand.WristZ, 1.0f), viewToCamera);
                        Vector3 unityWristCamera = new Vector3(wristVecCamera.X, wristVecCamera.Y, wristVecCamera.Z) / wristVecCamera.W;
                                               
                        //Add offsets in the ROI
                        float[] roi = new float[4];
                        roi[0] = hand.WristROIMinX - 10;
                        roi[1] = hand.WristROIMinY - 10;
                        roi[2] = hand.WristROIMaxX + 10;
                        roi[3] = hand.WristROIMaxY + 10;

                        //check if we already know it
                        bool created = false;
                        HandDetected handDetected = null;
                        foreach (HandDetected hd in m_handsDetected)
                        {
                            if (!hd.IsDetected && hd.HandCollision(roi) && (hd.CameraSpacePosition - unityHandCamera).magnitude <= 0.10) //Test the ROI and the magnitude in the position (no more than 5 cm)
                            {
                                handDetected = hd;
                                break;
                            }
                        }

                        //If not, this is a new hand!
                        if (handDetected == null)
                        {
                            handDetected = new HandDetected();
                            handDetected.NewDetection = true;
                            m_handsDetected.Add(handDetected);
                            created = true;
                        }

                        float smoothness = m_smoothness;
                        if (created == true)
                            smoothness = 0.0f;

                        //Smooth the hand
                        Vector3 smoothPosCamera = unityHandCamera * (1.0f - smoothness) + handDetected.CameraSpacePosition * smoothness; //Smooth the position
                        System.Numerics.Vector4 handVec = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(smoothPosCamera.x, smoothPosCamera.y, smoothPosCamera.z, 1.0f), cameraToWorld.Value);
                        Vector3 unityHandVec = new Vector3(handVec.X, handVec.Y, -handVec.Z) / handVec.W;

                        //Smooth the wrist
                        Vector3 smoothWristCamera = unityWristCamera * (1.0f - smoothness) + handDetected.CameraSpaceWristPosition * smoothness; //Smooth the position
                        System.Numerics.Vector4 wristVec = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(smoothWristCamera.x, smoothWristCamera.y, smoothWristCamera.z, 1.0f), cameraToWorld.Value);
                        Vector3 unityWristVec = new Vector3(wristVec.X, wristVec.Y, -wristVec.Z) / wristVec.W;

                        handDetected.PushPosition(unityHandVec, unityWristVec, smoothPosCamera, smoothWristCamera, roi);

                        //Clear fingers information
                        handDetected.Fingers.Clear();
                        handDetected.UppestFinger = null;

                        FingerDetected formerFinger = handDetected.UppestFinger;

                        if (hand.Fingers.Count > 0)
                        {
                            //Conver each fingers detected
                            foreach (Finger f in hand.Fingers)
                            {
                                //Register the finger position
                                System.Numerics.Vector4 fingerVec = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(f.TipX, f.TipY, f.TipZ, 1.0f), viewToCamera);
                                fingerVec = System.Numerics.Vector4.Transform(fingerVec, cameraToWorld.Value);
                                Vector3 unityFingerVec = new Vector3(fingerVec.X, fingerVec.Y, -fingerVec.Z) / fingerVec.W;
                                handDetected.Fingers.Add(new FingerDetected(unityFingerVec));
                            }

                            //Detect the uppest finger
                            float minFY = hand.Fingers[0].TipY;
                            handDetected.UppestFinger = handDetected.Fingers[0];

                            for (int i = 1; i < handDetected.Fingers.Count; i++)
                            {
                                if (minFY > hand.Fingers[0].TipY)
                                {
                                    minFY = hand.Fingers[0].TipY;
                                    handDetected.UppestFinger = handDetected.Fingers[i];
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < m_handsDetected.Count; i++)
                {
                    HandDetected hd = m_handsDetected[i];
                    //Handle non detected hands
                    if (!hd.IsDetected)
                    {
                        hd.PushUndetection();

                        //Delete the non valid hands
                        if (!hd.IsValid)
                        {
                            m_handsDetected.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the hands that are not on the body.
        /// </summary>
        /// <param name="minMagnitude">The minimum magnitude between the camera and the valid hands along the z and x axis</param>
        /// <param name="maxVerticalDistance">The maximum vertical distance between the hand and the camera allowed</param>
        /// <returns>The list of hands not on the body</returns>
        public List<HandDetected> GetHandsNotOnBody(float minMagnitude, float maxVerticalDistance=0.60f)
        {
            List<HandDetected> validHDs = new List<HandDetected>();

            foreach (HandDetected hd in HandsDetected)
            {
                if (hd.IsValid)
                {
                    Vector3 distBody = (hd.Position - Camera.main.transform.position);
                    Vector2 distBody2D = new Vector2(distBody.x, distBody.z);
                    if (distBody2D.magnitude > minMagnitude && distBody.y >= -maxVerticalDistance) //Discard detection that may be the chest "on the body" or too low
                    {
                        validHDs.Add(hd);
                    }
                }
            }

            return validHDs;
        }

        /// <summary>
        /// Get the farthest hand along the camera up axis from a list of valids hands
        /// </summary>
        /// <param name="validHDs">The valid hands</param>
        /// <returns>The farthest hands or null if the list is empty</returns>
        public HandDetected GetFarthestHand(List<HandDetected> validHDs)
        {
            if (validHDs.Count == 0)
                return null;

            Vector3 forward = Camera.main.transform.forward;
            forward = (new Vector3(forward.x, 0.0f, forward.z)).normalized;

            HandDetected hd = validHDs[0];
            float posZ = Vector3.Dot(forward, validHDs[0].Position - Camera.main.transform.position);
            for (int i = 1; i < validHDs.Count; i++)
            {
                float tempPosZ = Vector3.Dot(forward, validHDs[i].Position - Camera.main.transform.position);
                if (posZ < tempPosZ)
                {
                    posZ = tempPosZ;
                    hd = validHDs[i];
                }
            }
            return hd;
        }

        /// <summary>
        /// Get the optimal hand given a list of valid hands
        /// </summary>
        /// <param name="validHDs">The list of valid Hands</param>
        /// <returns></returns>
        public HandDetected GetOptimalHand(List<HandDetected> validHDs)
        {
            if (validHDs.Count == 0)
                return null;

            float optimalLimit = 0.40f;

            Vector3 forward = Camera.main.transform.forward;
            forward = (new Vector3(forward.x, 0.0f, forward.z)).normalized;

            HandDetected hd = validHDs[0];

            float maxPosZ = Vector3.Dot(forward, validHDs[0].Position - Camera.main.transform.position);
            float minPosZ = float.MaxValue;


            for (int i = 1; i < validHDs.Count; i++)
            {
                bool computeMin = false;
                float tempPosZ = Vector3.Dot(forward, validHDs[i].Position - Camera.main.transform.position);
                if (maxPosZ < tempPosZ)
                {
                    maxPosZ = tempPosZ;
                    if (tempPosZ < optimalLimit) //Take the farthest between a range of 0 and the optimal limit
                    {
                        hd = validHDs[i];
                    }
                    else
                        computeMin = true;
                }
                else if (tempPosZ < minPosZ && tempPosZ >= optimalLimit)
                    computeMin = true;

                if (computeMin)
                {
                    minPosZ = tempPosZ;
                    hd = validHDs[i];
                }
            }
            return hd;
        }
#endif

        /// <summary>
        /// The hands currently in detection.
        /// Lock this object before accessing this value
        /// </summary>
        public List<HandDetected> HandsDetected
        {
            get { return m_handsDetected; }
        }

        /// <summary>
        /// The smoothness to apply when updating the hands' position
        /// </summary>
        public float Smoothness
        {
            get { return m_smoothness; }
            set { m_smoothness = value; }
        }

        /// <summary>
        /// The user's handedness
        /// </summary>
        public Handedness Handedness
        {
            get { return m_handedness; }
            set { m_handedness = value; }
        }
    }
}
