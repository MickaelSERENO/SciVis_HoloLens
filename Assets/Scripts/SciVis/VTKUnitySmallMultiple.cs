using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Sereno.Datasets;
using Sereno.SciVis;

namespace Sereno.SciVis
{
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
        
        private Color[] m_textureColor = null;

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
            }

            //Gradient values
            for(UInt32 k = 1; k < descPts.Size[2]-1; k++)
                for(UInt32 j = 1; j < descPts.Size[1]-1; j++)
                    for(UInt32 i = 1; i < descPts.Size[0]-1; i++)
                    {
                        UInt64 indX1 = (i-1)+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indX2 = (i+1)+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indY1 = i+descPts.Size[0]*(j-1)+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indY2 = i+descPts.Size[0]*(j+1)+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indZ1 = i+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*(k-1);
                        UInt64 indZ2 = i+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*(k+1);

                        Vector3 grad = new Vector3((float)((val.ReadAsDouble(indX1 * fieldValue.NbValuesPerTuple) - val.ReadAsDouble(indX2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[0]),
                                                   (float)((val.ReadAsDouble(indY1 * fieldValue.NbValuesPerTuple) - val.ReadAsDouble(indY2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[1]),
                                                   (float)((val.ReadAsDouble(indZ1 * fieldValue.NbValuesPerTuple) - val.ReadAsDouble(indZ2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[2]));

                        float gradMag = grad.magnitude;
                        if(gradMag < m_minGrad)
                            m_minGrad = gradMag;
                        if(gradMag > m_maxGrad)
                            m_maxGrad = gradMag;
                    }

            //Update the amplitude of this SubDataset
            m_vtkSubDataset.MaxAmplitude = maxVal;
            m_vtkSubDataset.MinAmplitude = minVal;

            //Compute the values and the gradient
            ComputeValues(descPts, fieldValue, val, mask);
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
            UInt64 colorValueOff = 0;
            for(UInt32 k = 0; k < m_dimensions.z; k++)
            {
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
                        m_values[colorValueOff] = c;
                    }
                }
            }
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
            for(UInt32 k = 1; k < m_dimensions.z-1; k++)
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
                        UInt64 indX1 = (i-1) + descPts.Size[0]*j     + descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indX2 = (i+1) + descPts.Size[0]*j     + descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indY1 = i     + descPts.Size[0]*(j-1) + descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indY2 = i     + descPts.Size[0]*(j+1) + descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indZ1 = i     + descPts.Size[0]*j     + descPts.Size[1]*descPts.Size[0]*(k-1);
                        UInt64 indZ2 = i     + descPts.Size[0]*j     + descPts.Size[1]*descPts.Size[0]*(k+1);

                        Vector3 grad = new Vector3((float)((val.ReadAsDouble(indX2 * fieldValue.NbValuesPerTuple) - val.ReadAsDouble(indX1 * fieldValue.NbValuesPerTuple))/descPts.Spacing[0]),
                                                   (float)((val.ReadAsDouble(indY2 * fieldValue.NbValuesPerTuple) - val.ReadAsDouble(indY1 * fieldValue.NbValuesPerTuple))/descPts.Spacing[1]),
                                                   (float)((val.ReadAsDouble(indZ2 * fieldValue.NbValuesPerTuple) - val.ReadAsDouble(indZ1 * fieldValue.NbValuesPerTuple))/descPts.Spacing[2]));

                        m_grads[colorValueOff] = (float)grad.magnitude/m_maxGrad;
                    }
                }
            }

            //When gradient is not computable - default 0.0f
            for(UInt32 j = 0; j < m_dimensions.y; j++)
            {
                for(UInt32 i = 0; i < m_dimensions.x; i++)
                {

                    UInt64 colorValueOff1 = (UInt64)(i+m_dimensions.x*j);
                    UInt64 colorValueOff2 = (UInt64)(i+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*(m_dimensions.z-1));
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            }

            for(UInt32 k = 0; k < m_dimensions.z; k++)
            {
                for(UInt32 i = 0; i < m_dimensions.x; i++)
                {

                    UInt64 colorValueOff1 = (UInt64)(i+                                  m_dimensions.x*m_dimensions.y*k);
                    UInt64 colorValueOff2 = (UInt64)(i+m_dimensions.x*(m_dimensions.y-1)+m_dimensions.x*m_dimensions.y*k);
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            }

            for(UInt32 k = 0; k < m_dimensions.z; k++)
            {
                for(UInt32 j = 0; j < m_dimensions.y; j++)
                {

                    UInt64 colorValueOff1 = (UInt64)(                 m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);
                    UInt64 colorValueOff2 = (UInt64)(m_dimensions.x-1+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            }
        }

        public void OnColorRangeChange(SubDataset dataset, float min, float max, ColorMode mode)
        {
            UpdateRangeColor(min, max, mode);
        }

        public Color[] TextureColor {get => m_textureColor; set => m_textureColor = value;}

        public VTKSubDataset VTKSubDataset {get => m_vtkSubDataset;}

        public Vector3Int Dimensions {get => m_dimensions;}

        public VTKStructuredPoints DescPts {get => m_descPts;}
    }
}