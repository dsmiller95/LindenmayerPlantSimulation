using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [System.Serializable]
    public struct DefineDirective
    {
        public string name;
        public string replacement;
    }
}
