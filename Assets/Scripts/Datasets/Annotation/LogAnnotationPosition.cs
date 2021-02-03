using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Sereno.Datasets.Annotation
{
    /// <summary>
    /// Represent a view from AnnotationLog to read positions. 
    /// The positions CANNOT BE MODIFIED as this represents only a view
    /// </summary>
    public class LogAnnotationPosition : LogAnnotationComponent, IEnumerable<Vector3>
    {
        /// <summary>
        /// The X column indice
        /// </summary>
        private Int32 m_xInd = -1;

        /// <summary>
        /// The Y column indice
        /// </summary>
        private Int32 m_yInd = -1;

        /// <summary>
        /// The Z column indice
        /// </summary>
        private Int32 m_zInd = -1;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ann">the AnnotationLog to "view" on. The AnnotationPosition should not outlive ann</param>
        public LogAnnotationPosition(LogAnnotation ann) : base(ann)
        {}

        /// <summary>
        /// Get the vector 3 value at position (x). When a header is not available, 0 is the default value
        /// </summary>
        public Vector3 this[int x]
        {
            get
            {   
                var nf = CultureInfo.InvariantCulture.NumberFormat;

                float xVal = 0;
                float yVal = 0;
                float zVal = 0;

                try
                {
                    xVal = (m_xInd < 0) ? 0 : float.Parse(m_ann[x][m_xInd], nf);
                }
                catch(Exception){}

                try
                {
                    yVal = (m_yInd < 0) ? 0 : float.Parse(m_ann[x][m_yInd], nf);
                }
                catch(Exception){}

                try
                {
                    zVal = (m_zInd < 0) ? 0 : float.Parse(m_ann[x][m_zInd], nf);
                }
                catch(Exception){}

                return new Vector3(xVal, yVal, zVal);
            }
        }

        /// <summary>
        /// Set the x, y, and z column indices from the LogAnnotation to look upon. 
        /// Negative values == we do not look at that component
        /// </summary>
        /// <param name="x">the X values column indice</param>
        /// <param name="y">the Y values column indice</param>
        /// <param name="z">the Z values column indice</param>
        public void SetXYZIndices(Int32 x, Int32 y, Int32 z)
        {
            var h = Headers;
            m_xInd = (x < (Int32)m_ann.NbColumns ? x : -1); 
            m_yInd = (y < (Int32)m_ann.NbColumns ? y : -1);
            m_zInd = (z < (Int32)m_ann.NbColumns ? z : -1);
            CallOnUpdateHeaders(h);
        }

        /// <summary>
        ///  Set the x, y, and z column headers from the AnnotationLog to look upon. Header not found == we do not look at that component. See AnnotationLog::indiceFromHeader for more information
        /// </summary>
        /// <param name="x">the X values column header title</param>
        /// <param name="y">the Y values column header title</param>
        /// <param name="z">the Z values column header title</param>
        public void SetXYZHeaders(String x, String y, String z) 
        {
            var h = Headers;
            m_xInd = m_ann.IndiceFromHeader(x);
            m_yInd = m_ann.IndiceFromHeader(y);
            m_zInd = m_ann.IndiceFromHeader(z);
            CallOnUpdateHeaders(h);
        }

        public IEnumerator<Vector3> GetEnumerator()
        {
            for(int i = 0; i < m_ann.NbRows; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The headers (X, Y, Z) this component is using. Setting this value calls the function SetXYZIndices
        /// </summary>
        public override List<Int32> Headers
        {
            get => new List<Int32>(new Int32[]{m_xInd, m_yInd, m_zInd});
            set => SetXYZIndices(value[0], value[1], value[2]);
        }
    }
}