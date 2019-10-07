using System.Collections.Generic;
using System.Linq;

namespace Sereno.Datasets
{
    public class VTKDataset : Dataset
    {
        /// <summary>
        /// The VTKParser bound to this VTKDataset
        /// </summary>
        private VTKParser           m_parser;

        /// <summary>
        /// The VTKFieldValue point list to take account of
        /// </summary>
        private List<VTKFieldValue> m_ptFieldValues;

        /// <summary>
        /// The VTKFieldValue cell list to take account of
        /// </summary>
        private List<VTKFieldValue> m_cellFieldValues;

        /// <summary>
        /// Represent a VTKDataset
        /// </summary>
        /// <param name="parser">The VTKParser containing all the dataset.</param>
        /// <param name="ptFieldValues">Array of point field values to take account of</param>
        /// <param name="cellFieldValues">Array of cell field values to take account of</param>
        public VTKDataset(int id, VTKParser parser, VTKFieldValue[] ptFieldValues, VTKFieldValue[] cellFieldValues) : base(id)
        {
            m_parser = parser;
            m_ptFieldValues = new List<VTKFieldValue>(ptFieldValues);
            m_cellFieldValues = new List<VTKFieldValue>(cellFieldValues);
        }
        /// <summary>
        /// The VTKParser containing all the dataset
        /// </summary>
        public VTKParser           Parser         {get => m_parser;}

        /// <summary>
        /// List of point field values to take account of
        /// </summary>
        public List<VTKFieldValue> PtFieldValues  {get => m_ptFieldValues;}

        /// <summary>
        /// List of cell field values to take account of
        /// </summary>
        public List<VTKFieldValue> CellFieldValues {get => m_cellFieldValues;}
    }
}