﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sereno.Datasets
{
    public class CloudPointDataset : Dataset
    {
        private String  m_path      = "";
        private UInt32  m_nbPoints  = 0;
        private float[] m_positions = null;
        private float[] m_data      = null;

        public CloudPointDataset(int id, String name, String path) : base(id, name)
        {
            m_path = path;

            using(FileStream file = File.Open($"{Application.streamingAssetsPath}/{name}", FileMode.Open))
            {
                if (file == null)
                {
                    Debug.LogError($"Cannot open the file {m_path}.");
                    return;
                }

                //Read the first 4 bytes to get the number of points stored in the file
                byte[] array = new byte[4];
                file.Read(array, 0, 4);
                IntFloatUnion intFloatUnion = new IntFloatUnion();
                intFloatUnion.FillWithByteArray(array, 0);
                m_nbPoints = (UInt32)intFloatUnion.IntField;
                                
                //Add a point field descriptor corresponding to the unique data stored in the file
                PointFieldDescriptor desc = new PointFieldDescriptor();
                desc.Format           = VTKValueFormat.VTK_FLOAT;
                desc.ID               = 0;
                desc.Name             = "data";
                desc.NbValuesPerTuple = 1;
                desc.NbTuples         = (uint)m_nbPoints;
                m_ptFieldDescs.Add(desc);
            }
        }

        public override Task<int> LoadValues()
        {
            return Task.Run(() =>
            {
                using(FileStream file = File.Open($"{Application.streamingAssetsPath}/{m_path}", FileMode.Open))
                {
                    //Test the file
                    if(file == null)
                    {
                        Debug.LogError($"Cannot open the file {m_path}.");
                        return 0;
                    }

                    IntFloatUnion intFloatUnion = new IntFloatUnion();

                    //Check the size of the file
                    file.Seek(0, SeekOrigin.End);
                    if((file.Position - 4) / (4*sizeof(float)) != m_nbPoints)
                    {
                        Debug.LogError($"The file {Application.streamingAssetsPath}/{m_path} is invalid. Excepted {m_nbPoints}, but the file size suggests {(file.Position - 4) / (4*sizeof(float))}.");
                        return 0;
                    }

                    //A multi-purpose buffer to store data read
                    byte[] buffer = new byte[4096];

                    //Read all the positions
                    file.Seek(4, SeekOrigin.Begin);
                    m_positions = new float[3 * m_nbPoints];
                    Int64 i = (int)m_nbPoints-1;
                    while (i >= 0)
                    {
                        int nbPointInRead = (int)Math.Min(i+1, buffer.Length / (3 * sizeof(float)));
                        file.Read(buffer, 0, nbPointInRead * 3 * sizeof(float));

                        for(int j = 0; j < nbPointInRead; j++)
                        {
                            for(int k = 0; k < 3; k++)
                            {
                                intFloatUnion.FillWithByteArray(buffer, sizeof(float)*(3*j+k));
                                m_positions[3*(m_nbPoints-1-i) + k] = intFloatUnion.FloatField;
                            }
                            i--;
                        }
                    }

                    //Read all the data
                    m_data = new float[m_nbPoints];
                    i = (int)m_nbPoints-1;

                    float minVal = float.MaxValue;
                    float maxVal = -minVal;

                    while (i >= 0)
                    {
                        int nbPointInRead = (int)Math.Min(i+1, buffer.Length / (sizeof(float)));
                        file.Read(buffer, 0, nbPointInRead * sizeof(float));

                        for(int j = 0; j < nbPointInRead; j++)
                        {
                            intFloatUnion.FillWithByteArray(buffer, sizeof(float) * j);
                            m_data[m_nbPoints-1-i] = intFloatUnion.FloatField;
                            minVal = Math.Min(minVal, intFloatUnion.FloatField);
                            maxVal = Math.Max(maxVal, intFloatUnion.FloatField);
                            i--;
                        }
                    }

                    unsafe
                    {
                        fixed (float* p = m_data)
                        {
                            m_ptFieldDescs[0].Value = new VTKValue()
                            {
                                Value = (IntPtr)p,
                                Format = VTKValueFormat.VTK_FLOAT,
                                NbValues = m_nbPoints
                            };
                            m_ptFieldDescs[0].MaxVal = maxVal;
                            m_ptFieldDescs[0].MinVal = minVal;
                        }
                    }
                    m_isLoaded = true;
                    return 1;
                }
            });
        }

        /// <summary>
        /// Get the path containing all the data cloud points
        /// </summary>
        public String Path { get => m_path; }

        /// <summary>
        /// Get the number of points of this cloud
        /// </summary>
        public UInt32  NbPoints { get => m_nbPoints; }

        /// <summary>
        /// Get the 3D positions of this data.
        /// Packs of 3 values represent one position (x, y, z). Positions and data are aligned altogether (positions[0] corresponds to data[0])
        /// </summary>
        public float[] Position { get => m_positions; }
    }
}