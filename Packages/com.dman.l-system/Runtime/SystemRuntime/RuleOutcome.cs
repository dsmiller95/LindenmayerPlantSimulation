using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Linq;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct RuleOutcome
    {
        public double probability;
        public ReplacementSymbolGenerator[] replacementSymbols;
        private ushort replacementParameterCount;

        public RuleOutcome(double prob, ReplacementSymbolGenerator[] replacements)
        {
            probability = prob;
            replacementSymbols = replacements;
            replacementParameterCount = (ushort)replacementSymbols.Select(x => x.GeneratedParameterCount()).Sum();
        }

        public int OpMemoryRequirements => replacementSymbols.Sum(x => x.OpMemoryRequirements);
        public void WriteOpsIntoMemory(
            SystemLevelRuleNativeData dataArray,
            SymbolSeriesMatcherNativeDataWriter dataWriter)
        {
            foreach (var replacement in replacementSymbols)
            {
                replacement.WriteOpsIntoMemory(dataArray, dataWriter);
            }
        }

        public Blittable AsBlittable()
        {
            return new Blittable
            {
                probability = probability,
                replacementSymbolSize = replacementSymbols.Length,
                replacementParameterCount = replacementParameterCount
            };
        }
        public struct Blittable
        {
            public double probability;
            public int replacementSymbolSize;
            public ushort replacementParameterCount;
        }

        public ushort ReplacementSymbolCount()
        {
            return (ushort)replacementSymbols.Length;
        }
        public ushort ReplacementParameterCount()
        {
            return replacementParameterCount;
        }

        public SymbolString<float> GenerateReplacement(
            NativeArray<float> matchedParameters,
            JaggedIndexing parameterSpace,
            NativeArray<OperatorDefinition> operatorData,
            Allocator allocator = Allocator.Temp)
        {
            // TODO: less garbage
            var replacedSymbols = new int[replacementSymbols.Length];
            var replacedParams = new float[replacementSymbols.Length][];
            for (int symbolIndex = 0; symbolIndex < replacementSymbols.Length; symbolIndex++)
            {
                var replacementExpression = replacementSymbols[symbolIndex];

                replacedSymbols[symbolIndex] = replacementExpression.targetSymbol;
                replacedParams[symbolIndex] = replacementExpression.EvaluateNewParameters(
                    matchedParameters,
                    parameterSpace,
                    operatorData);
            }

            return new SymbolString<float>(replacedSymbols, replacedParams, allocator);
        }
    }
}
