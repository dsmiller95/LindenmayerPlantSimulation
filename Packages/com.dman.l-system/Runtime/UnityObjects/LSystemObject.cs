using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.ObjectSets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public class LSystemObject : IDableObject
    {
        public int seed;

        public LinkedFileSet linkedFiles;

        public LSystemStepper compiledSystem { get; private set; }
        public SymbolString<float> axiom => linkedFiles.GetAxiom();
        public int iterations => linkedFiles.GetIterations();

        /// <summary>
        /// Emits whenever the system is compiled
        /// </summary>
        public event Action OnCachedSystemUpdated;

        public ArrayParameterRepresenation<float> GetRuntimeParameters()
        {
            return ArrayParameterRepresenation<float>.GenerateFromList(linkedFiles.allGlobalRuntimeParams, p => p.name, p => p.defaultValue);
        }

        /// <summary>
        /// Compile this L-system into the <see cref="compiledSystem"/> property
        /// </summary>
        /// <param name="globalCompileTimeOverrides">overrides to the compile time directives. Will only be applied if the Key matches an already defined compile time parameter</param>
        public void CompileToCached(Dictionary<string, string> globalCompileTimeOverrides = null, bool silent = false)
        {
            var newSystem = CompileSystem(globalCompileTimeOverrides);
            if (newSystem != null)
            {
                compiledSystem?.Dispose();
                compiledSystem = newSystem;

                if (!silent)
                {
                    OnCachedSystemUpdated?.Invoke();
                }
            }
        }


        public string ConvertToReadableString(SymbolString<float> systemState)
        {
            var resultString = new StringBuilder();
            for (int i = 0; i < systemState.Length; i++)
            {
                var symbol = linkedFiles.GetLeafMostSymbolDefinition(systemState[i]);
                resultString.Append(symbol.characterInSourceFile);
                var paramDetails = systemState.parameters[i];
                if(paramDetails.length > 0)
                {
                    resultString.Append("(");
                }
                for (int p = 0; p < paramDetails.length; p++)
                {
                    var param = systemState.parameters[paramDetails, p];
                    if(p == paramDetails.length - 1)
                    {
                        resultString.Append($"{param:F1})");
                    }
                    else
                    {
                        resultString.Append($"{param:F1}, ");
                    }
                }
            }

            return resultString.ToString();
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
        public LSystemStepper CompileWithParameters(Dictionary<string, string> globalCompileTimeOverrides)
        {
            return CompileSystem(globalCompileTimeOverrides);
        }

        private LSystemStepper CompileSystem(Dictionary<string, string> globalCompileTimeOverrides)
        {
            try
            {
                return linkedFiles.CompileSystem(globalCompileTimeOverrides);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
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
            LoadFromFilePath(assetPath);
#endif
        }

        public void LoadFromFilePath(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var linker = new FileLinker(new FileSystemFileProvider());
                this.SetLinkedFiles(linker.LinkFiles(filePath));
            }
        }

        public static LSystemObject GetNewLSystemFromFiles(LinkedFileSet linkedFiles)
        {
            var systemObject = ScriptableObject.CreateInstance<LSystemObject>();
            systemObject.SetLinkedFiles(linkedFiles);

            return systemObject;
        }

        private void SetLinkedFiles(LinkedFileSet linkedFiles)
        {
            this.linkedFiles = linkedFiles;

            // compile the system right away to the cached system
            this.CompileToCached();
        }
    }
}
