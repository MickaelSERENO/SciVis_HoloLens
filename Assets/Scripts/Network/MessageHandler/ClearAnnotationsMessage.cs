﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class ClearAnnotationsMessage : ServerMessage
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
        /// Is this anchored annotation done in the public subdataset state?
        /// </summary>
        public byte InPublic;

        public ClearAnnotationsMessage(ServerType type) : base(type)
        { }

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            else if (Cursor == 2)
                return (byte)'b';
            return 0;
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DatasetID = value;
            else if (Cursor == 1)
                SubDatasetID = value;
            base.Push(value);
        }

        public override void Push(byte value)
        {
            if (Cursor == 2)
                InPublic = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 2;
        }
    }
}
