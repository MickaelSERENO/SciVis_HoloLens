using System;
using System.Collections.Generic;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Message for the lasso
    /// </summary>
    public class LassoMessage : ServerMessage
    {
        /// <summary>
        /// The lasso data
        /// </summary>
        public List<float> data = new List<float>();

        /// <summary>
        /// The size of the lasso data
        /// </summary>
        public Int32 size = 0;

        public LassoMessage(ServerType type) : base(type)
        {}
        
        public override byte GetCurrentType()
        {
            if(Cursor == 0)
                return (byte)'I';
            else
                return (byte)'f';
        }
        
        public override void Push(Int32 value)
        {
            size = value;
            base.Push(value);
        }
        
        public override void Push(float value)
        {
            data.Add(value);
            base.Push(value);
        }
        
        public override Int32 GetMaxCursor()
        {
            return size;
        }
    }
}