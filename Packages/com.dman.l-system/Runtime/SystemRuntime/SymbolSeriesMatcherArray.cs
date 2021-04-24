using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct SymbolSeriesMatcherNativeDataArray
    {
        /// <summary>
        /// used to index from the symbol series index into all the other data in 
        /// </summary>
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public JaggedNativeArray<int> rootSymbolSeriesAllocations;
    }
}
