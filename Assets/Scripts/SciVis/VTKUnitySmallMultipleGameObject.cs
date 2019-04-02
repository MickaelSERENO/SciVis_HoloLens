using System;
using Sereno.Datasets;
using UnityEngine;

namespace Sereno.SciVis
{
    /// <summary>
    /// Visualization of VTK Unity Small Multiple
    /// </summary>
    public class VTKUnitySmallMultipleGameObject : MonoBehaviour, ISubDatasetCallback
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
        /// The Texture 3D representing this Dataset
        /// </summary>
        private Texture3D m_texture3D;

        /// <summary>
        /// The model bound
        /// </summary>
        private VTKUnitySmallMultiple m_sm;

        /// <summary>
        /// The new Quaternion received from the SmallMultiple.
        /// </summary>
        private Quaternion m_newQ;

        /// <summary>
        /// The new position received from the SmallMultiple.
        /// </summary>
        private Vector3 m_newP;

        /// <summary>
        /// Should we update the rotation quaternion?
        /// </summary>
        private bool m_updateQ = false;

        /// <summary>
        /// Should we update the 3D position?
        /// </summary>
        private bool m_updateP = false;

        /// <summary>
        /// Initialize the visualization
        /// </summary>
        /// <param name="sm">The small multiple data to use</param>
        public void Init(VTKUnitySmallMultiple sm)
        {
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

            //Add external 3D objects
            m_outline = GameObject.Instantiate(Outline);
            m_outline.transform.parent = transform;
            m_outline.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            m_outline.transform.localScale    = new Vector3(1, 1, 1);
            m_outline.transform.localRotation = Quaternion.identity;

            //Change our transformation matrix
            float maxRatio = Math.Max(m_sm.DescPts.Size[0]/m_sm.Dimensions.x,
                                      Math.Max(m_sm.DescPts.Size[1]/m_sm.Dimensions.y,
                                               m_sm.DescPts.Size[2]/m_sm.Dimensions.z));

            transform.localScale = new Vector3((m_sm.DescPts.Size[0]/m_sm.Dimensions.x)/maxRatio,
                                               (m_sm.DescPts.Size[1]/m_sm.Dimensions.y)/maxRatio,
                                               (m_sm.DescPts.Size[2]/m_sm.Dimensions.z)/maxRatio);

            //Update position / rotation
            lock(m_sm)
            {
                OnRotationChange(m_sm.VTKSubDataset, m_sm.VTKSubDataset.Rotation);
                OnPositionChange(m_sm.VTKSubDataset, m_sm.VTKSubDataset.Position);
                m_sm.VTKSubDataset.AddListener(this);
            }
        }

        public void OnColorRangeChange(SubDataset dataset, float min, float max, ColorMode mode)
        {
        }

        public void OnPositionChange(SubDataset dataset, float[] position)
        {
            lock(this)
            {
                m_updateP = true;
                m_newP    = new Vector3(position[0], position[1], position[2]); 
            }
        }

        public void OnRotationChange(SubDataset dataset, float[] rotationQuaternion)
        {
            lock(this)
            {
                m_newQ = new Quaternion(rotationQuaternion[1],
                                        rotationQuaternion[2],
                                        rotationQuaternion[3],
                                        rotationQuaternion[0]); 
                m_updateQ = true;
            }        
        }

        private void Update()
        {
            //Update the 3D texture
            lock (m_sm)
            {
                if (m_sm.TextureColor != null)
                {
                    m_texture3D = new Texture3D(m_sm.Dimensions.x, m_sm.Dimensions.y, m_sm.Dimensions.z, TextureFormat.RGFloat, true);
                    m_texture3D.wrapModeU = TextureWrapMode.Repeat;
                    m_texture3D.wrapModeV = TextureWrapMode.Repeat;
                    m_texture3D.wrapModeW = TextureWrapMode.Repeat;

                    m_texture3D.SetPixels(m_sm.TextureColor);
                    m_texture3D.Apply();
                    m_sm.TextureColor = null;
                }
            }

            //Update the 3D transform of this game object
            lock (this)
            {
                if (m_updateP)
                    transform.localPosition = m_newP;
                m_updateP = false;
                if (m_updateQ)
                    transform.localRotation = m_newQ;
                m_updateQ = false;
            }

            //Draw the GameObject
            if (m_mesh != null && m_material != null)
            {
                UpdateMaterial();
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_material, 0);
            }
        }

        private void UpdateMaterial()
        {
            m_material.SetTexture("_TFTexture", m_sm.VTKSubDataset.TFTexture.Texture);
            m_material.SetTexture("_TextureData", m_texture3D);
            if(Camera.main != null)
            {
                //Determine the part of the object on screen
                Matrix4x4 mvp = (GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true) * Camera.main.worldToCameraMatrix * transform.localToWorldMatrix);

                Vector3[] screenPos = new Vector3[8];
                Vector3[] localPos = new Vector3[8];
                localPos[0] = m_mesh.bounds.min;
                localPos[1] = m_mesh.bounds.max;
                localPos[2] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.min.y, m_mesh.bounds.max.z);
                localPos[3] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.max.y, m_mesh.bounds.min.z);
                localPos[4] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.min.y, m_mesh.bounds.min.z);
                localPos[5] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.min.y, m_mesh.bounds.max.z);
                localPos[6] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.max.y, m_mesh.bounds.min.z);
                localPos[7] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.max.y, m_mesh.bounds.max.z);

                screenPos[0] = mvp * localPos[0];
                Vector2 minScreenPos = new Vector2(screenPos[0].x, screenPos[0].y);
                Vector2 maxScreenPos = new Vector2(screenPos[0].x, screenPos[0].y);

                for(int i = 1; i < 8; i++)
                {
                    screenPos[i] = mvp * localPos[i];
                    //Min
                    if (minScreenPos.x > screenPos[i].x)
                        minScreenPos.x = screenPos[i].x;
                    if (minScreenPos.y > screenPos[i].y)
                        minScreenPos.y = screenPos[i].y;

                    //Max
                    if (maxScreenPos.x < screenPos[i].x)
                        maxScreenPos.x = screenPos[i].x;
                    if (maxScreenPos.y < screenPos[i].y)
                        maxScreenPos.y = screenPos[i].y;
                }

                //Update the mesh
                Vector3[] meshPos = new Vector3[4];
                meshPos[0] = new Vector3(minScreenPos.x, minScreenPos.y, 0);
                meshPos[1] = new Vector3(maxScreenPos.x, minScreenPos.y, 0);
                meshPos[2] = new Vector3(maxScreenPos.x, maxScreenPos.y, 0);
                meshPos[3] = new Vector3(minScreenPos.x, maxScreenPos.y, 0);
                
                int[] meshFaces = new int[6];
                meshFaces[0] = 0; meshFaces[1] = 1; meshFaces[2] = 2;
                meshFaces[3] = 0; meshFaces[4] = 2; meshFaces[5] = 3;

                m_mesh.vertices  = meshPos;
                m_mesh.triangles = meshFaces;
                m_mesh.UploadMeshData(false);

                if(SystemInfo.graphicsShaderLevel < 40)
                    print("no decent shaders supported...\n");
                if(m_sm != null)
                    m_material.SetFloat("_MaxDimension", Math.Max(Math.Max(m_sm.Dimensions.x, m_sm.Dimensions.y), m_sm.Dimensions.z));
            }
        }
    }
}