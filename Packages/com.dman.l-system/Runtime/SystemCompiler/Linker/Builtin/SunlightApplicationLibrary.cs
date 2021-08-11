using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker.Builtin
{
    /// <summary>
    /// the sunlight library will put an amount of sunlight for each frame into the first parameter of the LightAmount symbol
    /// </summary>
    [Serializable]
    public class SunlightApplicationLibrary : LinkedFile
    {
        public SunlightApplicationLibrary()
        {
            isLibrary = true;
            fileSource = "sunlight";
            allSymbols = "a";
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
            if (exportedName == "LightAmount")
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
            customSymbols.hasSunlight = true;
            customSymbols.sunlightSymbol = GetExportedSymbol("LightAmount");
        }
    }
}
