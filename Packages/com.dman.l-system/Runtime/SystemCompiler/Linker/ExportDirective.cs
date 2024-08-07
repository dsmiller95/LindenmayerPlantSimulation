﻿using System;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [System.Serializable]
    public class ExportDirective : IEquatable<ExportDirective>
    {
        public string name;
        public char exportedSymbol;

        public bool Equals(ExportDirective other)
        {
            return other.name == name && other.exportedSymbol == exportedSymbol;
        }

        public override string ToString()
        {
            return $"export {name} {exportedSymbol}";
        }
    }
}
