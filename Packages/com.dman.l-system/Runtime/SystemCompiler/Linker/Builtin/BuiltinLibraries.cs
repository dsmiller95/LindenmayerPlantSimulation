using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.SystemCompiler.Linker.Builtin
{
    public class BuiltinLibraries
    {
        private Dictionary<string, LinkedFile> builtinFiles = new Dictionary<string, LinkedFile>();

        public void RegisterBuiltin(LinkedFile builtin)
        {
            this.builtinFiles[builtin.fileSource] = builtin;
        }

        public string[] AllBuiltins()
        {
            return builtinFiles.Select(x => x.Key).ToArray();
        }
        
        public LinkedFile GetBuiltinIfExists(string builtinName)
        {
            if(builtinFiles.TryGetValue(builtinName, out var file))
            {
                return file;
            }
            return null;
        }
    }
}
