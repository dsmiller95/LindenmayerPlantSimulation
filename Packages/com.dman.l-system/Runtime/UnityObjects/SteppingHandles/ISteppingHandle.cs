using System;
using System.Collections.Generic;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using JetBrains.Annotations;

namespace Dman.LSystem.UnityObjects.SteppingHandles
{
    public interface IRecompileableSteppingHandle : ISteppingHandle
    {
        public void RecompileLSystem(Dictionary<string, string> globalCompileTimeOverrides);
    }
    
    public interface ISteppingHandle : 
        IHaveRuntimeParameters,
        IDisposable
    {
        public bool IsUpdatePending();
        public void ForceCompleteImmediate();
        public bool HasValidSystem();

        public LSystemState<float> GetCurrentState();
        public void ResetState(uint? seed = null);
        public void InitiateStep(Concurrency concurrency, StateSelection stateSelection);
        
        public int GetStepCount();
        public bool DidLastUpdateCauseChange();
        
        public event Action OnSystemStateUpdated;
        
        public ISerializeableSteppingHandle SerializeToSerializableObject();

        [CanBeNull]
        public LSystemStepper TryGetUnderlyingStepper();
    }
    public enum Concurrency
    {
        Asyncronous,
        Synchronous
    }

    public enum StateSelection
    {
        RepeatLast,
        Next,
    }

    public interface IHaveRuntimeParameters
    {
        public void SetRuntimeParameter(string parameterName, float parameterValue);
    }

    public interface ISerializeableSteppingHandle
    {
        
    }
}