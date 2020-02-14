#define CHI2020 //This define a part of the project for CHI2020

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
            m_subDataset = subDataset;

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
            Task.Factory.StartNew(() =>
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

                    Debug.Log("Updating TF...");

                    VTKDataset vtk = (VTKDataset)m_subDataset.Parent;

                    if (vtk.IsLoaded == false || tf == null || tf.GetDimension() > vtk.PointFieldDescs.Count + 1 ||
                       (m_subDataset.OwnerID != -1 && m_subDataset.OwnerID != m_dataProvider.GetHeadsetID())) //Not a public subdataset
                    {
                        lock (this)
                        {
                            m_textureColor = null;
                        }
                        return;
                    }

                    else
                    {
                        unsafe
                        {
                            short[] colors = new short[m_dimensions.x * m_dimensions.y * m_dimensions.z]; //short because RGBA4444 == 2 bytes -> short

                            List<PointFieldDescriptor> ptDescs = m_subDataset.Parent.PointFieldDescs;
                            Parallel.For(0, m_dimensions.z,
                                k =>
                                {
                                    float[] partialRes = new float[ptDescs.Count + 1];
                                    fixed (short* pcolors = colors)
                                    {
                                        UInt64 ind = (UInt64)(k * m_dimensions.x * m_dimensions.y);
                                        for (int j = 0; j < m_dimensions.y; j++)
                                        {
                                            //Pre compute the indice up to J--K coordinate
                                            UInt64 readIntJK = (UInt64)(m_descPts.Size[0] * (j * m_descPts.Size[1] / m_dimensions.y) +
                                                                        m_descPts.Size[0] * m_descPts.Size[1] * (k * m_descPts.Size[2] / m_dimensions.z));

                                            for (int i = 0; i < m_dimensions.x; i++)
                                            {
                                                UInt64 readInd = (UInt64)(i * m_descPts.Size[0] / m_dimensions.x) + readIntJK;

                                                if (vtk.MaskValue != null && ((byte*)(vtk.MaskValue.Value))[readInd] == 0)
                                                    pcolors[ind] = 0;
                                                else
                                                {
                                                    //Determine transfer function coordinates
                                                    for (int l = 0; l < ptDescs.Count; l++)
                                                    {
                                                        if (ptDescs[l].NbValuesPerTuple == 1)
                                                            partialRes[l] = (ptDescs[l].Value.ReadAsFloat(readInd) - ptDescs[l].MinVal) / (ptDescs[l].MaxVal - ptDescs[l].MinVal);
                                                        else
                                                            partialRes[l] = (ptDescs[l].ReadMagnitude(readInd) - ptDescs[l].MinVal) / (ptDescs[l].MaxVal - ptDescs[l].MinVal);
                                                    }

                                                    partialRes[partialRes.Length - 1] = m_subDataset.Parent.Gradient[readInd]; //In case we need the gradient

                                                    float t = tf.ComputeColor(partialRes);
                                                    float a = tf.ComputeAlpha(partialRes);

                                                    Color c = SciVisColor.GenColor(tf.ColorMode, t);
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
                                                                           (b << 4)  + _a);
                                                }
                                                ind += 1;
                                            }
                                        }
                                    }
                                });

                            lock (this)
                            {
                                m_textureColor = colors;
                            }
                        }
                    }
                }
            });
        }
        
        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion)
        {}

        public void OnPositionChange(SubDataset dataset, float[] position)
        {}

        public void OnScaleChange(SubDataset dataset, float[] scale)
        {}

        public void OnLockOwnerIDChange(SubDataset dataset, int ownerID)
        {}

        public void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {
            UpdateTF();
        }

        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
            UpdateTF();
        }

        public void OnAddAnnotation(SubDataset dataset, Annotation annot)
        {}

        public void OnClearAnnotations(SubDataset dataset)
        {}

        /// <summary>
        /// The 3D Texure RGBA4444 byte array computed via the given transfer function.
        /// </summary>
        /// <returns></returns>
        public short[] TextureColor {get => m_textureColor; set => m_textureColor = value;}

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