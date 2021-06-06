using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [System.Serializable]
    public class SymbolRemap : IEquatable<SymbolRemap>
    {
        public char sourceCharacter;
        public int remappedSymbol;

        public bool Equals(SymbolRemap other)
        {
            return other.sourceCharacter == sourceCharacter && other.remappedSymbol == remappedSymbol;
        }

        public override string ToString()
        {
            return $"{sourceCharacter}: {remappedSymbol}";
        }
    }
}
