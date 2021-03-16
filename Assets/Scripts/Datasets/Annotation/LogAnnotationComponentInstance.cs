using System;
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

        private SubDataset m_sd;

        /// <summary>
        /// The Instance ID to identify this object
        /// </summary>
        private Int32 m_instanceID;

        /// <summary>
        /// Protected constructor
        /// </summary>
        /// <param name="data">The linked LogAnnotationComponent to use</param>
        /// <param name="sd">The subdaatset owning this instance</param>
        /// <param name="instanceID">The ID of this component</param>
        protected LogAnnotationComponentInstance(LogAnnotationComponent data, SubDataset sd, Int32 instanceID)
        {
            m_data = data;
            m_instanceID = instanceID;
            m_sd = sd;
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

        /// <summary>
        /// The ID to link this instance to the object it is attached to.
        /// </summary>
        public Int32 InstanceID
        {
            get => m_instanceID;
        }

        public SubDataset SubDataset
        {
            get => m_sd;
        }
    }
}       