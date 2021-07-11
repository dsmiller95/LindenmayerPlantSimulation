using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.UnityObjects;
using System;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.GlobalCoordinator
{
    public class LSystemGlobalResourceHandle : IDisposable
    {
        public uint uniqueIdOriginPoint;

        public uint uniqueIdReservationSize;
        public uint requestedNextReservationSize;

        private int indexInGlobalResources;
        private GlobalLSystemCoordinator parent;
        public LSystemBehavior associatedBehavior { get; private set; }

        public LSystemGlobalResourceHandle(
            uint originPoint,
            uint initialSpace,
            int indexInGlobalResources,
            GlobalLSystemCoordinator parent,
            LSystemBehavior associatedBehavior)
        {
            uniqueIdReservationSize = initialSpace;
            requestedNextReservationSize = uniqueIdReservationSize;
            uniqueIdOriginPoint = originPoint;

            this.indexInGlobalResources = indexInGlobalResources;
            this.parent = parent;
            this.associatedBehavior = associatedBehavior;
        }

        public JobHandle GlobalPostStep(
            LSystemState<float> systemState,
            CustomRuleSymbols customSymbols,
            int openBranchSymbol,
            int closeBranchSymbol)
        {
            while (systemState.maxUniqueOrganIds > requestedNextReservationSize * parent.idSpaceResizeThreshold)
            {
                requestedNextReservationSize = (uint)(requestedNextReservationSize * parent.idSpaceResizeMultiplier);
            }

            systemState.firstUniqueOrganId = uniqueIdOriginPoint;

            return parent.sunlightCamera?.ApplySunlightToSymbols(
                systemState,
                customSymbols,
                openBranchSymbol,
                closeBranchSymbol) ?? default(JobHandle);
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
