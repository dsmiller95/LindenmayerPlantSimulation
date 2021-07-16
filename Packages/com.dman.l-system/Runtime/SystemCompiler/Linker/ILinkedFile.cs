using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.CustomRules;
using System;
using System.Collections.Generic;

namespace Dman.LSystem.SystemCompiler.Linker
{
    /// <summary>
    /// Represents one compiled file, included as part of the linking process
    /// </summary>
    [Serializable]
    public abstract class LinkedFile
    {
        public string axiom = null;
        public int iterations = -1;
        public bool isLibrary;
        public string fileSource;

        public string allSymbols = "";
        public string globalCharacters = "";
        public string immaturityMarkerCharacters = "";

        /// <summary>
        /// These are used to build a list of truly global parameters, when the files are grouped into a <see cref="LinkedFileSet"/>
        /// </summary>
        public List<DefineDirective> delaredInFileCompileTimeParameters;
        public List<RuntimeParameterAndDefault> declaredInFileRuntimeParameters;
        public List<SymbolRemap> allSymbolAssignments = new List<SymbolRemap>();
        public List<IncludeLink> links;

        public abstract IEnumerable<string> GetRulesWithReplacements(Dictionary<string, string> replacementDirectives);

        /// <summary>
        /// returns every symbol which can be search contextually by rules in this file
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<int> GetAllIncludedContextualSymbols();
        public virtual IEnumerable<int> GetAllImmaturityMarkerSymbols()
        {
            yield break;
        }

        public abstract int GetExportedSymbol(string exportedName);
        public virtual void SetCustomRuleSymbols(ref CustomRuleSymbols customSymbols)
        {

        }
    }

    public static class LinkedFileExtensions
    {
        public static int GetSymbolInFile(this LinkedFile file, char symbol)
        {
            var match = file.allSymbolAssignments.Find(x => x.sourceCharacter == symbol);
            if (match == null)
            {
                throw new LSystemRuntimeException($"{file.fileSource} does not contain requested symbol '{symbol}'. Did you forget to declare it in a <color=blue>#symbols</color> directive?");
            }
            return match.remappedSymbol;
        }
    }
}
