using System;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Base class for any server messages
    /// </summary>
    public abstract class ServerMessage
    {
        /// <summary>
        /// The type of the message
        /// </summary>
        private ServerType m_type;

        /// <summary>
        /// The current cursor of the message
        /// </summary>
        public Int32 Cursor = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">The type of the message</param>
        public ServerMessage(ServerType type)
        {
            m_type = type;
        }

        /// <summary>
        /// Get the current type to parse at Cursor
        /// </summary>
        /// <returns>'I' for Int32, 'i' for Int16, 'f' for float and 's' for String</returns>
        public abstract byte GetCurrentType();

        /// <summary>
        /// Get the maximum cursor (included) this message can handle
        /// </summary>
        /// <returns>-1 if no data is needed</returns>
        public virtual Int32 GetMaxCursor() {return -1;}

        /// <summary>
        /// Push a byte value
        /// </summary>
        /// <param name="value">The value to push</param>
        public virtual void Push(byte value) { Cursor++; }

        /// <summary>
        /// Push a Int32 value
        /// </summary>
        /// <param name="value">The value to push</param>
        public virtual void Push(Int32  value) {Cursor++;}

        /// <summary>
        /// Push a Int16 value
        /// </summary>
        /// <param name="value">The value to push</param>
        public virtual void Push(Int16  value) {Cursor++;}

        /// <summary>
        /// Push a String value
        /// </summary>
        /// <param name="value">The value to push</param>
        public virtual void Push(String value) {Cursor++;}

        /// <summary>
        /// Push a float value
        /// </summary>
        /// <param name="value">The value to push</param>
        public virtual void Push(float  value) {Cursor++;}

        /// <summary>
        /// Push a double value
        /// </summary>
        /// <param name="value">The value to push</param>
        public virtual void Push(double value) {Cursor++;}

        /// <summary>
        /// Push a byte array value
        /// </summary>
        /// <param name="value">The value to push</param>
        public virtual void Push(byte[] value) {Cursor++;}
    
        /// <summary>
        /// The Type of the message
        /// </summary>
        public ServerType Type {get{return m_type;}}
    }
}