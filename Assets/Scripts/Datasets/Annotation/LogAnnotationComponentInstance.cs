using System.Collections.Generic;
using UnityEngine;

namespace Sereno.Datasets.Annotation
{
    /// <summary>
    /// Abstract base class for log annotation component to visualize
    /// </summary>
    public abstract class LogAnnotationComponentInstance
    {
        /// <summary>
        /// Listener interface to use for events linked to this class
        /// </summary>
        public interface ILogAnnotationComponentInstanceListener
        {
            /// <summary>
            /// Function called when a LogAnnotationComponentInstance must use time or not
            /// </summary>
            /// <param name="comp">The component calling this method</param>
            void OnSetUseTime(LogAnnotationComponentInstance comp);
        }

        /// <summary>
        /// Should we use time?
        /// </summary>
        private bool                   m_useTime = true;

        /// <summary>
        /// What is the data model?
        /// </summary>
        private LogAnnotationComponent m_data    = null;

        /// <summary>
        /// The listeners to call on events
        /// </summary>
        private List<ILogAnnotationComponentInstanceListener> m_listeners = new List<ILogAnnotationComponentInstanceListener>();

        /// <summary>
        /// Protected constructor
        /// </summary>
        /// <param name="data">The linked LogAnnotationComponent to use</param>
        protected LogAnnotationComponentInstance(LogAnnotationComponent data)
        {
            m_data = data;
        }

        /// <summary>
        /// Add a new Listener to call on events
        /// </summary>
        /// <param name="list">The listener to call</param>
        void AddListener(ILogAnnotationComponentInstanceListener list)
        {
            if(!m_listeners.Contains(list))
                m_listeners.Add(list);
        }

        /// <summary>
        /// Remove an already registered listener
        /// </summary>
        /// <param name="list">The listener to remove</param>
        void RemoveListener(ILogAnnotationComponentInstanceListener list)
        {
            if(m_listeners.Contains(list))
                m_listeners.Remove(list);
        }

        /// <summary>
        /// Should the visualization consider time or not? While you can set this as true or false, the "getter" considers, in addition, to the linked data model: it must have a valid TimeIdx.
        /// </summary>
        public bool UseTime
        {
            get => m_useTime && m_data != null && m_data.LogAnnotation.TimeIdx >= 0;
            set
            {
                bool old = UseTime;
                m_useTime = value;

                if(old != UseTime)
                    foreach(var l in m_listeners)
                        l.OnSetUseTime(this);
            }
        }    

        /// <summary>
        /// The linked log annotation component
        /// </summary>
        public LogAnnotationComponent Component
        {
            get => m_data;
        }
    }
}       