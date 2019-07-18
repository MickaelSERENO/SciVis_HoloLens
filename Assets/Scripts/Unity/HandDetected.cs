using System.Collections.Generic;
using UnityEngine;

namespace Sereno.Unity.HandDetector
{
    /// <summary>
    /// Class representing the detection of a hand
    /// </summary>
    public class HandDetected
    {
        /// <summary>
        /// Constant value defining how many frame a hand has to be detected to be "valid"
        /// </summary>
        public static readonly int MIN_NB_FRAME_DETECTED = 7;

        /// <summary>
        /// The Hand position
        /// </summary>
        private Vector3 m_position = new Vector3(0, 0, 0);

        /// <summary>
        /// The Wrist Position
        /// </summary>
        private Vector3 m_wristPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The hand's position in the camera space
        /// </summary>
        private Vector3 m_cameraSpacePosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The wrist position in the camera space
        /// </summary>
        private Vector3 m_cameraSpaceWristPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// List of fingers related to this hand
        /// </summary>
        private List<FingerDetected> m_fingers  = new List<FingerDetected>();

        /// <summary>
        /// The uppest finger found in the image coordinate system
        /// </summary>
        private FingerDetected m_uppestFingerInImage = null;

        /// <summary>
        /// The number of consecutive frames where this hand was not detected
        /// </summary>
        private int m_nbFrameNotDetected  = 0;

        /// <summary>
        /// The number of consecutive frames where this hand was detected
        /// </summary>
        private int m_nbFrameDetected     = 0;

        /// <summary>
        /// The number of last consecutive frames where this hand was detected before that the detection failed
        /// </summary>
        private int m_lastNbFrameDetected = 0;

        /// <summary>
        /// The hand ROI
        /// </summary>
        private float[] m_roi = new float[4];

        /// <summary>
        /// Start a new detection frame. This variable is useful to modify correctly the "m_nbFrameDetected" and cie
        /// </summary>
        private bool m_newDetection = false;

        /// <summary>
        /// Have we pushed a new position during this frame?
        /// </summary>
        private bool m_hasPushedPosition = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="smoothness">The smoothness to apply when updating the position. position = (1-smoothness)*newPosition + smoothness*oldPosition</param>
        public HandDetected()
        {
        }

        /// <summary>
        /// Push a new position for this hand. You first have to set "NewDetection" to true to activate that function
        /// </summary>
        /// <param name="pos">The new hand position</param>
        /// <param name="wristPosition">The new wrist position</param>
        /// <param name="cameraSpacePos">The hand's position in the camera's space</param>
        /// <param name="cameraSpaceWristPos">the wrist's position in the camera's space</param>
        /// <param name="newROI">The new ROI</param>
        public void PushPosition(Vector3 pos, Vector3 wristPosition, Vector3 cameraSpacePos, Vector3 cameraSpaceWristPos, float[] newROI)
        {
            if(m_newDetection)
            {
                m_position = pos;
                m_wristPosition = wristPosition;
                m_roi      = newROI;
                m_cameraSpacePosition = cameraSpacePos;
                m_cameraSpaceWristPosition = cameraSpaceWristPos;
                m_nbFrameDetected++;
                m_nbFrameNotDetected = 0;
                m_hasPushedPosition = true;
            }
        }

        /// <summary>
        /// Push the fact that this Hand has been undetected
        /// </summary>
        public void PushUndetection()
        {
            if(m_newDetection)
            {
                if (m_nbFrameDetected > 0)
                    m_lastNbFrameDetected = m_nbFrameDetected;
                m_nbFrameDetected = 0;
                m_nbFrameNotDetected++;
            }
        }

        /// <summary>
        /// Detect if the new roi corresponds to that hand
        /// </summary>
        /// <param name="roi">The hand ROI to test</param>
        /// <returns>true if this is the hand that collides a given ROI</returns>
        public bool HandCollision(float[] roi)
        {
            return !(roi[0] >= m_roi[2] ||
                     roi[2] <= m_roi[0] ||
                     roi[1] >= m_roi[3] ||
                     roi[3] <= m_roi[1]);
        }

        /// <summary>
        /// The Hand position
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
            }
        }
        
        /// <summary>
        /// The Wrist position
        /// </summary>
        public Vector3 WristPosition
        {
            get
            {
                return m_wristPosition;
            }
            set
            {
                m_wristPosition = value;
            }
        }

        /// <summary>
        /// The Hand position in camera space
        /// </summary>
        public Vector3 CameraSpacePosition
        {
            get
            {
                return m_cameraSpacePosition;
            }
            set
            {
                m_cameraSpacePosition = value;
            }
        }

        /// <summary>
        /// The Wrist position in camera space
        /// </summary>
        public Vector3 CameraSpaceWristPosition
        {
            get
            {
                return m_cameraSpaceWristPosition;
            }
            set
            {
                m_cameraSpaceWristPosition = value;
            }
        }


        /// <summary>
        /// Tells if this hand was detected in the last frame
        /// </summary>
        public bool IsDetected
        {
            get
            {
                return m_hasPushedPosition;
            }
        }

        /// <summary>
        /// Is this hand position valid?
        /// </summary>
        public bool IsValid
        {
            get
            {
                return m_nbFrameDetected >= MIN_NB_FRAME_DETECTED || 
                       (m_nbFrameNotDetected < MIN_NB_FRAME_DETECTED && m_lastNbFrameDetected >= MIN_NB_FRAME_DETECTED);
            }
        }

        /// <summary>
        /// Return the hand ROI
        /// </summary>
        public float[] ROI
        {
            get
            {
                return m_roi;
            }
            set
            {
                m_roi = value;
            }
        }

        /// <summary>
        /// Set the detection value. Setting that to true permits to update the m_nbFrame. Putting that to false will discard any attempts to PushNewPosition and PushUndetection
        /// </summary>
        public bool NewDetection
        {
            get
            {
                return m_newDetection;
            }
            set
            {
                m_hasPushedPosition = false;
                m_newDetection = value;
            }
        }

        /// <summary>
        /// The list of fingers detected
        /// </summary>
        public List<FingerDetected> Fingers
        {
            get
            {
                return m_fingers;
            }
            set
            {
                m_fingers = value;
            }
        }

        /// <summary>
        /// The uppest finger found. The height property is computed in the image coordinate system (in pixels)
        /// </summary>
        public FingerDetected UppestFinger
        {
            get
            {
                return m_uppestFingerInImage;
            }
            set
            {
                m_uppestFingerInImage = value;
            }
        }
    }
}
