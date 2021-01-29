using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem
{
    public class SymbolString<ParamType>
    {
        public int[] symbols;
        public ParamType[][] parameters;

        public SymbolString(string symbolString)
        {
            var symbolMatch = SymbolReplacementExpressionMatcher.ParseAllSymbolExpressions(symbolString, new string[0]).ToArray();
            symbols = new int[symbolMatch.Length];
            parameters = new ParamType[symbolMatch.Length][];
            for (int i = 0; i < symbolMatch.Length; i++)
            {
                var match = symbolMatch[i];
                symbols[i] = match.targetSymbol;
                parameters[i] = match.evaluators.Select(x => (ParamType)x.DynamicInvoke()).ToArray();
            }
        }
        public SymbolString(int[] symbols): this(symbols, new ParamType[symbols.Length][])
        {
        }
        public SymbolString(int[] symbols, ParamType[][] parameters)
        {
            this.symbols = symbols;
            this.parameters = parameters;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < symbols.Length; i++)
            {
                builder.Append((char)symbols[i]);
                var param = parameters[i];
                if(param == null || param.Length <= 0)
                {
                    continue;
                }
                builder.Append("(");
                for (int j = 0; j < param.Length; j++)
                {
                    builder.Append(param[j].ToString());
                    if(j < param.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
                builder.Append(")");
            }

            return builder.ToString();
        }

        public static SymbolString<ParamType> FromSingle(int symbol, ParamType[] paramters)
        {
            return new SymbolString<ParamType>(new int[] { symbol }, new ParamType[][] { paramters });
        }
        public static SymbolString<ParamType> ConcatAll(IList<SymbolString<ParamType>> symbolStrings)
        {
            var totalSize = symbolStrings.Sum(x => x.symbols.Length);
            var newSymbols = new int[totalSize];
            var newParameters = new ParamType[totalSize][];
            int currentIndex = 0;
            foreach (var symbolString in symbolStrings)
            {
                symbolString.symbols.CopyTo(newSymbols, currentIndex);
                symbolString.parameters.CopyTo(newParameters, currentIndex);

                currentIndex += symbolString.symbols.Length;
            }
            return new SymbolString<ParamType>(newSymbols, newParameters);
        }
    }
}
