using Dman.LSystem.SystemCompiler;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

namespace Dman.LSystem.SystemRuntime
{

    public class BasicRule
    {
        /// <summary>
        /// the symbol which this rule will replace. Apply rule will only ever be called with this symbol.
        /// </summary>
        public int TargetSymbol => _targetSymbolWithParameters.targetSymbol;

        public SymbolSeriesPrefixMatcher ContextPrefix {get; private set;}
        public SymbolSeriesSuffixMatcher ContextSuffix { get; private set; }

        public int CapturedLocalParameterCount { get; private set; }

        private readonly InputSymbol _targetSymbolWithParameters;
        private System.Delegate conditionalChecker;
        public bool HasConditional => conditionalChecker != null;

        public RuleOutcome[] possibleOutcomes;

        private SymbolSeriesPrefixBuilder backwardsMatchBuilder;
        private SymbolSeriesSuffixBuilder forwardsMatchBuilder;

        public BasicRule(ParsedRule parsedInfo, int branchOpenSymbol = '[', int branchCloseSymbol = ']')
        {
            _targetSymbolWithParameters = parsedInfo.coreSymbol;
            possibleOutcomes = new RuleOutcome[] {
                new RuleOutcome(1, parsedInfo.replacementSymbols)
            };

            conditionalChecker = parsedInfo.conditionalMatch;


            backwardsMatchBuilder = new SymbolSeriesPrefixBuilder(parsedInfo.backwardsMatch);

            forwardsMatchBuilder = new SymbolSeriesSuffixBuilder(parsedInfo.forwardsMatch);
            forwardsMatchBuilder.BuildGraphIndexes(branchOpenSymbol, branchCloseSymbol);


            CapturedLocalParameterCount = _targetSymbolWithParameters.parameterLength +
                backwardsMatchBuilder.targetSymbolSeries.Sum(x => x.parameterLength) +
                forwardsMatchBuilder.targetSymbolSeries.Sum(x => x.parameterLength);
        }
        /// <summary>
        /// Create a new basic rule with multiple random outcomes.
        /// It is garenteed and required that all of the stochastic rules will capture the
        /// same parameters
        /// </summary>
        /// <param name="parsedRules"></param>
        public BasicRule(IEnumerable<ParsedStochasticRule> parsedRules, int branchOpenSymbol = '[', int branchCloseSymbol = ']')
        {
            possibleOutcomes = parsedRules
                .Select(x =>
                    new RuleOutcome(x.probability, x.replacementSymbols)
                ).ToArray();
            var firstOutcome = parsedRules.First();
            _targetSymbolWithParameters = firstOutcome.coreSymbol;

            conditionalChecker = firstOutcome.conditionalMatch;

            backwardsMatchBuilder = new SymbolSeriesPrefixBuilder(firstOutcome.backwardsMatch);

            forwardsMatchBuilder = new SymbolSeriesSuffixBuilder(firstOutcome.forwardsMatch);
            forwardsMatchBuilder.BuildGraphIndexes(branchOpenSymbol, branchCloseSymbol);

            CapturedLocalParameterCount = _targetSymbolWithParameters.parameterLength +
                backwardsMatchBuilder.targetSymbolSeries.Sum(x => x.parameterLength) +
                forwardsMatchBuilder.targetSymbolSeries.Sum(x => x.parameterLength);
        }

        public RuleDataRequirements RequiredMemorySpace => new RuleDataRequirements
        {
            suffixChildren = forwardsMatchBuilder.RequiredChildrenMemSpace,
            suffixGraphNodes = forwardsMatchBuilder.RequiredGraphNodeMemSpace,
            prefixNodes = backwardsMatchBuilder.targetSymbolSeries.Length
        };


        public void WriteContextMatchesIntoMemory(
            SystemLevelRuleNativeData dataArray,
            SymbolSeriesMatcherNativeDataWriter dataWriter)
        {
            ContextSuffix = forwardsMatchBuilder.BuildIntoManagedMemory(dataArray, dataWriter);
            ContextPrefix = backwardsMatchBuilder.BuildIntoManagedMemory(dataArray, dataWriter);
        }

        public Blittable AsBlittable()
        {
            return new Blittable
            {
                contextPrefix = ContextPrefix,
                contextSuffix = ContextSuffix,
                targetSymbolWithParameters = _targetSymbolWithParameters.AsBlittable(),
                capturedLocalParameterCount = CapturedLocalParameterCount,
                possibleOutcomes = possibleOutcomes.Select(x => x.AsBlittable()).ToArray(),
                hasConditional = conditionalChecker != null
            };
        }

        public struct Blittable
        {
            public SymbolSeriesPrefixMatcher contextPrefix;
            public SymbolSeriesSuffixMatcher contextSuffix;
            public InputSymbol.Blittable targetSymbolWithParameters;
            public int capturedLocalParameterCount;
            public RuleOutcome.Blittable[] possibleOutcomes;
            public bool hasConditional;

            private byte SelectOutcomeIndex(ref Unity.Mathematics.Random rand)
            {
                if (possibleOutcomes.Length > 1)
                {
                    var sample = rand.NextDouble();
                    double currentPartition = 0;
                    for (byte i = 0; i < possibleOutcomes.Length; i++)
                    {
                        var possibleOutcome = possibleOutcomes[i];
                        currentPartition += possibleOutcome.probability;
                        if (sample <= currentPartition)
                        {
                            return i;
                        }
                    }
                    throw new LSystemRuntimeException("possible outcome probabilities do not sum to 1");
                }
                return 0;
            }

            public bool PreMatchCapturedParametersWithoutConditional(
                SymbolStringBranchingCache branchingCache,
                SymbolString<float> source,
                int indexInSymbols,
                NativeArray<float> parameterMemory,
                int startIndexInParameterMemory,
                out LSystemPotentialMatchData specificMatchData)
            {
                specificMatchData = default;
                var target = targetSymbolWithParameters;

                // parameters
                byte matchedParameterNum = 0;

                // context match
                if (contextPrefix.IsValid && contextPrefix.graphNodeMemSpace.length > 0)
                {
                    var backwardMatchMapping = branchingCache.MatchesBackwards(
                        indexInSymbols,
                        contextPrefix,
                        source.symbols,
                        source.newParameters.indexing
                        );
                    if (backwardMatchMapping == null)
                    {
                        // if backwards match exists, and does not match, then fail this match attempt.
                        return false;
                    }
                    for (int indexInPrefix = 0; indexInPrefix < contextPrefix.graphNodeMemSpace.length; indexInPrefix++)
                    {
                        if (!backwardMatchMapping.TryGetValue(indexInPrefix, out var matchingTargetIndex))
                        {
                            continue;
                        }
                        var parametersIndexing = source.newParameters[matchingTargetIndex];
                        for (int i = 0; i < parametersIndexing.length; i++)
                        {
                            var paramValue = source.newParameters[parametersIndexing, i];

                            parameterMemory[startIndexInParameterMemory + matchedParameterNum] = paramValue;
                            matchedParameterNum++;
                        }
                    }
                }

                var coreParametersIndexing = source.newParameters[indexInSymbols];
                if (coreParametersIndexing.length != target.parameterLength)
                {
                    return false;
                }
                if (coreParametersIndexing.length > 0)
                {
                    for (int i = 0; i < coreParametersIndexing.length; i++)
                    {
                        var paramValue = source.newParameters[coreParametersIndexing, i];

                        parameterMemory[startIndexInParameterMemory + matchedParameterNum] = paramValue;
                        matchedParameterNum++;
                    }
                }

                if (contextSuffix.IsCreated && contextSuffix.graphNodeMemSpace.length > 0)
                {
                    var forwardMatch = branchingCache.MatchesForward(
                        indexInSymbols,
                        contextSuffix,
                        source.symbols,
                        source.newParameters.indexing);
                    if (forwardMatch == null)
                    {
                        // if forwards match exists, and does not match, then fail this match attempt.
                        return false;
                    }
                    for (int indexInSuffix = 0; indexInSuffix < contextSuffix.graphNodeMemSpace.length; indexInSuffix++)
                    {
                        if (!forwardMatch.TryGetValue(indexInSuffix, out var matchingTargetIndex))
                        {
                            continue;
                        }

                        var parametersIndexing = source.newParameters[matchingTargetIndex];
                        for (int i = 0; i < parametersIndexing.length; i++)
                        {
                            var paramValue = source.newParameters[parametersIndexing, i];

                            parameterMemory[startIndexInParameterMemory + matchedParameterNum] = paramValue;
                            matchedParameterNum++;
                        }
                    }
                }

                specificMatchData = new LSystemPotentialMatchData
                {
                    matchedParameters = new JaggedIndexing
                    {
                        index = startIndexInParameterMemory,
                        length = matchedParameterNum
                    }
                };

                return true;
            }
        }

        public bool TryMatchSpecificMatch(
            NativeArray<float> globalParameters,
            NativeArray<float> parameterMemory,
            JaggedIndexing indexingInTmpParameterMemory,
            ref Unity.Mathematics.Random random,
            ref LSystemSingleSymbolMatchData matchSingletonData)
        {
            var matchedParameterNum = indexingInTmpParameterMemory.length;
            var parameterStartIndex = indexingInTmpParameterMemory.index;
            // conditional
            if (conditionalChecker != null)
            {
                var paramArray = new object[globalParameters.Length + matchedParameterNum];
                for (int i = 0; i < globalParameters.Length; i++)
                {
                    paramArray[i] = globalParameters[i];
                }
                for (int i = 0; i < matchedParameterNum; i++)
                {
                    paramArray[i + globalParameters.Length] = parameterMemory[parameterStartIndex + i];
                }
                var invokeResult = conditionalChecker.DynamicInvoke(paramArray);
                if (!(invokeResult is bool boolResult))
                {
                    // TODO: call this out a bit better. All compilation context is lost here
                    throw new LSystemRuntimeException($"Conditional expression must evaluate to a boolean");
                }
                var conditionalResult = boolResult;
                if (!conditionalResult)
                {
                    return false;
                }
            }

            // stochastic selection
            matchSingletonData.selectedReplacementPattern = SelectOutcomeIndex(ref random);
            var outcomeObject = possibleOutcomes[matchSingletonData.selectedReplacementPattern];

            matchSingletonData.tmpParameterMemorySpace = indexingInTmpParameterMemory;

            matchSingletonData.replacementSymbolIndexing = JaggedIndexing.GetWithOnlyLength(outcomeObject.ReplacementSymbolCount());
            matchSingletonData.replacementParameterIndexing = JaggedIndexing.GetWithOnlyLength(outcomeObject.ReplacementParameterCount());

            return true;
        }


        //public bool PreMatchCapturedParameters(
        //    SymbolStringBranchingCache branchingCache,
        //    SymbolString<float> source,
        //    int indexInSymbols,
        //    NativeArray<float> globalParameters,
        //    NativeArray<float> parameterMemory,
        //    ref Unity.Mathematics.Random random,
        //    ref LSystemSingleSymbolMatchData matchSingletonData)
        //{
        //    var target = _targetSymbolWithParameters;

        //    var parameterStartIndex = matchSingletonData.tmpParameterMemorySpace;

        //    // parameters
        //    byte matchedParameterNum = 0;

        //    // context match
        //    if (ContextPrefix.IsCreated && ContextPrefix.targetSymbolSeries.Length > 0)
        //    {
        //        var backwardMatchMapping = branchingCache.MatchesBackwards(
        //            indexInSymbols,
        //            ContextPrefix,
        //            source.symbols,
        //            source.newParameters.indexing
        //            );
        //        if (backwardMatchMapping == null)
        //        {
        //            // if backwards match exists, and does not match, then fail this match attempt.
        //            return false;
        //        }
        //        for (int indexInPrefix = 0; indexInPrefix < ContextPrefix.targetSymbolSeries.Length; indexInPrefix++)
        //        {
        //            if (!backwardMatchMapping.TryGetValue(indexInPrefix, out var matchingTargetIndex))
        //            {
        //                continue;
        //            }
        //            var parametersIndexing = source.newParameters[matchingTargetIndex];
        //            for (int i = 0; i < parametersIndexing.length; i++)
        //            {
        //                var paramValue = source.newParameters[parametersIndexing, i];

        //                parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
        //                matchedParameterNum++;
        //            }
        //        }
        //    }


        //    var coreParametersIndexing = source.newParameters[indexInSymbols];
        //    if (coreParametersIndexing.length != target.parameterLength)
        //    {
        //        return false;
        //    }
        //    if (coreParametersIndexing.length > 0)
        //    {
        //        for (int i = 0; i < coreParametersIndexing.length; i++)
        //        {
        //            var paramValue = source.newParameters[coreParametersIndexing, i];

        //            parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
        //            matchedParameterNum++;
        //        }
        //    }

        //    if (ContextSuffix.IsCreated && ContextSuffix.targetSymbolSeries.Length > 0)
        //    {
        //        var forwardMatch = branchingCache.MatchesForward(
        //            indexInSymbols,
        //            ContextSuffix,
        //            source.symbols,
        //            source.newParameters.indexing);
        //        if (forwardMatch == null)
        //        {
        //            // if forwards match exists, and does not match, then fail this match attempt.
        //            return false;
        //        }
        //        for (int indexInSuffix = 0; indexInSuffix < ContextSuffix.targetSymbolSeries.Length; indexInSuffix++)
        //        {
        //            if (!forwardMatch.TryGetValue(indexInSuffix, out var matchingTargetIndex))
        //            {
        //                continue;
        //            }

        //            var parametersIndexing = source.newParameters[matchingTargetIndex];
        //            for (int i = 0; i < parametersIndexing.length; i++)
        //            {
        //                var paramValue = source.newParameters[parametersIndexing, i];

        //                parameterMemory[parameterStartIndex + matchedParameterNum] = paramValue;
        //                matchedParameterNum++;
        //            }
        //        }
        //    }

        //    // conditional
        //    if (conditionalChecker != null)
        //    {
        //        var paramArray = new object[globalParameters.Length + matchedParameterNum];
        //        for (int i = 0; i < globalParameters.Length; i++)
        //        {
        //            paramArray[i] = globalParameters[i];
        //        }
        //        for (int i = 0; i < matchedParameterNum; i++)
        //        {
        //            paramArray[i + globalParameters.Length] = parameterMemory[parameterStartIndex + i];
        //        }
        //        var invokeResult = conditionalChecker.DynamicInvoke(paramArray);
        //        if (!(invokeResult is bool boolResult))
        //        {
        //            // TODO: call this out a bit better. All compilation context is lost here
        //            throw new LSystemRuntimeException($"Conditional expression must evaluate to a boolean");
        //        }
        //        var conditionalResult = boolResult;
        //        if (!conditionalResult)
        //        {
        //            return false;
        //        }
        //    }

        //    // stochastic selection
        //    matchSingletonData.selectedReplacementPattern = SelectOutcomeIndex(ref random);
        //    var outcomeObject = possibleOutcomes[matchSingletonData.selectedReplacementPattern];
        //    matchSingletonData.tmpParameterMatchedCount = matchedParameterNum;

        //    matchSingletonData.replacementSymbolLength = outcomeObject.ReplacementSymbolCount();
        //    matchSingletonData.replacementParameterCount = outcomeObject.ReplacementParameterCount();

        //    return true;
        //}

        private byte SelectOutcomeIndex(ref Unity.Mathematics.Random rand)
        {
            if (possibleOutcomes.Length > 1)
            {
                var sample = rand.NextDouble();
                double currentPartition = 0;
                for (byte i = 0; i < possibleOutcomes.Length; i++)
                {
                    var possibleOutcome = possibleOutcomes[i];
                    currentPartition += possibleOutcome.probability;
                    if (sample <= currentPartition)
                    {
                        return i;
                    }
                }
                throw new LSystemRuntimeException("possible outcome probabilities do not sum to 1");
            }
            return 0;
        }

        public void WriteReplacementSymbols(
            NativeArray<float> globalParameters,
            NativeArray<float> paramTempMemorySpace,
            SymbolString<float> target,
            LSystemSingleSymbolMatchData matchSingletonData)
        {
            var selectedReplacementPattern = matchSingletonData.selectedReplacementPattern;

            var matchedParametersIndexing = matchSingletonData.tmpParameterMemorySpace;
            var replacementSymbolsIndexing = matchSingletonData.replacementSymbolIndexing;
            var replacementParameterIndexing = matchSingletonData.replacementParameterIndexing;

            var orderedMatchedParameters = new object[globalParameters.Length + matchedParametersIndexing.length];
            for (int i = 0; i < globalParameters.Length; i++)
            {
                orderedMatchedParameters[i] = globalParameters[i];
            }
            for (int i = 0; i < matchedParametersIndexing.length; i++)
            {
                orderedMatchedParameters[globalParameters.Length + i] = paramTempMemorySpace[matchedParametersIndexing.index + i];
            }
            var outcome = possibleOutcomes[selectedReplacementPattern];

            var replacement = outcome.GenerateReplacement(orderedMatchedParameters);
            if(replacement.Length != replacementSymbolsIndexing.length)
            {
                throw new System.Exception("Unexpected state: replacement symbol size differs from expected");
            }
            if (replacement.newParameters.data.Length != replacementParameterIndexing.length)
            {
                throw new System.Exception("Unexpected state: replacement paremeter size differs from expected");
            }

            target.CopyFrom(replacement, replacementSymbolsIndexing.index, replacementParameterIndexing.index);
        }
    }
}
