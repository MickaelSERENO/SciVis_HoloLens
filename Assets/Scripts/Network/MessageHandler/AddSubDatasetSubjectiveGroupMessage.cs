using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Sereno.Network.MessageHandler
{
    public class AddSubDatasetSubjectiveGroupMessage : ServerMessage
    {
        public int SubjectiveViewType;
        public int BaseDatasetID;
        public int BaseSubDatasetID;
        public int SDG_ID;

        public AddSubDatasetSubjectiveGroupMessage(ServerType type) : base(type)
        {}

        public override void Push(int value)
        {
            if (Cursor == 1)
                BaseDatasetID = value;
            else if (Cursor == 2)
                BaseSubDatasetID = value;
            else if (Cursor == 3)
                SDG_ID = value;
            base.Push(value);
        }

        public override void Push(byte value)
        {
            SubjectiveViewType = value;
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor == 0)
                return (byte)'b';
            else if (Cursor < 4)
                return (byte)'I';
            return 0;
        }

        public override Int32 GetMaxCursor()
        {
            return 3;
        }
    }
}