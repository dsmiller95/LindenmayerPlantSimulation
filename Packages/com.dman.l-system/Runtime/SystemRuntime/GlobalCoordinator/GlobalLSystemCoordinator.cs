using Dman.LSystem.SystemRuntime.Sunlight;
using Dman.LSystem.UnityObjects;
using Dman.ObjectSets;
using Dman.SceneSaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.GlobalCoordinator
{
    /// <summary>
    /// responsible for all tasks which require coordination across all L-systems
    ///  for example:
    ///     providing sunlight information
    ///     ensuring all organs are unique between all l-systems, as well as inside the parent l-system
    /// </summary>
    public class GlobalLSystemCoordinator : MonoBehaviour, ISaveableData
    {
        public SunlightCamera sunlightCamera;

        public uint uniqueIdMinSpaceRequired;
        [Tooltip("The multiplier used to increase the size of the compute buffer on each resize event")]
        public float idSpaceResizeMultiplier = 2;
        [Tooltip("The usage percentage of the current id space which will trigger a resize")]
        [Range(0, 1)]
        public float idSpaceResizeThreshold = 0.9f;


        public static GlobalLSystemCoordinator instance;

        private List<LSystemGlobalResourceHandle> allResourceReservations;

        private void Awake()
        {
            var systemRegistry = RegistryRegistry.GetObjectRegistry<LSystemObject>();
            systemRegistry.AssignAllIDs();
            instance = this;
            allResourceReservations = new List<LSystemGlobalResourceHandle>();
        }

        public LSystemGlobalResourceHandle AllocateResourceHandle(LSystemBehavior associatedBehavior)
        {
            var lastReservation = allResourceReservations.LastOrDefault();
            uint originPoint = 1;
            if (lastReservation != null)
            {
                originPoint = lastReservation.uniqueIdOriginPoint + lastReservation.requestedNextReservationSize;
            }
            var newHandle = new LSystemGlobalResourceHandle(
                originPoint,
                uniqueIdMinSpaceRequired,
                this,
                associatedBehavior);
            allResourceReservations.Add(newHandle);

            return newHandle;
        }

        /// <summary>
        /// Get a managed resource handle, from a handle which was saved off independently
        /// </summary>
        /// <param name="savedHandle"></param>
        /// <returns></returns>
        public LSystemGlobalResourceHandle GetManagedResourceHandleFromSavedData(LSystemGlobalResourceHandle savedHandle, LSystemBehavior assocatedBehavior)
        {
            var matchingHandle = allResourceReservations
                .Where(x => x.uniqueIdOriginPoint == savedHandle.uniqueIdOriginPoint && x.uniqueIdReservationSize == savedHandle.uniqueIdReservationSize)
                .FirstOrDefault();
            if(matchingHandle == null)
            {
                throw new Exception($"Could not find global resource handle matching a saved handle from [{savedHandle.uniqueIdOriginPoint},{savedHandle.uniqueIdReservationSize + savedHandle.uniqueIdOriginPoint})");
            }
            matchingHandle.InitializePostDeserialize(assocatedBehavior, this);
            return matchingHandle;
        }

        /// <summary>
        /// make new room for all of the l-system resource reservations
        /// returns the total required size of all current reservations
        /// </summary>
        /// <returns></returns>
        public uint ResizeLSystemReservations()
        {
            uint currentOrigin = 1;
            //var layoutDescriptor = new StringBuilder();
            for (int i = 0; i < allResourceReservations.Count; i++)
            {
                var currentReservation = allResourceReservations[i];
                if (currentReservation.requestedNextReservationSize == 0)
                {
                    // skip this resource, and remove it from the list
                    allResourceReservations.RemoveAt(i);
                    i--;
                    continue;
                }
                currentReservation.uniqueIdOriginPoint = currentOrigin;

                currentReservation.uniqueIdReservationSize = currentReservation.requestedNextReservationSize;
                //layoutDescriptor.Append($"{currentOrigin}-{currentReservation.uniqueIdReservationSize}->");

                currentOrigin += currentReservation.uniqueIdReservationSize;
            }
            //Debug.Log(layoutDescriptor.ToString());

            return currentOrigin;
        }

        public LSystemBehavior GetBehaviorContainingOrganId(uint organId)
        {
            if (organId == 0)
            {
                return null;
            }

            foreach (var resourceAllocation in allResourceReservations)
            {
                if (resourceAllocation.ContainsId(organId)){
                    return resourceAllocation.associatedBehavior;
                }
            }
            return null;
        }


        #region Saving
        public string UniqueSaveIdentifier => "Global L System Coordinator";


        [System.Serializable]
        class GlobalLSystemState
        {
            private List<LSystemGlobalResourceHandle> resourceReservations;
            public GlobalLSystemState(GlobalLSystemCoordinator source)
            {
                this.resourceReservations = source.allResourceReservations;
            }

            public void Apply(GlobalLSystemCoordinator target)
            {
                target.allResourceReservations = this.resourceReservations;
            }
        }

        public object GetSaveObject()
        {
            return new GlobalLSystemState(this);
        }

        public void SetupFromSaveObject(object save)
        {
            if (save is GlobalLSystemState state)
            {
                state.Apply(this);
            }
        }

        public ISaveableData[] GetDependencies()
        {
            return new ISaveableData[0];
        }
        #endregion

    }
}
