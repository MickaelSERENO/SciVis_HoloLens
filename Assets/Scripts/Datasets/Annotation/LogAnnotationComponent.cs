using System;
using System.Collections.Generic;

namespace Sereno.Datasets.Annotation
{
    /// <summary>
    /// Describe an annotation component from log
    /// </summary>
    public class LogAnnotationComponent
    {
        /// <summary>
        /// The listener interface to call for LogAnnotationComponent modifications
        /// </summary>
        public interface ILogAnnotationComponentListener
        {
            /// <summary>
            /// Method to call when the headers used for the data has changed 
            /// </summary>
            /// <param name="component">the component calling this method</param>
            /// <param name="oldHeaders">the old component's headers</param>
            void OnUpdateHeaders(LogAnnotationComponent component, List<Int32> oldHeaders);
        }

        /// <summary>
        /// The connected LogAnnotation
        /// </summary>
        protected LogAnnotation m_ann;

        /// <summary>
        /// The registered listeners to call on events
        /// </summary>
        protected List<ILogAnnotationComponentListener> m_listeners;

        /// <summary>
        /// The ID of this component as defined by the server
        /// </summary>
        public Int32 ID = -1;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ann">The LogAnnotation object containing the log data</param>
        public LogAnnotationComponent(LogAnnotation ann)
        {
            m_ann = ann;
        }

        /// <summary>
        /// Add a listener to consider each time this object is modified
        /// </summary>
        /// <param name="l">the object to call on events</param>
        /// <returns> true if this listener was not already added. Otherwise, the function returns false and the object is not added</returns>
        public bool AddListener(ILogAnnotationComponentListener l)
        {
            if(!m_listeners.Contains(l))
            {
                m_listeners.Add(l);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove an already registered listener which we considered each time this object is modified
        /// </summary>
        /// <param name="l">the object to stop calling on events</param>
        /// <returns>true if this listener was already added. Otherwise, the function returns false and nothing is done</returns>
        public bool RemoveListener(ILogAnnotationComponentListener l)
        {
            if(m_listeners.Contains(l))
            {
                m_listeners.Remove(l);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Call the event 'OnUpdateHeaders' on all the registered listeners
        /// </summary>
        /// <param name="oldHeaders">The previous used headers</param>
        protected void CallOnUpdateHeaders(List<Int32> oldHeaders) 
        {
            foreach(var it in m_listeners)
                it.OnUpdateHeaders(this, oldHeaders);
        }

        /// <summary>
        /// The headers this component is using
        /// </summary>
        public virtual List<Int32> Headers
        {
            get => new List<Int32>();
            set {}
        }

        /// <summary>
        /// The connected LogAnnotation to read from
        /// </summary>
        public LogAnnotation LogAnnotation
        {
            get => m_ann;
        }
    }
}