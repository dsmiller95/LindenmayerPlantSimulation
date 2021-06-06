using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Dman.LSystem.SystemCompiler.Linker
{
    /// <summary>
    /// Represents one compiled file, included as part of the linking process
    /// </summary>
    [System.Serializable]
    public class ParsedFile
    {
        public bool isLibrary;

        public string axiom = null;
        public int iterations = -1;
        public List<string> ruleLines;

        public List<DefineDirective> globalCompileTimeParameters;
        public List<RuntimeParameterAndDefault> globalRuntimeParameters;

        public string ignoredCharacters = null;
        public string globalCharacters = null;

        public List<IncludeLink> links;
        public List<ExportDirective> exports;

        public ParsedFile(string fullFile, bool isLibrary = false)
        {
            this.isLibrary = isLibrary;

            globalCompileTimeParameters = new List<DefineDirective>();
            globalRuntimeParameters = new List<RuntimeParameterAndDefault>();
            ruleLines = new List<string>();

            links = new List<IncludeLink>();
            exports = new List<ExportDirective>();

            var allLines = fullFile.Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            foreach (var inputLine in allLines)
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
                    ruleLines.Add(inputLine);
                }
            }

            ruleLines = ruleLines.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
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
                    if (this.isLibrary)
                    {
                        throw new SyntaxException($"axiom cannot be defined in a library file", directiveMatch.Groups["directive"]);
                    }
                    axiom = directiveMatch.Groups["parameter"].Value;
                    return;
                case "iterations":
                    if (this.isLibrary)
                    {
                        throw new SyntaxException($"iterations cannot be defined in a library file", directiveMatch.Groups["directive"]);
                    }
                    if (!int.TryParse(directiveMatch.Groups["parameter"].Value, out int iterations))
                    {
                        throw new SyntaxException($"iterations must be an integer", directiveMatch.Groups["parameter"]);
                    }
                    this.iterations = iterations;
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
                    globalRuntimeParameters.Add(new RuntimeParameterAndDefault
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
                    globalCompileTimeParameters.Add(new DefineDirective
                    {
                        name = nameReplacementMatch.Groups["variable"].Value,
                        replacement = nameReplacementMatch.Groups["replacement"].Value
                    });
                    return;
                case "ignore":
                    ignoredCharacters = directiveMatch.Groups["parameter"].Value;
                    return;
                case "global":
                    globalCharacters = directiveMatch.Groups["parameter"].Value;
                    return;
                case "export":
                    if (!this.isLibrary)
                    {
                        throw new SyntaxException($"export can only be defined in a library file", directiveMatch.Groups["directive"]);
                    }
                    var exportDefinition = Regex.Match(directiveMatch.Groups["parameter"].Value, @"(?<named>[^ ]+)\s+(?<symbol>[^ ])");
                    if (!exportDefinition.Success)
                    {
                        throw new SyntaxException($"export directive requires 2 parameters", directiveMatch.Groups["parameter"]);
                    }
                    exports.Add(new ExportDirective
                    {
                        exportedSymbol = exportDefinition.Groups["symbol"].Value[0],
                        name = exportDefinition.Groups["named"].Value
                    });
                    return;
                case "include":
                    var includeDirective = Regex.Match(directiveMatch.Groups["parameter"].Value, @"(?<filepath>[^ ]+)\s+(?<remapping>.*)");
                    if (!includeDirective.Success)
                    {
                        throw new SyntaxException($"include directive requires a filepath", directiveMatch.Groups["parameter"]);
                    }
                    var link = new IncludeLink
                    {
                        relativeImportPath = includeDirective.Groups["filepath"].Value,
                        importedSymbols = new List<SymbolRemap>()
                    };
                    var remaps = Regex.Matches(includeDirective.Groups["remapping"].Value, @"\((?<name>\w+)->(?<symbol>\w)\)");
                    foreach (Match match in remaps)
                    {
                        link.importedSymbols.Add(new SymbolRemap
                        {
                            importName = match.Groups["name"].Value,
                            remappedSymbol = match.Groups["symbol"].Value[0]
                        });
                    }
                    links.Add(link);
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
