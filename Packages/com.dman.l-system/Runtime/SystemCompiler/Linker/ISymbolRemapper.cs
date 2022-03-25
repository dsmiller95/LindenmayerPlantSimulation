using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.Utilities.SerializableUnityObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Dman.LSystem.SystemCompiler.Linker
{
    public interface ISymbolRemapper
    {
        public int GetSymbolFromRoot(char character);
        public char GetCharacterInRoot(int symbol);
    }

    /// <summary>
    /// maps the character to its character code directly
    /// </summary>
    public class SimpleSymbolRemapper : ISymbolRemapper
    {
        public char GetCharacterInRoot(int symbol)
        {
            return (char)symbol;
        }

        public int GetSymbolFromRoot(char character)
        {
            return character;
        }
    }
}


