using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno
{
    /// <summary>
    /// Class permitting to get the next row in a CSV File
    /// </summary>
    class CSVRow : IEnumerable<string>
    {
        /// <summary>
        /// The separator used to read the separate tokens
        /// </summary>
        private char m_separator;

        /// <summary>
        /// The list of tokens read
        /// </summary>
        private List<String> m_data = new List<String>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="separator">The separator to use to separate tokens in the CSV</param>
        public CSVRow(char separator = ',')
        {
            m_separator = separator;
        }

        /// <summary>
        /// Enumerate all the tokens read
        /// </summary>
        /// <returns>An IEnumerator\<string\> permitting to enumerate all the tokens read</returns>
        public IEnumerator<string> GetEnumerator()
        {
            return m_data.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get the token at indice 'x'
        /// </summary>
        /// <param name="x">The indice to read</param>
        /// <returns>The token at indice x.</returns>
        public String this[int x]
        {
            get => m_data[x];
        }
        
        /// <summary>
        /// Read the next row in a StreamReader object
        /// </summary>
        /// <param name="str">The stream to read from</param>
        /// <returns>true if we have reached the end of the file, false otherwise</returns>
        public bool ReadNextRow(System.IO.StreamReader str)
        {
            m_data.Clear();
            String cell = "";
            bool inStr = false;
            bool inSecondQuote = false;
            int r;

            while((r = str.Read()) != -1)
            {
                char c = (char)r;

                switch (c)
                {
                    case '"':
                        if (inStr)
                        {
                            if (inSecondQuote)
                            {
                                cell += '"';
                                inSecondQuote = false;
                            }
                            else
                                inSecondQuote = true;
                        }
                        else
                            inStr = true;
                        break;
                    default:
                        if (inSecondQuote)
                        {
                            inStr = false;
                            inSecondQuote = false;
                        }

                        if (inStr)
                        {
                            cell += c;
                        }

                        else if (c == m_separator)
                        {
                            m_data.Add(cell);
                            cell = "";
                        }
                        else if (c == '\n')
                        {
                            m_data.Add(cell);
                            return str.EndOfStream;
                        }
                        else if (c != '\r') //remove carriage return
                            cell += c;
                        break;
                }
            }

            if (m_data.Count() > 0 || cell.Count() != 0)
                m_data.Add(cell);
            return str.EndOfStream;
        }
        
        /// <summary>
        /// Get the number of tokens read
        /// </summary>
        public int Count
        {
            get => m_data.Count();
        }

        /// <summary>
        /// The data being read
        /// </summary>
        public List<String> Data
        {
            get => m_data;
        }
    }

    /// <summary>
    /// Class allowing to iterate over a CSV stream
    /// </summary>
    class CSVRange : IEnumerable<CSVRow>
    {
        /// <summary>
        /// The stream to read from
        /// </summary>
        private StreamReader m_str;

        /// <summary>
        /// The separator to use between tokens in the CSV Stream
        /// </summary>
        private char         m_separator;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="str">The stream to read from</param>
        /// <param name="separator">The separator to use to separate tokens in the CSV stream</param>
        public CSVRange(StreamReader str, char separator=',')
        {
            m_str       = str;
            m_separator = separator;
        }

        /// <summary>
        /// Enumerate all the rows of the CSV object
        /// </summary>
        /// <returns>An IEnumerator\<CSVRow\> to enumerate the rows in the CSV stream</returns>
        public IEnumerator<CSVRow> GetEnumerator()
        {
            while(!m_str.EndOfStream)
            {
                CSVRow r = new CSVRow(m_separator);
                r.ReadNextRow(m_str);
                yield return r;
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
