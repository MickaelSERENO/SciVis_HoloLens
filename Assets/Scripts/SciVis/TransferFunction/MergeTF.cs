using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sereno.SciVis
{
    /// <summary>
    /// Transfer Function whose result is the interpolation between two others transfer functions
    /// </summary>
    class MergeTF : TransferFunction
    {
        /// <summary>
        /// The first transfer function to interpolate (at t==0.0f)
        /// </summary>
        private TransferFunction m_tf1 = null;

        /// <summary>
        /// The second transfer function to interpolate (at t==1.0f)
        /// </summary>
        private TransferFunction m_tf2 = null;

        /// <summary>
        /// The interpolation "t" parameter.
        /// </summary>
        private float m_t = 0.0f;
        
        public MergeTF(TransferFunction tf1, TransferFunction tf2, float t=0.0f) : base(tf1.ColorMode)
        {
            m_tf1 = tf1.Clone();
            m_tf2 = tf2.Clone();
            m_t   = t;
        }

        public override TransferFunction Clone()
        {
            MergeTF tf  = new MergeTF(m_tf1, m_tf2, m_t);
            tf.Timestep = Timestep;
            return tf;
        }

        /// <summary>
        /// Compute the alpha parameter using (1.0-t)*m_tf1 + t*m_tf2
        /// </summary>
        /// <param name="values">The values to compute the color. Must be between 0.0f and 1.0f</param>
        /// <returns>The alpha t parameter. Negative values when errors occure (e.g array too large)</returns>
        public override float ComputeAlpha(float[] values)
        {
            float tf1Val;
            float tf2Val;

            uint dim = GetDimension();

            //We need to rearrange "values" because of the gradient of the lowest dimension object

            //Check tf1
            if (m_tf1.GetDimension() < dim && m_tf1.HasGradient())
            {
                float temp = values[m_tf1.GetDimension() - 1];
                values[m_tf1.GetDimension() - 1] = values[dim - 1];
                tf1Val = m_tf1.ComputeAlpha(values);
                values[m_tf1.GetDimension() - 1] = temp;
            }
            else
                tf1Val = m_tf1.ComputeAlpha(values);

            //Check tf2
            if (m_tf2.GetDimension() < dim && m_tf2.HasGradient())
            {
                float temp = values[m_tf2.GetDimension() - 1];
                values[m_tf2.GetDimension() - 1] = values[dim - 1];
                tf2Val = m_tf2.ComputeAlpha(values);
                values[m_tf2.GetDimension() - 1] = temp;
            }
            else
                tf2Val = m_tf2.ComputeAlpha(values);

            return (1.0f - m_t)*tf1Val + m_t*tf2Val;
        }

        /// <summary>
        /// Compute the Color t parameter using (1.0-t)*m_tf1 + t*m_tf2
        /// </summary>
        /// <param name="values">The values to compute the color. Must be between 0.0f and 1.0f. Length: at minimum the length of GetDimension()</param>
        /// <returns>The color</returns>
        public override Color ComputeColor(float[] values)
        {
            Color tf1Val;
            Color tf2Val;

            uint dim = GetDimension();

            //We need to rearrange "values" because of the gradient of the lowest dimension object

            //Check tf1
            if (m_tf1.GetDimension() < dim && m_tf1.HasGradient())
            {
                float temp = values[m_tf1.GetDimension() - 1];
                values[m_tf1.GetDimension() - 1] = values[dim - 1];
                tf1Val = m_tf1.ComputeColor(values);
                values[m_tf1.GetDimension() - 1] = temp;
            }
            else
                tf1Val = m_tf1.ComputeColor(values);

            //Check tf2
            if (m_tf2.GetDimension() < dim && m_tf2.HasGradient())
            {
                float temp = values[m_tf2.GetDimension() - 1];
                values[m_tf2.GetDimension() - 1] = values[dim - 1];
                tf2Val = m_tf2.ComputeColor(values);
                values[m_tf2.GetDimension() - 1] = temp;
            }
            else
                tf2Val = m_tf2.ComputeColor(values);

            return (1.0f - m_t)*tf1Val + m_t*tf2Val;
        }

        public override uint GetDimension()
        {
            uint dim = Math.Max(m_tf1.GetDimension(), m_tf2.GetDimension());

            if(m_tf1.GetDimension() <=  m_tf2.GetDimension() &&
               m_tf1.HasGradient()  && !m_tf2.HasGradient())
                dim++;

            else if(m_tf2.GetDimension() <=  m_tf1.GetDimension() &&
                    m_tf2.HasGradient()  && !m_tf1.HasGradient())
                dim++;

            return dim;
        }

        public override bool HasGradient()
        {
            return m_tf1.HasGradient() || m_tf2.HasGradient();
        }
    }
}
