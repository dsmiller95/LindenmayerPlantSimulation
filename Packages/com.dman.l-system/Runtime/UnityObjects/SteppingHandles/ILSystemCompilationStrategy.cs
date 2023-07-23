using System;
using System.Collections.Generic;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;

namespace Dman.LSystem.UnityObjects.SteppingHandles
{
    public interface ILSystemCompilationStrategy : IDisposable
    {
        public void SetGlobalCompileTimeParameters(Dictionary<string, string> globalCompileTimeOverrides);
        
        LSystemStepper GetCompiledLSystem();
        
        public bool IsCompiledSystemValid();
        
        public event Action OnCompiledSystemChanged;

        public ISavedCompilationStrategy GetSerializableType();
    }

    public interface ISavedCompilationStrategy
    {
        public ILSystemCompilationStrategy Rehydrate();
    }
}