using Dman.LSystem.SystemRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;

namespace Dman.LSystem
{

    public struct SymbolString<ParamType> : System.IEquatable<SymbolString<ParamType>>, ISymbolString, IDisposable where ParamType: unmanaged
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<int> symbols;
        [NativeDisableParallelForRestriction]
        public JaggedNativeArray<ParamType> newParameters;
        public int Length => symbols.Length;

        public int this[int index] => symbols[index];

        public SymbolString(string symbolString, Allocator allocator = Allocator.Persistent)
        {
            var symbolMatch = ReplacementSymbolGeneratorParser.ParseReplacementSymbolGenerators(
                symbolString,
                new string[0]).ToArray();
            symbols = new NativeArray<int>(
                symbolMatch.Select(x => x.targetSymbol).ToArray(),
                allocator);

            var jaggedParameters = symbolMatch
                .Select(x => x.evaluators.Select(y => (ParamType)y.DynamicInvoke()).ToArray())
                .ToArray();
            newParameters = new JaggedNativeArray<ParamType>(jaggedParameters, allocator);
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
            this.newParameters = new JaggedNativeArray<ParamType>(paramArray, allocator);
        }
        public SymbolString(int symbolsTotal, int parametersTotal, Allocator allocator)
        {
            symbols = new NativeArray<int>(symbolsTotal, allocator, NativeArrayOptions.UninitializedMemory);
            newParameters = new JaggedNativeArray<ParamType>(symbolsTotal, parametersTotal, allocator);
        }
        public SymbolString(SymbolString<ParamType> other, Allocator newAllocator)
        {
            symbols = new NativeArray<int>(other.symbols, newAllocator);
            newParameters = new JaggedNativeArray<ParamType>(other.newParameters, newAllocator);
        }

        public int ParameterSize(int index)
        {
            return newParameters[index].length;
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

            newParameters.CopyFrom(source.newParameters, targetIndex, targetParamIndex);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < symbols.Length; i++)
            {
                builder.Append((char)symbols[i]);
                var iIndex = newParameters[i];
                if (iIndex.length <= 0)
                {
                    continue;
                }
                builder.Append("(");
                for (int j = 0; j < iIndex.length; j++)
                {
                    var paramValue = newParameters[iIndex, j];
                    builder.Append(paramValue.ToString());
                    if (j < iIndex.length - 1)
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
            for (int i = 0; i < symbols.Length; i++)
            {
                if (other.symbols[i] != symbols[i])
                {
                    return false;
                }
            }
            if (!newParameters.Equals(other.newParameters))
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            this.newParameters.Dispose();
            this.symbols.Dispose();
        }
    }
}
