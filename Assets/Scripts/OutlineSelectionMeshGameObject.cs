using System;
using System.Collections;
using System.Collections.Generic;
using Thirdparties;
using UnityEngine;

namespace Sereno
{
    public class OutlineSelectionMeshGameObject : MonoBehaviour
    {
        public TubeRenderer SelectionMeshPrefab;

        private OutlineSelectionMeshData m_model = null;

        private List<TubeRenderer> m_lassoMeshes = new List<TubeRenderer>();
        private List<TubeRenderer> m_connectionMeshes = new List<TubeRenderer>();

        public void Init(OutlineSelectionMeshData model)
        {
            m_model = model;
        }

        // Update is called once per frame
        void Update()
        {
            if (m_model == null)
                return;

            lock (m_model)
            {
                if (!m_model.ShouldUpdate)
                    return;

                m_model.ShouldUpdate = false;
                Func<TubeRenderer> genTubeRenderer = () =>
                {
                    TubeRenderer go = Instantiate(SelectionMeshPrefab);
                    go.gameObject.SetActive(true);
                    go.transform.parent = this.transform;
                    go.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                    go.transform.localRotation = Quaternion.identity;

                    return go;
                };

                //Generate objects
                for (int i = m_lassoMeshes.Count; i < m_model.LassoPoints.Count; i++)
                    m_lassoMeshes.Add(genTubeRenderer());
                for (int i = m_connectionMeshes.Count; i < m_model.ConnectionPoints.Count; i++)
                    m_connectionMeshes.Add(genTubeRenderer());

                //Update objects
                for (int i = 0; i < m_model.LassoPoints.Count; i++)
                    m_lassoMeshes[i].SetPositions(m_model.LassoPoints[i].ToArray());
                for (int i = 0; i < m_model.ConnectionPoints.Count; i++)
                    m_connectionMeshes[i].SetPositions(m_model.ConnectionPoints[i].ToArray());
            }
        }
    }
}