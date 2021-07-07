using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    public class SunlightCalculator : IDisposable
    {
        private SunlightCamera sunlightCamera;

        public SunlightCalculator(
            SunlightCamera sunlightCamera)
        {
            this.sunlightCamera = sunlightCamera;
        }

        public JobHandle ApplySunlightToSymbols(
            LSystemState<float> systemState,
            CustomRuleSymbols customSymbols, int openBranchSymbol, int closeBranchSymbol)
        {
            var swapper = sunlightCamera.uniqueSunlightAssignments;
            var idsNativeArray = swapper.ActiveData;
            if(idsNativeArray == null)
            {
                Debug.LogError("no sunlight data available");
                return default;
            }

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight result apply");

            var tmpIdentityStack = new TmpNativeStack<SunlightExposurePreProcessRule.BranchIdentity>(10, Allocator.TempJob);
            var applyJob = new SunlightExposurePreProcessRule
            {
                symbols = systemState.currentSymbols.Data,
                organCountsByIndex = idsNativeArray.Data,
                lastIdentityStack = tmpIdentityStack,

                sunlightPerPixel = sunlightCamera.sunlightPerPixel,
                branchOpen = openBranchSymbol,
                branchClose = closeBranchSymbol,
                customSymbols = customSymbols,
            };

            var dependency = applyJob.Schedule();
            systemState.currentSymbols.RegisterDependencyOnData(dependency);
            idsNativeArray.RegisterDependencyOnData(dependency);

            dependency = JobHandle.CombineDependencies(
                idsNativeArray.Dispose(dependency),
                tmpIdentityStack.Dispose(dependency)
                );

            UnityEngine.Profiling.Profiler.EndSample();
            return dependency;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
