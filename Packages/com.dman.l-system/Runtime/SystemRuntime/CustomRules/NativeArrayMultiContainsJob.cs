using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules
{
    /// <summary>
    /// will make no modifcations, only read what is there.
    ///     checks to see if the symbol string contains any of the provided symbols
    /// </summary>
    [BurstCompile]
    struct NativeArrayMultiContainsJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> symbols;
        [ReadOnly]
        public NativeHashSet<int> symbolsToCheckFor;

        public NativeArray<bool> doesContainSymbols;

        public void Execute()
        {
            var containsSymbols = false;
            for (int symbolIndex = 0; symbolIndex < symbols.Length; symbolIndex++)
            {
                var symbol = symbols[symbolIndex];
                if (symbolsToCheckFor.Contains(symbol))
                {
                    containsSymbols = true;
                    break;
                }
            }
            doesContainSymbols[0] = containsSymbols;
        }
    }
}
