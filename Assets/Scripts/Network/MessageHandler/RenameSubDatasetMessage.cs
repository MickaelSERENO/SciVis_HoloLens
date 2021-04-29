using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class RenameSubDatasetMessage : ServerMessage
    {
        /// <summary>
        /// The Dataset ID of this SubDataset
        /// </summary>
        public Int32 DatasetID;

        /// <summary>
        /// The pre-defined SubDataset ID to use
        /// </summary>
        public Int32 SubDatasetID;

        /// <summary>
        /// The name of the SubDataset
        /// </summary>
        public String Name;

        public RenameSubDatasetMessage(ServerType type) : base(type) {}

        public override byte GetCurrentType()
        {
            if (Cursor <= 1)
                return (byte)'I';
            else if (Cursor == 2)
                return (byte)'s';
            return 0;
        }

        public override void Push(string value)
        {
            Name = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DatasetID = value;
            else if (Cursor == 1)
                SubDatasetID = value;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 2;
        }
    }
}
