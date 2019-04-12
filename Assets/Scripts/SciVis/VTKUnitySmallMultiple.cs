using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Sereno.Datasets;
using Sereno.SciVis;
using System.Threading.Tasks;

namespace Sereno.SciVis
{
    /// <summary>
    /// VTK Small Multiple datasets. Computes data to use for 3D volumetric rendering
    /// </summary>
    public class VTKUnitySmallMultiple : ISubDatasetCallback
    {
        /// <summary>
        /// The VTKSubDataset to use
        /// </summary>
        private VTKSubDataset m_vtkSubDataset;

        /// <summary>
        /// The value currently in use. Size : density.x*density.y*density.z
        /// </summary>
        private float[] m_values = null;
        
        /// <summary>
        /// The gradient values currently in use. Size : density.x*density.y*density.z
        /// </summary>
        private float[] m_grads  = null;

        /// <summary>
        /// The dimensions in use
        /// </summary>
        private Vector3Int m_dimensions;

        /// <summary>
        /// The actual spacing between each cell (captured one)
        /// </summary>
        private Vector3    m_spacing;

        /// <summary>
        ///The offset along each axis to apply : pos = x* offset.x + y* offset.y + z* offset.z
        /// </summary>
        private Vector3Int m_offset;

        /// <summary>
        /// The maximum gradient found
        /// </summary>
        private float m_maxGrad;

        /// <summary>
        /// The minimum gradient found
        /// </summary>
        private float m_minGrad;
        
        /// <summary>
        /// The 3D texture color to use along a transfer function 2D texture
        /// </summary>
        private Color[] m_textureColor = null;

        /// <summary>
        /// The VTKStructuredPoints associated with this small multiples
        /// </summary>
        private VTKStructuredPoints m_descPts;

        /// <summary>
        /// Initialize the small multiple from point field data
        /// </summary>
        /// <param name="parser">The VTK Parser</param>
        /// <param name="fieldValue">The VTKFieldValue to use</param>
        /// <param name="subDataset">The SubDataset bound to this VTK view</param>
        /// <param name="offset">The offset along each axis to apply : pos = x*offset.x + y*offset.y + z*offset.z</param>
        /// <param name="dimensions">The dimensions in use</param>
        /// <param name="mask">The mask to apply along each point</param>
        /// <returns></returns>
        public unsafe bool InitFromPointField(VTKParser parser, VTKFieldValue fieldValue, VTKSubDataset subDataset,
                                              Vector3Int offset, Vector3Int dimensions, byte* mask)
        {
            subDataset.AddListener(this);

            //Copy the variables
            m_values        = new float[dimensions.x*dimensions.y*dimensions.z];
            m_grads         = new float[dimensions.x*dimensions.y*dimensions.z];
            m_dimensions    = dimensions;
            m_vtkSubDataset = subDataset;
            m_offset        = offset;

            VTKStructuredPoints descPts = parser.GetStructuredPointsDescriptor();
            m_descPts = descPts;

            float  maxAxis   = (float)Math.Max(descPts.Spacing[0]*m_dimensions[0],
                                               Math.Max(descPts.Spacing[1]*m_dimensions[1],
                                                        descPts.Spacing[2]*m_dimensions[2]));

            m_spacing = new Vector3();
            for(int i = 0; i < 3; i++)
                m_spacing[i] = (float)(m_dimensions[i]*descPts.Spacing[i]/m_dimensions[i]/maxAxis);

            //Read the points values
            VTKValue      val    = parser.ParseAllFieldValues(fieldValue);

            //Determine the minimum and maximum values
            float maxVal = float.MinValue;
            float minVal = float.MaxValue;
            m_minGrad    = minVal;
            m_maxGrad    = maxVal;

            //Amplitude values
            for(UInt32 i = 0; i < val.NbValues; i++)
            {
                if(mask != null && mask[i]==0)
                    continue;
                float v = (float)val.ReadAsDouble(i * fieldValue.NbValuesPerTuple);

                if(maxVal < v)
                    maxVal = v;
                if(minVal > v)
                    minVal = v;

                //TODO some optimization can be done here to not read again the dataset (even if in memory)
            }
            //Update the amplitude of this SubDataset
            m_vtkSubDataset.MaxAmplitude = maxVal;
            m_vtkSubDataset.MinAmplitude = minVal;

            //Compute the values
            ComputeValues(descPts, fieldValue, val, mask);

            //Gradient values
            for(UInt32 k = 1; k < m_dimensions[2]-1; k++)
                for(UInt32 j = 1; j < m_dimensions[1]-1; j++)
                    for(UInt32 i = 1; i < m_dimensions[0]-1; i++)
                    {
                        Int64 indX1 = (i-1)+m_dimensions[0]*j+m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indX2 = (i+1)+m_dimensions[0]*j+m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indY1 = i+m_dimensions[0]*(j-1)+m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indY2 = i+m_dimensions[0]*(j+1)+m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indZ1 = i+m_dimensions[0]*j+m_dimensions[1]*m_dimensions[0]*(k-1);
                        Int64 indZ2 = i+m_dimensions[0]*j+m_dimensions[1]*m_dimensions[0]*(k+1);

                        Vector3 grad = new Vector3((float)(m_values[indX1] - m_values[indX2])/(2.0f*m_spacing[0]),
                                                   (float)(m_values[indY1] - m_values[indY2])/(2.0f*m_spacing[1]),
                                                   (float)(m_values[indZ1] - m_values[indZ2])/(2.0f*m_spacing[2]));

                        float gradMag = grad.magnitude;
                        if(gradMag < m_minGrad)
                            m_minGrad = gradMag;
                        if(gradMag > m_maxGrad)
                            m_maxGrad = gradMag;

                        //TODO some optimization can be done here to not read again the dataset (even if in memory)
                    }
            //Compute the gradient
            ComputeGradients(descPts, fieldValue, val, mask);
            UpdateRangeColor(m_vtkSubDataset.MinClamp, m_vtkSubDataset.MaxClamp, m_vtkSubDataset.ColorMode);

            return true;
        }

        /// <summary>
        /// Change the color range to apply (0.0 = minimum value, 1.0 = maximum value)
        /// <param name="minRange"> The minimum range to apply</param>
        /// <param name="maxRange"> The maximum range to apply</param>
        /// <param name="mode"> The color mode to apply</param>
        /// </summary>
        private void UpdateRangeColor(float minRange, float maxRange, ColorMode mode)
        {
            Color[] colors = new Color[m_dimensions.x*m_dimensions.y*m_dimensions.z];

            //Determine transfer function coordinate
            for(int i = 0; i < m_dimensions.x*m_dimensions.y*m_dimensions.z; i++)
            {
                float v = m_values[i];
                if(v < minRange || v > maxRange)
                    colors[i] = new Color(-1, -1, 0, 0);
                else
                    colors[i] = new Color(v, m_grads[i], 0, 0);
            }
            m_textureColor = colors;
        }

        /// <summary>
        /// Compute the array of values
        /// </summary>
        /// <param name="descPts">The structured point descriptor</param>
        /// <param name="fieldValue">The field value descriptor</param>
        /// <param name="val">The actual values of the field</param>
        /// <param name="mask">the mask to apply</param>
        private unsafe void ComputeValues(VTKStructuredPoints descPts, VTKFieldValue fieldValue, VTKValue val, byte* mask)
        {
            float minVal = m_vtkSubDataset.MinAmplitude;
            float maxVal = m_vtkSubDataset.MaxAmplitude;

            //Save the values normalized for later use
            Parallel.For(0, m_dimensions.z, k => 
            {
                UInt64 colorValueOff = (UInt64)(k*m_dimensions.x*m_dimensions.y);
                for(UInt32 j = 0; j < m_dimensions.y; j++)
                {
                    for(UInt32 i = 0; i < m_dimensions.x; i++, colorValueOff++)
                    {
                        UInt64 fieldOff = (UInt64)(i*m_offset.x + j*m_offset.y + k*m_offset.z);

                        //Check the mask
                        if(mask != null && *(mask + fieldOff) == 0)
                        {
                            m_values[colorValueOff] = -1.0f;
                            continue;
                        }

                        //Value
                        float c = (float)val.ReadAsDouble(fieldOff*fieldValue.NbValuesPerTuple);
                        c = (c - minVal) / (maxVal-minVal);
                        m_values[colorValueOff] = Math.Min(Math.Max(c, 0.01f), 1.0f-0.01f);
                    }
                }
            });
        }

        /// <summary>
        /// Compute the array of gradient of this field
        /// </summary>
        /// <param name="descPts">The structured point descriptor</param>
        /// <param name="fieldValue">The field value descriptor</param>
        /// <param name="val">The actual values of the field</param>
        /// <param name="mask">the mask to apply</param>
        private unsafe void ComputeGradients(VTKStructuredPoints descPts, VTKFieldValue fieldValue, VTKValue val, byte* mask)
        {
            //When gradient is computable
            Parallel.For(1, m_dimensions.z-1, k => 
            {
                for(UInt32 j = 1; j < m_dimensions.y-1; j++)
                {
                    for(UInt32 i = 1; i < m_dimensions.x-1; i++)
                    {
                        UInt64 fieldOff      = (UInt64)(i*m_offset.x + j*m_offset.y + k*m_offset.z);
                        UInt64 colorValueOff = (UInt64)(i+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);

                        //Check the mask
                        if(mask != null && *(mask + fieldOff) == 0)
                        {
                            m_grads[colorValueOff]  = 0.0f;
                            continue;
                        }

                        //Gradient
                        Int64 indX1 = (i-1) + m_dimensions[0]*j     + m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indX2 = (i+1) + m_dimensions[0]*j     + m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indY1 = i     + m_dimensions[0]*(j-1) + m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indY2 = i     + m_dimensions[0]*(j+1) + m_dimensions[1]*m_dimensions[0]*k;
                        Int64 indZ1 = i     + m_dimensions[0]*j     + m_dimensions[1]*m_dimensions[0]*(k-1);
                        Int64 indZ2 = i     + m_dimensions[0]*j     + m_dimensions[1]*m_dimensions[0]*(k+1);

                        Vector3 grad = new Vector3((float)(m_values[indX2] - m_values[indX1])/(2.0f*m_spacing[0]),
                                                   (float)(m_values[indY2] - m_values[indY1])/(2.0f*m_spacing[1]),
                                                   (float)(m_values[indZ2] - m_values[indZ1])/(2.0f*m_spacing[2]));

                        m_grads[colorValueOff] = Math.Min((float)grad.magnitude/m_maxGrad, 1.0f);
                    }
                }
            });

            //When gradient is not computable - default 0.0f
            Parallel.For(0, m_dimensions.y, j => 
            {
                for(UInt32 i = 0; i < m_dimensions.x; i++)
                {

                    UInt64 colorValueOff1 = (UInt64)(i+m_dimensions.x*j);
                    UInt64 colorValueOff2 = (UInt64)(i+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*(m_dimensions.z-1));
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            });

            Parallel.For(0, m_dimensions.z, k => 
            {
                for(UInt32 i = 0; i < m_dimensions.x; i++)
                {

                    UInt64 colorValueOff1 = (UInt64)(i+                                  m_dimensions.x*m_dimensions.y*k);
                    UInt64 colorValueOff2 = (UInt64)(i+m_dimensions.x*(m_dimensions.y-1)+m_dimensions.x*m_dimensions.y*k);
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            });

            Parallel.For(0, m_dimensions.z, k => 
            {
                for(UInt32 j = 0; j < m_dimensions.y; j++)
                {

                    UInt64 colorValueOff1 = (UInt64)(                 m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);
                    UInt64 colorValueOff2 = (UInt64)(m_dimensions.x-1+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            });
        }

        public void OnColorRangeChange(SubDataset dataset, float min, float max, ColorMode mode)
        {
            UpdateRangeColor(min, max, mode);
        }

        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion)
        {}

        public void OnPositionChange(SubDataset dataset, float[] position)
        {}

        public void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {}

        /// <summary>
        /// The 3D Texure Color computed. This array has to be used along a transfer function
        /// </summary>
        /// <returns></returns>
        public Color[] TextureColor {get => m_textureColor; set => m_textureColor = value;}

        /// <summary>
        /// The VTKSubDataset bound to this Small Multiple
        /// </summary>
        /// <returns>The VTKSubDataset bound to this Small Multiple</returns>
        public VTKSubDataset VTKSubDataset {get => m_vtkSubDataset;}

        /// <summary>
        /// The three axis dimensions
        /// </summary>
        /// <returns>The Three axis dimensions</returns>
        public Vector3Int Dimensions {get => m_dimensions;}

        /// <summary>
        /// The Point descriptors of the structured grid used
        /// </summary>
        /// <returns>A VTKStructuredPoints describing the grid</returns>
        public VTKStructuredPoints DescPts {get => m_descPts;}
    }
}