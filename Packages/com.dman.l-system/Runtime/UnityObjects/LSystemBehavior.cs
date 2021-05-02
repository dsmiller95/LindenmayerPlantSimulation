using Dman.LSystem.SystemRuntime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public abstract class LSystemCompileTimeParameterGenerator : MonoBehaviour
    {
        public abstract Dictionary<string, string> GenerateCompileTimeParameters();
    }

    public class LSystemBehavior : MonoBehaviour
    {
        /// <summary>
        /// The L System definition to execute
        /// </summary>
        public LSystemObject systemObject;

        public event Action OnSystemStateUpdated;
        public LSystemSteppingHandle steppingHandle { get; private set; }
        /// <summary>
        /// the value of Time.time when this system was last updated
        /// </summary>
        public float lastUpdateTime { get; private set; }


        private void Awake()
        {
            lastUpdateTime = Time.time + UnityEngine.Random.Range(.3f, 0.6f);
            if (systemObject != null)
            {
                this.SetSystem(systemObject);
            }
        }

        /// <summary>
        /// Assign a new system definition to this behavior.
        /// </summary>
        /// <param name="newSystemObject"></param>
        public void SetSystem(LSystemObject newSystemObject)
        {
            systemObject = newSystemObject;
            if(systemObject != null)
            {
                steppingHandle?.Dispose();
                var globalParams = GetComponent<LSystemCompileTimeParameterGenerator>();
                if (globalParams == null)
                {
                    steppingHandle = new LSystemSteppingHandle(this, systemObject, true);
                }
                else
                {
                    steppingHandle = new LSystemSteppingHandle(this, systemObject, false);
                }
                steppingHandle.OnSystemStateUpdated += LSystemStateWasUpdated;
            }

            this.ResetState();
        }

        /// <summary>
        /// Reset the system state to the Axiom, and re-initialize the Random provider with a random seed unless otherwise specified
        /// </summary>
        public void ResetState()
        {
            var globalParams = GetComponent<LSystemCompileTimeParameterGenerator>();
            if (globalParams != null)
            {
                Debug.Log("compiling new system");
                var extraGlobalParams = globalParams.GenerateCompileTimeParameters();
                this.steppingHandle.RecompileLSystem(extraGlobalParams);
            }else
            {
                steppingHandle.ResetState();
            }

            lastUpdateTime = Time.time + UnityEngine.Random.Range(0f, 0.3f);
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
            this.steppingHandle.StepSystem();
        }

        private void LSystemStateWasUpdated()
        {
            OnSystemStateUpdated?.Invoke();
            lastUpdateTime = Time.time + UnityEngine.Random.Range(0, 0.1f);
        }

        /// <summary>
        /// Set a runtime parameter exposed by the system object.
        ///     Will log an error to the console if no parameter by <paramref name="parameterName"/> is exposed by the system
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        public void SetRuntimeParameter(string parameterName, float parameterValue)
        {
            this.steppingHandle.runtimeParameters.SetParameter(parameterName, parameterValue);
        }


        private void OnDestroy()
        {
            steppingHandle.Dispose();
        }
    }
}
