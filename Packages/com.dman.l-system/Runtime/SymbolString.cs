using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem
{

    public struct SymbolString<ParamType> :
        System.IEquatable<SymbolString<ParamType>>,
        ISymbolString,
        INativeDisposable
        where ParamType : unmanaged
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> symbols;
        [NativeDisableParallelForRestriction]
        public JaggedNativeArray<ParamType> parameters;
        public int Length => symbols.Length;

        public int this[int index]
        {
            get => symbols[index];
            set => symbols[index] = value;
        }


        public static SymbolString<float> FromString(string symbolString, Allocator allocator = Allocator.Persistent, Func<char, int> symbolMapping = null)
        {
            var symbolMatch = ReplacementSymbolGeneratorParser.ParseReplacementSymbolGenerators(
                symbolString,
                new string[0],
                x => symbolMapping == null ? x : symbolMapping(x)).ToArray();
            var symbols = new NativeArray<int>(
                symbolMatch.Select(x => x.targetSymbol).ToArray(),
                allocator);

            var jaggedParameters = symbolMatch
                .Select(x => x.evaluators.Select(y => y.DynamicInvoke()).ToArray())
                .ToArray();
            var newParameters = new JaggedNativeArray<float>(jaggedParameters, allocator);

            return new SymbolString<float>
            {
                symbols = symbols,
                parameters = newParameters
            };
        }
        public SymbolString(int symbol, ParamType[] parameters) :
            this(new int[] { symbol }, new ParamType[][] { parameters })
        {
        }
        public SymbolString(int[] symbols) : this(symbols, new ParamType[symbols.Length][])
        {
        }
        public SymbolString(int[] symbols, ParamType[][] paramArray, Allocator allocator = Allocator.Persistent)
        {
            this.symbols = new NativeArray<int>(symbols, allocator);
            parameters = new JaggedNativeArray<ParamType>(paramArray, allocator);
        }
        public SymbolString(int symbolsTotal, int parametersTotal, Allocator allocator)
        {
            symbols = new NativeArray<int>(symbolsTotal, allocator, NativeArrayOptions.UninitializedMemory);
            parameters = new JaggedNativeArray<ParamType>(symbolsTotal, parametersTotal, allocator);
        }
        public SymbolString(SymbolString<ParamType> other, Allocator newAllocator)
        {
            symbols = new NativeArray<int>(other.symbols, newAllocator);
            parameters = new JaggedNativeArray<ParamType>(other.parameters, newAllocator);
        }

        public int ParameterSize(int index)
        {
            return parameters[index].length;
        }

        /// <summary>
        /// copy all symbols and parameters in <paramref name="source"/>, into this symbol string at <paramref name="targetIndex"/>
        ///     Also blindly copies the parameters into <paramref name="targetParamIndex"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targetIndex"></param>
        public void CopyFrom(SymbolString<ParamType> source, int targetIndex, int targetParamIndex)
        {
            for (int i = 0; i < source.Length; i++)
            {
                var replacementSymbolIndex = targetIndex + i;
                symbols[replacementSymbolIndex] = source.symbols[i];
            }

            parameters.CopyFrom(source.parameters, targetIndex, targetParamIndex);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < symbols.Length; i++)
            {
                ToString(i, builder);
            }

            return builder.ToString();
        }

        public string ToString(Dictionary<int, char> symbolMapping)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < symbols.Length; i++)
            {
                ToString(i, builder, symbolMapping);
            }

            return builder.ToString();
        }

        public void ToString(int index, StringBuilder builder, Dictionary<int, char> symbolMapping = null)
        {
            var mappedSymbol = symbolMapping == null ? (char)symbols[index] : symbolMapping[symbols[index]];
            builder.Append(mappedSymbol);
            WriteParamString(index, builder);
        }
        public void WriteParamString(int index, StringBuilder builder)
        {
            var iIndex = parameters[index];
            if (iIndex.length <= 0)
            {
                return;
            }
            builder.Append("(");
            for (int j = 0; j < iIndex.length; j++)
            {
                var paramValue = parameters[iIndex, j];
                builder.Append(paramValue.ToString());
                if (j < iIndex.length - 1)
                {
                    builder.Append(", ");
                }
            }
            builder.Append(")");
        }

        public override bool Equals(object obj)
        {
            if (obj is SymbolString<ParamType> other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return symbols.Length.GetHashCode();
        }
        public bool Equals(SymbolString<ParamType> other)
        {
            if (other.symbols.Length != symbols.Length)
            {
                return false;
            }
            for (int i = 0; i < symbols.Length; i++)
            {
                if (other.symbols[i] != symbols[i])
                {
                    return false;
                }
            }
            if (!parameters.Equals(other.parameters))
            {
                return false;
            }
            return true;
        }

        public bool IsCreated => symbols.IsCreated || parameters.IsCreated;

        public void Dispose()
        {
            parameters.Dispose();
            symbols.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(
                parameters.Dispose(inputDeps),
                symbols.Dispose(inputDeps));
        }
    }
}
