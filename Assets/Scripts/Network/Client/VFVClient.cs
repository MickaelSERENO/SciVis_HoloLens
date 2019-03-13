using System.IO;
using System;

using Sereno.Network.MessageHandler;

namespace Sereno.Network
{
    public enum VFVSendCommand
    {
        SEND_IDENT_HOLOLENS = 0,
        SEND_UPDATE_HEADSET = 5,
    }

    /// <summary> 
    /// The VFV Client Class. Rewrite the read part of the Client class
    /// </summary> 
    public class VFVClient : Client
    {
        MessageBuffer m_msgBuf;

        public VFVClient(IMessageBufferCallback clbk) : base("192.168.1.132", 8000, null)
        {
            m_msgBuf = new MessageBuffer();
            m_msgBuf.AddListener(clbk);

            ClientStatusClbk = new ClientStatusClbk(OnConnectionStatus);
        }

        /// <summary>
        /// Send the hololens ident message to the server
        /// </summary>
        public void SendHeadsetIdent()
        {
            byte[] data = new byte[2];
            WriteInt16(data, 0, (Int16)VFVSendCommand.SEND_IDENT_HOLOLENS);
            Send(data);
        }

        /// <summary>
        /// Send the streaming update data of the hololens to the server
        /// </summary>
        /// <param name="updateData">The data to stream</param>
        public void SendHeadsetUpdateData(HeadsetUpdateData updateData)
        {
            byte[] data = new byte[2 + 3*4 + 4*4];
            Int32 offset = 0;

            //Type
            WriteInt16(data, offset, (Int16)VFVSendCommand.SEND_UPDATE_HEADSET);
            offset += 2;

            //Position
            for(int i = 0; i < 3; i++, offset += 4)
                WriteFloat(data, offset, updateData.Position[i]);

            //Rotation
            for(int i = 0; i < 4; i++, offset += 4)
                WriteFloat(data, offset, updateData.Rotation[i]);

            Send(data);
        }

        /// <summary>
        /// Write a Int16 (short) value into a buffer in Big Endian
        /// </summary>
        /// <param name="data">the buffer to write to data in </param>
        /// <param name="offset">The offset in the buffer to start writting</param>
        /// <param name="value">The value to write</param>
        public static void WriteInt16(byte[] data, int offset, Int16 value)
        {
            data[offset]   = (byte)((value >> 8) & 0xff);
            data[offset+1] = (byte)(value & 0xff);
        }

        /// <summary>
        /// Write a Int32 (int) value into a buffer in Big Endian
        /// </summary>
        /// <param name="data">the buffer to write to data in </param>
        /// <param name="offset">The offset in the buffer to start writting</param>
        /// <param name="value">The value to write</param>
        public static void WriteInt32(byte[] data, int offset, Int32 value)
        {
            data[offset]   = (byte)((value >> 24) & 0xff);
            data[offset+1] = (byte)((value >> 16) & 0xff);
            data[offset+2] = (byte)((value >> 8 ) & 0xff);
            data[offset+3] = (byte)(value & 0xff);
        }

        /// <summary>
        /// Write a float value into a buffer in Big Endian
        /// </summary>
        /// <param name="data">the buffer to write to data in </param>
        /// <param name="offset">The offset in the buffer to start writting</param>
        /// <param name="value">The value to write</param>
        public static void WriteFloat(byte[] data, int offset, float value)
        {
            unsafe
            {
                Int32 v = *((Int32*)(&value));
                WriteInt32(data, offset, v);
            }
        }

        protected override void HandleMessage(byte[] msg)
        {
            m_msgBuf.Push(msg);
        }

        private void OnConnectionStatus(ConnectionStatus status)
        {
            if(status == ConnectionStatus.CONNECTED)
                SendHeadsetIdent(); 
        }
    }
}