using Sereno.SciVis;
using Sereno.Unity.HandDetector;
using UnityEngine;

namespace Sereno.Pointing
{
    public class ARWIMRay : ARWIM
    {
        /// <summary>
        /// The Ray object linking the WIM and the real dataset
        /// </summary>
        public GameObject RayObject;
        
        public override void Init(HandDetectorProvider hdProvider, DefaultSubDatasetGameObject go, Vector3 wimScale, Transform headsetTransform = null) 
        {
            base.Init(hdProvider, go, wimScale, headsetTransform);
            RayObject.transform.SetParent(null, false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(RayObject);
        }

        protected override void Update()
        {
            base.Update();

            //Link the WIM and the ray
            Vector3 wimPos                 = m_wim.transform.localToWorldMatrix.MultiplyPoint3x4(m_targetPosition);
            Vector3 rayTarget              = m_original.transform.localToWorldMatrix.MultiplyPoint3x4(m_targetPosition) - wimPos;
            RayObject.transform.up         = rayTarget.normalized;
            RayObject.transform.localScale = new Vector3(RayObject.transform.localScale.x, rayTarget.magnitude/2.0f, RayObject.transform.localScale.z);
            RayObject.transform.position   = wimPos + RayObject.transform.up*RayObject.transform.localScale.y;
        }
    }
}
