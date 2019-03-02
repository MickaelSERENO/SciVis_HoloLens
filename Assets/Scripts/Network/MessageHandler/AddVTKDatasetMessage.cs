using System;

namespace Sereno.Network.MessageHandler
{
    public class AddVTKDatasetMessage : ServerMessage
    {
        public Int32  DataID;
        public String Path;
        public Int32[] PtFieldValueIndices;
        public Int32[] CellFieldValueIndices;

        public Int32 NbPtFieldValueIndices = 0;
        public Int32 NbCellFieldValueIndices = 0;

        public AddVTKDatasetMessage(ServerType type) : base(type)
        {}

        public override byte GetCurrentType()
        {
            if(Cursor == 1)
                return (byte)'s';
            return (byte)'I';
        }

        public override void Push(string value)
        {
            Path = value;
            base.Push(value);
        }

        public override void Push(Int32 value)
        {
            if(Cursor == 0)
                DataID = value;
            else if(Cursor == 2)
            {
                NbPtFieldValueIndices = value;
                PtFieldValueIndices = new Int32[value];
            }
            else if(Cursor - 3 < NbPtFieldValueIndices)
                PtFieldValueIndices[Cursor-3] = value;
            else if(Cursor == 3+NbPtFieldValueIndices)
            {
                NbCellFieldValueIndices = value;
                CellFieldValueIndices = new Int32[value];
            }
            else if(Cursor - 4 - NbPtFieldValueIndices < NbCellFieldValueIndices)
            {
                CellFieldValueIndices[Cursor-4-NbPtFieldValueIndices] = value;
            }
            else
                return;
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            return 3+NbPtFieldValueIndices+NbCellFieldValueIndices;
        }
    }
}