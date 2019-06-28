using Sereno.Pointing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// The start annotation command
    /// </summary>
    public class StartAnnotationMessage : ServerMessage
    {
        /// <summary>
        /// The DataID bound to this message
        /// </summary>
        public Int32 DatasetID;

        /// <summary>
        /// The SubDataID of the DataID which is being rotated
        /// </summary>
        public Int32 SubDatasetID;

        /// <summary>
        /// Ths Interaction technique ID
        /// </summary>
        public PointingIT PointingID;

        public StartAnnotationMessage(ServerType type) : base(type)
        {}
        
        public override byte GetCurrentType()
        {
            if (Cursor <= 2)
                return (byte)'I';
            return 0;
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DatasetID = value;
            else if (Cursor == 1)
                SubDatasetID = value;
            else if (Cursor == 2)
                PointingID = (PointingIT)value;
            base.Push(value);
        }
        
        public override Int32 GetMaxCursor()
        {
            return 2;
        }
    }
}
