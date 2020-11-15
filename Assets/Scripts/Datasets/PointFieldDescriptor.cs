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
        public float MinVal = float.MaxValue;

        /// <summary>
        /// The maximum value loaded
        /// </summary>
        public float MaxVal = float.MinValue;

        /// <summary>
        /// The Value loaded wrapped using the VTK value container. Each cell contains a timestep data
        /// </summary>
        public List<VTKValue> Value = new List<VTKValue>();

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
        /// <param name="t">The timestep to look at </param>
        /// <returns>The vector magnitude</returns>
        public float ReadMagnitude(UInt64 ind, int t) 
        {
            float mag = 0;
            for(UInt32 i = 0; i<NbValuesPerTuple; i++)
            {
                float r = Value[t].ReadAsFloat(NbValuesPerTuple * ind + i);
                mag += r* r;
            }
            return (float) Math.Sqrt(mag);
        }
    }
}
