using Dman.LSystem.Packages.com.dman.l_system.Runtime.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.Utilities.SerializableUnityObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [Serializable]
    public class LinkedFileSet
    {
        public string originFile;

        public SerializableDictionary<string, int> fileIndexesByFullIdentifier = new SerializableDictionary<string, int>();
        public SerializableList<LinkedFile> allFiles;
        public List<SymbolDefinition> allSymbolDefinitionsLeafFirst;
        public SerializableDictionary<int, int> defaultSymbolDefinitionIndexBySymbol = new SerializableDictionary<int, int>();

        public List<DefineDirective> allGlobalCompileTimeParams;
        public List<RuntimeParameterAndDefault> allGlobalRuntimeParams;

        public LinkedFileSet(
            string originFile,
            Dictionary<string, LinkedFile> allFilesByFullIdentifier,
            List<SymbolDefinition> allSymbolDefinitionsLeafFirst)
        {
            fileIndexesByFullIdentifier = new SerializableDictionary<string, int>();
            this.allSymbolDefinitionsLeafFirst = allSymbolDefinitionsLeafFirst;
            this.originFile = originFile;

            var originFileData = allFilesByFullIdentifier[originFile];
            if (originFileData.isLibrary)
            {
                throw new LinkException(LinkExceptionType.BASE_FILE_IS_LIBRARY, $"Origin file '{originFile}' is a library. origin file must be a .lsystem file");
            }

            allFiles = new SerializableList<LinkedFile>();
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
                allGlobalCompileTimeParams = compileTimes.Values.ToList();

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
                allGlobalRuntimeParams = runTimes.Values.ToList();
            }


            defaultSymbolDefinitionIndexBySymbol = new SerializableDictionary<int, int>();
            for (var i = 0; i < allSymbolDefinitionsLeafFirst.Count; i++)
            {
                var definition = allSymbolDefinitionsLeafFirst[i];
                if (defaultSymbolDefinitionIndexBySymbol.ContainsKey(definition.actualSymbol))
                {
                    continue;
                }
                defaultSymbolDefinitionIndexBySymbol[definition.actualSymbol] = i;
            }


            if (!fileIndexesByFullIdentifier.ContainsKey(originFile))
            {
                throw new LinkException(LinkExceptionType.BAD_ORIGIN_FILE, $"could not find origin file '{originFile}'");
            }
        }

        public SymbolString<float> GetAxiom(Allocator allocator = Allocator.Persistent)
        {

            if (!fileIndexesByFullIdentifier.ContainsKey(originFile))
            {
                throw new LinkException(LinkExceptionType.BAD_ORIGIN_FILE, $"could not find origin file '{originFile}'");
            }
            var originFileData = allFiles.backing[fileIndexesByFullIdentifier[originFile]];

            return SymbolString<float>.FromString(originFileData.axiom, allocator, chr => originFileData.GetSymbolInFile(chr));
        }

        public SymbolDefinition GetLeafMostSymbolDefinition(int symbol)
        {
            return allSymbolDefinitionsLeafFirst[defaultSymbolDefinitionIndexBySymbol[symbol]];
        }

        public int GetIterations()
        {
            if (!fileIndexesByFullIdentifier.ContainsKey(originFile))
            {
                throw new LinkException(LinkExceptionType.BAD_ORIGIN_FILE, $"could not find origin file '{originFile}'");
            }
            var originFileData = allFiles.backing[fileIndexesByFullIdentifier[originFile]];
            return originFileData.iterations;
        }

        public int GetSymbolFromRoot(char characterInFile)
        {
            return GetSymbol(originFile, characterInFile);
        }
        public int GetSymbol(string fileName, char characterInFile)
        {
            if (!fileIndexesByFullIdentifier.ContainsKey(fileName))
            {
                throw new LSystemRuntimeException("could not find file: " + fileName);
            }
            var fileData = allFiles.backing[fileIndexesByFullIdentifier[fileName]];
            return fileData.GetSymbolInFile(characterInFile);
        }

        public LSystemStepper CompileSystem(Dictionary<string, string> globalCompileTimeOverrides = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("L System compilation");

            var openSymbol = GetSymbol(originFile, '[');
            var closeSymbol = GetSymbol(originFile, ']');

            var compiledRules = CompileAllRules(
                globalCompileTimeOverrides,
                out var nativeRuleData,
                openSymbol, closeSymbol);


            var everySymbol = new HashSet<int>(allFiles.backing.SelectMany(x => x.allSymbolAssignments).Select(x => x.remappedSymbol));

            var includedByFile = allFiles.backing
                .Select(x => x.GetAllIncludedContextualSymbols())
                .Select(x => new HashSet<int>(x))
                .ToArray();

            var customSymbols = new CustomRuleSymbols();
            foreach (var file in allFiles.backing)
            {
                file.SetCustomRuleSymbols(ref customSymbols);
            }

            var result = new LSystemStepper(
                compiledRules,
                nativeRuleData,
                expectedGlobalParameters: allGlobalRuntimeParams.Count,
                includedCharactersByRuleIndex: includedByFile,
                branchOpenSymbol: openSymbol,
                branchCloseSymbol: closeSymbol,
                customSymbols: customSymbols
            );
            UnityEngine.Profiling.Profiler.EndSample();
            return result;
        }

        public IEnumerable<BasicRule> CompileAllRules(
            Dictionary<string, string> compileTimeOverrides,
            out SystemLevelRuleNativeData ruleNativeData,
            int openSymbol, int closeSymbol
            )
        {
            var allValidRuntimeParameters = allGlobalRuntimeParams.Select(x => x.name).ToArray();
            var allReplacementDirectives = GetCompileTimeReplacementsWithOverrides(compileTimeOverrides);
            var parsedRules = allFiles.backing
                .SelectMany((file, index) =>
                {
                    Func<char, int> remappingFunction = character => file.GetSymbolInFile(character);
                    return file.GetRulesWithReplacements(allReplacementDirectives)
                        .Select(x => RuleParser.ParseToRule(x, remappingFunction, (short)index, allValidRuntimeParameters));
                })
                .Where(x => x != null)
                .ToArray();
            return RuleParser.CompileAndCheckParsedRules(parsedRules, out ruleNativeData, openSymbol, closeSymbol);
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

        public void OnBeforeSerialize()
        {
            throw new NotImplementedException();
        }

        public void OnAfterDeserialize()
        {
            throw new NotImplementedException();
        }
    }
}


