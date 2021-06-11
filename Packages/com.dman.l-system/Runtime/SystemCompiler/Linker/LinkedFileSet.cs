using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [Serializable]
    public class LinkedFileSet
    {
        private string originFile;

        public Dictionary<string, int> fileIndexesByFullIdentifier = new Dictionary<string, int>();
        public List<ParsedFile> allFiles;
        public List<SymbolDefinition> allSymbolDefinitions;

        public List<DefineDirective> allGlobalCompileTimeParams;
        public List<RuntimeParameterAndDefault> allGlobalRuntimeParams;

        public LinkedFileSet(
            string originFile,
            Dictionary<string, ParsedFile> allFilesByFullIdentifier,
            List<SymbolDefinition> allSymbolDefinitions)
        {
            this.allSymbolDefinitions = allSymbolDefinitions;
            this.originFile = originFile;

            var originFileData = allFilesByFullIdentifier[originFile];
            if (originFileData.isLibrary)
            {
                throw new LinkException(LinkExceptionType.BASE_FILE_IS_LIBRARY, $"Origin file '{originFile}' is a library. origin file must be a .lsystem file");
            }

            allFiles = new List<ParsedFile>();
            var compileTimes = new Dictionary<string, DefineDirective>();
            var runTimes = new Dictionary<string, RuntimeParameterAndDefault>();
            foreach (var kvp in allFilesByFullIdentifier)
            {
                allFiles.Add(kvp.Value);
                fileIndexesByFullIdentifier[kvp.Key] = allFiles.Count - 1;

                foreach (var compileTime in kvp.Value.delaredInFileCompileTimeParameters)
                {
                    if (compileTimes.ContainsKey(compileTime.name))
                    {
                        throw new LinkException(LinkExceptionType.GLOBAL_VARIABLE_COLLISION, $"Duplicated global compile time variable '{compileTime.name}' declared in {kvp.Value.fileSource}");
                    }
                    else
                    {
                        compileTimes[compileTime.name] = compileTime;
                    }
                }
                this.allGlobalCompileTimeParams = compileTimes.Values.ToList();

                foreach (var runTime in kvp.Value.declaredInFileRuntimeParameters)
                {
                    if (runTimes.ContainsKey(runTime.name))
                    {
                        throw new LinkException(LinkExceptionType.GLOBAL_VARIABLE_COLLISION, $"Duplicated global run time variable '{runTime.name}' declared in {kvp.Value.fileSource}");
                    }
                    else
                    {
                        runTimes[runTime.name] = runTime;
                    }
                }
                this.allGlobalRuntimeParams = runTimes.Values.ToList();
            }
        }

        public SymbolString<float> GetAxiom(Allocator allocator = Allocator.Persistent)
        {
            var originFileData = allFiles[fileIndexesByFullIdentifier[originFile]];

            return SymbolString<float>.FromString(originFileData.axiom, allocator, chr => originFileData.GetSymbolInFile(chr));
        }

        public int GetSymbol(string fileName, char characterInFile)
        {
            var fileData = allFiles[fileIndexesByFullIdentifier[fileName]];
            return fileData.GetSymbolInFile(characterInFile);
        }

        public LSystemStepper CompileSystem(Dictionary<string, string> globalCompileTimeOverrides = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("L System compilation");


            var compiledRules = CompileAllRules(
                globalCompileTimeOverrides,
                out var nativeRuleData);

            UnityEngine.Profiling.Profiler.EndSample();

            var everySymbol = new HashSet<int>(allFiles.SelectMany(x => x.allSymbolAssignments).Select(x => x.remappedSymbol));

            var ignoredByFile = allFiles
                .Select(x => everySymbol.Except(x.GetAllIncludedContextualSymbols()))
                .Select(x => new HashSet<int>(x))
                .ToArray();

            return new LSystemStepper(
                compiledRules,
                nativeRuleData,
                allGlobalRuntimeParams.Count,
                ignoredCharactersByRuleGroupIndex: ignoredByFile
            );
        }

        public IEnumerable<BasicRule> CompileAllRules(
            Dictionary<string, string> compileTimeOverrides,
            out SystemLevelRuleNativeData ruleNativeData
            )
        {
            var allValidRuntimeParameters = this.allGlobalRuntimeParams.Select(x => x.name).ToArray();
            var allReplacementDirectives = GetCompileTimeReplacementsWithOverrides(compileTimeOverrides);
            var parsedRules = this.allFiles
                .SelectMany((file, index) =>
                {
                    Func<char, int> remappingFunction = character => file.GetSymbolInFile(character);
                    return file.GetRulesWithReplacements(allReplacementDirectives)
                        .Select(x => RuleParser.ParseToRule(x, remappingFunction, (short)index, allValidRuntimeParameters));
                })
                .Where(x => x != null)
                .ToArray();
            return RuleParser.CompileAndCheckParsedRules(parsedRules, out ruleNativeData);
        }

        private Dictionary<string, string> GetCompileTimeReplacementsWithOverrides(Dictionary<string, string> overrides)
        {
            var resultReplacements = new Dictionary<string, string>();
            foreach (var replacement in allGlobalCompileTimeParams)
            {
                var replacementString = replacement.replacement;
                if (overrides != null
                    && overrides.TryGetValue(replacement.name, out var overrideValue))
                {
                    replacementString = overrideValue;
                }
                resultReplacements[replacement.name] = replacementString;
            }
            return resultReplacements;
        }
    }
}
