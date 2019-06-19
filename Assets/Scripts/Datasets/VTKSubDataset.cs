namespace Sereno.Datasets
{
    public class VTKSubDataset : SubDataset
    {
        private VTKFieldValue m_fieldValue;

        public VTKSubDataset(Dataset parent, VTKFieldValue fieldValue) : base(parent)
        {
            m_fieldValue = fieldValue;
        }

        public VTKFieldValue FieldValue { get => m_fieldValue; }
    }
}