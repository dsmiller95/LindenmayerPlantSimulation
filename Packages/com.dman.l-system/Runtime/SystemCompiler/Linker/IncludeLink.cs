using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [Serializable]
    public class IncludeImportRemap : IEquatable<IncludeImportRemap>
    {
        public string importName;
        public char remappedSymbol;

        public bool Equals(IncludeImportRemap other)
        {
            return other.importName == importName && other.remappedSymbol == remappedSymbol;
        }

        public override string ToString()
        {
            return $"({importName}->{remappedSymbol})";
        }
    }

    [Serializable]
    public class IncludeLink
    {
        public string fullImportIdentifier;
        public List<IncludeImportRemap> importedSymbols;
    }
}
