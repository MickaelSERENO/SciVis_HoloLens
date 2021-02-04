using Sereno.Datasets;
using Sereno.Datasets.Annotation;
using System;
using UnityEngine;

namespace Sereno.SciVis
{
    public class CanvasAnnotationGameObject : MonoBehaviour
    {
        private CanvasAnnotation m_annot = null;

        public virtual void Init(CanvasAnnotation annot)
        {
            Annotation = annot;
            transform.localPosition = new Vector3(annot.LocalPosition[0],
                                                  annot.LocalPosition[1],
                                                  annot.LocalPosition[2]);
        }

        void Start()
        {
            
        }

        void Update()
        {
            
        }

        public CanvasAnnotation Annotation
        {
            get => m_annot;
            set
            {
                m_annot = value;
            }
        }
    }
}
