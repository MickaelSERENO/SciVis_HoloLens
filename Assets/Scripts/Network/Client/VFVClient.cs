using System.IO;
using System;

using Sereno.Network.MessageHandler;
using System.Net.Sockets;
using Sereno.Datasets;

namespace Sereno.Network
{
    /// <summary>
    /// Different type of command this client can send to the server
    /// </summary>
    public enum VFVSendCommand
    {
        SEND_IDENT_HOLOLENS = 0,
        SEND_UPDATE_HEADSET = 5,
        SEND_ANCHORING_DATA_SEGMENT = 7,
        SEND_ANCHORING_DATA_STATUS  = 8,
        SEND_ANCHOR_ANNOTATION = 15,
    }

    /// <summary> 
    /// The VFV Client Class. Rewrite the read part of the Client class
    /// </summary> 
    public class VFVClient : Client
    {
        /// <summary>
        /// The message buffer reading the data
        /// </summary>
        private MessageBuffer m_msgBuf;

        //Has the connection message being sent?
        private bool          m_headsetConnectionSent = false;

        public VFVClient(IMessageBufferCallback clbk) : base("192.168.43.44", 8000)
        {
            m_msgBuf = new MessageBuffer();
            base.AddListener(new ClientStatusCallback(OnConnectionStatus));
            m_msgBuf.AddListener(clbk);
        }

        /// <summary>
        /// Send the hololens ident message to the server
        /// </summary>
        public void SendHeadsetIdent()
        {
            byte[] data = new byte[2];
            WriteInt16(data, 0, (Int16)VFVSendCommand.SEND_IDENT_HOLOLENS);
            Send(data);
            m_headsetConnectionSent = true;
        }

        /// <summary>
        /// Send the streaming update data of the hololens to the server
        /// </summary>
        /// <param name="updateData">The data to stream</param>
        public void SendHeadsetUpdateData(HeadsetUpdateData updateData)
        {
            byte[] data = new byte[2 + 3*4 + 4*4 + 3*4 + 1 + 3*4 + 3*4 + 4*4];
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

            //PointingIT
            WriteInt32(data, offset, (Int32)updateData.PointingIT);
            offset += 4;

            //Pointing Dataset's ID
            WriteInt32(data, offset, updateData.PointingDatasetID);
            offset += 4;

            //Pointing SubDataset's ID
            WriteInt32(data, offset, updateData.PointingSubDatasetID);
            offset += 4;

            //Pointing in public space
            data[offset++] = (byte)(updateData.PointingInPublic ? 1 : 0);

            //Pointing's position
            for (int i = 0; i < 3; i++, offset += 4)
                WriteFloat(data, offset, updateData.PointingLocalSDPosition[i]);

            //The headset starting position when the pointing interaction technique was created
            for (int i = 0; i < 3; i++, offset += 4)
                WriteFloat(data, offset, updateData.PointingHeadsetStartPosition[i]);

            //The headset starting orientation when the pointing interaction technique was created
            for (int i = 0; i < 4; i++, offset += 4)
                WriteFloat(data, offset, updateData.PointingHeadsetStartOrientation[i]);

            Send(data);
        }

        /// <summary>
        /// Send a new anchoring data segment to the server
        /// </summary>
        /// <param name="seg">the segment to send</param>
        public void SendAnchoringDataSegment(byte[] seg)
        {
            byte[] data = new byte[2+4]; //Header, just contain type + length
            Int32 offset = 0;

            //Type
            WriteInt16(data, offset, (Int16)VFVSendCommand.SEND_ANCHORING_DATA_SEGMENT);
            offset += 2;

            //Length
            WriteInt32(data, offset, seg.Length);
            offset += 4;

            lock(m_writeThread)
            {
                Send(data);
                Send(seg);
            }
        }

        /// <summary>
        /// Send the status of the anchoring data
        /// </summary>
        /// <param name="succeed">Whether or not the encoding succeed</param>
        public void SendAnchoringDataStatus(bool succeed)
        {
            byte[] data = new byte[2+1];
            Int32 offset = 0;

            //Type
            WriteInt16(data, offset, (Int16)VFVSendCommand.SEND_ANCHORING_DATA_STATUS);
            offset += 2;

            //Succeed status
            data[offset++] = (byte)(succeed ? 1 : 0);

            Send(data);
        }

        public void SendAnchorAnnotation(SubDatasetMetaData sd, float[] position)
        {
            byte[] data = new byte[2 + 2*4 + 1 + 3*4];
            Int32 offset = 0;

            //Type
            WriteInt16(data, offset, (Int16)VFVSendCommand.SEND_ANCHOR_ANNOTATION);
            offset += 2;

            //DatasetID
            WriteInt32(data, offset, sd.CurrentSubDataset.Parent.ID);
            offset += 4;

            //SubDataset ID
            WriteInt32(data, offset, sd.SubDatasetPublicState.Parent.GetSubDatasetID(sd.SubDatasetPublicState));
            offset += 4;

            //Public state
            byte inPublic = (byte)(sd.CurrentSubDataset == sd.SubDatasetPrivateState ? 0 : 1);
            data[offset++] = inPublic;

            for (int i = 0; i < 3; i++, offset += sizeof(float))
                WriteFloat(data, offset, position[i]); 

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

        /// <summary>
        /// Handles changement in the connection status
        /// </summary>
        /// <param name="s">The socket being used. It can be closed after this call</param>
        /// <param name="status">The new status to take account of</param>
        private void OnConnectionStatus(Socket s, ConnectionStatus status)
        {
            m_headsetConnectionSent = true;
            if(status == ConnectionStatus.CONNECTED)
                SendHeadsetIdent(); 
        }

        /// <summary>
        /// Has the headset connection being send?
        /// </summary>
        public bool HeadsetConnectionSent { get => m_headsetConnectionSent; }
    }
}