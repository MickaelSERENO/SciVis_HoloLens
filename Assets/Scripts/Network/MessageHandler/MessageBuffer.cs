using System.ComponentModel;
using System.IO;
using System;
using System.Collections.Generic;

namespace Sereno.Network.MessageHandler
{
    public interface IMessageBufferCallback
    {
        void OnEmptyMessage(MessageBuffer messageBuffer, EmptyMessage msg);
        void OnAddVTKDataset(MessageBuffer messageBuffer, AddVTKDatasetMessage msg);
    }

    public class MessageBuffer
    {
        private int    m_stringPos = -1;
        private byte[] m_string = null;

        private ServerMessage m_msg = null;

        private byte[] m_data = new byte[4]; //Maximum 4 values
        private int    m_dataPos = 0; //The data position
        
        private List<IMessageBufferCallback> m_listeners = new List<IMessageBufferCallback>();

        public MessageBuffer()
        {}

        public void AddListener(IMessageBufferCallback clbk)
        {
            m_listeners.Add(clbk);
        }

        public void RemoveListener(IMessageBufferCallback clbk)
        {
            m_listeners.Remove(clbk);
        }

        public void Push(byte[] buf)
        {
            int bufPos = 0;
            while(true)
            {
                if(m_msg == null)
                {
                    if(!ReadInt16(buf, bufPos, out Int16 type, out bufPos))
                        return;
                    AllocateData(type);
                }

                while(m_msg != null && m_msg.GetMaxCursor() >= m_msg.Cursor)
                {
                    switch(m_msg.GetCurrentType())
                    {
                        case (byte)'s':
                        {
                            if(!ReadString(buf, bufPos, out String s, out bufPos))
                                return;
                            m_msg.Push(s);
                            break;
                        }
                        case (byte)'I':
                        {
                            if(!ReadInt32(buf, bufPos, out Int32 i, out bufPos))
                                return;
                            m_msg.Push(i);
                            break;
                        }
                        case (byte)'i':
                        {
                            if(!ReadInt16(buf, bufPos, out Int16 i, out bufPos))
                                return;
                            m_msg.Push(i);
                            break;
                        }
                        case (byte)'f':
                        {
                            if(!ReadFloat(buf, bufPos, out float i, out bufPos))
                                return;
                            m_msg.Push(i);
                            break;
                        }
                    }
                }

                if(m_msg != null)
                {
                    switch(m_msg.Type)
                    {
                        case ServerType.GET_ADD_VTK_DATASET:
                            foreach(var l in m_listeners)
                                l.OnAddVTKDataset(this, (AddVTKDatasetMessage)m_msg);
                            break;
                        default:
                            break;
                    }
                    m_msg = null;
                }
            }
        }

        private bool ReadInt16(byte[] data, int offset, out Int16 val, out int newOff)
        {
            newOff = offset;
            val = 0;
            if(data.Length+m_dataPos - offset < 2)
            {
                for(int i = offset; i < data.Length - offset; i++, newOff++)
                    m_data[m_dataPos++] = data[i];
                return false;
            }

            for(int i = newOff; m_dataPos < 2; i++, newOff++)
                m_data[m_dataPos++] = data[i];

            val = (Int16)((m_data[0] << 8) + 
                          (m_data[1]));
            m_dataPos = 0;
            return true;
        }

        private bool ReadInt32(byte[] data, int offset, out Int32 val, out int newOff)
        {
            val = 0;
            newOff = offset;
            if(data.Length+m_dataPos - offset < 4)
            {
                for(int i = offset; i < data.Length - offset; i++, newOff++)
                    m_data[m_dataPos++] = data[i];
                return false;
            }

            for(int i = newOff; m_dataPos < 4; i++, newOff++)
                m_data[m_dataPos++] = data[i];

            val = (Int32)((m_data[0] << 24) + 
                          (m_data[1] << 16) +
                          (m_data[2] << 8)  + 
                          (m_data[3]));
            m_dataPos = 0;
            return true;
        }

        private bool ReadFloat(byte[] data, int offset, out float val, out int newOff)
        {
            val = 0;
            newOff = offset;
            Int32 d;
            if(!ReadInt32(data, offset, out d, out newOff))
                return false;
            unsafe
            {
                val = *(float*)&d;
                return true;
            }
        }

        private bool ReadString(byte[] data, int offset, out String s, out Int32 newOffset)
        {
            newOffset = offset;
            s         = "";
            if(m_string == null)
            {
                if(!ReadInt32(data, newOffset, out m_stringPos, out newOffset))
                    return false;
                m_string     = new byte[m_stringPos];
                m_stringPos = 0;
            }

            for(; m_stringPos < m_string.Length && newOffset < data.Length; m_stringPos++)
                m_string[m_stringPos] = data[newOffset++];
            
            if(newOffset < data.Length)
            {
                s = System.Text.Encoding.ASCII.GetString(m_string);
                m_string = null;
                return true;
            }

            return false;
        }

        private void AllocateData(Int16 type)
        {
            switch((ServerType)type)
            {
                case ServerType.GET_ADD_VTK_DATASET:
                    m_msg = new AddVTKDatasetMessage(ServerType.GET_ADD_VTK_DATASET);
                    break;
            }
        }
    }
}