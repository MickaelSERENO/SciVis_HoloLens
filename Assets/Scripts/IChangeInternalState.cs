using Sereno.Datasets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno
{
    public interface IChangeInternalState
    {
        /// <summary>
        /// Set the internal model
        /// </summary>
        /// <param name="sd">The new internal model</param>
        void SetSubDatasetState(SubDataset sd);
    }
}
