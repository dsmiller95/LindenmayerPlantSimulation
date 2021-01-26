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

        public SymbolString(string symbols) : this(symbols.ToIntArray())
        {
        }
        public SymbolString(int[] symbols): this(symbols, new ParamType[symbols.Length][])
        {
        }
        public SymbolString(int[] symbols, ParamType[][] parameters)
        {
            this.symbols = symbols;
            this.parameters = parameters;
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
