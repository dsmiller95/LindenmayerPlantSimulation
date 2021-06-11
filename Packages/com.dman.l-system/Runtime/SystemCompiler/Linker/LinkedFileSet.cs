using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.Utilities.SerializableUnityObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemCompiler.Linker
{
    [Serializable]
    public class LinkedFileSet
    {
        public string originFile;

        public SerializableDictionary<string, int> fileIndexesByFullIdentifier = new SerializableDictionary<string, int>();
        public List<ParsedFile> allFiles;
        public List<SymbolDefinition> allSymbolDefinitions;

        public List<DefineDirective> allGlobalCompileTimeParams;
        public List<RuntimeParameterAndDefault> allGlobalRuntimeParams;

        public LinkedFileSet(
            string originFile,
            Dictionary<string, ParsedFile> allFilesByFullIdentifier,
            List<SymbolDefinition> allSymbolDefinitions)
        {
            this.fileIndexesByFullIdentifier = new SerializableDictionary<string, int>();
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
        public int GetIterations()
        {
            var originFileData = allFiles[fileIndexesByFullIdentifier[originFile]];
            return originFileData.iterations;
        }

        public int GetSymbolFromRoot(char characterInFile)
        {
            return this.GetSymbol(originFile, characterInFile);
        }
        public int GetSymbol(string fileName, char characterInFile)
        {
            var fileData = allFiles[fileIndexesByFullIdentifier[fileName]];
            return fileData.GetSymbolInFile(characterInFile);
        }

        public LSystemStepper CompileSystem(Dictionary<string, string> globalCompileTimeOverrides = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("L System compilation");

            var openSymbol = this.GetSymbol(originFile, '[');
            var closeSymbol = this.GetSymbol(originFile, ']');

            var compiledRules = CompileAllRules(
                globalCompileTimeOverrides,
                out var nativeRuleData,
                openSymbol, closeSymbol);


            var everySymbol = new HashSet<int>(allFiles.SelectMany(x => x.allSymbolAssignments).Select(x => x.remappedSymbol));

            var ignoredByFile = allFiles
                .Select(x => everySymbol.Except(x.GetAllIncludedContextualSymbols()))
                .Select(x => new HashSet<int>(x))
                .ToArray();


            var result = new LSystemStepper(
                compiledRules,
                nativeRuleData,
                expectedGlobalParameters: allGlobalRuntimeParams.Count,
                ignoredCharactersByRuleGroupIndex: ignoredByFile,
                branchOpenSymbol: openSymbol,
                branchCloseSymbol: closeSymbol
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
    }
}


namespace Dman.Utilities.SerializableUnityObjects
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        public Dictionary<TKey, TValue> backingDictionary;

        public SerializableDictionary()
        {
            backingDictionary = new Dictionary<TKey, TValue>();
        }

        [SerializeField]
        private List<InternalKeyPair> keyValuePairs;

        [Serializable]
        class InternalKeyPair
        {
            public TKey key;
            public TValue value;
        }

        public void OnAfterDeserialize()
        {
            backingDictionary = keyValuePairs.ToDictionary(x => x.key, x => x.value);
        }

        public void OnBeforeSerialize()
        {
            keyValuePairs = backingDictionary.Select(x => new InternalKeyPair
            {
                key = x.Key,
                value = x.Value
            }).ToList();
        }


        #region interface reimplementation
        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)backingDictionary)[key]; set => ((IDictionary<TKey, TValue>)backingDictionary)[key] = value; }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)backingDictionary).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)backingDictionary).Values;

        public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)backingDictionary).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)backingDictionary).IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)backingDictionary).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)backingDictionary).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)backingDictionary).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)backingDictionary).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)backingDictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)backingDictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)backingDictionary).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)backingDictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)backingDictionary).Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)backingDictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)backingDictionary).GetEnumerator();
        }
        #endregion
    }
}

