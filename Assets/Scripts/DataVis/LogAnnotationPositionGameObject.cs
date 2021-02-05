using UnityEngine;
using Sereno.Datasets.Annotation;
using System.Collections.Generic;
using System;
using Thirdparties;

namespace Sereno.DataVis
{
    /// <summary>
    /// The GameObject script allowing to update log annotations position information
    /// </summary>
    public class LogAnnotationPositionGameObject : MonoBehaviour, LogAnnotationComponent.ILogAnnotationComponentListener, LogAnnotationContainer.ILogAnnotationContainerListener, LogAnnotationPositionInstance.ILogAnnotationPositionInstanceListener
    {
        /// <summary>
        /// A class regrouping time + position. We use it for ordering the data (if it is not)
        /// </summary>
        private class AssociatedData : IComparable<AssociatedData>
        {
            public float   Time;
            public Vector3 Pos;

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
        private bool m_updatePos = true;

        /// <summary>
        /// Should we update the color of the material?
        /// </summary>
        private bool m_updateColor = true;

        /// <summary>
        /// The graphical component tube renderer 
        /// </summary>
        private TubeRenderer m_tubeRenderer = null;

        /// <summary>
        /// What are the current data to use?
        /// </summary>
        private List<AssociatedData> m_data = new List<AssociatedData>();

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
            m_tubeRenderer = gameObject.GetComponent<TubeRenderer>();
            ReadPosition();
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
                    m_updatePos = false;
                }

                //Update the color
                if(m_updateColor)
                {
                    m_tubeRenderer.material.color = Component.Color;
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
                    for(int i = 0; i < posData.Count; i++)
                        m_data.Add(new AssociatedData() { Time = t[i], Pos = posData[i] });
                    m_data.Sort();
                }
                else
                {
                    List<Vector3> posData = container.GetPositionsFromView(pos);
                    for (int i = 0; i < posData.Count; i++)
                        m_data.Add(new AssociatedData() { Time = -1, Pos = posData[i] });
                }

                //Default y position : 0.5 + small offset
                if(Component.Component.Headers[2] < 0)
                    for(int i = 0; i < m_data.Count; i++)
                        m_data[i].Pos.z = 0.51f;

                m_updatePos = true;
            }
        }
    }
}