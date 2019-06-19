using System.Collections.Generic;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Sereno.HandDetector;
using HandDetector_Native;
using Windows.Perception.Spatial;
#endif

namespace Sereno.Unity.HandDetector
{
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
        private HandDetector.HandDetector m_handDetector = null;
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
            m_handDetector = await HandDetector.HandDetector.CreateAsync(null);
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
                        //Add offsets in the ROI
                        float[] roi = new float[4];
                        roi[0] = hand.WristROIMinX - 10;
                        roi[1] = hand.WristROIMinY - 10;
                        roi[2] = hand.WristROIMaxX + 10;
                        roi[3] = hand.WristROIMaxY + 10;

                        //check if we already know it
                        HandDetected handDetected = null;
                        foreach (HandDetected hd in m_handsDetected)
                        {
                            if (!hd.IsDetected && hd.HandCollision(roi))
                            {
                                handDetected = hd;
                                break;
                            }
                        }

                        //If not, this is a new hand!
                        if (handDetected == null)
                        {
                            handDetected = new HandDetected(m_smoothness);
                            handDetected.NewDetection = true;
                            m_handsDetected.Add(handDetected);
                        }

                        //Compute the hand 3D position in the left-handed coordinate system
                        System.Numerics.Matrix4x4? cameraToWorld = CoordinateSystem.TryGetTransformTo(m_spatialCoordinateSystem).Value;
                        System.Numerics.Matrix4x4 viewToCamera;
                        System.Numerics.Matrix4x4.Invert(cameraParam.CameraViewTransform, out viewToCamera);
                        if (cameraToWorld == null)
                            cameraToWorld = System.Numerics.Matrix4x4.Identity;

                        System.Numerics.Vector4 handVec = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(hand.PalmX, hand.PalmY, hand.PalmZ, 1.0f), viewToCamera);
                        handVec = System.Numerics.Vector4.Transform(handVec, cameraToWorld.Value);
                        Vector3 unityHandVec = new Vector3(handVec.X, handVec.Y, -handVec.Z) / handVec.W;

                        System.Numerics.Vector4 wristVec = System.Numerics.Vector4.Transform(new System.Numerics.Vector4(hand.WristX, hand.WristY, hand.WristZ, 1.0f), viewToCamera);
                        wristVec = System.Numerics.Vector4.Transform(wristVec, cameraToWorld.Value);
                        Vector3 unityWristVec = new Vector3(wristVec.X, wristVec.Y, -wristVec.Z) / wristVec.W;

                        handDetected.PushPosition(unityHandVec, unityWristVec, roi);

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

                            //Apply smoothness on this particular finger
                            if (formerFinger != null)
                                handDetected.UppestFinger.Position = (1.0f - m_smoothness) * handDetected.UppestFinger.Position + m_smoothness * formerFinger.Position;

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
            set { m_smoothness = value; foreach (var hd in m_handsDetected) hd.Smoothness = value; }
        }
    }
}
