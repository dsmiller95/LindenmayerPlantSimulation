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

    public class SimpleSymbolRemapper : ISymbolRemapper, IEnumerable<KeyValuePair<int, char>>
    {
        private Dictionary<int, char> remapping = new Dictionary<int, char> ();

        public void SetMapping(char character, int symbol)
        {
            remapping[symbol] = character;
        }

        public void Add(char character, int symbol)
        {
            this.SetMapping(character, symbol);
        }

        public char GetCharacterInRoot(int symbol)
        {
            return remapping[symbol];
        }

        public int GetSymbolFromRoot(char character)
        {
            foreach (var pair in remapping)
            {
                if (pair.Value == character)
                {
                    return pair.Key;
                }
            }
            throw new KeyNotFoundException();
        }

        public IEnumerator<KeyValuePair<int, char>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<int, char>>)remapping).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)remapping).GetEnumerator();
        }
    }
}


