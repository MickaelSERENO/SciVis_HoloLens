using Sereno.Datasets;
using Sereno.SciVis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sereno.Pointing
{
    /// <summary>
    /// This class handles the workspace awareness for pointing technique per collabroator
    /// </summary>
    public class ARCollabPointingIT : MonoBehaviour
    {
        /// <summary>
        /// The current GameObject being used
        /// </summary>
        private GameObject m_currentGO        = null;

        /// <summary>
        /// The current Pointing Interaction technique object being used
        /// </summary>
        private IPointingIT m_currentITObj = null;

        /// <summary>
        /// The Current SubDataset GameObject to display
        /// </summary>
        private DefaultSubDatasetGameObject m_currentSdGameObject = null;

        /// <summary>
        /// The current interaction technique in use
        /// </summary>
        private PointingIT m_currentIT        = PointingIT.NONE;

        /// <summary>
        /// The attached headset's transform object
        /// </summary>
        private Transform  m_headsetTransform = null;

        /// <summary>
        /// The targeted position
        /// </summary>
        private Vector3 m_targetPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The position of the headset when the interaction technique started
        /// </summary>
        private Vector3 m_headsetStartPosition = new Vector3(0, 0, 0);

        /// <summary>
        /// The headset orientation when the interaction technique was created
        /// </summary>
        protected Quaternion m_headsetStartOrientation = Quaternion.identity;

        /// <summary>
        /// AR GOGO Technique prefab
        /// </summary>
        public ARGoGo   ARGoGoPrefab;

        /// <summary>
        /// AR WIM Technique prefab
        /// </summary>
        public ARWIM    ARWIMPrefab;

        /// <summary>
        /// AR WIM+Ray Technique prefab
        /// </summary>
        public ARWIMRay ARWIMRayPrefab;

        /// <summary>
        /// AR Manual technique prefab
        /// </summary>
        public ARManual ARManualPrefab;

        /// <summary>
        /// Recreate the interaction technique game object. This can also destroy any interaction game object if nothing is to show
        /// </summary>
        private void RecreateIT()
        {
            if (m_currentGO != null)
            {
                Destroy(m_currentGO);
                m_currentGO    = null;
                m_currentITObj = null;
            }

            if (m_currentIT == PointingIT.NONE || m_currentSdGameObject == null || m_headsetTransform == null)
                return;

            switch (m_currentIT)
            {
                case PointingIT.NONE:
                    break;

                case PointingIT.GOGO:
                {
                    ARGoGo go = Instantiate(ARGoGoPrefab);
                    go.CurrentSubDataset = m_currentSdGameObject;
                    go.HeadsetTransform = m_headsetTransform;
                    go.gameObject.SetActive(true);
                    m_currentGO = go.gameObject;
                    m_currentITObj = go;
                    break;
                }
                case PointingIT.WIM: //Create a new WIM with the correct copy
                {
                    ARWIM go = Instantiate(ARWIMPrefab);
                    go.Init(null, m_currentSdGameObject, new Vector3(0.30f, 0.30f, 0.30f), m_headsetTransform);
                    go.gameObject.SetActive(true);
                    m_currentGO = go.gameObject;
                    m_currentITObj = go;
                    break;
                }

                case PointingIT.WIM_RAY:
                {
                    ARWIMRay go = Instantiate(ARWIMRayPrefab);
                    go.Init(null, m_currentSdGameObject, new Vector3(0.30f, 0.30f, 0.30f), m_headsetTransform);
                    go.gameObject.SetActive(true);
                    m_currentGO = go.gameObject;
                    m_currentITObj = go;
                    break;
                }

                case PointingIT.MANUAL:
                {
                    ARManual go = Instantiate(ARManualPrefab);
                    go.Init(null, m_currentSdGameObject, m_headsetTransform);
                    go.gameObject.SetActive(true);
                    m_currentGO = go.gameObject;
                    m_currentITObj = go;
                    break;
                }
            }

            //Reset correctly all the values
            m_currentGO.transform.SetParent(null, false);
            
            m_currentITObj.HeadsetStartPosition    = m_headsetStartPosition;
            m_currentITObj.HeadsetStartOrientation = m_headsetStartOrientation;
            m_currentGO.SetActive(gameObject.activeSelf);
        }

        private void OnDisable()
        {
            if (m_currentGO != null)
                m_currentGO.SetActive(false);
        }

        private void OnEnable()
        {
            if (m_currentGO != null)
                m_currentGO.SetActive(true);
        }

        /// <summary>
        /// The Headset Transform object associated to the interaction technique
        /// </summary>
        public Transform HeadsetTransform
        {
            get { return m_headsetTransform; }
            set
            {
                if (m_headsetTransform != value)
                {
                    m_headsetTransform = value;
                    RecreateIT();
                }
            }
        }

        /// <summary>
        /// The current pointing interaction technique in use
        /// </summary>
        public PointingIT PointingIT
        {
            get { return m_currentIT; }
            set
            {
                if(m_currentIT != value)
                {
                    m_currentIT = value;
                    RecreateIT();
                }
            }
        }

        /// <summary>
        /// The target position of the interaction technique in the subdataset local space
        /// </summary>
        public Vector3 TargetPosition
        {
            get { return m_targetPosition; }
            set
            {
                m_targetPosition = value;
                if (m_currentITObj != null)
                    m_currentITObj.TargetPosition = value;
            }
        }

        /// <summary>
        /// The SubDataset GameObject attached to the interaction technique being used
        /// </summary>
        public DefaultSubDatasetGameObject SubDatasetGameObject
        {
            get { return m_currentSdGameObject; }
            set
            {
                if(m_currentSdGameObject != value)
                {
                    m_currentSdGameObject = value;
                    RecreateIT();
                }
            }
        }

        /// <summary>
        /// The position of the headset when this interaction technique started
        /// </summary>
        public Vector3 HeadsetStartPosition
        {
            get { return m_headsetStartPosition; }
            set
            {
                m_headsetStartPosition = value;
                if (m_currentITObj != null)
                    m_currentITObj.HeadsetStartPosition = value;
            }
        }

        /// <summary>
        /// The orientation of the headset when this interaction technique started
        /// </summary>
        public Quaternion HeadsetStartOrientation
        {
            get { return m_headsetStartOrientation; }
            set
            {
                m_headsetStartOrientation = value;
                if (m_currentITObj != null)
                    m_currentITObj.HeadsetStartOrientation = value;
            }
        }
    }
}
