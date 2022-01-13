using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.ObjectSets;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
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


        private CompletableHandle<LSystemState<float>> lSystemPendingCompletable;

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

            if (useSharedSystem)
            {
                this.systemObject.OnCachedSystemUpdated += OnSharedSystemRecompiled;
            }

            if(GlobalLSystemCoordinator.instance == null)
            {
                throw new Exception("No global l system coordinator singleton object. make a single GlobalLSystemCoordinator per scene");
            }
            globalResourceHandle = GlobalLSystemCoordinator.instance.AllocateResourceHandle(associatedBehavior);
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
            if (lSystemPendingCompletable != null)
            {
                lSystemPendingCompletable.Cancel();
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

        public bool HasValidSystem()
        {
            return compiledSystem != null;
        }
        public bool CanStep()
        {
            return lSystemPendingCompletable == null;
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
            StepSystemAsync(runtimeParameters);
        }
        public void StepSystemImmediate()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            StepSystemAsync(runtimeParameters);
            lSystemPendingCompletable.CompleteImmediate();
        }

        public void RepeatLastStepImmediate()
        {
            if (!CanStep())
            {
                Debug.LogError("System is already waiting for an update!! To many!!");
                return;
            }
            StepSystemAsync(runtimeParameters, repeatLast: true);
            lSystemPendingCompletable.CompleteImmediate();
        }

        /// <summary>
        /// Root implementation of Step system, all other step calls funnel here
        /// </summary>
        /// <param name="runtimeParameters"></param>
        /// <param name="repeatLast">True if this system should just repeat the last update. Useful if a runtime parameter changed, or </param>
        private void StepSystemAsync(
            ArrayParameterRepresenation<float> runtimeParameters,
            bool repeatLast = false)
        {
            ICompletable<LSystemState<float>> pendingStateHandle;
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
                        runtimeParameters.GetCurrentParameters());
                }
                else
                {
                    globalResourceHandle.UpdateUniqueIdReservationSpace(currentState);
                    var sunlightJob = globalResourceHandle.ApplyPrestepEnvironment(
                        currentState,
                        compiledSystem.customSymbols,
                        compiledSystem.branchOpenSymbol,
                        compiledSystem.branchCloseSymbol);

                    pendingStateHandle = compiledSystem.StepSystemJob(
                        currentState,
                        runtimeParameters.GetCurrentParameters(),
                        parameterWriteDependency: sunlightJob);
                }
                if (pendingStateHandle == null)
                {
                    lSystemPendingCompletable = null;
                    return;
                }
            }
            catch (System.Exception e)
            {
                lastUpdateChanged = false;
                Debug.LogException(e);
                lSystemPendingCompletable = null;
                return;
            }

            lSystemPendingCompletable = CompletableExecutor.Instance.RegisterCompletable(pendingStateHandle);

            lSystemPendingCompletable.OnCompleted += (nextState) =>
            {
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

                lSystemPendingCompletable = null;
                UnityEngine.Profiling.Profiler.EndSample();
                OnSystemStateUpdated?.Invoke();
            };
        }


        private void SetNewCompiledSystem(LSystemStepper newSystem)
        {
            if (!useSharedSystem)
            {
                compiledSystem?.Dispose();
            }
            compiledSystem = newSystem;
        }
        public void Dispose()
        {
            if (useSharedSystem)
            {
                systemObject.OnCachedSystemUpdated -= OnSharedSystemRecompiled;
            }
            else
            {
                compiledSystem?.Dispose();
                compiledSystem = null;
            }
            lSystemPendingCompletable?.Cancel();
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

                return target;
            }
        }


        public void InitializePostDeserialize(LSystemBehavior handleOwner)
        {
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

            if (!useSharedSystem)
            {
                var newSystem = systemObject.CompileWithParameters(compiledGlobalCompiletimeReplacements);
                if (lSystemPendingCompletable != null)
                {
                    lSystemPendingCompletable.Cancel();
                }
                compiledSystem?.Dispose();
                compiledSystem = newSystem;
            }

            if (globalResourceHandle.uniqueIdOriginPoint != lastHandle.uniqueIdOriginPoint)
            {
                Debug.Log("global handle was changed when loading from save");
                // the unique ID origin point changed. therefore, the l-system must step again to update the IDs based on the serialized LastState
                //  this could be done faster with a simple ID update
                StepSystemAsync(runtimeParameters, repeatLast: true);
            }
        }

        #endregion
    }
}
