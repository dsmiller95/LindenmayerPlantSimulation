using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.ObjectSets;
using UnityEngine;
using UnityEngine.Assertions;

namespace Dman.LSystem.UnityObjects.SteppingHandles
{
    public class SteppingHandle : IRecompileableSteppingHandle
    {
        public ArrayParameterRepresenation<float> runtimeParameters { get; private set; }
        
        public event Action OnSystemStateUpdated;
        private CancellationTokenSource lSystemPendingCancellation;
        private CancellationTokenSource lSystemForceSyncCancellation;
        private bool isLSystemUpdatePending = false;
        private ILSystemCompilationStrategy compilationStrategy;
        private bool isDisposed = false;
        
        private int stepCount = 0;
        private bool? lastUpdateChanged = null;


        private LSystemState<float> currentState;
        private LSystemState<float> lastState;
        
        private LSystemGlobalResourceHandle globalResourceHandle;
        private LSystemObject mySystemObject;
        
        public SteppingHandle(
            LSystemObject mySystemObject,
            LSystemBehavior associatedBehavior,
            ILSystemCompilationStrategy compilationStrategy)
        {
            stepCount = 0;
            lastUpdateChanged = true;
            
            if (GlobalLSystemCoordinator.instance == null)
            {
                throw new Exception("No global l system coordinator singleton object. make a single GlobalLSystemCoordinator per scene");
            }
            globalResourceHandle = GlobalLSystemCoordinator.instance.AllocateResourceHandle(associatedBehavior);
            this.mySystemObject = mySystemObject;
            
            this.compilationStrategy = compilationStrategy;
            this.compilationStrategy.OnCompiledSystemChanged += OnSystemRecompiled;
            if (this.compilationStrategy.IsCompiledSystemValid())
            {
                OnSystemRecompiled();
            }
        }

        public SteppingHandle(
            SavedData savedData,
            LSystemBehavior associatedBehavior)
        {
            savedData.ApplyChangesTo(this);
            this.InitializePostDeserialize(associatedBehavior);
        }
        private SteppingHandle()
        {
            
        }
        
        private void OnSystemRecompiled()
        {
            this.ResetStateInternal();
            OnSystemStateUpdated?.Invoke();
        }

        private void ResetStateInternal(uint? seed = null)
        {
            seed ??= (uint)UnityEngine.Random.Range(1, int.MaxValue);

            if (lSystemPendingCancellation != null)
            {
                lSystemPendingCancellation.Cancel();
            }
            lastState?.currentSymbols.Dispose();
            lastState = null;
            currentState?.currentSymbols.Dispose();
            currentState = null;

            stepCount = 0;
            lastUpdateChanged = true;
            currentState = new DefaultLSystemState(mySystemObject.axiom, seed.Value);

            runtimeParameters = mySystemObject.GetRuntimeParameters();
        }
        public void ResetState(uint? seed = null)
        {
            if (isDisposed)
            {
                throw new Exception("trying to access disposed stepping handle");
            }
            ResetStateInternal(seed);
            OnSystemStateUpdated?.Invoke();
        }
        
        public void RecompileLSystem(Dictionary<string, string> globalCompileTimeOverrides)
        {
            // TODO: this will conditionally throw an exception if the compilation strategy is not recompileable. STINKY LISKOV VIOLATION
            //  maybe we can dynamically switch the compilation strategy?
            this.compilationStrategy.SetGlobalCompileTimeParameters(globalCompileTimeOverrides);
        }
        
        public void SetRuntimeParameter(string parameterName, float parameterValue)
        {
            runtimeParameters.SetParameter(parameterName, parameterValue);
        }
        
        public bool IsUpdatePending()
        {
            return isLSystemUpdatePending;
        }

        public void ForceCompleteImmediate()
        {
            if (IsUpdatePending())
            {
                lSystemForceSyncCancellation?.Cancel();
            }
            Assert.IsFalse(IsUpdatePending());
        }

        public bool HasValidSystem()
        {
            return compilationStrategy.IsCompiledSystemValid() && !isDisposed;
        }

        public int GetStepCount()
        {
            return stepCount;
        }

        public bool DidLastUpdateCauseChange()
        {
            Assert.IsTrue(lastUpdateChanged.HasValue);
            return lastUpdateChanged.Value;
        }

        public LSystemState<float> GetCurrentState()
        {
            return currentState;
        }
        
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            compilationStrategy.Dispose();

            lSystemPendingCancellation?.Cancel();
            lSystemPendingCancellation?.Dispose();
            
            lastState?.currentSymbols.Dispose();
            lastState = null;
            currentState?.currentSymbols.Dispose();
            currentState = null;

            globalResourceHandle.Dispose();
        }
        
        public void InitiateStep(Concurrency concurrency, StateSelection stateSelection)
        {
            StepSystemAsync(concurrency, stateSelection);
        }
        
        private void StepSystemAsync(Concurrency concurrency, StateSelection stateSelection)
        {
            if (lSystemPendingCancellation == null || lSystemPendingCancellation.IsCancellationRequested)
            {
                lSystemPendingCancellation = new CancellationTokenSource();
            }

            if (lSystemForceSyncCancellation == null ||
                (lSystemForceSyncCancellation.IsCancellationRequested && concurrency == Concurrency.Asyncronous))
            {
                lSystemForceSyncCancellation = new CancellationTokenSource();
            }

            if (concurrency == Concurrency.Synchronous)
            {
                lSystemForceSyncCancellation.Cancel();
            }
            
            var result = AsyncStepSystemFunction(
                stateSelection,
                lSystemForceSyncCancellation.Token,
                lSystemPendingCancellation.Token);
            result.Forget();
        }

        private async UniTask AsyncStepSystemFunction(
            StateSelection stateSelection,
            CancellationToken forceSynchronous,
            CancellationToken cancel)
        {
            if (isDisposed)
            {
                throw new Exception("trying to access disposed stepping handle");
            }

            isLSystemUpdatePending = true;
            UniTask<LSystemState<float>> pendingStateHandle;
            try
            {
                if (!compilationStrategy.IsCompiledSystemValid())
                {
                    Debug.LogError("No Compiled system available!");
                }
                if (stateSelection == StateSelection.RepeatLast)
                {
                    globalResourceHandle.UpdateUniqueIdReservationSpace(lastState);
                    pendingStateHandle = compilationStrategy.GetCompiledLSystem().StepSystemJob(
                        lastState,
                        forceSynchronous,
                        cancel,
                        runtimeParameters.GetCurrentParameters());
                }
                else
                {
                    globalResourceHandle.UpdateUniqueIdReservationSpace(currentState);
                    var sunlightJob = globalResourceHandle.ApplyPrestepEnvironment(
                        currentState,
                        compilationStrategy.GetCompiledLSystem().customSymbols);

                    pendingStateHandle = compilationStrategy.GetCompiledLSystem().StepSystemJob(
                        currentState,
                        forceSynchronous,
                        cancel,
                        runtimeParameters.GetCurrentParameters(),
                        parameterWriteDependency: sunlightJob);
                }
            }
            catch (Exception e)
            {
                lastUpdateChanged = false;
                Debug.LogException(e);
                isLSystemUpdatePending = false;
                return;
            }

            var nextState = await pendingStateHandle;

            UnityEngine.Profiling.Profiler.BeginSample("updating stepping handle state");
            if (stateSelection == StateSelection.RepeatLast)
            {
                // dispose the current state, since it is about to be replaced
                currentState?.currentSymbols.Dispose();
            }
            else
            {
                // dispose the last state
                lastState?.currentSymbols.Dispose();
                lastState = currentState;
                stepCount++;
            }
            currentState = nextState;
            // if there are immature markers, use those instead. avoiding an equality check saves time.
            var hasImmatureMarkers = mySystemObject.linkedFiles.immaturitySymbolMarkers.Length > 0;
            lastUpdateChanged = hasImmatureMarkers || !(currentState?.currentSymbols.Data.Equals(lastState.currentSymbols.Data) ?? false);

            isLSystemUpdatePending = false;
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("notifying system state listeners");
            OnSystemStateUpdated?.Invoke();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        
        public LSystemStepper TryGetUnderlyingStepper()
        {
            return compilationStrategy.GetCompiledLSystem();
        }
        public bool HasCompletedIterations()
        {
            return stepCount >= mySystemObject.iterations;
        }

        #region Serialization
        public ISerializeableSteppingHandle SerializeToSerializableObject()
        {
            return new SavedData(this);
        }

        

        [Serializable]
        public class SavedData : ISerializeableSteppingHandle
        {
            private int stepCount;
            private bool? lastUpdateChanged;

            private LSystemState<float> currentState;
            private LSystemState<float> lastState;
            private LSystemGlobalResourceHandle oldHandle;

            private int systemObjectId;

            private ArrayParameterRepresenation<float> runtimeParameters;
            
            public ISavedCompilationStrategy savedCompilationStrategy;

            public SavedData(SteppingHandle source)
            {
                this.stepCount = source.stepCount;
                this.lastUpdateChanged = source.lastUpdateChanged;

                this.currentState = source.currentState;
                this.lastState = source.lastState;
                this.oldHandle = source.globalResourceHandle;

                this.systemObjectId = source.mySystemObject.myId;

                this.runtimeParameters = source.runtimeParameters;
                this.savedCompilationStrategy = source.compilationStrategy.GetSerializableType();
            }

            public SteppingHandle Deserialize()
            {
                var target = new SteppingHandle();
                return ApplyChangesTo(target);
            }

            public SteppingHandle ApplyChangesTo(
                SteppingHandle target)
            {
                target.stepCount = this.stepCount;
                target.lastUpdateChanged = this.lastUpdateChanged;
                target.currentState = this.currentState;
                target.lastState = this.lastState;
                target.globalResourceHandle = this.oldHandle;

                var lSystemObjectRegistry = RegistryRegistry.GetObjectRegistry<LSystemObject>();
                target.mySystemObject = lSystemObjectRegistry.GetUniqueObjectFromID(systemObjectId);

                target.runtimeParameters = this.runtimeParameters;
                
                target.compilationStrategy = savedCompilationStrategy.Rehydrate();
                
                target.compilationStrategy.OnCompiledSystemChanged += target.OnSystemRecompiled;

                if (target.isDisposed)
                {
                    throw new Exception("deserialized disposed stepping handle");
                }
                if (target.globalResourceHandle?.isDisposed ?? false)
                {
                    throw new Exception("deserialized disposed global resource handle");
                }

                return target;
            }

        }


        public void InitializePostDeserialize(LSystemBehavior handleOwner)
        {
            if (isDisposed || globalResourceHandle.isDisposed)
            {
                throw new Exception("stepping handle is disposed when deserializing");
            }
            if (GlobalLSystemCoordinator.instance == null)
            {
                throw new Exception("No global l system coordinator singleton object. A single GlobalLSystemCoordinator must be present");
            }

            var lastHandle = globalResourceHandle;
            globalResourceHandle = GlobalLSystemCoordinator.instance.GetManagedResourceHandleFromSavedData(lastHandle, handleOwner);
            if (globalResourceHandle.isDisposed)
            {
                throw new Exception("global resource handle is already disposed");
            }

            if (globalResourceHandle.uniqueIdOriginPoint != lastHandle.uniqueIdOriginPoint)
            {
                // the unique ID origin point changed. therefore, the l-system must step again to update the IDs based on the serialized LastState
                //  this could be done faster with a simple ID update
                StepSystemAsync(Concurrency.Asyncronous, StateSelection.RepeatLast);
            }
        }
        
        #endregion
    }
}