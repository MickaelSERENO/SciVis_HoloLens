using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sereno.SciVis;
using System.Threading.Tasks;

namespace Sereno.Datasets
{
    public class VTKSubDataset : SubDataset
    {
        private VTKFieldValue m_fieldValue;

        public VTKSubDataset(Dataset parent, VTKFieldValue fieldValue) : base(parent)
        {
            m_fieldValue = fieldValue;
            m_tfTexture  = new TFTexture(new TriangularGTF(new float[]{0.5f, 0.5f}, new float[]{0.25f, 0.25f}), new Vector2Int(512, 512), ColorMode.RAINBOW);
            float[] values = new float[512*512*2];

            Parallel.For(0, 512, j =>
            {
                for(int i = 0; i < 512; i++)
                {
                    values[2*(i+j*512)]   = i/(512.0f-1.0f);
                    values[2*(i+j*512)+1] = j/(512.0f-1.0f);
                }
            });
            m_tfTexture.ComputeTexture(values, 2);
        }

        public void UpdateGraphics()
        {
            m_tfTexture.UpdateTexture();
        }

        public VTKFieldValue FieldValue { get => m_fieldValue; }
    }
}