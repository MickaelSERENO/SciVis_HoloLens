﻿using Sereno.Datasets;
using System;
using UnityEngine;

namespace Sereno.SciVis
{
    public class AnnotationGameObject : MonoBehaviour
    {
        private Annotation m_annot = null;

        public virtual void Init(Annotation annot)
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

        public Annotation Annotation
        {
            get => m_annot;
            set
            {
                m_annot = value;
            }
        }
    }
}
