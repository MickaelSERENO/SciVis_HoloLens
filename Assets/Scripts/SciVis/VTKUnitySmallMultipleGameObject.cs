using UnityEngine;

namespace Sereno.SciVis
{
    /// <summary>
    /// Visualization of VTK Unity Small Multiple
    /// </summary>
    public class VTKUnitySmallMultipleGameObject : MonoBehaviour
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
            m_outline.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
            m_outline.transform.localScale    = new Vector3(1, 1, 1);
            m_outline.transform.localRotation = Quaternion.identity;

            //Change our transformation matrix
            transform.localScale = new Vector3(m_sm.DescPts.Size[0] / m_sm.Dimensions.x,
                                               m_sm.DescPts.Size[1] / m_sm.Dimensions.y,
                                               m_sm.DescPts.Size[2] / m_sm.Dimensions.z);
        }
        
        private void Update()
        {
            //Update the 3D texture
            lock(m_sm)
            {
                if(m_sm.TextureColor != null)
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

            //Update the vtk sub dataset
            lock(m_sm.VTKSubDataset)
            {
                if(m_sm.VTKSubDataset.RotationUpdated)
                {
                    m_sm.VTKSubDataset.RotationUpdated = false;
                    transform.rotation = new Quaternion(m_sm.VTKSubDataset.Rotation[1],
                                                        m_sm.VTKSubDataset.Rotation[2],
                                                        m_sm.VTKSubDataset.Rotation[3],
                                                        m_sm.VTKSubDataset.Rotation[0]);
                }
            }

            //Draw the GameObject
            if(m_mesh != null && m_material != null)
            {
                m_material.SetTexture("_TFTexture", m_sm.VTKSubDataset.TFTexture.Texture);
                m_material.SetTexture("_TextureData", m_texture3D);
                Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_material, 1);
            }
        }
    }
}