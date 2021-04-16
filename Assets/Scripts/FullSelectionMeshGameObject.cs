using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    public class FullSelectionMeshGameObject : MonoBehaviour
    {
        private FullSelectionMeshData m_model = null;

        private Mesh m_mesh = null;

        /// <summary>
        /// The prefab of the graphical mesh game object
        /// </summary>
        public GameObject SelectionMeshPrefab;

        public void Init(FullSelectionMeshData model)
        {
            m_model = model;

            GameObject go = Instantiate(SelectionMeshPrefab);
            go.SetActive(true);
            go.transform.parent = this.transform;
            go.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            go.transform.localRotation = Quaternion.identity;

            m_mesh = new Mesh();
            m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            go.GetComponent<MeshFilter>().mesh = m_mesh;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (m_model == null)
                return;
            lock (m_model)
            {
                if (m_model.ShouldUpdate)
                {
                    m_mesh.vertices = m_model.Points.ToArray();
                    m_mesh.triangles = m_model.Triangles.ToArray();

                    m_model.ShouldUpdate = false;
                }
            }
        }
    }
}