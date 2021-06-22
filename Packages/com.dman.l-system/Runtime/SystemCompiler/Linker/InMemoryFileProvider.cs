using Dman.LSystem.SystemCompiler.Linker.Builtin;
using System.Collections.Generic;
using System.IO;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public class InMemoryFileProvider : IFileProvider
    {
        private Dictionary<string, string> fileContents = new Dictionary<string, string>();
        private BuiltinLibraries builtins;

        public InMemoryFileProvider(BuiltinLibraries builtins = null)
        {
            this.builtins = builtins;
        }

        public void RegisterFileWithIdentifier(string fileIdentifier, string fileContent)
        {
            fileContents[fileIdentifier] = fileContent;
        }

        public LinkedFile ReadLinkedFile(string fullIdentifier)
        {
            var builtin = builtins?.GetBuiltinIfExists(fullIdentifier);
            if(builtin != null)
            {
                return builtin;
            }
            if (!fileContents.ContainsKey(fullIdentifier))
            {
                return null;
            }
            var fileText = fileContents[fullIdentifier];
            return new ParsedFile(fullIdentifier, fileText, Path.GetExtension(fullIdentifier) == ".lsyslib", builtins.AllBuiltins());
        }
    }
}
