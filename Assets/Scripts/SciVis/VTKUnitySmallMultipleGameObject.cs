using System;
using Sereno.Datasets;
using UnityEngine;
using static UnityEngine.Camera;

namespace Sereno.SciVis
{
    /// <summary>
    /// Visualization of VTK Unity Small Multiple
    /// </summary>
    public class VTKUnitySmallMultipleGameObject : DefaultSubDatasetGameObject
    {
        /// <summary>
        /// Material to use
        /// </summary>
        public Material ColorMaterial = null;

        /// <summary>
        /// The VTK Prefab to create miniatures
        /// </summary>
        public VTKUnitySmallMultipleGameObject VTKMiniaturePrefab;

        /// <summary>
        /// The mesh to use
        /// </summary>
        private Mesh m_mesh = null;

        /// <summary>
        /// The material in use (copy from the Material setted in the Unity Editor)
        /// </summary>
        private Material m_material;

        /// <summary>
        /// The Texture 3D representing this Dataset
        /// </summary>
        private Texture3D m_texture3D;

        /// <summary>
        /// The model bound
        /// </summary>
        private VTKUnitySmallMultiple m_sm;

        /// <summary>
        /// Initialize the visualization
        /// </summary>
        /// <param name="sm">The small multiple data to use</param>
        public void Init(VTKUnitySmallMultiple sm, IDataProvider provider, bool isMiniature=false)
        {
            base.Init(sm.SubDataset, provider, isMiniature);
            m_sm = sm;

            m_material = new Material(ColorMaterial);            

            //Compute the Mesh. A regular rectangle where ray casting will be applied
            Vector3[] meshPos   = new Vector3[4];
            int[]     meshFaces = new int[6];

            meshPos[0] = new Vector3(-1, -1, 0);
            meshPos[1] = new Vector3( 1, -1, 0);
            meshPos[2] = new Vector3( 1,  1, 0);
            meshPos[3] = new Vector3(-1,  1, 0);

            meshFaces[0] = 0; meshFaces[1] = 1; meshFaces[2] = 2;
            meshFaces[3] = 0; meshFaces[4] = 2; meshFaces[5] = 3;

            m_mesh           = new Mesh();
            m_mesh.vertices  = meshPos;
            m_mesh.triangles = meshFaces;
            m_mesh.UploadMeshData(false);
            m_mesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            
            //Change our transformation matrix
            float maxRatio = Math.Max(m_sm.DescPts.Size[0]/m_sm.Dimensions.x,
                                      Math.Max(m_sm.DescPts.Size[1]/m_sm.Dimensions.y,
                                               m_sm.DescPts.Size[2]/m_sm.Dimensions.z));

            transform.localScale = new Vector3((m_sm.DescPts.Size[0]/m_sm.Dimensions.x)/maxRatio,
                                               (m_sm.DescPts.Size[1]/m_sm.Dimensions.y)/maxRatio,
                                               (m_sm.DescPts.Size[2]/m_sm.Dimensions.z)/maxRatio);

            LinkToSM();

            m_outlineColor = m_dataProvider.GetHeadsetColor(-1);
            Check3DTexture();
        }

        public void Check3DTexture()
        {
            if (!m_isMiniature)
            {
                lock(m_sm)
                { 
                    if (m_sm.TextureColor != null)
                    {
                        m_texture3D = new Texture3D(m_sm.Dimensions.x, m_sm.Dimensions.y, m_sm.Dimensions.z, TextureFormat.RGBA32, false);
                        m_texture3D.wrapModeU = TextureWrapMode.Clamp;
                        m_texture3D.wrapModeV = TextureWrapMode.Clamp;
                        m_texture3D.wrapModeW = TextureWrapMode.Clamp;
                        m_texture3D.SetPixels32(m_sm.TextureColor);
                        m_texture3D.Apply();
                        Debug.Log("Applied new texture");
                        m_sm.TextureColor = null;
                    }
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void LateUpdate()
        {
            base.LateUpdate();

            //Update the 3D texture
            if (!m_isMiniature)
            {
                lock (m_sm)
                {
                    Check3DTexture();
                }
            }

            //Draw the GameObject
            if (m_mesh != null && m_material != null)
            {
                UpdateMaterial();
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_material, 0, Camera.main);
            }
        }

        private void UpdateMaterial()
        {
            m_material.SetTexture("_TextureData", m_texture3D);
            m_material.SetFloat("_MaxDimension", Math.Max(Math.Max(m_sm.Dimensions.x, m_sm.Dimensions.y), m_sm.Dimensions.z));

            Vector2 minScreenPos = new Vector2(-1, -1);
            Vector2 maxScreenPos = new Vector2(1 ,  1);

            if (Camera.main != null)
            {
                StereoscopicEye[] enumEye = new StereoscopicEye[2];
                enumEye[0] = StereoscopicEye.Left;
                enumEye[1] = StereoscopicEye.Right;

                foreach(StereoscopicEye eye in enumEye)
                { 
                    //Determine the part of the object on screen to narrow down the viewport
                    Matrix4x4 mvp = (GL.GetGPUProjectionMatrix(Camera.main.GetStereoProjectionMatrix(eye), true) * Camera.main.GetStereoViewMatrix(eye) * transform.localToWorldMatrix);

                    Vector3[] localPos = new Vector3[8];
                    localPos[0] = m_mesh.bounds.min;
                    localPos[1] = m_mesh.bounds.max;
                    localPos[2] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.min.y, m_mesh.bounds.max.z);
                    localPos[3] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.max.y, m_mesh.bounds.min.z);
                    localPos[4] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.min.y, m_mesh.bounds.min.z);
                    localPos[5] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.min.y, m_mesh.bounds.max.z);
                    localPos[6] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.max.y, m_mesh.bounds.min.z);
                    localPos[7] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.max.y, m_mesh.bounds.max.z);

                    Vector3 screenPos;

                    for(int i = 0; i < 8; i++)
                    {
                        screenPos = mvp * localPos[i];
                        //Min
                        if (minScreenPos.x > screenPos.x)
                            minScreenPos.x = screenPos.x;
                        if (minScreenPos.y > screenPos.y)
                            minScreenPos.y = screenPos.y;

                        //Max
                        if (maxScreenPos.x < screenPos.x)
                            maxScreenPos.x = screenPos.x;
                        if (maxScreenPos.y < screenPos.y)
                            maxScreenPos.y = screenPos.y;
                    }
                }
                //Update the mesh
                Vector3[] meshPos = new Vector3[4];
                meshPos[0] = new Vector3(minScreenPos.x, minScreenPos.y, 0);
                meshPos[1] = new Vector3(maxScreenPos.x, minScreenPos.y, 0);
                meshPos[2] = new Vector3(maxScreenPos.x, maxScreenPos.y, 0);
                meshPos[3] = new Vector3(minScreenPos.x, maxScreenPos.y, 0);

                m_mesh.vertices  = meshPos;
                m_mesh.UploadMeshData(false);
            }
        }

        /// <summary>
        /// Create a new miniature. Updates on the subdataset states will also change this miniature. However linking to another SubDataset will break the link between these objects!
        /// </summary>
        /// <returns>The created GameObject</returns>
        public override DefaultSubDatasetGameObject CreateMiniature()
        {
            VTKUnitySmallMultipleGameObject vtkSDGO = Instantiate(VTKMiniaturePrefab);
            vtkSDGO.Init(m_sm, m_dataProvider, true);
            vtkSDGO.m_texture3D = m_texture3D;

            return vtkSDGO;
        }
    }
}