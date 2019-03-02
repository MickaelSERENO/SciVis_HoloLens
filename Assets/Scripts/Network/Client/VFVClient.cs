using System.IO;
using System;

using Sereno.Network.MessageHandler;

namespace Sereno.Network
{
    public enum VFVSendCommand
    {
        SEND_IDENT_HOLOLENS = 0
    }

    /// <summary> 
    /// The VFV Client Class. Rewrite the read part of the Client class
    /// </summary> 
    public class VFVClient : Client
    {
        MessageBuffer m_msgBuf;
        public VFVClient(IMessageBufferCallback clbk) : base("127.0.0.1", 8000, null)
        {
            m_msgBuf = new MessageBuffer();
            m_msgBuf.AddListener(clbk);

            ClientStatusClbk = new ClientStatusClbk(OnConnectionStatus);
        }

        public void SendHololensIdent()
        {
            byte[] data = new byte[2];
            WriteInt16(data, 0, (Int16)VFVSendCommand.SEND_IDENT_HOLOLENS);
            Send(data);
        }

        public static void WriteInt16(byte[] data, int offset, Int16 value)
        {
            data[offset]   = (byte)(value >> 8);
            data[offset+1] = (byte)(value & 255);
        }

        protected override void HandleMessage(byte[] msg)
        {
            m_msgBuf.Push(msg);
        }

        private void OnConnectionStatus(ConnectionStatus status)
        {
            if(status == ConnectionStatus.CONNECTED)
                SendHololensIdent(); 
        }
    }
}