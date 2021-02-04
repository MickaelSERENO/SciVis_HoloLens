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

        /// <summary>
        /// The listeners to call on events
        /// </summary>
        private List<ILogAnnotationPositionInstanceListener> m_listeners = new List<ILogAnnotationPositionInstanceListener>();

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
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="container">Container which has loaded the values</param>
        /// <param name="pos">The position data model</param>
        /// <returns></returns>
        public LogAnnotationPositionInstance(LogAnnotationContainer container, LogAnnotationPosition pos) : base(pos)
        {
            m_container = container;
            m_data      = pos;
        }

        
        /// <summary>
        /// Add a new Listener to call on events
        /// </summary>
        /// <param name="list">The listener to call</param>
        void AddListener(ILogAnnotationPositionInstanceListener list)
        {
            if(!m_listeners.Contains(list))
                m_listeners.Add(list);
        }

        /// <summary>
        /// Remove an already registered listener
        /// </summary>
        /// <param name="list">The listener to remove</param>
        void RemoveListener(ILogAnnotationPositionInstanceListener list)
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
    }
}