using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Linq;
using Dman.LSystem.Extern;
using Unity.Collections;
using OperatorDefinition = Dman.LSystem.SystemRuntime.DynamicExpressions.OperatorDefinition;

namespace Dman.LSystem.SystemRuntime
{
    public class RuleOutcome
    {
        public double probability;
        public ReplacementSymbolGenerator[] replacementSymbols;
        private ushort replacementParameterCount;

        private Blittable blittable;

        public RuleOutcome(double prob, ReplacementSymbolGenerator[] replacements)
        {
            probability = prob;
            replacementSymbols = replacements;
            replacementParameterCount = (ushort)replacementSymbols.Select(x => x.GeneratedParameterCount()).Sum();
        }

        public RuleDataRequirements MemoryReqs => new RuleDataRequirements
        {
            replacementSymbolsMemory = replacementSymbols.Length
        } + replacementSymbols.Aggregate(new RuleDataRequirements(), (a, b) => a + b.MemoryReqs);
        public void WriteIntoMemory(
            SystemLevelRuleNativeData dataArray,
            SymbolSeriesMatcherNativeDataWriter dataWriter)
        {
            var replacementSymbolSpace = new JaggedIndexing
            {
                index = dataWriter.indexInReplacementSymbolsMemory,
                length = (ushort)replacementSymbols.Length
            };
            for (int i = 0; i < replacementSymbols.Length; i++)
            {
                var blittableReplacement = replacementSymbols[i].WriteOpsIntoMemory(dataArray, dataWriter);
                dataArray.replacementsSymbolMemorySpace[i + replacementSymbolSpace.index] = blittableReplacement;
            }
            dataWriter.indexInReplacementSymbolsMemory += replacementSymbolSpace.length;

            blittable = new Blittable
            {
                probability = probability,
                replacementSymbolSize = (ushort)replacementSymbols.Length,
                replacementParameterCount = replacementParameterCount,
                replacementSymbols = replacementSymbolSpace
            };
        }

        public Blittable AsBlittable()
        {
            return blittable;
        }
        public struct Blittable
        {
            public double probability;
            public ushort replacementSymbolSize;
            public ushort replacementParameterCount;
            public JaggedIndexing replacementSymbols;
            public void WriteReplacement(
                NativeArray<float> matchedParameters,
                JaggedIndexing parameterSpace,
                NativeArray<OperatorDefinition> operatorData,
                SymbolString<float> target,
                NativeArray<ReplacementSymbolGenerator.Blittable> replacementSymbolSpace,
                NativeArray<StructExpression> structExpressionSpace,
                int firstIndexInSymbols,
                int firstIndexInParamters)
            {
                var writeIndexInParams = firstIndexInParamters;
                for (int symbolIndex = 0; symbolIndex < replacementSymbols.length; symbolIndex++)
                {
                    var replacementExpression = replacementSymbolSpace[symbolIndex + replacementSymbols.index];
                    target[symbolIndex + firstIndexInSymbols] = replacementExpression.replacementSymbol;
                    replacementExpression.WriteNewParameters(
                        matchedParameters,
                        parameterSpace,
                        operatorData,
                        target.parameters,
                        structExpressionSpace,
                        ref writeIndexInParams,
                        symbolIndex + firstIndexInSymbols
                        );
                }
            }

        }

        public ushort ReplacementSymbolCount()
        {
            return (ushort)replacementSymbols.Length;
        }
        public ushort ReplacementParameterCount()
        {
            return replacementParameterCount;
        }
    }
}
