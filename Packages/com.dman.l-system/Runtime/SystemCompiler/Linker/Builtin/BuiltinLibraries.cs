using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dman.LSystem.SystemCompiler.Linker.Builtin
{
    public class BuiltinLibraries : IEnumerable<LinkedFile>
    {
        private Dictionary<string, LinkedFile> builtinFiles = new Dictionary<string, LinkedFile>();

        public static BuiltinLibraries Default()
        {
            return new BuiltinLibraries
            {
                new DiffusionLibrary(),
                new OrganIdentifyingLibrary(),
                new SunlightApplicationLibrary(),
                new AutophagyLibrary(),
                new ExtraVertexDataLibrary()
            };
        }

        public void Add(LinkedFile builtin)
        {
            builtinFiles[builtin.fileSource] = builtin;
        }

        public string[] AllBuiltins()
        {
            return builtinFiles.Select(x => x.Key).ToArray();
        }

        public LinkedFile GetBuiltinIfExists(string builtinName)
        {
            if (builtinFiles.TryGetValue(builtinName, out var file))
            {
                return file;
            }
            return null;
        }

        public IEnumerator<LinkedFile> GetEnumerator()
        {
            return builtinFiles.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return builtinFiles.Values.GetEnumerator();
        }
    }
}
