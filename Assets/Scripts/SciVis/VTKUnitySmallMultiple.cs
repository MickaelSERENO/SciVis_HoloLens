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
        /// The 3D texture color
        /// </summary>
        private Color32[] m_textureColor = null;

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
            m_minGrad    = float.MaxValue;
            m_maxGrad    = float.MinValue;

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
            //Compute maximum and minimum gradient values
            /*object lockObject = new object(); //This if used as a lock

            Parallel.For(1, descPts.Size[2]-1,
                () => new float[2] { float.MaxValue, float.MinValue },
                (k, loopState, partialRes) =>
                {
                    for(UInt32 j = 1; j < descPts.Size[1]-1; j++)
                        for(UInt32 i = 1; i < descPts.Size[0]-1; i++)
                        {
                            UInt64 fieldOff = (UInt64)(i + j*descPts.Size[0] + k*descPts.Size[1]*descPts.Size[0]);

                            //Check the mask
                            if(mask != null && *(mask + fieldOff) == 0)
                                continue;

                            UInt64 indX1 = (UInt64)((i-1)+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*k);
                            UInt64 indX2 = (UInt64)((i+1)+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*k);
                            UInt64 indY1 = (UInt64)(i+descPts.Size[0]*(j-1)+descPts.Size[1]*descPts.Size[0]*k);
                            UInt64 indY2 = (UInt64)(i+descPts.Size[0]*(j+1)+descPts.Size[1]*descPts.Size[0]*k);
                            UInt64 indZ1 = (UInt64)(i+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*(k-1));
                            UInt64 indZ2 = (UInt64)(i+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*(k+1));

                            Vector3 grad = new Vector3((float)((val.ReadAsDouble(fieldValue.NbValuesPerTuple*indX1) - val.ReadAsDouble(fieldValue.NbValuesPerTuple*indX2))/(2.0f*descPts.Spacing[0])),
                                                       (float)((val.ReadAsDouble(fieldValue.NbValuesPerTuple*indY1) - val.ReadAsDouble(fieldValue.NbValuesPerTuple*indY2))/(2.0f*descPts.Spacing[1])),
                                                       (float)((val.ReadAsDouble(fieldValue.NbValuesPerTuple*indZ1) - val.ReadAsDouble(fieldValue.NbValuesPerTuple*indZ2))/(2.0f*descPts.Spacing[2])));

                            float gradMag = grad.magnitude;
                            if(gradMag < partialRes[0])
                                partialRes[0] = gradMag;
                            if(gradMag > partialRes[1])
                                partialRes[1] = gradMag;

                        }
                    return partialRes;
                },
                (partialRes) =>
                {
                    lock(lockObject)
                    {
                        m_minGrad = Math.Min(m_minGrad, partialRes[0]);
                        m_maxGrad = Math.Max(m_maxGrad, partialRes[1]);
                    }
                });*/
            //Compute the gradient
            ComputeGradients(descPts, fieldValue, val, mask);
            UpdateRangeColor(m_vtkSubDataset.MinClamp, m_vtkSubDataset.MaxClamp);

            return true;
        }

        /// <summary>
        /// Change the color range to apply (0.0 = minimum value, 1.0 = maximum value)
        /// <param name="minRange"> The minimum range to apply</param>
        /// <param name="maxRange"> The maximum range to apply</param>
        /// <param name="mode"> The color mode to apply</param>
        /// </summary>
        private void UpdateRangeColor(float minRange, float maxRange)
        {
            Color32[] colors = new Color32[m_dimensions.x*m_dimensions.y*m_dimensions.z];
            TransferFunction tf = m_vtkSubDataset.TransferFunction;

            if(tf == null)
            {
                Parallel.For(0, m_dimensions.x*m_dimensions.y*m_dimensions.z, i =>
                {
                    //Apply grayscale
                    float v = m_values[i];
                    if(v < minRange || v > maxRange)
                        colors[i] = new Color32(0, 0, 0, 0);
                    else
                    {
                        byte vb = (byte)(255.0f*v);
                        colors[i] = new Color32(vb, vb, vb, (byte)m_grads[i]);
                    }
                });
            }
            else
            {
                Parallel.For(0, m_dimensions.x*m_dimensions.y*m_dimensions.z, i =>
                {
                    //Determine transfer function coordinate
                    float v = m_values[i];
                    if(v < minRange || v > maxRange)
                        colors[i] = new Color32(0, 0, 0, 0);
                    else
                    {
                        float[] ind = new float[]{v, m_grads[i]};
                        float t = tf.ComputeColor(ind);
                        float a = tf.ComputeAlpha(ind);

                        Color c   = SciVisColor.GenColor(tf.ColorMode, t);
                        colors[i] = c;
                        colors[i].a = (byte)(255.0f*a);
                    }
                });
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
                        m_values[colorValueOff] = c;
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
            object lockObject = new object();
            Parallel.For(1,m_dimensions[2]-1,
                () => new float[2] { float.MaxValue, float.MinValue },
                (k, loopState, partialRes) =>
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
                            UInt64 indX1 = colorValueOff-1;
                            UInt64 indX2 = colorValueOff+1;
                            UInt64 indY1 = colorValueOff - (UInt64)(m_dimensions[0]);
                            UInt64 indY2 = colorValueOff + (UInt64)(m_dimensions[0]);
                            UInt64 indZ1 = colorValueOff - (UInt64)(m_dimensions[1]*m_dimensions[0]);
                            UInt64 indZ2 = colorValueOff + (UInt64)(m_dimensions[1]*m_dimensions[0]);

                            Vector3 grad = new Vector3((float)(m_values[indX2] - m_values[indX1])/(2.0f*m_spacing[0]),
                                                       (float)(m_values[indY2] - m_values[indY1])/(2.0f*m_spacing[1]),
                                                       (float)(m_values[indZ2] - m_values[indZ1])/(2.0f*m_spacing[2]));

                            m_grads[colorValueOff] = grad.magnitude;

                            float gradMag = grad.magnitude;
                            if(gradMag < partialRes[0])
                                partialRes[0] = gradMag;
                            if(gradMag > partialRes[1])
                                partialRes[1] = gradMag;
                        }
                    }
                    return partialRes;
                },
                (partialRes) =>
                {
                    lock(lockObject)
                    {
                        m_minGrad = Math.Min(m_minGrad, partialRes[0]);
                        m_maxGrad = Math.Max(m_maxGrad, partialRes[1]);
                    }
                });

            Parallel.For(1, m_dimensions.z-1, k => 
            {
                for(UInt32 j = 1; j < m_dimensions.y-1; j++)
                {
                    for(UInt32 i = 1; i < m_dimensions.x-1; i++)
                    {
                        UInt64 colorValueOff = (UInt64)(i+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);
                        m_grads[colorValueOff] = m_grads[colorValueOff]/m_maxGrad;

                        //This was based on maxGrad == maxGrad in the whole field
                        /*
                        UInt64 fieldOff      = (UInt64)(i*m_offset.x + j*m_offset.y + k*m_offset.z);
                        UInt64 colorValueOff = (UInt64)(i+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);

                        //Check the mask
                        if(mask != null && *(mask + fieldOff) == 0)
                        {
                            m_grads[colorValueOff]  = 0.0f;
                            continue;
                        }

                        //Gradient
                        UInt64 indX1 = colorValueOff-1;
                        UInt64 indX2 = colorValueOff+1;
                        UInt64 indY1 = colorValueOff - (UInt64)(m_dimensions[0]);
                        UInt64 indY2 = colorValueOff + (UInt64)(m_dimensions[0]);
                        UInt64 indZ1 = colorValueOff - (UInt64)(m_dimensions[1]*m_dimensions[0]);
                        UInt64 indZ2 = colorValueOff + (UInt64)(m_dimensions[1]*m_dimensions[0]);

                        Vector3 grad = new Vector3((float)(m_values[indX2] - m_values[indX1])/(2.0f*m_spacing[0]),
                                                   (float)(m_values[indY2] - m_values[indY1])/(2.0f*m_spacing[1]),
                                                   (float)(m_values[indZ2] - m_values[indZ1])/(2.0f*m_spacing[2]));

                        m_grads[colorValueOff] = grad.magnitude/m_maxGrad;*/
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

        public void OnColorRangeChange(SubDataset dataset, float min, float max)
        {
            UpdateRangeColor(min, max);
        }

        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion)
        {}

        public void OnPositionChange(SubDataset dataset, float[] position)
        {}

        public void OnScaleChange(SubDataset dataset, float[] scale)
        {}

        public void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {}
        
        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
            UpdateRangeColor(m_vtkSubDataset.MinClamp, m_vtkSubDataset.MaxClamp);
        }

        /// <summary>
        /// The 3D Texure Color computed. This array has to be used along a transfer function
        /// </summary>
        /// <returns></returns>
        public Color32[] TextureColor {get => m_textureColor; set => m_textureColor = value;}

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