using Dman.LSystem.SystemCompiler.Linker.Builtin;
using System.IO;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public class FileSystemFileProvider : IFileProvider
    {
        private BuiltinLibraries builtins;
        public FileSystemFileProvider(BuiltinLibraries builtins = null)
        {
            if(builtins == null)
            {
                builtins = new BuiltinLibraries();
                builtins.RegisterBuiltin(new DiffusionLibrary());
            }
            this.builtins = builtins;
        }

        public LinkedFile ReadLinkedFile(string fullIdentifier)
        {
            var builtin = builtins?.GetBuiltinIfExists(fullIdentifier);
            if (builtin != null)
            {
                return builtin;
            }

            var fullPath = fullIdentifier;
            var fileText = File.ReadAllText(fullPath);
            return new ParsedFile(fullPath, fileText, Path.GetExtension(fullPath) == ".lsyslib", builtins.AllBuiltins());
        }
    }
}
