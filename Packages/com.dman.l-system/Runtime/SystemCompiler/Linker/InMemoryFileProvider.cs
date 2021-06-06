using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public class InMemoryFileProvider: IFileProvider
    {
        private Dictionary<string, string> fileContents = new Dictionary<string, string>();

        public void RegisterFileWithIdentifier(string fileIdentifier, string fileContent)
        {
            fileContents[fileIdentifier] = fileContent;
        }

        public ParsedFile ReadLinkedFile(string fullIdentifier)
        {
            if (!fileContents.ContainsKey(fullIdentifier))
            {
                return null;
            }
            var fileText = fileContents[fullIdentifier];
            return new ParsedFile(fullIdentifier, fileText, Path.GetExtension(fullIdentifier) == ".lsyslib");
        }
    }
}
