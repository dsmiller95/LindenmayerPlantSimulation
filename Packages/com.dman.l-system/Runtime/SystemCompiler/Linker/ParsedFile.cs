using System;
using System.Collections.Generic;
using System.IO;
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
        public string fileSource;
        public Guid uuid;

        public string axiom = null;
        public int iterations = -1;
        public List<string> ruleLines;

        /// <summary>
        /// These are used to build a list of truly global parameters, when the files are grouped into a <see cref="LinkedFileSet"/>
        /// </summary>
        public List<DefineDirective> delaredInFileCompileTimeParameters;
        public List<RuntimeParameterAndDefault> declaredInFileRuntimeParameters;

        public string allSymbols = "[]";
        public string ignoredCharacters = "";
        public string globalCharacters = "[]";

        public List<IncludeLink> links;
        public List<ExportDirective> exports;

        public List<SymbolRemap> allSymbolAssignments = new List<SymbolRemap>();

        public ParsedFile(string fileSource, string fullFile, bool isLibrary = false)
        {
            this.fileSource = fileSource;
            this.isLibrary = isLibrary;
            this.uuid = Guid.NewGuid();

            delaredInFileCompileTimeParameters = new List<DefineDirective>();
            declaredInFileRuntimeParameters = new List<RuntimeParameterAndDefault>();
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

            if(allSymbols == null)
            {
                throw new SyntaxException($"{fileSource} must define #symbols directive(s)");
            }

            ruleLines = ruleLines.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

            foreach (var definedSymbol in links
                .SelectMany(x => x.importedSymbols.Select(x => x.remappedSymbol))
                .Concat(exports.Select(x => x.exportedSymbol))
                .Concat(globalCharacters ?? ""))
            {
                if (!allSymbols.Contains(definedSymbol))
                {
                    throw new SyntaxException($"{fileSource} does not define all symbols used, missing {definedSymbol}");
                }
            }
        }

        public IEnumerable<string> GetRulesWithReplacements(Dictionary<string, string> replacementDirectives)
        {
            IEnumerable<string> rulesPostReplacement = ruleLines;
            foreach (var replacement in replacementDirectives)
            {
                rulesPostReplacement = rulesPostReplacement.Select(x => x.Replace(replacement.Key, replacement.Value));
            }
            return rulesPostReplacement;
        }

        /// <summary>
        /// returns every symbol which can be search contextually by rules in this file
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> GetAllIncludedContextualSymbols()
        {
            return allSymbolAssignments
                .Where(x => !ignoredCharacters.Contains(x.sourceCharacter))
                .Select(x => x.remappedSymbol);
        }

        public int GetExportedSymbol(string exportedName)
        {
            var sourceSymbol = exports.Find(x => x.name == exportedName);
            if (sourceSymbol == null)
            {
                throw new LinkException(LinkExceptionType.MISSING_EXPORT, $"trying to import \"{exportedName}\" from {fileSource}, but it is not exported");
            }
            var remappedSymbolInSource = allSymbolAssignments.Find(x => x.sourceCharacter == sourceSymbol.exportedSymbol);
            if(remappedSymbolInSource == null)
            {
                throw new Exception($"poorly ordered linking. tried to get ${exportedName} from ${fileSource}, but ${fileSource} has not been fully linked yet");
            }
            return remappedSymbolInSource.remappedSymbol;
        }

        public int GetSymbolInFile(char symbol)
        {
            var match = allSymbolAssignments.Find(x => x.sourceCharacter == symbol);
            if(match == null)
            {
                throw new Exception($"{fileSource} does not contain requested symbol '{symbol}'. Did you forget to declare it in a <color=blue>#symbols</color> directive?");
            }
            return match.remappedSymbol;
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
                    declaredInFileRuntimeParameters.Add(new RuntimeParameterAndDefault
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
                    delaredInFileCompileTimeParameters.Add(new DefineDirective
                    {
                        name = nameReplacementMatch.Groups["variable"].Value,
                        replacement = nameReplacementMatch.Groups["replacement"].Value
                    });
                    return;
                case "ignore":
                    ignoredCharacters = directiveMatch.Groups["parameter"].Value;
                    return;
                case "symbols":
                    if (allSymbols == null) allSymbols = "";
                    foreach (var symbol in directiveMatch.Groups["parameter"].Value)
                    {
                        if (!allSymbols.Contains(symbol))
                        {
                            allSymbols += symbol;
                        }
                    }
                    return;
                case "global":
                    foreach (var symbol in directiveMatch.Groups["parameter"].Value)
                    {
                        if (!globalCharacters.Contains(symbol))
                        {
                            globalCharacters += symbol;
                        }
                    }
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
                    var includeDirective = Regex.Match(directiveMatch.Groups["parameter"].Value, @"(?<filepath>[^ ]+)(?:\s+(?<remapping>.*))?");
                    if (!includeDirective.Success)
                    {
                        throw new SyntaxException($"include directive requires a filepath", directiveMatch.Groups["parameter"]);
                    }
                    var relativeImportPath = includeDirective.Groups["filepath"].Value;
                    var absoluteIdentifier = Path.Combine(Path.GetDirectoryName(this.fileSource), relativeImportPath);
                    var link = new IncludeLink
                    {
                        fullImportIdentifier = absoluteIdentifier,
                        importedSymbols = new List<IncludeImportRemap>()
                    };
                    if (includeDirective.Groups["remapping"].Success)
                    {
                        var remaps = Regex.Matches(includeDirective.Groups["remapping"].Value, @"\((?<name>\w+)->(?<symbol>\w)\)");
                        foreach (Match match in remaps)
                        {
                            link.importedSymbols.Add(new IncludeImportRemap
                            {
                                importName = match.Groups["name"].Value,
                                remappedSymbol = match.Groups["symbol"].Value[0]
                            });
                        }
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
