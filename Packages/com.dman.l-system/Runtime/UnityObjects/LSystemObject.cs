using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public class LSystemObject : ScriptableObject
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
            UnityEngine.Profiling.Profiler.BeginSample("L System compilation");
            try
            {
                return linkedFiles.CompileSystem(globalCompileTimeOverrides);
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
            LoadFromFilePath(assetPath);
#endif
        }

        public void LoadFromFilePath(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var linker = new FileLinker(new FileSystemFileProvider());
                linkedFiles = linker.LinkFiles(filePath);
            }
        }
    }
}
