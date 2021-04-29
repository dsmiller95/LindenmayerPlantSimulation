using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime.DynamicExpressions;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public class ReplacementSymbolGenerator
    {
        public int targetSymbol;
        public DynamicExpressionData[] evaluators;
        public StructExpression[] structExpressions;
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

        public int OpMemoryRequirements => evaluatorMemSpaceReqs;

        public void WriteOpsIntoMemory(
            SystemLevelRuleNativeData dataArray,
            SymbolSeriesMatcherNativeDataWriter dataWriter)
        {
            var origin = dataWriter.indexInOperatorMemory;
            this.structExpressions = new StructExpression[evaluators.Length];
            for (int i = 0; i < evaluators.Length; i++)
            {
                var opSize = (ushort)evaluators[i].OperatorSpaceNeeded;
                structExpressions[i] = evaluators[i].WriteIntOpDataArray(
                    dataArray.dynamicOperatorMemory,
                    new JaggedIndexing
                    {
                        index = origin,
                        length = opSize
                    });
                origin += opSize;
            }

            dataWriter.indexInOperatorMemory = origin;
        }

        public int GeneratedParameterCount()
        {
            return evaluators.Length;
        }

        public float[] EvaluateNewParameters(
            NativeArray<float> matchedParameters,
            JaggedIndexing parameterSpace,
            NativeArray<OperatorDefinition> operatorData)
        {
            return structExpressions
                .Select(x => x.EvaluateExpression(
                    matchedParameters,
                    parameterSpace,
                    operatorData))
                .ToArray();
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
