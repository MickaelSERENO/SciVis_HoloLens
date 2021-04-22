using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Sereno.Datasets;
using Sereno.SciVis;
using System.Threading.Tasks;
using Unity.Burst;
using System.Threading;
using Sereno.Datasets.Annotation;

namespace Sereno.SciVis
{
    /// <summary>
    /// VTK Small Multiple datasets. Computes data to use for 3D volumetric rendering
    /// </summary>
    public class VTKUnitySmallMultiple : ISubDatasetCallback
    {
        /// <summary>
        /// The internal state of this small multiple
        /// </summary>
        private SubDataset m_subDataset;
        
        /// <summary>
        /// The dimensions in use
        /// </summary>
        private Vector3Int m_dimensions;

        /// <summary>
        /// The actual spacing between each cell (captured one)
        /// </summary>
        private Vector3    m_spacing;

        /// <summary>
        /// The 3D texture color RGBA4444 byte Array 
        /// </summary>
        private short[] m_textureColor = null;

        /// <summary>
        /// The VTKStructuredPoints associated with this small multiple
        /// </summary>
        private VTKStructuredPoints m_descPts;

        /// <summary>
        /// The update TF lock
        /// </summary>
        private System.Object m_updateTFLock = new System.Object();

        /// <summary>
        /// Is the lock to use to check wether is updateTFLock or not
        /// </summary>
        private System.Object m_isTFUpdateLock = new System.Object();

        /// <summary>
        /// Is a thread waiting to update the transfer function?
        /// </summary>
        private bool m_waitToUpdateTF = false;

        /// <summary>
        /// The application data provider
        /// </summary>
        private IDataProvider m_dataProvider = null;

        /// <summary>
        /// Initialize the small multiple
        /// </summary>
        /// <param name="parser">The VTK Parser</param>
        /// <param name="subDataset">The SubDataset bound to this VTK view</param>
        /// <param name="dimensions">The dimensions in use</param>
        /// <param name="dataProvider">The application data provider (a.k.a, Main)</param>
        /// <returns></returns>
        public unsafe bool Init(VTKParser parser, SubDataset subDataset, Vector3Int dimensions, IDataProvider dataProvider)
        {
            subDataset.AddListener(this);
            m_subDataset = subDataset;
            m_dataProvider = dataProvider;

            //Copy the variables
            m_dimensions = dimensions;

            VTKStructuredPoints descPts = parser.GetStructuredPointsDescriptor();
            m_descPts = descPts;

            float  maxAxis   = (float)Math.Max(descPts.Spacing[0]*m_dimensions[0],
                                               Math.Max(descPts.Spacing[1]*m_dimensions[1],
                                                        descPts.Spacing[2]*m_dimensions[2]));

            m_spacing = new Vector3();
            for(int i = 0; i < 3; i++)
                m_spacing[i] = (float)(descPts.Spacing[i]/maxAxis);

            UpdateTF();

            return true;
        }

        /// <summary>
        /// Update the computation based on the transfer function
        /// </summary>
        public void UpdateTF()
        {
            Task.Run(UpdateTF_Thread);
        }

        private void UpdateTF_Thread()
        {
            //First, discard multiple call to this function (only two calls are available)
            lock (m_isTFUpdateLock)
            {
                if (m_waitToUpdateTF)
                {
                    Debug.Log("Already waiting for TF");
                    return;
                }
                else
                    m_waitToUpdateTF = true;
            }

            //The wait to update the TF
            lock (m_updateTFLock)
            {
                //Another one can wait to update the TF if it wants (max: two parallel call, one pending, one executing)
                lock (m_isTFUpdateLock)
                    m_waitToUpdateTF = false;

                TransferFunction tf = null;
                lock (m_subDataset)
                    tf = (TransferFunction)m_subDataset.TransferFunction.Clone();

                TextureColor = ComputeTFColor(tf);
            }
        }

        [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
        private short[] ComputeTFColor(TransferFunction tf)
        {
            VTKDataset vtk = (VTKDataset)m_subDataset.Parent;
            int hasGradient = (tf.HasGradient() ? 1 : 0);

            if (vtk.IsLoaded == false || tf == null || tf.GetDimension() - hasGradient > vtk.PointFieldDescs.Count ||
               (m_subDataset.OwnerID != -1 && m_subDataset.OwnerID != m_dataProvider.GetHeadsetID())) //Not a public subdataset
                return null;

            int t1 = (int)Math.Floor(tf.Timestep);
            int t2 = (int)Math.Ceiling(tf.Timestep);
            int[] times = new int[]{t1, t2};

            unsafe
            {
                int[] indices = new int[vtk.PointFieldDescs.Count];
                for (int i = 0; i < indices.Length; i++)
                    indices[i] = i;

                Datasets.Gradient gradient = vtk.GetGradient(indices);
                short[] colors = new short[m_dimensions.x * m_dimensions.y * m_dimensions.z]; //short because RGBA4444 == 2 bytes -> short
                float frac = tf.Timestep - (float)Math.Truncate(tf.Timestep);

                List<PointFieldDescriptor> ptDescs = m_subDataset.Parent.PointFieldDescs;

                Parallel.For(0, m_dimensions.z, new ParallelOptions { MaxDegreeOfParallelism = 8 },
                (k, state) =>
                {

                    float[] partialResT1 = new float[indices.Length + hasGradient];
                    float[] partialResT2 = new float[indices.Length + hasGradient];

                    fixed (short* pcolors = colors)
                    {
                        //int maxOldK = Math.Max(10 * (curK + 1), m_dimensions.z);
                        //for(int k = 10*curK; k < maxOldK; k++)
                        { 
                            UInt64 ind      = (UInt64)(k * m_dimensions.x * m_dimensions.y);
                            UInt64 readIntK = (UInt64)(m_descPts.Size[0] * m_descPts.Size[1] * (k * m_descPts.Size[2] / m_dimensions.z));
                            for (int j = 0; j < m_dimensions.y; j++)
                            {
                                //Pre compute the indice up to J--K coordinate
                                UInt64 readIntJK = (UInt64)(m_descPts.Size[0] * (j * m_descPts.Size[1] / m_dimensions.y)) + readIntK;

                                for (int i = 0; i < m_dimensions.x; i++)
                                {
                                    UInt64 readInd = (UInt64)(i * m_descPts.Size[0] / m_dimensions.x) + readIntJK;

                                    if (vtk.MaskValue != null && ((byte*)(vtk.MaskValue.Value))[readInd] == 0 ||
                                        (m_subDataset.EnableVolumetricMask && m_subDataset.GetVolumetricMaskAt((int)readInd) == false))
                                        pcolors[ind] = 0;
                                    else
                                    {
                                        float[][] partialRes = new float[][] { partialResT1, partialResT2 };

                                        for (int p = 0; p < 2; p++)
                                        {
                                            //Determine transfer function coordinates
                                            for (int l = 0; l < indices.Length; l++)
                                            {
                                                int ids = indices[l];
                                                if (ptDescs[ids].NbValuesPerTuple == 1)
                                                    partialRes[p][ids] = (ptDescs[ids].Value[times[p]].ReadAsFloat(readInd) - ptDescs[ids].MinVal) / (ptDescs[ids].MaxVal - ptDescs[ids].MinVal);
                                                else
                                                    partialRes[p][ids] = (ptDescs[ids].ReadMagnitude(readInd, times[p]) - ptDescs[ids].MinVal) / (ptDescs[ids].MaxVal - ptDescs[ids].MinVal);
                                            }

                                            if (tf.HasGradient())
                                            {
                                                if (gradient != null)
                                                    partialRes[p][partialRes[p].Length - 1] = gradient.Values[times[p]][readInd]; //In case we need the gradient
                                                else
                                                    partialRes[p][partialRes[p].Length - 1] = 0.0f;
                                            }
                                        }

                                        //Linear interpolation
                                        Color c = (1.0f - frac) * tf.ComputeColor(partialResT1) + frac * tf.ComputeColor(partialResT2);
                                        float a = (1.0f - frac) * tf.ComputeAlpha(partialResT1) + frac * tf.ComputeAlpha(partialResT2);

                                        short r = (short)(16 * c.r);
                                        if (r > 15)
                                            r = 15;
                                        short g = (short)(16 * c.g);
                                        if (g > 15)
                                            g = 15;
                                        short b = (short)(16 * c.b);
                                        if (b > 15)
                                            b = 15;
                                        short _a = (short)(16 * a);
                                        if (_a > 15)
                                            _a = 15;

                                        pcolors[ind] = (short)((r << 12) + (g << 8) + //RGBA4444 color format
                                                               (b << 4 ) + _a);
                                    }
                                    ind += 1;
                                }
                            }
                        }
                    }
                });
                return colors;
            }
        }
        
        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion){}

        public void OnPositionChange(SubDataset dataset, float[] position){}

        public void OnScaleChange(SubDataset dataset, float[] scale){}

        public void OnLockOwnerIDChange(SubDataset dataset, int ownerID){}

        public void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {
            UpdateTF();
        }

        public void OnNameChange(SubDataset dataset, String name){}

        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
            UpdateTF();
        }

        public void OnAddCanvasAnnotation(SubDataset dataset, CanvasAnnotation annot){}

        public void OnClearCanvasAnnotations(SubDataset dataset){}
        
        public void OnToggleMapVisibility(SubDataset dataset, bool visibility)
        {}

        public void OnChangeVolumetricMask(SubDataset dataset)
        {
            UpdateTF();
        }

        public void OnAddLogAnnotationPosition(SubDataset dataset, LogAnnotationPositionInstance annot)
        {}

        public void OnChangeDepthClipping(SubDataset dataset, float depth)
        {}

        public void OnSetSubDatasetGroup(SubDataset dataset, SubDatasetGroup sdg)
        {}

        /// <summary>
        /// The 3D Texure RGBA4444 byte array computed via the given transfer function.
        /// </summary>
        /// <returns></returns>
        public short[] TextureColor {get => m_textureColor; set { lock (this) { m_textureColor = value; } } }

        /// <summary>
        /// The SubDataset representing this Small Multiple
        /// </summary>
        public SubDataset SubDataset
        {
            get => m_subDataset;
            set
            {
                if(m_subDataset != null)
                    m_subDataset.RemoveListener(this);
                m_subDataset = value;
                m_subDataset.AddListener(this);
            }
        }

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