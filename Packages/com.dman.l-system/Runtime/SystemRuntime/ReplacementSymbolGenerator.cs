using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Collections.Generic;
using System.Linq;
using Dman.LSystem.Extern;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public class ReplacementSymbolGenerator
    {
        public int targetSymbol;
        public DynamicExpressionData[] evaluators;
        private Blittable blittable;
        private int evaluatorMemSpaceReqs;
        public ReplacementSymbolGenerator(int targetSymbol)
        {
            this.targetSymbol = targetSymbol;
            evaluators = new DynamicExpressionData[0];
            evaluatorMemSpaceReqs = 0;
        }
        public ReplacementSymbolGenerator(int targetSymbol, IEnumerable<DynamicExpressionData> evaluatorExpressions)
        {
            this.targetSymbol = targetSymbol;
            evaluators = evaluatorExpressions.ToArray();
            evaluatorMemSpaceReqs = evaluators.Sum(x => x.OperatorSpaceNeeded);
        }

        public RuleDataRequirements MemoryReqs => new RuleDataRequirements
        {
            operatorMemory = evaluatorMemSpaceReqs,
            structExpressionMemory = evaluators.Length
        };

        public Blittable WriteOpsIntoMemory(
            SystemLevelRuleNativeData dataArray,
            SymbolSeriesMatcherNativeDataWriter dataWriter)
        {
            var structExpressionsSpace = new JaggedIndexing
            {
                index = dataWriter.indexInStructExpressionMemory,
                length = (ushort)evaluators.Length
            };

            var totalOperations = 0;
            for (int i = 0; i < evaluators.Length; i++)
            {
                var opSize = evaluators[i].OperatorSpaceNeeded;
                dataArray.structExpressionMemorySpace[i + structExpressionsSpace.index] = evaluators[i].WriteIntoOpDataArray(
                    dataArray.dynamicOperatorMemory,
                    new JaggedIndexing
                    {
                        index = dataWriter.indexInOperatorMemory + totalOperations,
                        length = opSize
                    });
                totalOperations += opSize;
            }
            dataWriter.indexInOperatorMemory += totalOperations;

            dataWriter.indexInStructExpressionMemory += structExpressionsSpace.length;

            return (blittable = new Blittable
            {
                structExpressionSpace = structExpressionsSpace,
                replacementSymbol = targetSymbol
            });
        }

        public struct Blittable
        {
            public JaggedIndexing structExpressionSpace;
            public int replacementSymbol;
            public void WriteNewParameters(
                NativeArray<float> matchedParameters,
                JaggedIndexing parameterSpace,
                NativeArray<OperatorDefinition> operatorData,
                JaggedNativeArray<float> targetParams,
                NativeArray<StructExpression> structExpressionData,
                ref int writeIndexInParamSpace,
                int indexInParams)
            {
                var targetSpace = new JaggedIndexing
                {
                    index = writeIndexInParamSpace,
                    length = (ushort)structExpressionSpace.length
                };
                targetParams[indexInParams] = targetSpace;
                for (int i = 0; i < structExpressionSpace.length; i++)
                {
                    var structExp = structExpressionData[i + structExpressionSpace.index];
                    targetParams[targetSpace, i] = StructExpression.EvaluateExpression(structExp, matchedParameters,
                        parameterSpace,
                        operatorData);
                }
                writeIndexInParamSpace += targetSpace.length;
            }
        }
        public Blittable AsBlittable()
        {
            return blittable;
        }


        public int GeneratedParameterCount()
        {
            return evaluators.Length;
        }

        public override string ToString()
        {
            string result = ((char)targetSymbol) + "";
            if (evaluators.Length > 0)
            {
                result += @$"({evaluators
                    .Select(x => x.ToString())
                    .Aggregate((agg, curr) => agg + ", " + curr)})";
            }
            return result;
        }
    }

}
