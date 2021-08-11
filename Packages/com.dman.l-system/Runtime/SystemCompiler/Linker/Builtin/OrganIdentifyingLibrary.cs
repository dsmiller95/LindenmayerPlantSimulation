using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker.Builtin
{
    [Serializable]
    public class OrganIdentifyingLibrary : LinkedFile
    {
        public OrganIdentifyingLibrary()
        {
            isLibrary = true;
            fileSource = "organIdentity";
            allSymbols = "i";
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
            if (exportedName == "Identifier")
            {
                return this.GetSymbolInFile('i');
            }
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetRulesWithReplacements(Dictionary<string, string> replacementDirectives)
        {
            yield break;
        }

        public override void SetCustomRuleSymbols(ref SystemRuntime.CustomRules.CustomRuleSymbols customSymbols)
        {
            customSymbols.hasIdentifiers = true;
            customSymbols.identifier = GetExportedSymbol("Identifier");
        }
    }
}
