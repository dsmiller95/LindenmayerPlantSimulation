using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
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
    public class LSystemObject : ScriptableObject
    {
        public int iterations = 7;

        public string axiom;
        public int seed;
        [Multiline(30)]
        public string rules;

        public string ignoredCharacters;

        public List<ParameterAndDefault> defaultGlobalRuntimeParameters;
        public List<DefineDirectives> defaultGlobalCompileTimeParameters;

        public LSystem<double> compiledSystem { get; private set; }

        /// <summary>
        /// Emits whenever the system is compiled
        /// </summary>
        public event Action OnSystemUpdated;

        public ArrayParameterRepresenation<double> GetRuntimeParameters()
        {
            return ArrayParameterRepresenation<double>.GenerateFromList(defaultGlobalRuntimeParameters, p => p.name, p => p.defaultValue);
        }

        /// <summary>
        /// Compile this L-system into the <see cref="compiledSystem"/> property
        /// </summary>
        /// <param name="globalCompileTimeOverrides">overrides to the compile time directives. Will only be applied if the Key matches an already defined compile time parameter</param>
        public void CompileToCached(Dictionary<string, string> globalCompileTimeOverrides = null)
        {
            var newSystem = this.CompileSystem(globalCompileTimeOverrides);
            if(newSystem != null)
            {
                compiledSystem = newSystem;
            }
        }

        /// <summary>
        /// Compile this L-system and return the result, not caching it into this object
        /// </summary>
        /// <param name="globalCompileTimeOverrides">overrides to the compile time directives. Will only be applied if the Key matches an already defined compile time parameter</param>
        public LSystem<double> CompileWithParameters(Dictionary<string, string> globalCompileTimeOverrides)
        {
            return this.CompileSystem(globalCompileTimeOverrides);
        }

        private LSystem<double> CompileSystem(Dictionary<string, string> globalCompileTimeOverrides)
        {
            UnityEngine.Profiling.Profiler.BeginSample("L System compilation");
            try
            {
                var rulesPostReplacement = rules;
                foreach (var replacement in defaultGlobalCompileTimeParameters)
                {
                    var replacementString = replacement.replacement;
                    if (globalCompileTimeOverrides != null && globalCompileTimeOverrides.TryGetValue(replacement.name, out var overrideValue))
                    {
                        replacementString = overrideValue;
                    }
                    rulesPostReplacement = rulesPostReplacement.Replace(replacement.name, replacementString);
                }
                var ruleLines = rulesPostReplacement
                    .Split('\n')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x));
                return LSystemBuilder.DoubleSystem(
                    ruleLines,
                    defaultGlobalRuntimeParameters.Select(x => x.name).ToArray(),
                    ignoredCharacters);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                UnityEngine.Profiling.Profiler.EndSample();
                OnSystemUpdated?.Invoke();
            }
            return null;
        }

        /// <summary>
        /// Reload this asset from the .lsystem file assocated with it
        /// NO-op if not in editor mode
        /// </summary>
        public void TriggerReloadFromFile()
        {
#if UNITY_EDITOR
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                var lSystemCode = File.ReadAllText(assetPath);
                ParseRulesFromCode(lSystemCode);
            }
#endif
        }

        /// <summary>
        /// Parse a whole code file into this object. does not compile the system. 
        /// </summary>
        /// <param name="fullCode">the entire .lsystem file</param>
        public void ParseRulesFromCode(string fullCode)
        {
            defaultGlobalRuntimeParameters = new List<ParameterAndDefault>();
            defaultGlobalCompileTimeParameters = new List<DefineDirectives>();
            var ruleLines = fullCode.Split('\n');
            var outputRules = new StringBuilder();

            foreach (var inputLine in ruleLines.Where(x => !string.IsNullOrEmpty(x)))
            {
                if (inputLine[0] == '#')
                {
                    try
                    {
                        ParseDirective(inputLine.Substring(1));
                    }
                    catch (SyntaxException e)
                    {
                        e.RecontextualizeIndex(1, inputLine);
                        Debug.LogException(e);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(inputLine))
                {
                    outputRules.AppendLine(inputLine);
                }
            }

            rules = outputRules.ToString();
        }


        private void ParseDirective(string directiveText)
        {
            var dirParams = Regex.Matches(directiveText, @"(?<param>[^ ]+)\s+");
            if (!dirParams[0].Success)
            {
                throw new SyntaxException($"missing directive after hash", -1, 1);
            }
            if (dirParams.Count < 2)
            {
                throw new SyntaxException($"missing directive parameter", dirParams[0]);
            }

            switch (dirParams[0].Groups["param"].Value)
            {
                case "axiom":
                    axiom = dirParams[1].Groups["param"].Value;
                    return;
                case "iterations":
                    if (!int.TryParse(dirParams[1].Groups["param"].Value, out int iterations))
                    {
                        throw new SyntaxException($"iterations must be an integer", dirParams[1]);
                    }
                    this.iterations = iterations;
                    return;
                case "ignore":
                    this.ignoredCharacters = dirParams[1].Groups["param"].Value;
                    return;
                case "runtime":
                    if (dirParams.Count < 3)
                    {
                        throw new SyntaxException($"runtime directive requires 2 parameters", dirParams[0]);
                    }
                    if (!int.TryParse(dirParams[2].Groups["param"].Value, out int runtimeDefault))
                    {
                        throw new SyntaxException($"runtime parameter must default to a number", dirParams[2]);
                    }
                    defaultGlobalRuntimeParameters.Add(new ParameterAndDefault
                    {
                        name = dirParams[1].Groups["param"].Value,
                        defaultValue = runtimeDefault
                    });
                    return;
                case "define":
                    if (dirParams.Count < 3)
                    {
                        throw new SyntaxException($"define directive requires 2 parameters", dirParams[0]);
                    }
                    defaultGlobalCompileTimeParameters.Add(new DefineDirectives
                    {
                        name = dirParams[1].Groups["param"].Value,
                        replacement = dirParams[2].Groups["param"].Value
                    });
                    return;
                case "#":
                    return;
                default:
                    throw new SyntaxException(
                        $"unrecognized directive name \"{dirParams[0].Groups["param"].Value}\"",
                        dirParams[0]);
            }
        }
    }
}
