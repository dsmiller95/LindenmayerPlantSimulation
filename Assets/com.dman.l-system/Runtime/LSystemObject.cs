using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [CreateAssetMenu(fileName = "LSystem", menuName = "LSystem/SystemDefinition")]
    public class LSystemObject : ScriptableObject
    {
        public string axiom;
        public int seed;
        [Multiline(30)]
        public string rules;

        public LSystem Compile(int? seedOverride = null)
        {
            try
            {
                return new LSystem(
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
