using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker.Builtin
{
    [Serializable]
    public class DiffusionLibrary : LinkedFile
    {
        public DiffusionLibrary()
        {
            isLibrary = true;
            fileSource = "diffusion";
            allSymbols = "na";
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
            if (exportedName == "Node")
            {
                return this.GetSymbolInFile('n');
            }
            if (exportedName == "Amount")
            {
                return this.GetSymbolInFile('a');
            }
            throw new NotImplementedException();
        }

        public override IEnumerable<string> GetRulesWithReplacements(Dictionary<string, string> replacementDirectives)
        {
            yield break;
        }

        public override void SetCustomRuleSymbols(ref SystemRuntime.CustomRules.CustomRuleSymbols customSymbols)
        {
            customSymbols.hasDiffusion = true;
            customSymbols.diffusionAmount = GetExportedSymbol("Amount");
            customSymbols.diffusionNode = GetExportedSymbol("Node");
            customSymbols.diffusionStepsPerStep = 1;
        }
    }
}
