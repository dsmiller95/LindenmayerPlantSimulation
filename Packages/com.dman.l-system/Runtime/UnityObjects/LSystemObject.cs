using Dman.LSystem.SystemCompiler;
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
        public float defaultValue;
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

        public LSystem compiledSystem { get; private set; }

        /// <summary>
        /// Emits whenever the system is compiled
        /// </summary>
        public event Action OnCachedSystemUpdated;

        public ArrayParameterRepresenation<float> GetRuntimeParameters()
        {
            return ArrayParameterRepresenation<float>.GenerateFromList(defaultGlobalRuntimeParameters, p => p.name, p => p.defaultValue);
        }

        /// <summary>
        /// Compile this L-system into the <see cref="compiledSystem"/> property
        /// </summary>
        /// <param name="globalCompileTimeOverrides">overrides to the compile time directives. Will only be applied if the Key matches an already defined compile time parameter</param>
        public void CompileToCached(Dictionary<string, string> globalCompileTimeOverrides = null)
        {
            var newSystem = CompileSystem(globalCompileTimeOverrides);
            if (newSystem != null)
            {
                compiledSystem?.Dispose();
                compiledSystem = newSystem;

                OnCachedSystemUpdated?.Invoke();
            }
        }

        private void OnDisable()
        {
            compiledSystem?.Dispose();
            compiledSystem = null;
        }

        private void OnDestroy()
        {
            compiledSystem?.Dispose();
            compiledSystem = null;
        }

        /// <summary>
        /// Compile this L-system and return the result, not caching it into this object
        /// </summary>
        /// <param name="globalCompileTimeOverrides">overrides to the compile time directives. Will only be applied if the Key matches an already defined compile time parameter</param>
        public LSystem CompileWithParameters(Dictionary<string, string> globalCompileTimeOverrides)
        {
            return CompileSystem(globalCompileTimeOverrides);
        }

        private LSystem CompileSystem(Dictionary<string, string> globalCompileTimeOverrides)
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
                return LSystemBuilder.FloatSystem(
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
            var ruleLines = fullCode.Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x));
            var outputRules = new StringBuilder();

            foreach (var inputLine in ruleLines)
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
            if (directiveText[0] == '#')
            {
                // comment line
                return;
            }
            var directiveMatch = Regex.Match(directiveText, @"(?<directive>[^ ]+)\s+(?<parameter>.+)");
            if (!directiveMatch.Success)
            {
                throw new SyntaxException($"missing directive after hash", -1, 1);
            }

            switch (directiveMatch.Groups["directive"].Value)
            {
                case "axiom":
                    axiom = directiveMatch.Groups["parameter"].Value;
                    return;
                case "iterations":
                    if (!int.TryParse(directiveMatch.Groups["parameter"].Value, out int iterations))
                    {
                        throw new SyntaxException($"iterations must be an integer", directiveMatch.Groups["parameter"]);
                    }
                    this.iterations = iterations;
                    return;
                case "ignore":
                    ignoredCharacters = directiveMatch.Groups["parameter"].Value;
                    return;
                case "runtime":
                    var nameValueMatch = Regex.Match(directiveMatch.Groups["parameter"].Value, @"(?<variable>[^ ]+)\s+(?<value>[^ ]+)");
                    if (!nameValueMatch.Success)
                    {
                        throw new SyntaxException($"runtime directive requires 2 parameters", directiveMatch.Groups["parameter"]);
                    }
                    if (!float.TryParse(nameValueMatch.Groups["value"].Value, out var runtimeDefault))
                    {
                        throw new SyntaxException($"runtime parameter must default to a number", nameValueMatch.Groups["value"]);
                    }
                    defaultGlobalRuntimeParameters.Add(new ParameterAndDefault
                    {
                        name = nameValueMatch.Groups["variable"].Value,
                        defaultValue = runtimeDefault
                    });
                    return;
                case "define":
                    var nameReplacementMatch = Regex.Match(directiveMatch.Groups["parameter"].Value, @"(?<variable>[^ ]+)\s+(?<replacement>.+)");
                    if (!nameReplacementMatch.Success)
                    {
                        throw new SyntaxException($"define directive requires 2 parameters", directiveMatch.Groups["parameter"]);
                    }
                    defaultGlobalCompileTimeParameters.Add(new DefineDirectives
                    {
                        name = nameReplacementMatch.Groups["variable"].Value,
                        replacement = nameReplacementMatch.Groups["replacement"].Value
                    });
                    return;
                default:
                    if (directiveMatch.Groups["directive"].Value.StartsWith("#"))
                    {
                        return;
                    }
                    throw new SyntaxException(
                        $"unrecognized directive name \"{directiveMatch.Groups["directive"].Value}\"",
                        directiveMatch.Groups["directive"]);
            }
        }
    }
}
