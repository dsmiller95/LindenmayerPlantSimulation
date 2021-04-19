using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime
{
    public struct JaggedIndexing : IEquatable<JaggedIndexing>
    {
        public int index;
        public ushort length;

        public int Start => index;
        public int End => index + length;

        public bool Equals(JaggedIndexing other)
        {
            return other.index == index && other.length == length;
        }
        public override bool Equals(object obj)
        {
            if (obj is JaggedIndexing indexing)
            {
                return this.Equals(indexing);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return index << 31 | length;
        }
    }

    public struct SymbolString<ParamType> : System.IEquatable<SymbolString<ParamType>>, ISymbolString, IDisposable where ParamType: unmanaged
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> symbols;
        [NativeDisableParallelForRestriction]
        public NativeArray<JaggedIndexing> parameterIndexes;
        [NativeDisableParallelForRestriction]
        public NativeArray<ParamType> parameters;
        public int Length => symbols.Length;

        public int this[int index] => symbols[index];

        public SymbolString(string symbolString, Allocator allocator = Allocator.Persistent)
        {
            var symbolMatch = ReplacementSymbolGeneratorParser.ParseReplacementSymbolGenerators(
                symbolString,
                new string[0]).ToArray();
            symbols = new NativeArray<int>(symbolMatch.Length, allocator);
            parameterIndexes = new NativeArray<JaggedIndexing>(symbolMatch.Length, allocator);
            var paramList = new List<ParamType>();
            var paramSum = 0;
            for (int i = 0; i < symbolMatch.Length; i++)
            {
                var match = symbolMatch[i];
                symbols[i] = match.targetSymbol;
                var parameters = match.evaluators.Select(x => (ParamType)x.DynamicInvoke()).ToArray();
                parameterIndexes[i] = new JaggedIndexing
                {
                    index = (int)paramSum,
                    length = (ushort)parameters.Length
                };
                paramSum += parameters.Length;

                paramList.AddRange(parameters);
            }
            parameters = new NativeArray<ParamType>(paramList.ToArray(), allocator);
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
            parameterIndexes = new NativeArray<JaggedIndexing>(symbols.Length, allocator, NativeArrayOptions.UninitializedMemory);
            var paramSum = 0;
            var paramList = new List<ParamType>();
            for (int i = 0; i < paramArray.Length; i++)
            {
                parameterIndexes[i] = new JaggedIndexing
                {
                    index = (int)paramSum,
                    length = (ushort)paramArray[i].Length
                };
                paramSum += paramArray[i].Length;
                paramList.AddRange(paramArray[i]);
            }
            parameters = new NativeArray<ParamType>(paramList.ToArray(), allocator);
        }
        public SymbolString(int symbolsTotal, int parametersTotal, Allocator allocator)
        {
            symbols = new NativeArray<int>(symbolsTotal, allocator, NativeArrayOptions.UninitializedMemory);
            parameterIndexes = new NativeArray<JaggedIndexing>(symbolsTotal, allocator, NativeArrayOptions.UninitializedMemory);
            parameters = new NativeArray<ParamType>(parametersTotal, allocator, NativeArrayOptions.UninitializedMemory);
        }
        public SymbolString(SymbolString<ParamType> other, Allocator newAllocator)
        {
            symbols = new NativeArray<int>(other.symbols, newAllocator);
            parameterIndexes = new NativeArray<JaggedIndexing>(other.parameterIndexes, newAllocator);
            parameters = new NativeArray<ParamType>(other.parameters, newAllocator);
        }

        public int ParameterSize(int index)
        {
            return parameterIndexes[index].length;
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

                var replacementParamIndexing = source.parameterIndexes[i];
                parameterIndexes[replacementSymbolIndex] = new JaggedIndexing
                {
                    index = targetParamIndex + replacementParamIndexing.index,
                    length = replacementParamIndexing.length
                };
            }
            for (int i = 0; i < source.parameters.Length; i++)
            {
                parameters[targetParamIndex + i] = source.parameters[i];
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < symbols.Length; i++)
            {
                builder.Append((char)symbols[i]);
                var paramIndexing = parameterIndexes[i];
                if (paramIndexing.length <= 0)
                {
                    continue;
                }
                builder.Append("(");
                for (int j = 0; j < paramIndexing.length; j++)
                {
                    var paramValue = parameters[paramIndexing.index + j];
                    builder.Append(paramValue.ToString());
                    if (j < paramIndexing.length - 1)
                    {
                        builder.Append(", ");
                    }
                }
                builder.Append(")");
            }

            return builder.ToString();
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
            var paramTypeComparer = EqualityComparer<ParamType>.Default;
            for (int i = 0; i < symbols.Length; i++)
            {
                if (other.symbols[i] != symbols[i])
                {
                    return false;
                }
                if (!other.parameterIndexes[i].Equals(parameterIndexes[i]))
                {
                    return false;
                }
                var paramIndexer = parameterIndexes[i];
                for (int j = 0; j < paramIndexer.length; j++)
                {
                    var index = paramIndexer.index + j;
                    if (!paramTypeComparer.Equals(other.parameters[index], parameters[index]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void Dispose()
        {
            this.parameterIndexes.Dispose();
            this.parameters.Dispose();
            this.symbols.Dispose();
        }
    }
}
