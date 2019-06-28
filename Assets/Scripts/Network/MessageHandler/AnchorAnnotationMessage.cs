﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class AnchorAnnotationMessage : ServerMessage
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
        /// The Annotation ID
        /// </summary>
        public Int32 AnnotationID;

        /// <summary>
        /// The Headset ID which has created the annotation. Can be itself or -1 for the server itself
        /// </summary>
        public Int32 HeadsetID;

        /// <summary>
        /// Is this anchored annotation done in the public subdataset state?
        /// </summary>
        public byte InPublic;

        /// <summary>
        /// The Annotation anchor position in the subdataset local space
        /// </summary>
        public float[] LocalPosition = new float[3];

        public AnchorAnnotationMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if (Cursor <= 3)
                return (byte)'I';
            else if (Cursor == 4)
                return (byte)'b';
            else if (Cursor <= 7)
                return (byte)'f';
            return 0;
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DatasetID = value;
            else if (Cursor == 1)
                SubDatasetID = value;
            else if (Cursor == 2)
                AnnotationID = value;
            else if (Cursor == 3)
                HeadsetID = value;
            base.Push(value);
        }

        public override void Push(float value)
        {
            if(Cursor <= 7 && Cursor >= 5)
                LocalPosition[Cursor - 4] = value;
        }
        
        public override Int32 GetMaxCursor()
        {
            return 7;
        }
    }
}