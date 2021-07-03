using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker.Builtin
{
    /// <summary>
    /// the Autophagy Library allows for convenient removal of all symbols following a certain point in the current branching pattern
    /// </summary>
    [Serializable]
    public class AutophagyLibrary : LinkedFile
    {
        public AutophagyLibrary()
        {
            isLibrary = true;
            fileSource = "autophagy";
            // a is the autophagy symbol. z is the null-symbol which will be used to replace every other symbol following the autophagy symbol
            allSymbols = "az";
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
            if (exportedName == "Necrose")
            {
                return this.GetSymbolInFile('a');
            }
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetRulesWithReplacements(Dictionary<string, string> replacementDirectives)
        {
            yield return "z ->";
            yield break;
        }

        public override void SetCustomRuleSymbols(ref SystemRuntime.CustomRules.CustomRuleSymbols customSymbols)
        {
            customSymbols.hasAutophagy = true;
            customSymbols.autophagicSymbol = this.GetSymbolInFile('a');
            customSymbols.deadSymbol = this.GetSymbolInFile('z');
        }
    }
}
