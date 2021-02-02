using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sereno.Datasets.Annotation
{
    /// <summary>
    /// Represent a row in a log annotation object
    /// </summary>
    class LogEntry : IEnumerable<string>
    {
        /// <summary>
        /// The stored values
        /// </summary>
        private List<String> m_values;

        /// <summary>
        /// Pointer to the headers of the parent annotation
        /// </summary>
        private List<String> m_headers;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="values">The values to store</param>
        /// <param name="headers">The associated header. null if no headers are available</param>
        public LogEntry(List<String> values, List<String> headers)
        {
            m_values  = values;
            m_headers = headers;
        }

        /// <summary>
        /// Get the String value at column indice 'x'
        /// </summary>
        /// <param name="x">The column ID to look at</param>
        /// <returns>the value of this row at column 'x'</returns>
        public String this[int x]
        {
            get => m_values[x];
            set => m_values[x] = value;
        }

        /// <summary>
        /// Get the String value at column header 'h'
        /// </summary>
        /// <param name="h">The column header to look at</param>
        /// <returns>the value of this row at column 'h'</returns>
        public String this[String h]
        {
            get => m_values[LogAnnotation.IndiceFromHeader(m_headers, h)];
            set => m_values[LogAnnotation.IndiceFromHeader(m_headers, h)] = value;
        }
        
        /// <summary>
        /// Get the column indice corresponding to the header 'h'
        /// </summary>
        /// <param name="h">The header to evaluate</param>
        /// <returns>-1 if the header is not found, the indice corresponding at the header 'h' otherwise</returns>
        public int IndiceFromHeader(String h)
        {
            return LogAnnotation.IndiceFromHeader(m_headers, h);
        }

        /// <summary>
        /// Has this LogEntry the header 'h'?
        /// </summary>
        /// <param name="h">The header to look at</param>
        /// <returns>true if 'h' is part of this LogEntry, false otherwise</returns>
        public bool HasStringHeader(String h)
        {
            return HasHeader && m_headers.Contains(h);
        }
        
        public IEnumerator<string> GetEnumerator()
        {
            return m_values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get the number of columns this LogEntry possesses
        /// </summary>
        public int Count
        {
            get => m_values.Count;
        }

        /// <summary>
        /// Is this LogEntry associated to any headers?
        /// </summary>
        public bool HasHeader
        {
            get => m_headers != null;
        }
    }

    class LogAnnotation : IEnumerable<LogEntry>
    {
        private bool           m_hasHeader;
        private List<String>   m_headers = new List<string>();
        private List<LogEntry> m_values  = new List<LogEntry>();
        private int            m_timeIdx = -1;
        private bool m_hasRead = false;

        public static int IndiceFromHeader(List<String> headers, String h)
        {
            if (headers == null)
                return -1;

            for (int i = 0; i < headers.Count; i++)
                if (headers[i] == h)
                    return i;
            return -1;
        }

        public LogAnnotation(bool header=true)
        {
            m_hasHeader = header;
        }

        public LogEntry this[int x]
        {
            get => m_values[x];
        }

        protected virtual void OnParsed() { }

        public bool ReadFromCSV(StreamReader reader, char separator=',')
        {
            int size = -1;

            //Clear values
            m_headers.Clear();
            m_values.Clear();

            //Read header
            if (m_hasHeader)
            {
                CSVRow row = new CSVRow(separator);
                row.ReadNextRow(reader);
                m_headers = row.Data;
                size = m_headers.Count;
            }

            //Read values
            foreach(CSVRow row in new CSVRange(reader))
            {
                if(size != -1)
                {
                    if(row.Count != size)
                    {
                        Debug.LogError($"Error at parsing CSV: Different column numbers per row");
                        return false;
                    }
                }
                else
                    size = row.Count;

                m_values.Add(new LogEntry(row.Data, (m_hasHeader ? m_headers : null)));
            }

            m_hasRead = true;
            m_timeIdx = -1;
            OnParsed();

            return true;
        }
                
        public bool HasStringHeader(String s)
        {
            return HasHeader && m_headers.Contains(s);
        }

        public int IndiceFromHeader(String h)
        {
            return LogAnnotation.IndiceFromHeader(m_headers, h);
        }

        public IEnumerator<LogEntry> GetEnumerator()
        {
            return m_values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int NbRows
        {
            get => m_values.Count;
        }
        
        public int NbColumns
        {
            get => (m_values.Count > 0 ? m_values[0].Count : 0);
        }

        public bool HasHeader
        {
            get => m_hasHeader;
        }

        public List<String> Headers
        {
            get => m_headers;
        }

        protected virtual void OnSetTimeIdx() {}

        public bool SetTimeIdx(int timeCol)
        {
            if(timeCol < 0)
            {
                m_timeIdx = timeCol;
                return true;
            }

            if (!m_hasRead)
                return false;

            if (m_hasHeader)
            {
                if (timeCol >= m_headers.Count)
                    return false;
                m_timeIdx = timeCol;
            }
            else
            {
                if (m_values.Count != 0)
                {
                    if (timeCol >= m_values[0].Count)
                        return false;
                    m_timeIdx = timeCol;
                }
                else
                    m_timeIdx = timeCol;
            }

            OnSetTimeIdx();
            return true;
        }

        public bool SetTimeIdx(String timeHeader)
        {
            if(!m_hasRead || !m_hasHeader)
                return false;

            int timeCol = IndiceFromHeader(timeHeader);
            if (timeCol < 0)
                return false;

            return SetTimeIdx(timeCol);
        }

        public int TimeIdx
        {
            get => m_timeIdx;
            set => SetTimeIdx(value);
        }

        public IEnumerator<float> TimeValues
        {
            get
            {
                if(m_timeIdx < 0)
                    yield break;

                foreach (LogEntry l in this)
                    yield return float.Parse(l[m_timeIdx]);
            }
        }
    }
}
