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
                replacementSymbolSize = (ushort)replacementSymbols.Length,
                replacementParameterCount = replacementParameterCount
            };
        }
        public struct Blittable
        {
            public double probability;
            public ushort replacementSymbolSize;
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

        public void WriteReplacement(
            NativeArray<float> matchedParameters,
            JaggedIndexing parameterSpace,
            NativeArray<OperatorDefinition> operatorData,
            SymbolString<float> target,
            int firstIndexInSymbols,
            int firstIndexInParamters)
        {
            var writeIndexInParams = firstIndexInParamters;
            for (int symbolIndex = 0; symbolIndex < replacementSymbols.Length; symbolIndex++)
            {
                var replacementExpression = replacementSymbols[symbolIndex];
                target[symbolIndex + firstIndexInSymbols] = replacementExpression.targetSymbol;
                replacementExpression.WriteNewParameters(
                    matchedParameters,
                    parameterSpace,
                    operatorData,
                    target.newParameters,
                    ref writeIndexInParams,
                    symbolIndex + firstIndexInSymbols
                    );
            }
        }
    }
}
