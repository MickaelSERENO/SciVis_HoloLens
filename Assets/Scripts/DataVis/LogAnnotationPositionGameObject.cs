using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Sereno.Datasets.Annotation;
using System.Collections.Generic;
using System;
using Thirdparties;
using System.Globalization;
using Sereno.SciVis;
using Sereno.Datasets;

namespace Sereno.DataVis
{
    /// <summary>
    /// The GameObject script allowing to update log annotations position information
    /// </summary>
    public class LogAnnotationPositionGameObject : MonoBehaviour, LogAnnotationComponent.ILogAnnotationComponentListener, 
                                                   LogAnnotationContainer.ILogAnnotationContainerListener, 
                                                   LogAnnotationPositionInstance.ILogAnnotationPositionInstanceListener,
                                                   ISubDatasetCallback
    {
        /// <summary>
        /// A class regrouping time + position. We use it for ordering the data (if it is not)
        /// </summary>
        private class AssociatedData : IComparable<AssociatedData>
        {
            public float   Time;
            public Vector3 Pos;
            public Int32   Idx;

            public int CompareTo(AssociatedData other)
            {
                if(other == null)
                    return 1;
                return Time.CompareTo(other.Time);
            }
        }

        /// <summary>
        /// Should we update the positions?
        /// </summary>
        private bool m_updatePos = false;

        /// <summary>
        /// Should we update the color of the material?
        /// </summary>
        private bool m_updateColor = false;

        /// <summary>
        /// The graphical component tube renderer 
        /// </summary>
        private TubeRenderer m_tubeRenderer = null;

        /// <summary>
        /// What are the current data to use?
        /// </summary>
        private List<AssociatedData> m_data = new List<AssociatedData>();

        private List<float[]> m_mappedData = new List<float[]>();

        private Int32[] m_oldIdx    = new Int32[0];
        private Int32[] m_mappedIdx = new Int32[0];

        /// <summary>
        /// What is the data model?
        /// </summary>
        public LogAnnotationPositionInstance Component;

        /// <summary>
        /// Initialize the object
        /// </summary>
        /// <param name="data">The data model to use (see Component)</param>
        public void Init(LogAnnotationPositionInstance data)
        {
            Component = data;
            Component.Component.AddListener(this);
            Component.Container.AddListener(this);
            Component.SubDataset.AddListener(this);
            Component.AddListener(this);
            m_tubeRenderer = gameObject.GetComponent<TubeRenderer>();
            ReadPosition();
            m_mappedIdx = (Int32[])Component.MappedIndices.Clone(); 
            m_updateColor = m_updatePos = true;
        }

        public void OnUpdateHeaders(LogAnnotationComponent component, List<int> oldHeaders)
        {
            ReadPosition();
        }

        public void OnSetTime(LogAnnotationContainer container)
        {
            ReadPosition();
        }

        public void OnSetColor(LogAnnotationPositionInstance comp)
        {
            lock (this)
                m_updateColor = true;
        }

        public void OnSetCurrentTime(LogAnnotationPositionInstance comp)
        {
            lock(this)
                m_updatePos = true;
        }

        public void OnSetMappedIndices(LogAnnotationPositionInstance comp, Int32[] old)
        {
            lock(this)
            {
                m_mappedIdx = (Int32[])comp.MappedIndices.Clone();
                m_updateColor = true;
            }
        }

        public void LateUpdate()
        {
            lock(this)
            {
                //Update the 3D positions of the line
                if(m_updatePos)
                {
                    List<Vector3> pos = new List<Vector3>();
                    if(Component.UseTime)
                        for(int i = 0; i < m_data.Count && m_data[i].Time < Component.CurrentTime; i++)
                            pos.Add(m_data[i].Pos);
                    else
                        for (int i = 0; i < m_data.Count; i++)
                            pos.Add(m_data[i].Pos);

                    m_tubeRenderer.SetPositions(pos.ToArray());
                    m_updatePos   = false;
                    m_updateColor = true;
                }

                //Update the color
                if(m_updateColor)
                {
                    //Use default color
                    if(m_mappedIdx.Length == 0)
                    {
                        m_tubeRenderer.Mesh.colors = null;
                        m_tubeRenderer.material.color = Component.Color;
                    }

                    //Use the mapped data
                    else
                    {
                        //Read the data
                        Func<int, float[]> computeMappedData = (x) =>
                        {
                            float[] subdata = new float[Component.Container.NbRows];
                            if (x < 0 || x >= Component.Container.NbRows)
                                for (int i = 0; i < Component.Container.NbRows; i++)
                                    subdata[i] = 0.0f;
                            else
                            {
                                var nf = CultureInfo.InvariantCulture.NumberFormat;
                                int i = 0;
                                foreach (string s in Component.Container.GetColumn(x))
                                {
                                    try
                                    {
                                        subdata[i] = float.Parse(s, nf);
                                    }
                                    catch (Exception)
                                    {
                                        subdata[i] = 0.0f;
                                    }
                                    i++;
                                }
                            }

                            return subdata;
                        };

                        if (m_mappedIdx.Length == m_oldIdx.Length)
                        {
                            for (int i = 0; i < m_mappedIdx.Length; i++)
                            {
                                if (m_mappedIdx[i] != m_oldIdx[i])
                                {
                                    m_mappedData[i] = computeMappedData(m_mappedIdx[i]);
                                    m_oldIdx[i] = m_mappedIdx[i];
                                }
                            }
                        }
                        else
                        {
                            m_mappedData.Clear();
                            m_mappedData.Capacity = m_mappedIdx.Length;
                            for (int i = 0; i < m_mappedIdx.Length; i++)
                                m_mappedData.Add(computeMappedData(m_mappedIdx[i]));

                            m_oldIdx = (Int32[])m_mappedIdx.Clone();
                        }

                        //Compute color using the transfer function
                        TransferFunction tf = Component.SubDataset.TransferFunction;
                        List<PointFieldDescriptor> ptDescs = Component.SubDataset.Parent.PointFieldDescs;
                        int hasGradient = (tf.HasGradient() ? 1 : 0);

                        if (tf != null)
                        {
                            if (m_mappedIdx.Length > tf.GetDimension() - hasGradient ||
                               tf.GetDimension() - hasGradient > ptDescs.Count)
                            {
                                //Use default color
                                if (m_mappedIdx.Length == 0)
                                    m_tubeRenderer.material.color = Component.Color;
                            }
                            else
                            {
                                m_tubeRenderer.material.color = Color.white;
                                Color[] color = new Color[m_tubeRenderer.Mesh.vertices.Length];
                                for (int i = 0; i < m_tubeRenderer.Positions.Length; i++)
                                {
                                    float[] mappedVal = new float[tf.GetDimension() - hasGradient];
                                    for (int j = 0; j < tf.GetDimension() - hasGradient; j++)
                                        mappedVal[j] = (m_mappedData[j][m_data[i].Idx] - 22) / 2;

                                    Color c = tf.ComputeColor(mappedVal);

                                    for (int j = 0; j < m_tubeRenderer.NbVerticesPerPosition; j++)
                                        color[i * m_tubeRenderer.NbVerticesPerPosition + j] = c;
                                }

                                m_tubeRenderer.Mesh.colors = color;
                            }
                        }
                    }
                    m_updateColor = false;
                }
            }
        }

        /// <summary>
        /// Update the stored data. This is not the graphical data
        /// </summary>
        private void ReadPosition()
        {
            lock (this)
            {
                LogAnnotationPosition  pos       = Component.Position;
                LogAnnotationContainer container = Component.Container;
                m_data.Clear();

                //Order position based on time and store the result (if time is available)
                if(Component.Component.LogAnnotation.TimeIdx >= 0)
                {
                    List<float>   t       = container.ParsedTimeValues;
                    List<Vector3> posData = container.GetPositionsFromView(pos);
                    for (int i = 0; i < posData.Count; i++)
                        m_data.Add(new AssociatedData() { Time = t[i], Pos = posData[i], Idx = i });
                    m_data.Sort();
                }
                else
                {
                    List<Vector3> posData = container.GetPositionsFromView(pos);
                    for (int i = 0; i < posData.Count; i++)
                        m_data.Add(new AssociatedData() { Time = -1, Pos = posData[i], Idx = i });
                }

                //Default y position : 0.5 + small offset
                if(Component.Component.Headers[2] < 0)
                    for(int i = 0; i < m_data.Count; i++)
                        m_data[i].Pos.z = 0.51f;

                m_updatePos = true;
            }
        }

        public void OnTransferFunctionChange(SubDataset dataset, TransferFunction tf)
        {
            lock (this)
                m_updateColor = true;
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
        {}

        public void OnNameChange(SubDataset dataset, string name)
        {}

        public void OnAddCanvasAnnotation(SubDataset dataset, CanvasAnnotation annot)
        {}

        public void OnAddLogAnnotationPosition(SubDataset dataset, LogAnnotationPositionInstance annot)
        {}

        public void OnClearCanvasAnnotations(SubDataset dataset)
        {}

        public void OnToggleMapVisibility(SubDataset dataset, bool visibility)
        {}

        public void OnChangeVolumetricMask(SubDataset dataset)
        {}

        public void OnChangeDepthClipping(SubDataset dataset, float depth)
        {}

        public void OnSetSubDatasetGroup(SubDataset dataset, SubDatasetGroup sdg)
        {}
    }
}