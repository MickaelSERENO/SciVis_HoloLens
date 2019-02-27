using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno
{
    public class VTKSubDataset : SubDataset
    {
        private VTKFieldValue m_fieldValue;

        public VTKSubDataset(Dataset parent, VTKFieldValue fieldValue) : base(parent)
        {
            m_fieldValue = fieldValue;
            m_tfTexture  = new TFTexture(new TriangularGTF(new float[]{0.5f}, new float[]{0.5f}), new Vector2Int(512, 512), ColorMode.RAINBOW);
            float[] values = new float[512*512*2];
            for(int j = 0; j < 512; j++)
                for(int i = 0; i < 512; i++)
                {
                    values[2*(i+j*512)]   = i/511.0f;
                    values[2*(i+j*512)+1] = j/511.0f;
                }
            m_tfTexture.ComputeTexture(values, 2);
            m_tfTexture.Texture.Apply();
        }

        public VTKFieldValue FieldValue { get => m_fieldValue; }
    }
}
