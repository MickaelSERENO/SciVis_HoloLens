using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sereno.Datasets.Annotation
{
    /// <summary>
    /// Possible errors on parsing log annotations
    /// </summary>
    public enum LogAnnotParseError
    {
        INVALID_PARENT = -2,
        ERROR_HEADER_ALREADY_PRESENT = -1,
        NO_ERROR = 0,
    }

    /// <summary>
    /// A container of annotation log information
    /// </summary>
    public class LogAnnotationContainer : LogAnnotation, LogAnnotationComponent.ILogAnnotationComponentListener
    {
        private List<Int32> m_assignedHeaders = new List<Int32>();
        private Dictionary<LogAnnotationPosition, List<Vector3>> m_positions = new  Dictionary<LogAnnotationPosition, List<Vector3>>();
        private List<float> m_time = new List<float>();

        /// <summary>
        /// Initialize the Log reading
        /// </summary>
        /// <param name="file">The file to read from</param>
        /// <param name="hasHeader">header should we expect a header when reading data?</param>
        public LogAnnotationContainer(String file, bool hasHeader=true) : base(hasHeader)
        {
            String ext = Path.GetExtension(file);
            using(FileStream fs = File.OpenRead(file))
            { 
                using(StreamReader str = new StreamReader(fs))
                {
                    if(ext == ".csv")
                    {
                        ReadFromCSV(str);
                    }
                }
            }
        }

        /// <summary>
        /// Build an annotation positional view based on this Annotation Log. The returned values can then be parameterized and be sent to "parseAnnotationPosition" function for storing and reading its values.
        /// </summary>
        /// <returns>An AnnotationPosition that can be configured and then read in "ParseAnnotationPosition" function.</returns>
        public LogAnnotationPosition BuildAnnotationPositionView()
        {
            return new LogAnnotationPosition(this);
        }

        public void OnUpdateHeaders(LogAnnotationComponent component, List<int> oldHeaders)
        {
            bool changeHeaders = false;

            //Erase the headers as assigned
            foreach(Int32 h in oldHeaders)
            {
                for(int i = 0; i < m_assignedHeaders.Count;)
                {
                    if(m_assignedHeaders[i] == h)
                    {
                        m_assignedHeaders.RemoveAt(i);
                        break;
                    }
                    i++;
                }
            }

            //Add the assigned headers. Set as "-1" if the header is already taken
            List<Int32> currentHeaders = component.Headers;
            for(int i = 0; i < currentHeaders.Count; i++)
            {
                if(currentHeaders[i] != -1)
                {
                    Int32 oldVal = currentHeaders[i];

                    //If found: set header as -1
                    if(m_assignedHeaders.BinarySearch(currentHeaders[i]) >= 0)
                    {
                        currentHeaders[i] = -1;
                        changeHeaders     = true;
                    }

                    //We can "still" add it (even if, for the moment, it exists twice), because this function shall be called twice
                    int insertIT = m_assignedHeaders.BinarySearch(oldVal);
                    if(insertIT < 0)
                        insertIT = ~insertIT;
                    m_assignedHeaders.Insert(insertIT, oldVal);
                }
            }

            //Update internal data. Rechange the headers if required (then stop this function)
            foreach(var it in m_positions)
            {
                if(it.Key == component)
                {
                    if(changeHeaders)
                    {
                        it.Key.Headers = currentHeaders;
                        return; //This method shall be called again because we set the headers
                    }
                    else
                    {
                        it.Value.Clear();
                        it.Value.InsertRange(0, it.Key);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Parse and read an annotation position object
        /// </summary>
        /// <param name="pos">The log annotation position to read</param>
        /// <returns>Possible errors occured during the parsing</returns>
        public LogAnnotParseError ParseAnnotationPosition(LogAnnotationPosition pos)
        {
            //Check the parent
            if(pos.LogAnnotation != this)
                return LogAnnotParseError.INVALID_PARENT;

            //Check if we find the headers or not
            List<Int32> idx = pos.Headers;
            foreach(Int32 it in idx)
                if(it != -1 && m_assignedHeaders.BinarySearch(it) >= 0)
                    return LogAnnotParseError.ERROR_HEADER_ALREADY_PRESENT;
            
            m_positions.Add(pos, new List<Vector3>(pos)); //Parse the positions and store the values once and for all

            //Set the headers as already assigned
            foreach(Int32 it in idx)
            {
                if(it != -1)
                {
                    int p = ~m_assignedHeaders.BinarySearch(it);
                    m_assignedHeaders.Insert(p, it);
                }
            }
            pos.AddListener(this);

            return LogAnnotParseError.NO_ERROR;
        }

        /// <summary>
        /// Get a map of all the registered annotation position and the associated read position.
        /// </summary>
        /// <param name="pos">the view to look after</param>
        /// <returns>the associated 3D positions. NULL if pos was not found</returns>
        public List<Vector3> GetPositionsFromView(LogAnnotationPosition pos)
        {
            if(m_positions.ContainsKey(pos))
                return m_positions[pos];
            
            return null;
        }

        protected override void OnParsed()
        {
            SetTimeIdx(TimeIdx);
        }

        public override bool SetTimeIdx(int timeCol)
        {
            //Check if we already have this time. If yes --> error
            if(timeCol != TimeIdx)
                if(m_assignedHeaders.BinarySearch(timeCol) >= 0)
                    return false;

            int oldTime = TimeIdx; //save it to erase it from assigned headers
            bool res = base.SetTimeIdx(timeCol);
            if(!res)
                return false;

            //Erase old header
            if(oldTime != -1)
            {
                int it = m_assignedHeaders.BinarySearch(oldTime);
                if(it >= 0) //Should always be true
                    m_assignedHeaders.RemoveAt(it);
            }

            //Assign new header
            if(TimeIdx != -1)
            {
                int it = ~m_assignedHeaders.BinarySearch(TimeIdx);
                m_assignedHeaders.Insert(it, TimeIdx);
                m_time = new List<float>(TimeValues);
            }
            else
                m_time.Clear();
            return true;
        }

        /// <summary>
        /// all the parsed annotation positions and the corresponding 3D positions.
        /// </summary>
        public Dictionary<LogAnnotationPosition, List<Vector3>> LogAnnotationPositions
        {
            get => m_positions;
        }

        /// <summary>
        /// The already assigned headers. The list is already ordered
        /// </summary>
        public List<Int32> AssignedHeaders
        {
            get => m_assignedHeaders;
        }

        /// <summary>
        /// By elimination, provides a list of headers yet-to assign
        /// </summary>
        public List<Int32> RemainingHeaders
        {
            get
            {
                List<Int32> res = new List<Int32>();
                res.Capacity = NbColumns - m_assignedHeaders.Count;

                int i = 0;
                for(int j = 0; i < NbColumns && j < m_assignedHeaders.Count; i++, j++)
                    for(; i < NbColumns && i != m_assignedHeaders[j]; i++)//This works because every list is ordered
                        res.Add(i);
                
                for(; i < NbColumns; i++)
                    res.Add(i);

                return res;
            }
        }

        /// <summary>
        /// Get the time already parsed and store
        /// </summary>
        /// <value>A new time array. The size must match NbRows</value>
        public List<float> ParsedTimeValues
        {
            get => m_time;
            set
            {
                if(NbRows == value.Count)
                    m_time = value;
            }
        }
    }
}