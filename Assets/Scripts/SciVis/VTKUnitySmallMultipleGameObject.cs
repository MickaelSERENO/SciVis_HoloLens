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
        /// The new scale received from the SmallMultiple
        /// </summary>
        private Vector3 m_newS;

        /// <summary>
        /// Should we update the rotation quaternion?
        /// </summary>
        private bool m_updateQ = false;

        /// <summary>
        /// Should we update the 3D position?
        /// </summary>
        private bool m_updateP = false;

        /// <summary>
        /// Should we update the 3D scaling?
        /// </summary>
        private bool m_updateS = false;

        /// <summary>
        /// Object providing the needed data
        /// </summary>
        private IDataProvider m_dataProvider = null;

        /// <summary>
        /// The new outline color to apply
        /// </summary>
        private Color m_outlineColor;

        /// <summary>
        /// Should we update the outline color?
        /// </summary>
        private bool m_updateOutlineColor = false;

        /// <summary>
        /// Initialize the visualization
        /// </summary>
        /// <param name="sm">The small multiple data to use</param>
        public void Init(VTKUnitySmallMultiple sm, IDataProvider provider)
        {
            m_dataProvider = provider;
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

            //Update position / rotation / scaling
            lock(m_sm)
            {
                OnRotationChange(m_sm.VTKSubDataset, m_sm.VTKSubDataset.Rotation);
                OnPositionChange(m_sm.VTKSubDataset, m_sm.VTKSubDataset.Position);
                OnScaleChange(m_sm.VTKSubDataset, m_sm.VTKSubDataset.Scale);
                m_sm.VTKSubDataset.AddListener(this);
            }

            m_outlineColor = m_dataProvider.GetHeadsetColor(-1);
        }

        public void OnColorRangeChange(SubDataset dataset, float min, float max)
        {
        }

        public void OnOwnerIDChange(SubDataset dataset, int ownerID)
        {
            lock(this)
            {
                if(m_dataProvider != null)
                {
                    m_updateOutlineColor = true;
                    m_outlineColor = m_dataProvider.GetHeadsetColor(ownerID);
                }
            }
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

        public void OnScaleChange(SubDataset dataset, float[] scale)
        {
            lock(this)
            {
                m_newS    = new Vector3(scale[0], scale[1], scale[2]);
                m_updateS = true;
            }
        }

        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {}

        void LateUpdate()
        {
            //Update the 3D texture
            lock(m_sm)
            {
                if(m_sm.TextureColor != null)
                {
                    m_texture3D = new Texture3D(m_sm.Dimensions.x, m_sm.Dimensions.y, m_sm.Dimensions.z, TextureFormat.RGBA32, false);
                    m_texture3D.wrapModeU  = TextureWrapMode.Clamp;
                    m_texture3D.wrapModeV  = TextureWrapMode.Clamp;
                    m_texture3D.wrapModeW  = TextureWrapMode.Clamp;
                    m_texture3D.SetPixels32(m_sm.TextureColor);
                    m_texture3D.Apply();
                    m_sm.TextureColor = null;
                }
            }

            //Update the 3D transform of this game object
            lock(this)
            {
                if(m_updateP)
                    transform.localPosition = m_newP;
                m_updateP = false;

                if(m_updateQ)
                    transform.localRotation = m_newQ;
                m_updateQ = false;

                if(m_updateS)
                    transform.localScale = m_newS;
                m_updateS = false;

                if(m_updateOutlineColor)
                    m_outline.GetComponent<MeshRenderer>().material.color = m_outlineColor;
                m_updateOutlineColor = false;
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

            if(Camera.main != null)
            {
                //Determine the part of the object on screen to narrow down the viewport
                Matrix4x4 mvp = (GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true) * Camera.main.worldToCameraMatrix * transform.localToWorldMatrix);

                Vector3[] localPos = new Vector3[8];
                localPos[0] = m_mesh.bounds.min;
                localPos[1] = m_mesh.bounds.max;
                localPos[2] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.min.y, m_mesh.bounds.max.z);
                localPos[3] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.max.y, m_mesh.bounds.min.z);
                localPos[4] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.min.y, m_mesh.bounds.min.z);
                localPos[5] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.min.y, m_mesh.bounds.max.z);
                localPos[6] = new Vector3(m_mesh.bounds.max.x, m_mesh.bounds.max.y, m_mesh.bounds.min.z);
                localPos[7] = new Vector3(m_mesh.bounds.min.x, m_mesh.bounds.max.y, m_mesh.bounds.max.z);

                Vector3 screenPos = mvp * localPos[0];
                Vector2 minScreenPos = new Vector2(screenPos.x, screenPos.y);
                Vector2 maxScreenPos = new Vector2(screenPos.x, screenPos.y);

                for(int i = 1; i < 8; i++)
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

                //Update the mesh
                Vector3[] meshPos = new Vector3[4];
                meshPos[0] = new Vector3(minScreenPos.x, minScreenPos.y, 0);
                meshPos[1] = new Vector3(maxScreenPos.x, minScreenPos.y, 0);
                meshPos[2] = new Vector3(maxScreenPos.x, maxScreenPos.y, 0);
                meshPos[3] = new Vector3(minScreenPos.x, maxScreenPos.y, 0);
                
                //int[] meshFaces = new int[6];
                //meshFaces[0] = 0; meshFaces[1] = 1; meshFaces[2] = 2;
                //meshFaces[3] = 0; meshFaces[4] = 2; meshFaces[5] = 3;

                m_mesh.vertices  = meshPos;
                //m_mesh.triangles = meshFaces;
                m_mesh.UploadMeshData(false);
            }
        }
    }
}