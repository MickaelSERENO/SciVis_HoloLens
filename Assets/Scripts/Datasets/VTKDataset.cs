﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Burst;
using UnityEngine;

namespace Sereno.Datasets
{
    public class VTKDataset : Dataset
    {
        /// <summary>
        /// The VTKParser bound to this VTKDataset
        /// </summary>
        private VTKParser           m_parser;

        /// <summary>
        /// The VTKFieldValue point list to take account of
        /// </summary>
        private List<VTKFieldValue> m_ptFieldValues;

        /// <summary>
        /// The VTKFieldValue cell list to take account of
        /// </summary>
        private List<VTKFieldValue> m_cellFieldValues;

        /// <summary>
        /// The mask field value
        /// </summary>
        private VTKValue m_mask = null;
               
        /// <summary>
        /// Represent a VTKDataset
        /// </summary>
        /// <param name="id">The ID of the Dataset</param>
        /// <param name="name">The Dataset's name</param>
        /// <param name="parser">The VTKParser containing all the dataset.</param>
        /// <param name="ptFieldValues">Array of point field values to take account of</param>
        /// <param name="cellFieldValues">Array of cell field values to take account of</param>
        public VTKDataset(int id, String name, VTKParser parser, VTKFieldValue[] ptFieldValues, VTKFieldValue[] cellFieldValues) : base(id, name)
        {
            m_parser          = parser;
            m_ptFieldValues   = new List<VTKFieldValue>(ptFieldValues);
            m_cellFieldValues = new List<VTKFieldValue>(cellFieldValues);

            //Add all the field values to load
            for(int i = 0; i < m_ptFieldValues.Count; i++)
            {
                VTKFieldValue metaData = m_ptFieldValues[i];
                PointFieldDescriptor desc = new PointFieldDescriptor(metaData);
                desc.ID = (UInt32)i;
                m_ptFieldDescs.Add(desc);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        public override Task<int> LoadValues()
        {
            return Task.Run(() =>
            {
                bool succeed = true;

                //Load the mask
                foreach(VTKFieldValue fv in m_parser.GetPointFieldValueDescriptors())
                {
                    if (fv.Name == "vtkValidPointMask")
                    {
                        m_mask = m_parser.ParseAllFieldValues(fv);
                        break;
                    }
                }

                for (int i = 0; i < m_ptFieldDescs.Count; i++)
                {
                    //Get our working variables
                    PointFieldDescriptor desc = m_ptFieldDescs[i];
                    VTKFieldValue fieldVal = m_ptFieldValues[i];

                    //Set the range at an order permitting computation
                    desc.MinVal = float.MaxValue;
                    desc.MaxVal = float.MinValue;

                    //Load disk values
                    desc.Value = m_parser.ParseAllFieldValues(fieldVal);

                    object lockObject = new object(); //This if used as a lock

                    //Case 1: scalar values
                    if (desc.NbValuesPerTuple == 1)
                    {
                        //Compute min/max ranges
                        Parallel.For(0, desc.NbTuples,
                            () => new float[2] { float.MaxValue, float.MinValue },
                            (j, loopState, partialRes) =>
                            {

                                //Read mask
                                unsafe
                                {
                                    if(m_mask != null && ((byte*)m_mask.Value)[j] == 0)
                                        return partialRes;
                                }

                                float val = desc.Value.ReadAsFloat((UInt64)j);
                                if(float.IsNaN(val))
                                {
                                    if (m_mask != null)
                                    { 
                                        unsafe
                                        {
                                            ((byte*)m_mask.Value)[j] = 0;
                                        }
                                    }
                                    return partialRes;
                                }
                                partialRes[0] = Math.Min(partialRes[0], val);
                                partialRes[1] = Math.Max(partialRes[1], val);
                                return partialRes;
                            },
                            (partialRes) =>
                            {
                                lock(lockObject)
                                {
                                    desc.MinVal = Math.Min(desc.MinVal, partialRes[0]);
                                    desc.MaxVal = Math.Max(desc.MaxVal, partialRes[1]);
                                }
                            });
                    }

                    //Case 2: vector values
                    else
                    {
                        //Compute min/max ranges. Consider here only vector magnitude in the computation
                        Parallel.For(0, desc.NbTuples,
                                () => new float[2] { float.MaxValue, float.MinValue },
                                (j, loopState, partialRes) =>
                                {
                                    //Read mask
                                    unsafe
                                    {
                                        if (m_mask != null && ((byte*)m_mask.Value)[j] == 0)
                                            return partialRes;
                                    }

                                    float mag = 0;
                                    for (int k = 0; k < desc.NbValuesPerTuple; k++)
                                    {
                                        float val = desc.Value.ReadAsFloat((UInt64)(j * desc.NbValuesPerTuple + k));
                                        if (float.IsNaN(val))
                                        {
                                            if (m_mask != null)
                                            {
                                                unsafe
                                                {
                                                    ((byte*)m_mask.Value)[j] = 0;
                                                }
                                            }
                                            return partialRes;
                                        }
                                        mag += val * val;
                                    }                                  

                                    mag = (float)Math.Sqrt((double)mag);
                                    partialRes[0] = Math.Min(partialRes[0], mag);
                                    partialRes[1] = Math.Max(partialRes[1], mag);
                                    return partialRes;
                                },
                                (partialRes) =>
                                {
                                    lock (lockObject)
                                    {
                                        desc.MinVal = Math.Min(desc.MinVal, partialRes[0]);
                                        desc.MaxVal = Math.Max(desc.MaxVal, partialRes[1]);
                                    }
                                });
                    }
                }

                m_isLoaded = true;
                return (succeed ? 1 : 0);
            });
        }

        /// <summary>
        /// Compute the multi dimensionnal gradient of this dataset
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        protected override Gradient ComputeGradient(int[] indices)
        {
            if (indices.Count() == 0)
                return null;

            VTKStructuredPoints ptsDesc = m_parser.GetStructuredPointsDescriptor();
            float[] grad  = new float[ptsDesc.Size[0] * ptsDesc.Size[1] * ptsDesc.Size[2]];
            float   maxVal = float.MinValue;

            object lockObject = new object();

            ///////////////////////////////
            ///Compute "Inside" Gradient///
            ///////////////////////////////

            //Multi dimensionnal gradient computation, based on 
            //Joe Kniss, Gordon Kindlmann, and Charles Hansen. 2002. Multidimensional Transfer Functions for Interactive Volume Rendering. IEEE Transactions on Visualization and Computer Graphics 8, 3 (July 2002), 270-285. DOI: https://doi.org/10.1109/TVCG.2002.1021579 
            if (indices.Count() > 1)
            { 
                Parallel.For(1, ptsDesc.Size[2] - 1,
                    () => new {maxGrad = new float[1] { float.MinValue }, df = new float[3*indices.Count()], localGrad = new float[3], g = new float[9]},
                    (k, loopState, partialRes) =>
                    {
                        for (UInt32 j = 1; j < ptsDesc.Size[1] - 1; j++)
                        {
                            for (UInt32 i = 1; i < ptsDesc.Size[0] - 1; i++)
                            {
                                //Indices
                                UInt64 ind = (UInt64)(i + j * ptsDesc.Size[0] + k * ptsDesc.Size[1] * ptsDesc.Size[0]);
                                
                                //Read mask
                                unsafe
                                {
                                    if (m_mask != null && ((byte*)m_mask.Value)[ind] == 0)
                                    {
                                        grad[ind] = 0;
                                        continue;
                                    }
                                }

                                UInt64 indX1 = ind - 1;
                                UInt64 indX2 = ind + 1;
                                UInt64 indY1 = ind - (UInt64)(ptsDesc.Size[0]);
                                UInt64 indY2 = ind + (UInt64)(ptsDesc.Size[0]);
                                UInt64 indZ1 = ind - (UInt64)(ptsDesc.Size[1] * ptsDesc.Size[0]);
                                UInt64 indZ2 = ind + (UInt64)(ptsDesc.Size[1] * ptsDesc.Size[0]);

                                //Start computing the Df matrix.
                                for (int l = 0; l < indices.Count(); l++)
                                {
                                    int ids = indices[l];
                                    if (m_ptFieldDescs[ids].NbValuesPerTuple == 1)
                                    {
                                        partialRes.localGrad[0] = (m_ptFieldDescs[ids].Value.ReadAsFloat(indX2) - m_ptFieldDescs[ids].Value.ReadAsFloat(indX1)) / (2.0f * (float)ptsDesc.Spacing[0]);
                                        partialRes.localGrad[1] = (m_ptFieldDescs[ids].Value.ReadAsFloat(indY2) - m_ptFieldDescs[ids].Value.ReadAsFloat(indY1)) / (2.0f * (float)ptsDesc.Spacing[1]);
                                        partialRes.localGrad[2] = (m_ptFieldDescs[ids].Value.ReadAsFloat(indZ2) - m_ptFieldDescs[ids].Value.ReadAsFloat(indZ1)) / (2.0f * (float)ptsDesc.Spacing[2]);
                                    }
                                    else
                                    {
                                        partialRes.localGrad[0] = (m_ptFieldDescs[ids].ReadMagnitude(indX2) - m_ptFieldDescs[ids].ReadMagnitude(indX1)) / (2.0f * (float)ptsDesc.Spacing[0]);
                                        partialRes.localGrad[1] = (m_ptFieldDescs[ids].ReadMagnitude(indY2) - m_ptFieldDescs[ids].ReadMagnitude(indY1)) / (2.0f * (float)ptsDesc.Spacing[1]);
                                        partialRes.localGrad[2] = (m_ptFieldDescs[ids].ReadMagnitude(indZ2) - m_ptFieldDescs[ids].ReadMagnitude(indZ1)) / (2.0f * (float)ptsDesc.Spacing[2]);
                                    }

                                    //Fill df
                                    for (int ii = 0; ii < 3; ii++)
                                    {
                                        partialRes.df[3*l+ii] = partialRes.localGrad[ii] / (m_ptFieldDescs[ids].MaxVal - m_ptFieldDescs[ids].MinVal); 
                                    }
                                }

                                //Compute g = Df^T * Df
                                for (UInt32 l = 0; l < 9; l++)
                                    partialRes.g[l] = 0;
                                for(UInt32 n = 0; n < indices.Count(); n++)
                                    for(UInt32 l = 0; l < 3; l++)
                                        for(UInt32 m = 0; m < 3; m++)
                                            partialRes.g[3*l+m] += partialRes.df[3*n+l]*partialRes.df[3*n+m];

                                //Grad == L2 norm of g.
                                //Taking sqrt(sum_{k=0}^9 g[k]*g[k]) is a good approximation based on Kniss et al. paper
                                float gradMag = 0;
                                for(UInt32 l = 0; l < 9; l++)
                                    gradMag += partialRes.g[l]*partialRes.g[l];

                                gradMag = (float)Math.Sqrt(gradMag);
                                grad[ind] = gradMag;
                                partialRes.maxGrad[0] = Math.Max(partialRes.maxGrad[0], gradMag);
                            }
                        }
                        return partialRes;
                    },
                    (partialRes) =>
                    {
                        lock (lockObject)
                        {
                            maxVal = Math.Max(maxVal, partialRes.maxGrad[0]);
                        }
                    });
            }

            //1D gradient computation
            else if(m_ptFieldDescs[indices[0]].NbValuesPerTuple == 1)
            {
                int ids = indices[0];
                Parallel.For(1, ptsDesc.Size[2] - 1,
                    () => new { maxGrad = new float[1] { float.MinValue }, localGrad = new float[3] },
                    (k, loopState, partialRes) =>
                    {
                        for (UInt32 j = 1; j < ptsDesc.Size[1] - 1; j++)
                        {
                            for (UInt32 i = 1; i < ptsDesc.Size[0] - 1; i++)
                            {
                                //Indices
                                UInt64 ind = (UInt64)(i + j * ptsDesc.Size[0] + k * ptsDesc.Size[1] * ptsDesc.Size[0]);

                                //Read mask
                                unsafe
                                {
                                    if (m_mask != null && ((byte*)m_mask.Value)[ind] == 0)
                                    {
                                        grad[ind] = 0;
                                        continue;
                                    }
                                }

                                UInt64 indX1 = ind - 1;
                                UInt64 indX2 = ind + 1;
                                UInt64 indY1 = ind - (UInt64)(ptsDesc.Size[0]);
                                UInt64 indY2 = ind + (UInt64)(ptsDesc.Size[0]);
                                UInt64 indZ1 = ind - (UInt64)(ptsDesc.Size[1] * ptsDesc.Size[0]);
                                UInt64 indZ2 = ind + (UInt64)(ptsDesc.Size[1] * ptsDesc.Size[0]);

                                partialRes.localGrad[0] = (m_ptFieldDescs[ids].Value.ReadAsFloat(indX2) - m_ptFieldDescs[ids].Value.ReadAsFloat(indX1)) / (2.0f * (float)ptsDesc.Spacing[0]);
                                partialRes.localGrad[1] = (m_ptFieldDescs[ids].Value.ReadAsFloat(indY2) - m_ptFieldDescs[ids].Value.ReadAsFloat(indY1)) / (2.0f * (float)ptsDesc.Spacing[1]);
                                partialRes.localGrad[2] = (m_ptFieldDescs[ids].Value.ReadAsFloat(indZ2) - m_ptFieldDescs[ids].Value.ReadAsFloat(indZ1)) / (2.0f * (float)ptsDesc.Spacing[2]);
                                  
                                float gradMag = 0;
                                for (UInt32 l = 0; l < 3; l++)
                                {
                                    partialRes.localGrad[l] /= (m_ptFieldDescs[ids].MaxVal - m_ptFieldDescs[ids].MinVal);
                                    gradMag += partialRes.localGrad[l] * partialRes.localGrad[l];
                                }
                                gradMag = (float)Math.Sqrt(gradMag);
                                grad[ind] = gradMag;
                                partialRes.maxGrad[0] = Math.Max(partialRes.maxGrad[0], gradMag);
                            }
                        }
                        return partialRes;
                    },
                    (partialRes) =>
                    {
                        lock (lockObject)
                        {
                            maxVal = Math.Max(maxVal, partialRes.maxGrad[0]);
                        }
                    });
            }

            else
            {
                int ids = indices[0];

                Parallel.For(1, ptsDesc.Size[2] - 1,
                    () => new { maxGrad = new float[1] { float.MinValue }, localGrad = new float[3] },
                (k, loopState, partialRes) =>
                {
                    for (UInt32 j = 1; j < ptsDesc.Size[1] - 1; j++)
                    {
                        for (UInt32 i = 1; i < ptsDesc.Size[0] - 1; i++)
                        {
                            //Indices
                            UInt64 ind = (UInt64)(i + j * ptsDesc.Size[0] + k * ptsDesc.Size[1] * ptsDesc.Size[0]);

                            //Read mask
                            unsafe
                            {
                                if (m_mask != null && ((byte*)m_mask.Value)[ind] == 0)
                                {
                                    grad[ind] = 0;
                                    continue;
                                }
                            }

                            UInt64 indX1 = ind - 1;
                            UInt64 indX2 = ind + 1;
                            UInt64 indY1 = ind - (UInt64)(ptsDesc.Size[0]);
                            UInt64 indY2 = ind + (UInt64)(ptsDesc.Size[0]);
                            UInt64 indZ1 = ind - (UInt64)(ptsDesc.Size[1] * ptsDesc.Size[0]);
                            UInt64 indZ2 = ind + (UInt64)(ptsDesc.Size[1] * ptsDesc.Size[0]);
                            
                            partialRes.localGrad[0] = (m_ptFieldDescs[ids].ReadMagnitude(indX2) - m_ptFieldDescs[ids].ReadMagnitude(indX1)) / (2.0f * (float)ptsDesc.Spacing[0]);
                            partialRes.localGrad[1] = (m_ptFieldDescs[ids].ReadMagnitude(indY2) - m_ptFieldDescs[ids].ReadMagnitude(indY1)) / (2.0f * (float)ptsDesc.Spacing[1]);
                            partialRes.localGrad[2] = (m_ptFieldDescs[ids].ReadMagnitude(indZ2) - m_ptFieldDescs[ids].ReadMagnitude(indZ1)) / (2.0f * (float)ptsDesc.Spacing[2]);

                            float gradMag = 0;
                            for (UInt32 l = 0; l < 3; l++)
                            {
                                partialRes.localGrad[l] /= (m_ptFieldDescs[ids].MaxVal - m_ptFieldDescs[ids].MinVal);
                                gradMag += partialRes.localGrad[l] * partialRes.localGrad[l];
                            }

                            gradMag = (float)Math.Sqrt(gradMag);
                            grad[ind] = gradMag;
                            partialRes.maxGrad[0] = Math.Max(partialRes.maxGrad[0], gradMag);
                        }
                    }
                    return partialRes;
                },
                (partialRes) =>
                {
                    lock (lockObject)
                    {
                        maxVal = Math.Max(maxVal, partialRes.maxGrad[0]);
                    }
                });
            }

            ////////////////////////////////
            //////Set boundaries to 0///////
            ////////////////////////////////
            //When gradient is not computable - default 0.0f
            Parallel.For(0, ptsDesc.Size[1], j =>
            {
                for (UInt32 i = 0; i < ptsDesc.Size[0]; i++)
                {
                    UInt64 colorValueOff1 = (UInt64)(i + ptsDesc.Size[0] * j);
                    UInt64 colorValueOff2 = (UInt64)(i + ptsDesc.Size[0] * j + ptsDesc.Size[0] * ptsDesc.Size[1] * (ptsDesc.Size[2] - 1));
                    grad[colorValueOff1] = grad[colorValueOff2] = 0.0f;
                }
            });
            Parallel.For(0, ptsDesc.Size[2], k =>
            {
                for (UInt32 i = 0; i < ptsDesc.Size[0]; i++)
                {

                    UInt64 colorValueOff1 = (UInt64)(i + ptsDesc.Size[0] * ptsDesc.Size[1] * k);
                    UInt64 colorValueOff2 = (UInt64)(i + ptsDesc.Size[0] * (ptsDesc.Size[1] - 1) + ptsDesc.Size[0] * ptsDesc.Size[1] * k);
                    grad[colorValueOff1]  = grad[colorValueOff2] = 0.0f;
                }
            });
            Parallel.For(0, ptsDesc.Size[2], k =>
            {
                for (UInt32 j = 0; j < ptsDesc.Size[1]; j++)
                {

                    UInt64 colorValueOff1 = (UInt64)(ptsDesc.Size[0] * j + ptsDesc.Size[0] * ptsDesc.Size[1] * k);
                    UInt64 colorValueOff2 = (UInt64)(ptsDesc.Size[0] - 1 + ptsDesc.Size[0] * j + ptsDesc.Size[0] * ptsDesc.Size[1] * k);
                    grad[colorValueOff1]  = grad[colorValueOff2] = 0.0f;
                }
            });

            return new Gradient(indices, grad, maxVal);
        }

        public override uint GetNbSpatialValues()
        {
            VTKStructuredPoints p = m_parser.GetStructuredPointsDescriptor();
            return p.Size[0] * p.Size[1] * p.Size[2];
        }

        /// <summary>
        /// The VTKParser containing all the dataset
        /// </summary>
        public VTKParser           Parser         {get => m_parser;}

        /// <summary>
        /// List of point field values to take account of
        /// </summary>
        public List<VTKFieldValue> PtFieldValues  {get => m_ptFieldValues;}

        /// <summary>
        /// List of cell field values to take account of
        /// </summary>
        public List<VTKFieldValue> CellFieldValues {get => m_cellFieldValues;}

        /// <summary>
        /// Get the mask array value if defined (null if not defined)
        /// </summary>
        public VTKValue MaskValue { get => m_mask; }
    }
}