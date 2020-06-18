using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Datasets
{
    [StructLayout(LayoutKind.Explicit)]
    struct IntFloatUnion
    {
        [FieldOffset(0)]
        public Int32 IntField;

        [FieldOffset(0)]
        public float FloatField;

        /// <summary>
        /// Fill the Int/Float field based on a big endian byte array
        /// </summary>
        /// <param name="arr">The byte array to convert</param>
        /// <param name="offset">The offset to start reading the byte array. 4 values are read after the offset (offset included)</param>
        public void FillWithByteArray(byte[] arr, int offset)
        {
            IntField = (arr[offset+0] << 24) + (arr[offset+1] << 16) +
                       (arr[offset+2] << 8)  + (arr[offset+3]);
        }
    }
}
