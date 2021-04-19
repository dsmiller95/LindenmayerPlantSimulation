using Dman.LSystem.SystemRuntime;
using LSystem.Runtime.SystemRuntime;
using System;
using Unity.Collections;

namespace Dman.LSystem
{
    public interface IRule<T> where T : unmanaged
    {
        public SymbolSeriesMatcher ContextPrefix { get; }
        public SymbolSeriesMatcher ContextSuffix { get; }

        /// <summary>
        /// the symbols which this rule will replace. Apply rule will only ever be called when replacing these exact symbols.
        /// This property will always return the same value for any given instance. It can be used to index the rule
        /// </summary>
        public int TargetSymbol { get; }

        /// <summary>
        /// the number of local parameters captured by this rule from the symbol string
        /// </summary>
        public int CapturedLocalParameterCount { get; }

        /// <summary>
        /// Attempts to match this rule against the symbol string, and return success
        ///     will write match data to the intermediate match struct
        /// </summary>
        /// <returns>a list of captured parameters. null if no match</returns>
        public bool PreMatchCapturedParameters(
            SymbolStringBranchingCache branchingCache,
            SymbolString<T> source,
            int indexInSymbols,
            NativeArray<T> globalParameters,
            NativeArray<T> parameterMemory,
            ref Unity.Mathematics.Random random,
            ref LSystemStepMatchIntermediate matchSingletonData
            );
        /// <summary>
        /// Writes the replacement symbols into the target symbols arrays, beginning at <paramref name="originIndexInSymbols"/>.
        ///     should write exactly <paramref name="expectedReplacementSymbolLength"/> symbols
        /// </summary>
        /// <returns>a list of captured parameters. null if no match</returns>
        public void WriteReplacementSymbols(
            NativeArray<T> globalParameters,
            NativeArray<T> sourceParams,
            SymbolString<T> target,
            LSystemStepMatchIntermediate matchSingletonData
            );
    }
}
