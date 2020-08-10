using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

using Sereno.Datasets;
using Sereno.SciVis;

namespace Sereno.SciVis
{
    /// <summary>
    /// StructuredGrid of VTK Dataset Model
    /// </summary>
    public class VTKUnityStructuredGrid
    {
        /// <summary>
        /// The list of the small multiples
        /// </summary>
        private List<VTKUnitySmallMultiple> m_smallMultiples;

        /// <summary>
        /// The 3D dimensions of this StructuredGrid
        /// </summary>
        private Vector3Int m_dimensions;

        /// <summary>
        /// The 3D spacing of this StructuredGrid
        /// </summary>
        private Vector3 m_spacing;

        /// <summary>
        /// The point descriptor of the structured grid
        /// </summary>
        private VTKStructuredPoints m_ptsDesc;

        /// <summary>
        /// The desired density
        /// </summary>
        private UInt32 m_desiredDensity;

        /// <summary>
        /// The data provider to use
        /// </summary>
        private IDataProvider m_dataProvider;

        /// <summary>
        /// The Dataset bound to this SubDataset
        /// </summary>
        public VTKDataset m_dataset;

        public unsafe VTKUnityStructuredGrid (VTKDataset vtkDataset, UInt32 desiredDensity, IDataProvider dataProvider)
        {
            m_dataset = vtkDataset;
            m_desiredDensity = desiredDensity;
            m_dataProvider   = dataProvider;

            VTKParser parser = m_dataset.Parser;
            if(parser.GetDatasetType() != VTKDatasetType.VTK_STRUCTURED_POINTS)
            {
                Debug.Log("Error: The dataset should be a structured points dataset");
                return;
            }

            //Get the points and modify the points / normals buffer
            m_ptsDesc = parser.GetStructuredPointsDescriptor();

            //Determine the new dimension and spacing
            m_dimensions = GetDisplayableSize();
            float  maxAxis   = (float)Math.Max(m_ptsDesc.Spacing[0]*m_ptsDesc.Size[0],
                                               Math.Max(m_ptsDesc.Spacing[1]*m_ptsDesc.Size[1],
                                                        m_ptsDesc.Spacing[2]*m_ptsDesc.Size[2]));
            for(int i = 0; i < 3; i++)
                m_spacing[i] = (float)(m_ptsDesc.Size[i]*m_ptsDesc.Spacing[i]/m_dimensions[i]/maxAxis);
            
            //Small multiples array
            m_smallMultiples = new List<VTKUnitySmallMultiple>();
        }

        /// <summary>
        /// Create a small multiple object
        /// </summary>
        /// <parent name="sd">The SubDataset to use</parent>
        /// <returns>A VTKUnitySmallMultiple object.</returns>
        public VTKUnitySmallMultiple CreatePointFieldSmallMultiple(SubDataset sd)
        {
            VTKUnitySmallMultiple sm = new VTKUnitySmallMultiple();

            unsafe
            {
                if(sm.Init(m_dataset.Parser, sd, m_dimensions, m_dataProvider))
                {
                    m_smallMultiples.Add(sm);
                    return sm;
                }
            }
            return null;
        }

        /// <summary>
        /// Remove a known small multiple
        /// </summary>
        /// <param name="sm">the small multiple to remove</param>
        public void RemoveSmallMultiple(VTKUnitySmallMultiple sm)
        {
            m_smallMultiples.Remove(sm);
        }

        /// <summary>
        /// Remove a known small multiple from a subdataset
        /// </summary>
        /// <param name="sd">the subdataset to evaluate</param>
        public void RemoveSmallMultipleFromSubDataset(SubDataset sd)
        {
            int i = 0;
            while (i < m_smallMultiples.Count)
            {
                if (m_smallMultiples[i].SubDataset == sd)
                    m_smallMultiples.RemoveAt(i);
                else
                    i++;
            }
        }

        /// <summary>
        /// Get the point field name at ID = dataID
        /// </summary>
        /// <param name="dataID">The dataID</param>
        /// <returns>The point field name ID #dataID</returns>
        public string GetPointFieldName(UInt32 dataID)
        {
            List<VTKFieldValue> l = m_dataset.Parser.GetPointFieldValueDescriptors();
            if(l.Count <= dataID)
                return null;
            return l[(int)dataID].Name;
        }

        /// <summary>
        /// Get the size diviser used for the displayability of the dataset (structured grid)
        /// </summary>
        /// <returns>The field diviser applied along all axis</returns>
        public UInt32 GetFieldSizeDiviser()
        {
            VTKStructuredPoints m_ptsDesc = m_dataset.Parser.GetStructuredPointsDescriptor();

            if (DesiredDensity == 0)
                return 1;

            UInt32 x = (m_ptsDesc.Size[0] + DesiredDensity - 1) / DesiredDensity;
            UInt32 y = (m_ptsDesc.Size[1] + DesiredDensity - 1) / DesiredDensity;
            UInt32 z = (m_ptsDesc.Size[2] + DesiredDensity - 1) / DesiredDensity;

            return Math.Max(Math.Max(x, y), z);
        }

        /// <summary>
        /// get the displayable size of the vector field. Indeed, due to hardware limitation, we cannot display all the vector field at once
        /// </summary>
        /// <returns>The vector field size displayable</returns>
        private Vector3Int GetDisplayableSize()
        {
            VTKStructuredPoints m_ptsDesc = m_dataset.Parser.GetStructuredPointsDescriptor();
            if (m_ptsDesc.Size[0] == 0 || m_ptsDesc.Size[1] == 0 || m_ptsDesc.Size[2] == 0)
                return new Vector3Int(0, 0, 0);

            int maxRatio = (int)GetFieldSizeDiviser();
            return new Vector3Int((int)m_ptsDesc.Size[0] / maxRatio, (int)m_ptsDesc.Size[1] / maxRatio, (int)m_ptsDesc.Size[2] / maxRatio);
        }

        /// <summary>
        /// The Spacing along each cells (along X, Y and Z axis)
        /// </summary>
        public Vector3 Spacing
        {
            get{return m_spacing;}
        }

        /// <summary>
        /// The 3D dimensions used
        /// </summary>
        public Vector3Int Dimensions
        {
            get{ return m_dimensions; }
        }

        /// <summary>
        /// The Desired density to store
        /// </summary>
        public UInt32 DesiredDensity
        {
            get{return m_desiredDensity;}
        }

        /// <summary>
        /// Get the generated small multiples
        /// </summary>
        public List<VTKUnitySmallMultiple> SmallMultiples
        {
            get { return m_smallMultiples; }
        }
    }
}