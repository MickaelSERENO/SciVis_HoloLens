using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno.Datasets.Annotation
{
    /// <summary>
    /// Graphical instance values of a LogAnnotationPosition component
    /// </summary>
    public class LogAnnotationPositionInstance : LogAnnotationComponentInstance
    {
        /// <summary>
        /// Listener interface to use for events linked to this class
        /// </summary>
        public interface ILogAnnotationPositionInstanceListener
        {
            /// <summary>
            /// Function called when the color to render has been changed
            /// </summary>
            /// <param name="comp">The component calling this method</param>
            void OnSetColor(LogAnnotationPositionInstance comp);

            /// <summary>
            /// Function called when the current time to use has been changed
            /// </summary>
            /// <param name="comp">The component calling this method</param>
            void OnSetCurrentTime(LogAnnotationPositionInstance comp);

            /// <summary>
            /// Function called when the indices to read for graphical mapping from the log container has been changed
            /// </summary>
            /// <param name="comp">The component calling this method</param>
            /// <param name="old">The old indices that were used</param>
            void OnSetMappedIndices(LogAnnotationPositionInstance comp, Int32[] old);
        }

        /// <summary>
        /// The model containing the data
        /// </summary>
        private LogAnnotationPosition  m_data;

        /// <summary>
        /// The container containing the data. The idea is to not load multiple times the positions from the model
        /// </summary>
        private LogAnnotationContainer m_container;

        /// <summary>
        /// The color to use for rendering
        /// </summary>
        private Color                  m_color = Color.white;

        private Int32[]                m_mappedIdx = new Int32[0];

        /// <summary>
        /// What is the current time?
        /// </summary>
        private float m_currentTime = 0;

        /// <summary>
        /// The listeners to call on events
        /// </summary>
        private List<ILogAnnotationPositionInstanceListener> m_listeners = new List<ILogAnnotationPositionInstanceListener>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="container">Container which has loaded the values</param>
        /// <param name="pos">The position data model</param>
        /// <param name="sd">The subdaatset owning this instance</param>
        /// <param name="instanceID">The ID of this object to link it to its attached object</param>
        public LogAnnotationPositionInstance(LogAnnotationContainer container, LogAnnotationPosition pos, SubDataset sd, Int32 instanceID) : base(pos, sd, instanceID)
        {
            m_container = container;
            m_data      = pos;
        }

        
        /// <summary>
        /// Add a new Listener to call on events
        /// </summary>
        /// <param name="list">The listener to call</param>
        public void AddListener(ILogAnnotationPositionInstanceListener list)
        {
            if(!m_listeners.Contains(list))
                m_listeners.Add(list);
        }

        /// <summary>
        /// Remove an already registered listener
        /// </summary>
        /// <param name="list">The listener to remove</param>
        public void RemoveListener(ILogAnnotationPositionInstanceListener list)
        {
            if(m_listeners.Contains(list))
                m_listeners.Remove(list);
        }

        /// <summary>
        /// The color to use for rendering
        /// </summary>
        public Color Color
        {
            get => m_color;
            set
            {
                var old = Color;
                m_color = value;
                if(!old.Equals(Color))
                    foreach(ILogAnnotationPositionInstanceListener l in m_listeners)
                        l.OnSetColor(this);
            }
        }

        /// <summary>
        /// The indices to read from the annotation log container (Container) for graphical mapping. It should never be null. Instead, to disable the mapping, use an empty array.
        /// </summary>
        public Int32[] MappedIndices
        {
            get => m_mappedIdx;
            set
            {
                Int32[] old = (Int32[])m_mappedIdx.Clone();
                if (!old.Equals(value))
                {
                    m_mappedIdx = value;
                    foreach (ILogAnnotationPositionInstanceListener l in m_listeners)
                        l.OnSetMappedIndices(this, old);
                }
            }
        }

        /// <summary>
        /// The current time to go to (if this.UseTime is set at true)
        /// </summary>
        public float CurrentTime
        {
            get => m_currentTime;
            set
            {
                lock (this)
                {
                    float old = m_currentTime;
                    m_currentTime = value;
                    if(old != m_currentTime)
                        foreach (ILogAnnotationPositionInstanceListener l in m_listeners)
                            l.OnSetCurrentTime(this);
                }
            }
        }

        /// <summary>
        /// The container which has already read the data
        /// </summary>
        public LogAnnotationContainer Container
        {
            get => m_container;
        }

        /// <summary>
        /// The Actual LogAnnotationPosition
        /// </summary>
        public LogAnnotationPosition Position
        {
            get => m_data;
        }
    }
}