using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Sereno;

namespace Sereno
{
    public class VTKUnitySmallMultiple : MonoBehaviour 
    {
        /// <summary>
        /// Material to use
        /// </summary>
        public Material ColorMaterial = null;

        /// <summary>
        /// The Outline gameobject
        /// </summary>
        public GameObject Outline = null;
        
        /// <summary>
        /// The mesh to use
        /// </summary>
        private Mesh m_mesh = null;

        /// <summary>
        /// The outline gameobject created from "Outline"
        /// </summary>
        private GameObject m_outline;

        /// <summary>
        /// The material in use (copy from the Material setted in the Unity Editor)
        /// </summary>
        private Material m_material;

        /// <summary>
        /// The VTKSubDataset to use
        /// </summary>
        private SubDataset m_vtkSubDataset;

        /// <summary>
        /// The Texture 3D representing this Dataset
        /// </summary>
        private Texture3D m_texture3D;

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

        // Use this for initialization
        private void Start() 
        {
        }
        
        // Update is called once per frame
        private void Update()
        {
            if(m_mesh != null && m_material != null)
            {
                m_material.SetTexture("_TFTexture", m_vtkSubDataset.TFTexture.Texture);
                m_material.SetTexture("_TextureData", m_texture3D);
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_material, 1);
            }
        }

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
        public unsafe bool InitFromPointField(VTKParser parser, VTKFieldValue fieldValue, SubDataset subDataset,
                                              Vector3Int offset, Vector3Int dimensions, byte* mask)
        {
            //Copy the variables
            m_material      = new Material(ColorMaterial);
            m_values        = new float[dimensions.x*dimensions.y*dimensions.z];
            m_grads         = new float[dimensions.x*dimensions.y*dimensions.z];
            m_mesh          = new Mesh();
            m_dimensions    = dimensions;
            m_vtkSubDataset = subDataset;

            VTKStructuredPoints descPts = parser.GetStructuredPointsDescriptor();

            //Read the points values
            VTKValue      val    = parser.ParseAllFieldValues(fieldValue);

            //Determine the minimum and maximum values
            float maxVal = float.MinValue;
            float minVal = float.MaxValue;
            m_minGrad    = minVal;
            m_maxGrad    = maxVal;

            m_texture3D = new Texture3D(dimensions.x, dimensions.y, dimensions.z, TextureFormat.RGFloat, false);
            m_texture3D.wrapModeU = TextureWrapMode.Clamp;
            m_texture3D.wrapModeV = TextureWrapMode.Clamp;
            m_texture3D.wrapModeW = TextureWrapMode.Clamp;

            //Amplitude values
            for(UInt32 i = 0; i < val.NbValues; i++)
            {
                if(mask != null && mask[i]==0)
                    continue;
                float v = (float)val.ReadAsFloat(i * fieldValue.NbValuesPerTuple);
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

                        Vector3 grad = new Vector3((float)((val.ReadAsFloat(indX1 * fieldValue.NbValuesPerTuple) - val.ReadAsFloat(indX2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[0]),
                                                   (float)((val.ReadAsFloat(indY1 * fieldValue.NbValuesPerTuple) - val.ReadAsFloat(indY2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[1]),
                                                   (float)((val.ReadAsFloat(indZ1 * fieldValue.NbValuesPerTuple) - val.ReadAsFloat(indZ2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[2]));

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

            //Compute the Mesh. A regular rectangle where ray casting will be applied
            Vector3[] meshPos   = new Vector3[4];
            int[]     meshFaces = new int[6];

            meshPos[0] = new Vector3(-1, -1, 0);
            meshPos[1] = new Vector3( 1, -1, 0);
            meshPos[2] = new Vector3( 1,  1, 0);
            meshPos[3] = new Vector3(-1,  1, 0);

            meshFaces[0] = 0; meshFaces[1] = 1; meshFaces[2] = 2;
            meshFaces[3] = 0; meshFaces[4] = 2; meshFaces[5] = 3;
            m_mesh.vertices  = meshPos;
            m_mesh.triangles = meshFaces;
            m_mesh.UploadMeshData(false);
            m_mesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 1, 1));

            //Update the 3D texture coordinates
            OnRangeColorChange(0.0f, 1.0f, m_vtkSubDataset.ColorMode);

            //Add external 3D objects
            m_outline = GameObject.Instantiate(Outline);
            m_outline.transform.parent = transform;
            m_outline.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
            m_outline.transform.localScale    = new Vector3(1, 1, 1);
            m_outline.transform.localRotation = Quaternion.identity;

            //Change our transformation matrix
            transform.localScale = new Vector3(descPts.Size[0] / m_dimensions.x,
                                               descPts.Size[1] / m_dimensions.y,
                                               descPts.Size[2] / m_dimensions.z);
            return true;
        }

        /// <summary>
        /// Change the color range to apply (0.0 = minimum value, 1.0 = maximum value)
        /// <param name="minRange"> The minimum range to apply</param>
        /// <param name="maxRange"> The maximum range to apply</param>
        /// <param name="mode"> The color mode to apply</param>
        /// </summary>
        public void OnRangeColorChange(float minRange, float maxRange, ColorMode mode)
        {
            float minVal = m_vtkSubDataset.MinAmplitude;
            float maxVal = m_vtkSubDataset.MaxAmplitude;

            Color[] colors = new Color[m_dimensions.x*m_dimensions.y*m_dimensions.z];

            //Determine transfer function coordinate
            for(int k = 0; k < m_dimensions.z; k++)
                for(int j = 0; j < m_dimensions.y; j++)
                    for(int i = 0; i < m_dimensions.x; i++)
                    {
                        int colorValueOff = i+j*m_dimensions.x+k*m_dimensions.x*m_dimensions.y;

                        float v = m_values[colorValueOff];
                        if(v < minRange || v > maxRange)
                            colors[colorValueOff] = new Color(0, 0, 0, 0);
                        else
                            colors[colorValueOff] = new Color(v, m_grads[colorValueOff], 0, 0);
                    }
            m_texture3D.SetPixels(colors);
            m_texture3D.Apply();
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
                        /*if(mask != null && *(mask + fieldOff) == 0)
                        {
                            m_values[colorValueOff] = minVal;
                            m_grads[colorValueOff]  = m_minGrad;
                            continue;
                        }*/

                        //Value
                        float c = val.ReadAsFloat(fieldOff*fieldValue.NbValuesPerTuple);
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
                            m_grads[colorValueOff]  = m_minGrad;
                            continue;
                        }

                        //Gradient
                        UInt64 indX1 = (i-1)+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indX2 = (i+1)+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indY1 = i+descPts.Size[0]*(j-1)+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indY2 = i+descPts.Size[0]*(j+1)+descPts.Size[1]*descPts.Size[0]*k;
                        UInt64 indZ1 = i+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*(k-1);
                        UInt64 indZ2 = i+descPts.Size[0]*j+descPts.Size[1]*descPts.Size[0]*(k+1);

                        Vector3 grad = new Vector3((float)((val.ReadAsFloat(indX1 * fieldValue.NbValuesPerTuple) - val.ReadAsFloat(indX2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[0]),
                                                   (float)((val.ReadAsFloat(indY1 * fieldValue.NbValuesPerTuple) - val.ReadAsFloat(indY2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[1]),
                                                   (float)((val.ReadAsFloat(indZ1 * fieldValue.NbValuesPerTuple) - val.ReadAsFloat(indZ2 * fieldValue.NbValuesPerTuple))/descPts.Spacing[2]));

                        m_grads[colorValueOff] = (float)((grad.magnitude - m_minGrad)/(m_maxGrad-m_minGrad));
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

                    UInt64 colorValueOff1 = (UInt64)(i+m_dimensions.x*m_dimensions.y*k);
                    UInt64 colorValueOff2 = (UInt64)(i+m_dimensions.x*(m_dimensions.y-1)+m_dimensions.x*m_dimensions.y*k);
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            }

            for(UInt32 k = 0; k < m_dimensions.z; k++)
            {
                for(UInt32 j = 0; j < m_dimensions.y; j++)
                {

                    UInt64 colorValueOff1 = (UInt64)(m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);
                    UInt64 colorValueOff2 = (UInt64)(m_dimensions.x-1+m_dimensions.x*j+m_dimensions.x*m_dimensions.y*k);
                    m_grads[colorValueOff1] = m_grads[colorValueOff2] = 0.0f;
                }
            }
        }
    }
}