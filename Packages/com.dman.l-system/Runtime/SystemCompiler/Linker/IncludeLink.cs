using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public struct SymbolRemap: IEquatable<SymbolRemap>
    {
        public string importName;
        public char remappedSymbol;

        public bool Equals(SymbolRemap other)
        {
            return other.importName == importName && other.remappedSymbol == remappedSymbol;
        }

        public override string ToString()
        {
            return $"({importName}->{remappedSymbol})";
        }
    }

    public struct IncludeLink
    {
        public string relativeImportPath;
        public List<SymbolRemap> importedSymbols;
    }
}
