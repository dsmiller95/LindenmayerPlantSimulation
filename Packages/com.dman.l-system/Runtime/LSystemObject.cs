using Dman.LSystem.SystemRuntime;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [System.Serializable]
    public struct GlobalParameterAndDefault
    {
        public string name;
        public double defaultValue;
    }

    [CreateAssetMenu(fileName = "LSystem", menuName = "LSystem/SystemDefinition")]
    public class LSystemObject : ScriptableObject
    {
        public int iterations = 7;

        public string axiom;
        public int seed;
        [Multiline(30)]
        public string rules;

        public GlobalParameterAndDefault[] defaultGlobalParameters;

        public LSystem<double> Compile(int? seedOverride = null)
        {
            try
            {
                var ruleLines = rules.Split('\n')
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrEmpty(x));
                return LSystemBuilder.DoubleSystem(
                    axiom,
                    ruleLines,
                    seedOverride ?? seed,
                    defaultGlobalParameters.Select(x => x.name).ToArray());
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }
}
