using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.ObjectSets;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [Obsolete("use implementations of ISteppingHandle instead, via ISteppingHandleFactory")]
    public class LSystemSteppingHandle : IDisposable
    {
        // metadata
        public int totalSteps { get; private set; }
        public bool lastUpdateChanged { get; private set; }
        public ArrayParameterRepresenation<float> runtimeParameters { get; private set; }
        private Dictionary<string, string> compiledGlobalCompiletimeReplacements;
        public LSystemState<float> currentState { get; private set; }
        private LSystemState<float> lastState;

        public LSystemObject systemObject { get; private set; }
        private bool useSharedSystem;

        /// <summary>
        /// Emits whenever the system state changes
        /// </summary>
        public event Action OnSystemStateUpdated;

        private LSystemStepper compiledSystem;


        private CancellationTokenSource lSystemPendingCancellation;
        private bool isLSystemUpdatePending = false;

        private LSystemGlobalResourceHandle globalResourceHandle;

        public LSystemSteppingHandle(
            LSystemObject mySystemObject,
            bool useSharedSystem,
            LSystemBehavior associatedBehavior)
        {
            totalSteps = 0;
            lastUpdateChanged = true;

            this.systemObject = mySystemObject;
            this.useSharedSystem = useSharedSystem;
            
            if (GlobalLSystemCoordinator.instance == null)
            {
                throw new Exception("No global l system coordinator singleton object. make a single GlobalLSystemCoordinator per scene");
            }
            globalResourceHandle = GlobalLSystemCoordinator.instance.AllocateResourceHandle(associatedBehavior);

            if (useSharedSystem)
            {
                this.systemObject.OnCachedSystemUpdated += OnSharedSystemRecompiled;
                if(this.systemObject.compiledSystem != null)
                    OnSharedSystemRecompiled();
            }
        }

        private LSystemSteppingHandle()
        {

        }


        /// <summary>
        /// Sets the system state to the new state. will clear out all metadata
        ///     related to how the current state has been updated, and all changes to runtime parameters
        /// </summary>
        public void ResetState()
        {
            ResetState(
                new DefaultLSystemState(systemObject.axiom, (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                null);
        }

        public void RecompileLSystem(Dictionary<string, string> globalCompileTimeOverrides)
        {
            if (useSharedSystem)
            {
                Debug.LogError("Invalid operation, when using shared system must compile via the shared object handle");
                return;
            }
            compiledGlobalCompiletimeReplacements = globalCompileTimeOverrides;
            var newSystem = systemObject.CompileWithParameters(globalCompileTimeOverrides);
            ResetState(
                new DefaultLSystemState(systemObject.axiom, (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                newSystem);
        }
        private void OnSharedSystemRecompiled()
        {
            if (!useSharedSystem)
            {
                Debug.LogError("Invalid operation. should only listen for recompilation updates when using shared system");
                return;
            }
            ResetState(
                new DefaultLSystemState(systemObject.axiom, (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                systemObject.compiledSystem);
        }

        /// <summary>
        /// Optionally Set to a different L-system, and reset the state and the current runtime parameters
        /// </summary>
        private void ResetState(
           LSystemState<float> newState,
           LSystemStepper newSystem)
        {
            if (isDisposed)
            {
                throw new Exception("trying to access disposed stepping handle");
            }

            if (lSystemPendingCancellation != null)
            {
                lSystemPendingCancellation.Cancel();
            }
            lastState?.currentSymbols.Dispose();
            lastState = null;
            currentState?.currentSymbols.Dispose();
            currentState = null;

            totalSteps = 0;
            lastUpdateChanged = true;
            currentState = newState;

            runtimeParameters = systemObject.GetRuntimeParameters();

            if (newSystem != null)
            {
                SetNewCompiledSystem(newSystem);
            }

            // clear out the next state handle. if an update is pending, just abort it.

            OnSystemStateUpdated?.Invoke();
        }

        /// <summary>
        /// Only use this to get metadata, don't use it to trigger steps
        /// </summary>
        /// <returns></returns>
        public LSystemStepper Stepper()
        {
            return compiledSystem;
        }

        public bool HasCompletedIterations()
        {
            return totalSteps >= systemObject.iterations;
        }

        public bool HasValidSystem()
        {
            return compiledSystem != null && !isDisposed;
        }
        public bool CanStep()
        {
            return !isLSystemUpdatePending && HasValidSystem();
        }
        /// <summary>
        /// step the Lsystem forward one tick. when CompleteInLateUpdate is true, be very careful with changes to the L-system
        ///     it is not perfectly protected against threading race conditions, so be sure not to make any mutations to 
        ///     the L-system while the behaviors are executing.
        /// </summary>
        /// <param name="CompleteInLateUpdate">When set to true, the behavior will queue up the jobs and wait until the next frame to complete them</param>
        /// <returns>true if the state changed. false otherwise</returns>
        public void StepSystem()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            StepSystemAsync(runtimeParameters, forceSync: false);
        }
        public void StepSystemImmediate()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            StepSystemAsync(runtimeParameters, forceSync: true);
        }

        public void RepeatLastStepImmediate()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            StepSystemAsync(runtimeParameters, forceSync: true, repeatLast: true);
        }

        /// <summary>
        /// Root implementation of Step system, all other step calls funnel here
        /// </summary>
        /// <param name="runtimeParameters"></param>
        /// <param name="repeatLast">True if this system should just repeat the last update. Useful if a runtime parameter changed, or </param>
        private void StepSystemAsync(
            ArrayParameterRepresenation<float> runtimeParameters,
            bool forceSync,
            bool repeatLast = false)
        {
            if (lSystemPendingCancellation == null || lSystemPendingCancellation.IsCancellationRequested)
            {
                lSystemPendingCancellation = new CancellationTokenSource();
            }
            
            var result = AsyncStepSystemFunction(runtimeParameters, repeatLast, forceSync, lSystemPendingCancellation.Token);
            result.Forget();
        }

        private async UniTask AsyncStepSystemFunction(
            ArrayParameterRepresenation<float> runtimeParameters,
            bool repeatLast,
            bool forceSync,
            CancellationToken cancel)
        {
            if (isDisposed)
            {
                throw new Exception("trying to access disposed stepping handle");
            }

            
            using var cancelledSource = new CancellationTokenSource();
            if (forceSync)
            {
                cancelledSource.Cancel();
            }
            
            this.isLSystemUpdatePending = true;
            UniTask<LSystemState<float>> pendingStateHandle;
            try
            {
                if (compiledSystem == null || compiledSystem.isDisposed)
                {
                    Debug.LogError("No Compiled system available!");
                }
                if (repeatLast)
                {
                    globalResourceHandle.UpdateUniqueIdReservationSpace(lastState);
                    pendingStateHandle = compiledSystem.StepSystemJob(
                        lastState,
                        cancelledSource.Token,
                        cancel,
                        runtimeParameters.GetCurrentParameters());
                }
                else
                {
                    globalResourceHandle.UpdateUniqueIdReservationSpace(currentState);
                    var sunlightJob = globalResourceHandle.ApplyPrestepEnvironment(
                        currentState,
                        compiledSystem.customSymbols);

                    pendingStateHandle = compiledSystem.StepSystemJob(
                        currentState,
                        cancelledSource.Token,
                        cancel,
                        runtimeParameters.GetCurrentParameters(),
                        parameterWriteDependency: sunlightJob);
                }
            }
            catch (System.Exception e)
            {
                lastUpdateChanged = false;
                Debug.LogException(e);
                this.isLSystemUpdatePending = false;
                return;
            }

            var nextState = await pendingStateHandle;

            UnityEngine.Profiling.Profiler.BeginSample("updating stepping handle state");
            if (repeatLast)
            {
                // dispose the current state, since it is about to be replaced
                currentState?.currentSymbols.Dispose();
            }
            else
            {
                // dispose the last state
                lastState?.currentSymbols.Dispose();
                lastState = currentState;
                totalSteps++;
            }
            currentState = nextState;
            // if there are immature markers, use those instead. avoiding an equality check saves time.
            var hasImmatureMarkers = systemObject.linkedFiles.immaturitySymbolMarkers.Length > 0;
            lastUpdateChanged = hasImmatureMarkers || !(currentState?.currentSymbols.Data.Equals(lastState.currentSymbols.Data) ?? false);

            this.isLSystemUpdatePending = false;
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("notifying system state listeners");
            OnSystemStateUpdated?.Invoke();
            UnityEngine.Profiling.Profiler.EndSample();
        }


        private void SetNewCompiledSystem(LSystemStepper newSystem)
        {
            if (!useSharedSystem)
            {
                compiledSystem?.Dispose();
            }
            compiledSystem = newSystem;
        }
        public bool isDisposed { get; private set; } = false;
        public void Dispose()
        {
            if (isDisposed)
            {
                throw new Exception("disposing already disposed stepping handle");
            }
            isDisposed = true;
            if (useSharedSystem)
            {
                systemObject.OnCachedSystemUpdated -= OnSharedSystemRecompiled;
            }
            else
            {
                compiledSystem?.Dispose();
                compiledSystem = null;
            }

            lSystemPendingCancellation?.Cancel();
            lSystemPendingCancellation?.Dispose();
            
            lastState?.currentSymbols.Dispose();
            lastState = null;
            currentState?.currentSymbols.Dispose();
            currentState = null;

            globalResourceHandle.Dispose();
        }


        #region Serialization

        [System.Serializable]
        public class SavedData
        {
            private int totalSteps;
            private bool lastUpdateChanged;
            private bool useSharedSystem;

            private LSystemState<float> currentState;
            private LSystemState<float> lastState;
            private LSystemGlobalResourceHandle oldHandle;

            private int systemObjectId;

            private ArrayParameterRepresenation<float> runtimeParameters;
            private Dictionary<string, string> compiledGlobalCompiletimeReplacements;

            public SavedData(LSystemSteppingHandle source)
            {
                this.totalSteps = source.totalSteps;
                this.lastUpdateChanged = source.lastUpdateChanged;
                this.useSharedSystem = source.useSharedSystem;

                this.currentState = source.currentState;
                this.lastState = source.lastState;
                this.oldHandle = source.globalResourceHandle;

                this.systemObjectId = source.systemObject.myId;

                this.runtimeParameters = source.runtimeParameters;
                this.compiledGlobalCompiletimeReplacements = source.compiledGlobalCompiletimeReplacements;
            }

            public LSystemSteppingHandle Deserialize()
            {
                var target = new LSystemSteppingHandle();
                target.totalSteps = this.totalSteps;

                target.lastUpdateChanged = this.lastUpdateChanged;
                target.useSharedSystem = this.useSharedSystem;

                target.currentState = this.currentState;
                target.lastState = this.lastState;
                target.globalResourceHandle = this.oldHandle;

                var systemObjectId = this.systemObjectId;
                var lSystemObjectRegistry = RegistryRegistry.GetObjectRegistry<LSystemObject>();
                target.systemObject = lSystemObjectRegistry.GetUniqueObjectFromID(systemObjectId);


                target.runtimeParameters = this.runtimeParameters;
                target.compiledGlobalCompiletimeReplacements = this.compiledGlobalCompiletimeReplacements;

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
            if (useSharedSystem)
            {
                this.systemObject.OnCachedSystemUpdated += OnSharedSystemRecompiled;
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

            if (!useSharedSystem)
            {
                var newSystem = systemObject.CompileWithParameters(compiledGlobalCompiletimeReplacements);
                lSystemPendingCancellation?.Cancel();
                compiledSystem?.Dispose();
                compiledSystem = newSystem;
            }

            if (globalResourceHandle.uniqueIdOriginPoint != lastHandle.uniqueIdOriginPoint)
            {
                // the unique ID origin point changed. therefore, the l-system must step again to update the IDs based on the serialized LastState
                //  this could be done faster with a simple ID update
                StepSystemAsync(runtimeParameters, forceSync: false, repeatLast: true);
            }
        }

        #endregion
    }
}
