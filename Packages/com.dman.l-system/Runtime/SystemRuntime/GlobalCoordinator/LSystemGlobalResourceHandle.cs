using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
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

        public LSystemGlobalResourceHandle(
            uint originPoint,
            uint initialSpace,
            int indexInGlobalResources,
            GlobalLSystemCoordinator parent)
        {
            uniqueIdReservationSize = initialSpace;
            requestedNextReservationSize = uniqueIdReservationSize;
            uniqueIdOriginPoint = originPoint;

            this.indexInGlobalResources = indexInGlobalResources;
            this.parent = parent;
        }

        public JobHandle ApplySunlightToSymbols(
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

            return parent.sunlightCamera.ApplySunlightToSymbols(
                systemState,
                customSymbols,
                openBranchSymbol,
                closeBranchSymbol);
        }

        public void Dispose()
        {
            requestedNextReservationSize = 0;
        }
    }
}
