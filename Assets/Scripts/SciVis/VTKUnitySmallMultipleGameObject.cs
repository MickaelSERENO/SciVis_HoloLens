using System;
using System.Collections.Generic;
using Sereno.Datasets;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using static UnityEngine.Camera;
using UnityEngine.Rendering;
using System.IO;

namespace Sereno.SciVis
{
    /// <summary>
    /// RenderTexture Data class use to render textures on screen
    /// </summary>
    public class RenderTextureData
    {
        /// <summary>
        /// The RenderTexture to render
        /// </summary>
        public RenderTexture RenderTexture;

        /// <summary>
        /// The used material
        /// </summary>
        public Material      Material;

        /// <summary>
        /// The command buffer used to render that object on a texture
        /// </summary>
        public CommandBuffer CommandBuffer;
    }


    /// <summary>
    /// Visualization of VTK Unity Small Multiple
    /// </summary>
    public class VTKUnitySmallMultipleGameObject : DefaultSubDatasetGameObject
    {
        /// <summary>
        /// Material to use in the "normal scale" mode
        /// </summary>
        public Material ColorMaterial = null;


        /// <summary>
        /// Material to use in the "down scale" mode
        /// </summary>
        public Material ColorMaterialDownScale = null;


        /// <summary>
        /// The copy material to use
        /// </summary>
        public Material CopyTextureMaterial = null;

        /// <summary>
        /// The VTK Prefab to create miniatures
        /// </summary>
        public VTKUnitySmallMultipleGameObject VTKMiniaturePrefab;

        /// <summary>
        /// The resolution scaling to apply. 1 == same scale 
        /// </summary>
        [Range(0f, 1f)]
        public float ResolutionScaling = 1f;

        /// <summary>
        /// The model prefab to use when the data is not yet loaded    
        /// </summary>
        public GameObject UnloadModel;

        /// <summary>
        /// The mesh to use
        /// </summary>
        private Mesh m_mesh = null;

        /// <summary>
        /// The screen texture Mesh use to "Blit" the texture on screen
        /// </summary>
        private Mesh m_screenTextureMesh = null;

        /// <summary>
        /// The material in use for the normal scale (copy from the Material setted in the Unity Editor)
        /// </summary>
        private Material m_materialNormalScale;

        /// <summary>
        /// The material in use for the downscale format of this object (copy from the Material setted in the Unity Editor)
        /// </summary>
        private Material m_materialDownScale;

        /// <summary>
        /// The copy material to use (copy from the CopyMaterial setted in the Unity Editor)
        /// </summary>
        private Material m_copyMaterial;

        /// <summary>
        /// The model to use when the data is not yet loaded    
        /// </summary>
        private GameObject m_unloadModel;

        /// <summary>
        /// The Texture 3D representing this Dataset
        /// </summary>
        private Texture3D m_texture3D = null;

        /// <summary>
        /// The model bound
        /// </summary>
        private VTKUnitySmallMultiple m_sm;

        /// <summary>
        /// The RenderTexture to use for the volumetric rendering. One per camera
        /// </summary>
        private Dictionary<Camera, RenderTextureData> m_renderTextures = new Dictionary<Camera, RenderTextureData>();
        
        /// <summary>
        /// Initialize the visualization
        /// </summary>
        /// <param name="sm">The small multiple data to use</param>
        public void Init(VTKUnitySmallMultiple sm, IDataProvider provider, bool isMiniature=false)
        {
            base.Init(sm.SubDataset, provider, isMiniature);
            
            m_unloadModel = Instantiate(UnloadModel, transform);
            m_unloadModel.SetActive(!m_isMiniature);
            m_materialNormalScale = new Material(ColorMaterial);
            m_materialDownScale   = new Material(ColorMaterialDownScale);

            m_sm = sm;

            //Compute the Mesh. A regular rectangle where ray casting will be applied
            Vector2[] meshUV    = new Vector2[4];
            Vector3[] meshPos   = new Vector3[4];
            int[]     meshFaces = new int[6];

            meshPos[0] = new Vector3(-1, -1, 0);
            meshPos[1] = new Vector3( 1, -1, 0);
            meshPos[2] = new Vector3( 1,  1, 0);
            meshPos[3] = new Vector3(-1,  1, 0);

            meshUV[0] = new Vector2(0, 0);
            meshUV[1] = new Vector2(1, 0);
            meshUV[2] = new Vector2(1, 1);
            meshUV[3] = new Vector2(0, 1);

            meshFaces[0] = 0; meshFaces[1] = 1; meshFaces[2] = 2;
            meshFaces[3] = 0; meshFaces[4] = 2; meshFaces[5] = 3;

            m_mesh           = new Mesh();
            m_mesh.vertices  = meshPos;
            m_mesh.triangles = meshFaces;
            m_mesh.UploadMeshData(false);
            m_mesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 1, 1));

            //Quad screen mesh
            m_screenTextureMesh = new Mesh();
            m_screenTextureMesh.vertices  = meshPos;
            m_screenTextureMesh.triangles = meshFaces;
            m_screenTextureMesh.uv        = meshUV;
            m_screenTextureMesh.UploadMeshData(false);
            m_screenTextureMesh.bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 1, 1));

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
                //lock(m_sm) //I guess this is not necessary because the smallmultiple only apply a reference, i.e., atomic operation
                {
                    if (m_sd.OwnerID != -1 && m_sd.OwnerID != m_dataProvider.GetHeadsetID()) //Check owner
                    {
                        m_unloadModel.SetActive(true);
                        m_unloadModel.GetComponent<MeshRenderer>().material.color = m_dataProvider.GetHeadsetColor(m_sd.OwnerID);
                        m_texture3D = null;
                        m_materialNormalScale.SetTexture("_TextureData", m_texture3D);
                        m_materialDownScale.SetTexture("_TextureData", m_texture3D);
                    }

                    else if (m_sm.TextureColor != null) //New data?
                    {
                        m_texture3D = new Texture3D(m_sm.Dimensions.x, m_sm.Dimensions.y, m_sm.Dimensions.z, TextureFormat.RGBA4444, false);
                        m_texture3D.wrapModeU  = TextureWrapMode.Clamp;
                        m_texture3D.wrapModeV  = TextureWrapMode.Clamp;
                        m_texture3D.wrapModeW  = TextureWrapMode.Clamp;
                        m_texture3D.SetPixelData<short>(m_sm.TextureColor, 0);
                        m_texture3D.Apply();
                        m_unloadModel.SetActive(false);
                        m_sm.TextureColor = null;

                        m_materialNormalScale.SetTexture("_TextureData", m_texture3D);
                        m_materialDownScale.SetTexture("_TextureData", m_texture3D);
                    }

                    else if(m_texture3D == null) //Default color
                    {
                        m_unloadModel.GetComponent<MeshRenderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
                    }
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            //Release all the render textures allocated
            foreach (var v in m_renderTextures)
                v.Value.RenderTexture.Release();
        }

        public override void LateUpdate()
        {
            base.LateUpdate();

            if (m_sm == null)
                return;

            //Update the 3D texture
            Check3DTexture();

            //Draw the GameObject on render texture if the scaling needs to diminish
            if(ResolutionScaling < 1f)
                RenderToTextures();

            else if(m_mesh != null && m_materialNormalScale != null && m_texture3D != null)
            {
                UpdateMaterial(Camera.main);
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_materialNormalScale, 8, null, 0, null, false, false, false);
            }
        }

        private void RenderToTextures()
        {
            if (m_mesh != null && m_materialDownScale != null && m_texture3D != null)
            {
                foreach(Camera cam in Camera.allCameras)
                {
                    //Active and layer is correct
                    if(!(cam.isActiveAndEnabled && (cam.cullingMask & (1 << gameObject.layer)) != 0))
                        continue;

                    //Search for the correct render texture
                    var dictPair = m_renderTextures.FirstOrDefault(e => e.Key == cam);
                    RenderTextureData renderTexture = null;

                    if (dictPair.Key == null)
                    {
                        //Initialize a RenderTexture to render on it
                        RenderTextureDescriptor eyeDesc = XRSettings.eyeTextureDesc;
                        eyeDesc.depthBufferBits = 0; //No need of depth buffer
                        eyeDesc.width  = (int)(eyeDesc.width  * ResolutionScaling);
                        eyeDesc.height = (int)(eyeDesc.height * ResolutionScaling);
                        renderTexture = new RenderTextureData { RenderTexture = new RenderTexture(eyeDesc), Material = new Material(CopyTextureMaterial) };

                        //Initialize the material
                        renderTexture.Material.SetShaderPassEnabled("BackToFront", false);
                        renderTexture.Material.SetTexture("_MainTex", renderTexture.RenderTexture);
                                                                     
                        //Initialize the command buffers associated
                        renderTexture.CommandBuffer = new CommandBuffer();
                        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, renderTexture.CommandBuffer);

                        m_renderTextures.Add(cam, renderTexture);
                    }
                    else
                        renderTexture = dictPair.Value;

                    //Check if we need to resize the texture
                    if (renderTexture.RenderTexture.width != (int)(XRSettings.eyeTextureDesc.width * ResolutionScaling) || renderTexture.RenderTexture.height != (int)(XRSettings.eyeTextureDesc.height * ResolutionScaling))
                    {
                        Debug.Log($"Created a {renderTexture.RenderTexture.dimension} texture");

                        RenderTextureDescriptor eyeDesc = XRSettings.eyeTextureDesc;
                        //Initialize a RenderTexture to render on it
                        eyeDesc.depthBufferBits = 0; //No need of depth buffer
                        eyeDesc.width  = (int)(eyeDesc.width * ResolutionScaling);
                        eyeDesc.height = (int)(eyeDesc.height * ResolutionScaling);

                        renderTexture.RenderTexture.Release();
                        renderTexture.RenderTexture = new RenderTexture(eyeDesc);
                        renderTexture.Material.SetTexture("_MainTex", renderTexture.RenderTexture);
                    }

                    //Clear the render texture
                    //Draw the volumetric data
                    UpdateMaterial(cam);

                    renderTexture.CommandBuffer.Clear();
                    renderTexture.CommandBuffer.SetRenderTarget(renderTexture.RenderTexture, 0, CubemapFace.Unknown, -1); //-1 == all the color buffers (I guess, this is undocumented)
                    renderTexture.CommandBuffer.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
                    renderTexture.CommandBuffer.SetSinglePassStereo((SinglePassStereoMode)XRSettings.stereoRenderingMode);
                    //if(XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                    //    renderTexture.CommandBuffer.SetInstanceMultiplier(2);
                    renderTexture.CommandBuffer.DrawMesh(m_mesh, transform.localToWorldMatrix, m_materialDownScale, 0);
                    Graphics.DrawMesh(m_screenTextureMesh, transform.localToWorldMatrix, renderTexture.Material, gameObject.layer, cam, 0, null, false, false, false);
                }
            }
        }

        /*private void OnRenderObject()
        {
            if (m_renderTextures.ContainsKey(Camera.main))
            {
                RenderTextureData renderTexture = m_renderTextures[Camera.main];
                RenderTexture _active = RenderTexture.active;
                RenderTexture.active = renderTexture.RenderTexture;
                {
                    Texture2D texture = new Texture2D(renderTexture.RenderTexture.width, renderTexture.RenderTexture.height, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, renderTexture.RenderTexture.width, renderTexture.RenderTexture.height), 0, 0);
                    string path = $"C:/Temp/{Time.time}.png";
                    File.Create(path).Close();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                RenderTexture.active = _active;
            }
        }*/

        /// <summary>
        /// Update the material and the mesh of the GameObject
        /// </summary>
        /// <param name="camera">The camera used to determine the screen boundaries of the raymarching algorithm</param>
        private void UpdateMaterial(Camera camera)
        {
            UpdateMesh(camera);
            if (ResolutionScaling < 1f)
                m_materialDownScale.SetVector("_Dimensions", new Vector3(m_sm.Dimensions.x, m_sm.Dimensions.y, m_sm.Dimensions.z));
            else
                m_materialNormalScale.SetVector("_Dimensions", new Vector3(m_sm.Dimensions.x, m_sm.Dimensions.y, m_sm.Dimensions.z));
        }

        /// <summary>
        /// Update the mesh of the GameObject. The mesh represent the quad where the raymarching algorithm will perform
        /// </summary>
        /// <param name="camera">The camera used to determine the screen boundaries of the raymarching algorithm</param>
        private void UpdateMesh(Camera camera)
        {
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
                    Matrix4x4 mvp = (GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(eye), true) * camera.GetStereoViewMatrix(eye) * transform.localToWorldMatrix);

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