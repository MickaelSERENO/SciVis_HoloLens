using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    public class LogEntry : IEnumerable<string>
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

        /// <summary>
        /// Data representing the stored row 
        /// </summary>
        public List<String> Data
        {
            get => m_values;
        }
    }

    /// <summary>
    /// Basic class storing annotations from log. This has to be combined with AnnotationPosition for example to be useful
    /// </summary>
    public class LogAnnotation : IEnumerable<LogEntry>
    {
        /// <summary>
        /// Try to find the column indice corresponding to the header h
        /// </summary>
        /// <param name="headers">The list of headers to look on</param>
        /// <param name="h">h the header to look after</param>
        /// <returns> -1 if not found, the indice of the "h" value in "headers" otherwise</returns>
        public static int IndiceFromHeader(List<String> headers, String h)
        {
            if (headers == null)
                return -1;

            for (int i = 0; i < headers.Count; i++)
                if (headers[i] == h)
                    return i;
            return -1;
        }

        /// <summary>
        /// Is this data associated to a header?
        /// </summary>
        private bool           m_hasHeader;

        /// <summary>
        /// The list of available headers
        /// </summary>
        private List<String>   m_headers = new List<string>();

        /// <summary>
        /// All the rows read from streams
        /// </summary>
        private List<LogEntry> m_values  = new List<LogEntry>();

        /// <summary>
        /// The column indice containing time values
        /// </summary>
        private int            m_timeIdx = -1;

        /// <summary>
        /// Has this log being read?
        /// </summary>
        private bool m_hasRead = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        public LogAnnotation(bool header=true)
        {
            m_hasHeader = header;
        }

        /// <summary>
        /// Access the x-th row
        /// </summary>
        /// <value> a LogEntry representing the x-th row</value>
        public LogEntry this[int x]
        {
            get => m_values[x];
        }

        /// <summary>
        /// Function that can be override. This function is called after a stream has been successfully parsed
        /// </summary>
        protected virtual void OnParsed() { }

        /// <summary>
        /// Initialize the logs from a csv file
        /// </summary>
        /// <param name="reader">The stream to read from</param>
        /// <param name="separator">The CSV separator to use</param>
        /// <returns>true if success, false otherwise. A message is printed in Error in case of errors</returns>
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
                
        /// <summary>
        /// Is a header available?
        /// </summary>
        /// <param name="s">The header to evaluate</param>
        /// <returns>True if 's' is a header column of this object, false otherwise</returns>
        public bool HasStringHeader(String s)
        {
            return HasHeader && m_headers.Contains(s);
        }

        /// <summary>
        /// Try to find the column indice corresponding to the header h
        /// </summary>
        /// <param name="h">the header to look after</param>
        /// <returns>-1 if not found, the indice of the "h" values otherwise</returns>
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

        public IEnumerable<String> GetColumn(int x)
        {
            if(x < 0 || x > NbColumns)
                yield break;

            foreach (LogEntry l in this)
                yield return l[x];
        }

        /// <summary>
        /// The number of available rows
        /// </summary>
        public int NbRows
        {
            get => m_values.Count;
        }
        
        /// <summary>
        /// The number of available columns
        /// </summary>
        public int NbColumns
        {
            get => (m_values.Count > 0 ? m_values[0].Count : 0);
        }

        /// <summary>
        /// Has this log headers?
        /// </summary>
        public bool HasHeader
        {
            get => m_hasHeader;
        }

        /// <summary>
        /// What are the headers available on this object?
        /// </summary>
        public List<String> Headers
        {
            get => m_headers;
        }

        /// <summary>
        /// Set the column indice where time is expected. Negative values == no expected time
        /// </summary>
        /// <param name="timeCol">the time column indice. Negatif values for no expected time</param>
        /// <returns>return false in case of an invalid time column: No values were entered (and timeCol is positive), or timeCol is outside the number of columns of this annotation.</returns>
        public virtual bool SetTimeIdx(int timeCol)
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

            return true;
        }

        /// <summary>
        /// Set the time header based on the column header name. 
        /// </summary>
        /// <param name="timeHeader">The headers to evaluate</param>
        /// <returns>false if the header is not valid, true otherwise</returns>
        public bool SetTimeIdx(String timeHeader)
        {
            if(!m_hasRead || !m_hasHeader)
                return false;

            int timeCol = IndiceFromHeader(timeHeader);
            if (timeCol < 0)
                return false;

            return SetTimeIdx(timeCol);
        }

        /// <summary>
        /// The column index representing time values
        /// </summary>
        public int TimeIdx
        {
            get => m_timeIdx;
            set => SetTimeIdx(value);
        }

        /// <summary>
        /// The time values
        /// </summary>
        public IEnumerable<float> TimeValues
        {
            get
            {
                if(m_timeIdx < 0)
                    yield break;

                var nf = CultureInfo.InvariantCulture.NumberFormat;

                foreach (LogEntry l in this)
                {
                    float f = 0.0f;
                    try
                    {
                        f = float.Parse(l[m_timeIdx], nf);
                    }
                    catch(Exception){}
                    yield return f;
                }

            }
        }
    }
}
