using System.IO;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public class FileSystemFileProvider : IFileProvider
    {
        public ParsedFile ReadLinkedFile(string fullIdentifier)
        {
            var fullPath = fullIdentifier;
            var fileText = File.ReadAllText(fullPath);
            return new ParsedFile(fullPath, fileText, Path.GetExtension(fullPath) == ".lsyslib");
        }
    }
}
