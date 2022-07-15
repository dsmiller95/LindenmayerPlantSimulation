using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker.Builtin
{
    [Serializable]
    public class ExtraVertexDataLibrary : LinkedFile
    {
        public ExtraVertexDataLibrary()
        {
            isLibrary = true;
            fileSource = "extraVertexData";
            allSymbols = "v";
            links = new List<IncludeLink>();
            declaredInFileRuntimeParameters = new List<RuntimeParameterAndDefault>();
            delaredInFileCompileTimeParameters = new List<DefineDirective>();
        }

        public override IEnumerable<int> GetAllIncludedContextualSymbols()
        {
            yield break;
        }

        public override int GetExportedSymbol(string exportedName)
        {
            if (exportedName == "VertexData")
            {
                return this.GetSymbolInFile('v');
            }
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetRulesWithReplacements(Dictionary<string, string> replacementDirectives)
        {
            yield break;
        }

        public override void SetCustomRuleSymbols(ref SystemRuntime.CustomRules.CustomRuleSymbols customSymbols)
        {
            customSymbols.hasExtraVertexData = true;
            customSymbols.extraVertexDataSymbol = GetExportedSymbol("VertexData");
        }
    }
}
