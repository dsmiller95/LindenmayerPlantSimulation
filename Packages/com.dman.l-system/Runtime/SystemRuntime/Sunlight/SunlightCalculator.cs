using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    public class SunlightCalculator : IDisposable
    {
        private SunlightCamera sunlightCamera;
        private bool useJob;
        private ComputeShader uniqueSummationShader;

        private uint[] sunlightSumData;
        private ComputeBuffer sunlightSumBuffer;

        public SunlightCalculator(
            SunlightCamera sunlightCamera,
            ComputeShader uniqueSummationShader,
            bool useJob = true,
            int uniqueOrgansInitialAllocation = 4096)
        {
            this.sunlightCamera = sunlightCamera;
            this.useJob = useJob;
            this.uniqueSummationShader = uniqueSummationShader;


            //var handleInitialize = uniqueSummationShader.FindKernel("HistogramInitialize");
            //var handleMain = uniqueSummationShader.FindKernel("HistogramMain");
            //sunlightSumBuffer = new ComputeBuffer(uniqueOrgansInitialAllocation, sizeof(uint));
            //sunlightSumData = new uint[uniqueOrgansInitialAllocation];

            //if (handleInitialize < 0 || handleMain < 0 ||
            //   null == sunlightSumBuffer || null == sunlightSumData)
            //{
            //    Debug.Log("Initialization failed.");
            //    throw new System.Exception("Could not initialize sunlight camera");
            //}

            //uniqueSummationShader.SetTexture(handleMain, "InputTexture", sunlightCamera.sunlightTexture);
            //uniqueSummationShader.SetBuffer(handleMain, "HistogramBuffer", sunlightSumBuffer);
            //uniqueSummationShader.SetBuffer(handleInitialize, "HistogramBuffer", sunlightSumBuffer);
        }

        public JobHandle ApplySunlightToSymbols(
            LSystemState<float> systemState,
            CustomRuleSymbols customSymbols, int openBranchSymbol, int closeBranchSymbol)
        {
            if (useJob)
            {
                return ApplySunlightWithJob(systemState, customSymbols, openBranchSymbol, closeBranchSymbol);
            }
            else
            {
                return ApplySunlightWithComputeShader(systemState, customSymbols, openBranchSymbol, closeBranchSymbol);
            }
        }


        public void Dispose()
        {
            sunlightSumBuffer?.Dispose();
            sunlightSumBuffer = null;
        }

        private JobHandle ApplySunlightWithComputeShader(
            LSystemState<float> systemState,
            CustomRuleSymbols customSymbols, int openBranchSymbol, int closeBranchSymbol)
        {

            return default;
        }

        private JobHandle ApplySunlightWithJob(
        LSystemState<float> systemState,
        CustomRuleSymbols customSymbols, int openBranchSymbol, int closeBranchSymbol)
        {
            if (!(customSymbols.hasSunlight && customSymbols.hasIdentifiers))
            {
                return default;
            }

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight Texture Getting");

            var sunlightTexture = sunlightCamera.sunlightTexture;
            var targetTexture = new Texture2D(sunlightTexture.width, sunlightTexture.height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            targetTexture.alphaIsTransparency = false;

            RenderTexture.active = sunlightTexture;
            targetTexture.ReadPixels(new Rect(0f, 0f, sunlightTexture.width, sunlightTexture.height), 0, 0);
            RenderTexture.active = null;
            UnityEngine.Profiling.Profiler.EndSample();


            UnityEngine.Profiling.Profiler.BeginSample("Sunlight Texture Summation");
            var textureData = targetTexture.GetRawTextureData<uint>();
            if (textureData.Length != targetTexture.width * targetTexture.height)
            {
                Debug.LogError("texture data size does not match pixel size");
            }

            var counter = new CountByDistinct(textureData);
            var organCounts = counter.GetCounts();
            var dependency = counter.Schedule();

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight result apply");

            var tmpIdentityStack = new TmpNativeStack<SunlightExposurePreProcessRule.BranchIdentity>(10, Allocator.TempJob);
            var applyJob = new SunlightExposurePreProcessRule
            {
                symbols = systemState.currentSymbols.Data,
                organIdCounts = organCounts,
                lastIdentityStack = tmpIdentityStack,

                sunlightPerPixel = sunlightCamera.sunlightPerPixel,
                branchOpen = openBranchSymbol,
                branchClose = closeBranchSymbol,
                customSymbols = customSymbols
            };

            dependency = applyJob.Schedule(dependency);
            systemState.currentSymbols.RegisterDependencyOnData(dependency);

            dependency = JobHandle.CombineDependencies(
                organCounts.Dispose(dependency),
                tmpIdentityStack.Dispose(dependency)
                );

            UnityEngine.Profiling.Profiler.EndSample();


            return dependency;
        }

    }
}
