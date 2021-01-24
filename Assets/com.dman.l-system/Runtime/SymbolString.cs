using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem
{
    public class SymbolString
    {
        public int[] symbols;
        public float[][] parameters;

        public SymbolString(string symbols) : this(symbols.ToIntArray())
        {
        }
        public SymbolString(int[] symbols): this(symbols, new float[symbols.Length][])
        {
        }
        public SymbolString(int[] symbols, float[][] parameters)
        {
            this.symbols = symbols;
            this.parameters = parameters;
        }

        public static SymbolString FromSingle(int symbol, float[] paramters)
        {
            return new SymbolString(new int[] { symbol }, new float[][] { paramters });
        }
        public static SymbolString ConcatAll(IList<SymbolString> symbolStrings)
        {
            var totalSize = symbolStrings.Sum(x => x.symbols.Length);
            var newSymbols = new int[totalSize];
            var newParameters = new float[totalSize][];
            int currentIndex = 0;
            foreach (var symbolString in symbolStrings)
            {
                symbolString.symbols.CopyTo(newSymbols, currentIndex);
                symbolString.parameters.CopyTo(newParameters, currentIndex);

                currentIndex += symbolString.symbols.Length;
            }
            return new SymbolString(newSymbols, newParameters);
        }
    }
}
