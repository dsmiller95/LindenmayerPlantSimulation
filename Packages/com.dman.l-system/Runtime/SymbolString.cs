using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dman.LSystem.SystemRuntime
{
    public class SymbolString<ParamType> : System.IEquatable<SymbolString<ParamType>>, ISymbolString
    {

        public int[] symbols;
        public ParamType[][] parameters;

        public int Length => symbols?.Length ?? 0;

        public int this[int index] => symbols[index];

        public SymbolString(string symbolString)
        {
            var symbolMatch = ReplacementSymbolGeneratorParser.ParseReplacementSymbolGenerators(
                symbolString,
                new string[0]).ToArray();
            symbols = new int[symbolMatch.Length];
            parameters = new ParamType[symbolMatch.Length][];
            for (int i = 0; i < symbolMatch.Length; i++)
            {
                var match = symbolMatch[i];
                symbols[i] = match.targetSymbol;
                parameters[i] = match.evaluators.Select(x => (ParamType)x.DynamicInvoke()).ToArray();
            }
        }
        public SymbolString(int symbol, ParamType[] parameters) :
            this(new int[] { symbol }, new ParamType[][] { parameters })
        {
        }
        public SymbolString(int[] symbols) : this(symbols, new ParamType[symbols.Length][])
        {
        }
        public SymbolString(int[] symbols, ParamType[][] parameters)
        {
            this.symbols = symbols;
            this.parameters = parameters;
        }
        private SymbolString()
        {

        }


        public override string ToString()
        {
            var builder = new StringBuilder();
            for (int i = 0; i < symbols.Length; i++)
            {
                builder.Append((char)symbols[i]);
                var param = parameters[i];
                if (param == null || param.Length <= 0)
                {
                    continue;
                }
                builder.Append("(");
                for (int j = 0; j < param.Length; j++)
                {
                    builder.Append(param[j].ToString());
                    if (j < param.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
                builder.Append(")");
            }

            return builder.ToString();
        }

        public static SymbolString<ParamType> ConcatAll(IEnumerable<SymbolString<ParamType>> symbolStrings)
        {
            var totalSize = 0;
            foreach (var symbolString in symbolStrings)
            {
                if (symbolString == null)
                    continue;
                totalSize += symbolString.symbols.Length;
            }
            var newSymbols = new int[totalSize];
            var newParameters = new ParamType[totalSize][];
            int currentIndex = 0;
            foreach (var symbolString in symbolStrings)
            {
                if(symbolString == null)
                    continue;
                symbolString.symbols.CopyTo(newSymbols, currentIndex);
                symbolString.parameters.CopyTo(newParameters, currentIndex);

                currentIndex += symbolString.symbols.Length;
            }
            return new SymbolString<ParamType>(newSymbols, newParameters);
        }


        public bool Equals(SymbolString<ParamType> other)
        {
            if (other == null)
            {
                return false;
            }
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
                if (other.parameters[i].Length != parameters[i].Length)
                {
                    return false;
                }
                for (int j = 0; j < parameters[i].Length; j++)
                {
                    if (!paramTypeComparer.Equals(other.parameters[i][j], parameters[i][j]))
                    {
                        return false;
                    }
                }
            }
            return true;
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

    }
}
