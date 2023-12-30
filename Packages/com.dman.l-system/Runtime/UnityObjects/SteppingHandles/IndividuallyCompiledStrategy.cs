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

        private LSystemStepper compiledSystem = null;
        // TODO: only used for serialization
        private Dictionary<string, string> compiledGlobalCompiletimeReplacements;
        
        public IndividuallyCompiledStrategy(LSystemObject systemObject)
        {
            this.systemObject = systemObject;
        }

        public void SetGlobalCompileTimeParameters(Dictionary<string, string> globalCompileTimeOverrides)
        {
            var newSystem = systemObject.CompileWithParameters(globalCompileTimeOverrides);
            compiledSystem?.Dispose();
            compiledSystem                             = newSystem;
            compiledGlobalCompiletimeReplacements = globalCompileTimeOverrides;
            OnCompiledSystemChanged?.Invoke();
        }
        
        public bool IsCompiledSystemValid()
        {
            return compiledSystem is { isDisposed: false }; 
        }
        
        [CanBeNull]
        public LSystemStepper GetCompiledLSystem()
        {
            return compiledSystem;
        }
        
        public void Dispose()
        {
            compiledSystem?.Dispose();
            compiledSystem = null;
        }
        
        
        #region Serialization
        
        public ISavedCompilationStrategy GetSerializableType()
        {
            return new SaveData(this);
        }
        
        [Serializable]
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