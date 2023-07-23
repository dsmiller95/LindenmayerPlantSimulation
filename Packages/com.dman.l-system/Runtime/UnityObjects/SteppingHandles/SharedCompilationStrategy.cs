using System;
using System.Collections.Generic;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.ObjectSets;
using JetBrains.Annotations;

namespace Dman.LSystem.UnityObjects.SteppingHandles
{
    public class SharedCompilationStrategy : ILSystemCompilationStrategy
    {
        private readonly LSystemObject systemObject;
    
        public event Action OnCompiledSystemChanged;

        // todo: replace this with a computed property referencing the LSystemObject?
        private LSystemStepper compiledSystem = null;
        
        public SharedCompilationStrategy(LSystemObject systemObject)
        {
            this.systemObject = systemObject;
            
            this.systemObject.OnCachedSystemUpdated += OnSharedSystemRecompiled;
            if(this.systemObject.compiledSystem != null)
                OnSharedSystemRecompiled();
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
        
        public void SetGlobalCompileTimeParameters(Dictionary<string, string> globalCompileTimeOverrides)
        {
            throw new NotImplementedException();
        }

        [CanBeNull]
        public LSystemStepper GetCompiledLSystem()
        {
            return compiledSystem;
        }
        

        public void Dispose()
        {
            systemObject.OnCachedSystemUpdated -= OnSharedSystemRecompiled;
        }

        public void SetCompileTimeParameters(Dictionary<string, string> compileTimeParameters)
        {
            throw new NotImplementedException();
        }
        
        #region Serialization
        
        public ISavedCompilationStrategy GetSerializableType()
        {
            return new SaveData(this);
        }
        
        public class SaveData : ISavedCompilationStrategy
        {
            private readonly int systemObjectId;

            public SaveData(SharedCompilationStrategy source)
            {
                this.systemObjectId = source.systemObject.myId;
            }
            
            public ILSystemCompilationStrategy Rehydrate()
            {
                var lSystemObjectRegistry = RegistryRegistry.GetObjectRegistry<LSystemObject>();
                var systemObject = lSystemObjectRegistry.GetUniqueObjectFromID(systemObjectId);
                return new SharedCompilationStrategy(systemObject);
            }
        }

        #endregion
    }
}