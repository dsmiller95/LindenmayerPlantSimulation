using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.UnityObjects;
using System;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.GlobalCoordinator
{
    [Serializable]
    public class LSystemGlobalResourceHandle : IDisposable
    {
        public uint uniqueIdOriginPoint;

        public uint uniqueIdReservationSize;
        public uint requestedNextReservationSize;

        /// <summary>
        /// globally unique and constant plant id for this handle
        /// </summary>
        public uint globalPlantId;

        [NonSerialized]
        private GlobalLSystemCoordinator parent;
        [NonSerialized]
        private LSystemBehavior _associatedBehavior;
        public LSystemBehavior associatedBehavior { get => _associatedBehavior; private set => _associatedBehavior = value; }

        public LSystemGlobalResourceHandle(
            uint originPoint,
            uint initialSpace,
            uint globalPlantId,
            GlobalLSystemCoordinator parent,
            LSystemBehavior associatedBehavior)
        {
            uniqueIdReservationSize = initialSpace;
            requestedNextReservationSize = uniqueIdReservationSize;
            uniqueIdOriginPoint = originPoint;
            this.globalPlantId = globalPlantId;

            this.parent = parent;
            this.associatedBehavior = associatedBehavior;
        }

        public void InitializePostDeserialize(LSystemBehavior associatedBehavior, GlobalLSystemCoordinator parent)
        {
            this.associatedBehavior = associatedBehavior;
            this.parent = parent;
        }

        public void UpdateUniqueIdReservationSpace(LSystemState<float> systemState)
        {
            while (systemState.maxUniqueOrganIds > requestedNextReservationSize * parent.idSpaceResizeThreshold)
            {
                requestedNextReservationSize = (uint)(requestedNextReservationSize * parent.idSpaceResizeMultiplier);
            }

            systemState.firstUniqueOrganId = uniqueIdOriginPoint;
            systemState.uniquePlantId = globalPlantId;
        }

        public JobHandle ApplyPrestepEnvironment(
            LSystemState<float> systemState,
            CustomRuleSymbols customSymbols)
        {
            return parent.sunlightCameraSingleton?.ApplySunlightToSymbols(
                systemState,
                customSymbols) ?? default(JobHandle);
        }

        public bool ContainsId(uint organId)
        {
            return organId >= uniqueIdOriginPoint &&
                organId - uniqueIdOriginPoint < uniqueIdReservationSize;
        }

        public void Dispose()
        {
            requestedNextReservationSize = 0;
        }
    }
}
