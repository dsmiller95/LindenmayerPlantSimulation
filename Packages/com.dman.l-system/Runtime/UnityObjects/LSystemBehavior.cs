using Dman.ObjectSets;
using Dman.SceneSaveSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public interface ILSystemCompileTimeParameterGenerator
    {
        public abstract Dictionary<string, string> GenerateCompileTimeParameters();
    }

    public class LSystemBehavior : MonoBehaviour, ISaveableData
    {
        public bool logStates = false;

        /// <summary>
        /// The L System definition to execute
        /// </summary>
        public LSystemObject systemObject;

        public event Action OnSystemStateUpdated;
        public event Action OnSystemObjectUpdated;
        public LSystemSteppingHandle steppingHandle { get; private set; }
        /// <summary>
        /// the value of Time.time when this system was last updated
        /// </summary>
        public float lastUpdateTime { get; private set; }


        private void Awake()
        {
            SetLastUpdateTime();
            if (systemObject != null)
            {
                SetSystem(systemObject);
            }
        }
        private void OnDestroy()
        {
            steppingHandle?.Dispose();
        }

        /// <summary>
        /// Assign a new system definition to this behavior.
        /// </summary>
        /// <param name="newSystemObject"></param>
        public void SetSystem(LSystemObject newSystemObject)
        {
            systemObject = newSystemObject;
            if (systemObject != null)
            {
                steppingHandle?.Dispose();
                var globalParams = GetComponent<ILSystemCompileTimeParameterGenerator>();
                if (globalParams == null)
                {
                    steppingHandle = new LSystemSteppingHandle(systemObject, true, this);
                }
                else
                {
                    steppingHandle = new LSystemSteppingHandle(systemObject, false, this);
                }
                steppingHandle.OnSystemStateUpdated += LSystemStateWasUpdated;
            }
            OnSystemObjectUpdated?.Invoke();

            ResetState();
        }

        /// <summary>
        /// Reset the system state to the Axiom, and re-initialize the Random provider with a random seed unless otherwise specified
        /// </summary>
        public void ResetState()
        {
            LogMyState();
            var globalParams = GetComponent<ILSystemCompileTimeParameterGenerator>();
            if (globalParams != null)
            {
                var extraGlobalParams = globalParams.GenerateCompileTimeParameters();
                steppingHandle.RecompileLSystem(extraGlobalParams);
            }
            else
            {
                steppingHandle.ResetState();
            }

            SetLastUpdateTime();
        }

        /// <summary>
        /// step the Lsystem forward one tick. when CompleteInLateUpdate is true, be very careful with changes to the L-system
        ///     it is not perfectly protected against threading race conditions, so be sure not to make any mutations to 
        ///     the L-system while the behaviors are executing.
        /// </summary>
        /// <returns>true if the state changed. false otherwise</returns>
        public void StepSystem()
        {
            steppingHandle.StepSystem();
        }

        private void LSystemStateWasUpdated()
        {
            LogMyState();
            OnSystemStateUpdated?.Invoke();
            SetLastUpdateTime();
        }

        private void LogMyState()
        {
            if (logStates)
            {
                var state = steppingHandle?.currentState?.currentSymbols?.Data;
                if (state.HasValue)
                {
                    Debug.Log(systemObject.ConvertToReadableString(state.Value));
                }
            }
        }

        private void SetLastUpdateTime()
        {
            lastUpdateTime = Time.unscaledTime + UnityEngine.Random.Range(0, 0.3f);
        }

        /// <summary>
        /// Set a runtime parameter exposed by the system object.
        ///     Will log an error to the console if no parameter by <paramref name="parameterName"/> is exposed by the system
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        public void SetRuntimeParameter(string parameterName, float parameterValue)
        {
            steppingHandle.runtimeParameters.SetParameter(parameterName, parameterValue);
        }

        #region Saving
        public string UniqueSaveIdentifier => "L System Behavior";

        public int LoadOrderPriority => 0;

        [System.Serializable]
        class LSystemBehaviorSaveState
        {
            private int lSystemId;
            private LSystemSteppingHandle.SavedData steppingHandle;
            public LSystemBehaviorSaveState(LSystemBehavior source)
            {
                this.lSystemId = source.systemObject.myId;
                this.steppingHandle = new LSystemSteppingHandle.SavedData(source.steppingHandle);
            }

            public void Apply(LSystemBehavior target)
            {
                target.lastUpdateTime = 0;
                var systemRegistry = RegistryRegistry.GetObjectRegistry<LSystemObject>();
                target.systemObject = systemRegistry.GetUniqueObjectFromID(lSystemId);

                target.steppingHandle?.Dispose();

                target.steppingHandle = steppingHandle.Deserialize();
                target.steppingHandle.InitializePostDeserialize(target);
                target.steppingHandle.OnSystemStateUpdated += target.LSystemStateWasUpdated;

                target.OnSystemObjectUpdated?.Invoke();
                target.OnSystemStateUpdated?.Invoke();
            }
        }

        public object GetSaveObject()
        {
            return new LSystemBehaviorSaveState(this);
        }

        public void SetupFromSaveObject(object save)
        {
            if (save is LSystemBehaviorSaveState savedState)
            {
                savedState.Apply(this);
            }
        }
        #endregion
    }
}
