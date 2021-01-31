using Dman.LSystem.SystemRuntime;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [System.Serializable]
    public struct ParameterAndDefault
    {
        public string name;
        public double defaultValue;
    }

    [System.Serializable]
    public struct DefineDirectives
    {
        public string name;
        public string replacement;
    }
    [CreateAssetMenu(fileName = "LSystem", menuName = "LSystem/SystemDefinition")]
    public class LSystemObject : ScriptableObject
    {
        public int iterations = 7;

        public string axiom;
        public int seed;
        [Multiline(30)]
        public string rules;

        public ParameterAndDefault[] defaultGlobalRuntimeParameters;
        public DefineDirectives[] defaultGlobalCompileTimeParameters;

        public LSystem<double> Compile(int? seedOverride = null)
        {
            try
            {
                var rulesPostReplacement = rules;
                foreach (var replacement in defaultGlobalCompileTimeParameters)
                {
                    rulesPostReplacement = rulesPostReplacement.Replace(replacement.name, replacement.replacement);
                }
                var ruleLines = rulesPostReplacement
                    .Split('\n')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x));
                return LSystemBuilder.DoubleSystem(
                    axiom,
                    ruleLines,
                    seedOverride ?? seed,
                    defaultGlobalRuntimeParameters.Select(x => x.name).ToArray());
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
