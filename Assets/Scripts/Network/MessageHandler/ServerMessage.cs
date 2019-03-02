using System;

namespace Sereno.Network.MessageHandler
{
    public abstract class ServerMessage
    {
        private ServerType m_type;
        public Int32 Cursor = 0;

        public ServerMessage(ServerType type)
        {
            m_type = type;
        }

        public abstract byte GetCurrentType();

        public virtual Int32 GetMaxCursor() {return -1;}

        public virtual void Push(Int32  value) {Cursor++;}
        public virtual void Push(Int16  value) {Cursor++;}
        public virtual void Push(String value) {Cursor++;}
        public virtual void Push(float  value) {Cursor++;}
        public virtual void Push(double value) {Cursor++;}
    
        public ServerType Type {get{return m_type;}}
    }
}