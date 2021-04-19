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
        /// retrun the symbol string to replace the rule's matching symbols with. return null if no match
        /// </summary>
        /// <param name="parameters">the parameters applied to the symbol,
        ///     indexed based on matches inside the target symbol series.
        ///     Could be an array of null if no parameters.
        ///     Will always be the same length as what is returned from TargetSymbolSeries</param>
        /// <returns></returns>
        public SymbolString<T> ApplyRule(
            SymbolStringBranchingCache branchingCache,
            SymbolString<T> symbols,
            int indexInSymbols,
            ref Unity.Mathematics.Random random,
            T[] globalParameters);

        /// <summary>
        /// Attempts to match this rule against the symbol string, and return success
        ///     will write match data to the intermediate match struct
        /// </summary>
        /// <param name="branchingCache"></param>
        /// <param name="symbols"></param>
        /// <param name="indexInSymbols"></param>
        /// <returns>a list of captured parameters. null if no match</returns>
        public bool PreMatchCapturedParameters(
            SymbolStringBranchingCache branchingCache,
            NativeArray<int> sourceSymbols,
            NativeArray<JaggedIndexing> sourceParameterIndexes,
            NativeArray<float> sourceParameters,
            int indexInSymbols,
            float[] globalParameters,
            NativeArray<float> parameterMemory,
            ref Unity.Mathematics.Random random,
            ref LSystemStepMatchIntermediate matchSingletonData
            );
        /// <summary>
        /// Writes the replacement symbols into the target symbols arrays, beginning at <paramref name="originIndexInSymbols"/>.
        ///     should write exactly <paramref name="expectedReplacementSymbolLength"/> symbols
        /// </summary>
        /// <param name="branchingCache"></param>
        /// <param name="symbols"></param>
        /// <param name="indexInSymbols"></param>
        /// <returns>a list of captured parameters. null if no match</returns>
        public void WriteReplacementSymbols(
            T[] globalParameters,
            NativeArray<T> sourceParams,
            SymbolString<float> target,
            LSystemStepMatchIntermediate matchSingletonData
            );
    }
}
