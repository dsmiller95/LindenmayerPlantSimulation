using System;
using System.Collections.Generic;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.ObjectSets;
using JetBrains.Annotations;

namespace Dman.LSystem.UnityObjects.SteppingHandles
{
    public class IndividuallyCompiledStrategy : ILSystemCompilationStrategy
    {
        private readonly LSystemObject systemObject;
    
        public event Action OnCompiledSystemChanged;
        public string StrategyTypeName => "SharedCompilationStrategy";

        private LSystemStepper compiledSystem = null;
        // TODO: only used for serialization
        private Dictionary<string, string> compiledGlobalCompiletimeReplacements;
        
        public IndividuallyCompiledStrategy(LSystemObject systemObject)
        {
            this.systemObject = systemObject;
            
            this.systemObject.OnCachedSystemUpdated += OnSharedSystemRecompiled;
            if(this.systemObject.compiledSystem != null)
                OnSharedSystemRecompiled();
        }

        public void SetGlobalCompileTimeParameters(Dictionary<string, string> globalCompileTimeOverrides)
        {
            var newSystem = systemObject.CompileWithParameters(globalCompileTimeOverrides);
            compiledSystem?.Dispose();
            compiledSystem = newSystem;
            OnCompiledSystemChanged?.Invoke();
        }
        
        public bool IsCompiledSystemValid()
        {
            return compiledSystem is { isDisposed: false }; 
        }

        private void OnSharedSystemRecompiled()
        {
            compiledSystem = systemObject.compiledSystem;
            OnCompiledSystemChanged?.Invoke();
        }
        
        [CanBeNull]
        public LSystemStepper GetCompiledLSystem()
        {
            return compiledSystem;
        }
        

        public void Dispose()
        {
            systemObject.OnCachedSystemUpdated -= OnSharedSystemRecompiled;
            
            var newSystem = systemObject.CompileWithParameters(compiledGlobalCompiletimeReplacements);
            compiledSystem?.Dispose();
            compiledSystem = newSystem;
        }
        
        
        #region Serialization
        
        public ISavedCompilationStrategy GetSerializableType()
        {
            return new SaveData(this);
        }
        
        public class SaveData : ISavedCompilationStrategy
        {
            private readonly int systemObjectId;
            private Dictionary<string, string> compiledGlobalCompiletimeReplacements;

            public SaveData(IndividuallyCompiledStrategy source)
            {
                this.systemObjectId = source.systemObject.myId;
                this.compiledGlobalCompiletimeReplacements = source.compiledGlobalCompiletimeReplacements;
            }
            
            public ILSystemCompilationStrategy Rehydrate()
            {
                var lSystemObjectRegistry = RegistryRegistry.GetObjectRegistry<LSystemObject>();
                var systemObject = lSystemObjectRegistry.GetUniqueObjectFromID(systemObjectId);
                var hydrated = new IndividuallyCompiledStrategy(systemObject);
                hydrated.SetGlobalCompileTimeParameters(compiledGlobalCompiletimeReplacements);

                return hydrated;
            }
        }

        #endregion
    }
}