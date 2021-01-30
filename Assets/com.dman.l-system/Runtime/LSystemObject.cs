using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [CreateAssetMenu(fileName = "LSystem", menuName = "LSystem/SystemDefinition")]
    public class LSystemObject : ScriptableObject
    {
        public int iterations = 7;

        public string axiom;
        public int seed;
        [Multiline(30)]
        public string rules;

        public LSystem<double> Compile(int? seedOverride = null)
        {
            try
            {
                return new LSystem<double>(
                    axiom,
                    ParsedRule.CompileRules(
                        rules
                            .Split('\n')
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrEmpty(x))),
                    seedOverride ?? seed);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
