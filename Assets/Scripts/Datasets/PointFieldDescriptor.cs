using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Datasets
{
    /// <summary>
    /// Class representing the descriptor of a point field for a given Dataset
    /// We reuse the VTK descriptor which suits our purposes
    /// </summary>
    public class PointFieldDescriptor : FieldValueMetaData
    {
        /// <summary>
        /// The point ID in the dataset
        /// </summary>
        public UInt32 ID;

        /// <summary>
        /// The minimum value loaded
        /// </summary>
        public float MinVal = float.MinValue;

        /// <summary>
        /// The maximum value loaded
        /// </summary>
        public float MaxVal = float.MaxValue;

        /// <summary>
        /// The Value loaded wrapped using the VTK value container
        /// </summary>
        public VTKValue Value = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PointFieldDescriptor()
        {}

        /// <summary>
        /// Perform a deep copy of FieldValueMetaData objects
        /// </summary>
        /// <param name="metaData">The object to deep copy</param>
        public PointFieldDescriptor(FieldValueMetaData metaData) : base(metaData)
        {}


        /// <summary>
        /// Read the magnitude of a vector at indice "ind"
        /// </summary>
        /// <param name="ind">The indice to look at, without taking into account the parameter NbValuesPerTuple (i.e., 0 <= ind < nbTuples </param>
        /// <returns>The vector magnitude</returns>
        public float ReadMagnitude(UInt64 ind) 
        {
            float mag = 0;
            for(UInt32 i = 0; i<NbValuesPerTuple; i++)
            {
                float r = Value.ReadAsFloat(NbValuesPerTuple * ind + i);
                mag += r* r;
            }
            return (float) Math.Sqrt(mag);
        }
    }
}
