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
        private bool useJob;
        private ComputeShader uniqueSummationShader;

        //private uint[] sunlightSumData;
        private ComputeBuffer sunlightSumBuffer;

        private int handleInitialize;
        private int handleMain;

        public SunlightCalculator(
            SunlightCamera sunlightCamera,
            ComputeShader uniqueSummationShader,
            bool useJob = false,
            int uniqueOrgansInitialAllocation = 4096)
        {
            this.sunlightCamera = sunlightCamera;
            this.useJob = useJob;
            this.uniqueSummationShader = uniqueSummationShader;


            handleInitialize = uniqueSummationShader.FindKernel("SunlightInitialize");
            handleMain = uniqueSummationShader.FindKernel("SunlightMain");
            sunlightSumBuffer = new ComputeBuffer(uniqueOrgansInitialAllocation, sizeof(uint));
            //sunlightSumData = new uint[uniqueOrgansInitialAllocation];

            if (handleInitialize < 0 || handleMain < 0 ||
               null == sunlightSumBuffer)
            {
                Debug.Log("Initialization failed.");
                throw new System.Exception("Could not initialize sunlight camera");
            }

            uniqueSummationShader.SetTexture(handleMain, "InputTexture", sunlightCamera.sunlightTexture, 0, UnityEngine.Rendering.RenderTextureSubElement.Color);
            uniqueSummationShader.SetBuffer(handleMain, "IdResultBuffer", sunlightSumBuffer);
            uniqueSummationShader.SetBuffer(handleInitialize, "IdResultBuffer", sunlightSumBuffer);
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
                //var dep = ApplySunlightWithJob(systemState, customSymbols, openBranchSymbol, closeBranchSymbol);
                return ApplySunlightWithComputeShader(systemState, customSymbols, openBranchSymbol, closeBranchSymbol);

                //return dep;
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
            UnityEngine.Profiling.Profiler.BeginSample("Sunlight compute shader");
            // divided by 64 in x because of [numthreads(64,1,1)] in the compute shader code
            uniqueSummationShader.Dispatch(handleInitialize, sunlightSumBuffer.count / 64, 1, 1);

            // divided by 8 in x and y because of [numthreads(8,8,1)] in the compute shader code
            uniqueSummationShader.Dispatch(handleMain, (sunlightCamera.sunlightTexture.width + 7) / 8, (sunlightCamera.sunlightTexture.height + 7) / 8, 1);

            var idsNativeArray = GetComputeDataDirectly();

            UnityEngine.Profiling.Profiler.EndSample();


            //var result = new StringBuilder();
            //for (int i = 0; i < idsNativeArray.Length; i++)
            //{
            //    var sun = idsNativeArray[i];
            //    if (sun != 0)
            //    {
            //        result.Append($"{i}: {sun}\n");
            //    }
            //}
            //Debug.Log(result.ToString());

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight result apply");

            var tmpIdentityStack = new TmpNativeStack<SunlightExposurePreProcessRuleAsArray.BranchIdentity>(10, Allocator.TempJob);
            var applyJob = new SunlightExposurePreProcessRuleAsArray
            {
                symbols = systemState.currentSymbols.Data,
                organCountsByIndex = idsNativeArray,
                lastIdentityStack = tmpIdentityStack,

                sunlightPerPixel = sunlightCamera.sunlightPerPixel,
                branchOpen = openBranchSymbol,
                branchClose = closeBranchSymbol,
                customSymbols = customSymbols,
            };

            var dependency = applyJob.Schedule();
            systemState.currentSymbols.RegisterDependencyOnData(dependency);

            dependency = JobHandle.CombineDependencies(
                idsNativeArray.Dispose(dependency),
                tmpIdentityStack.Dispose(dependency)
                );

            UnityEngine.Profiling.Profiler.EndSample();
            return dependency;

            //return default;
        }

        private NativeArray<uint> GetComputeDataDirectly()
        {
            var arr = new uint[sunlightSumBuffer.count];
            sunlightSumBuffer.GetData(arr);

            return new NativeArray<uint>(arr, Allocator.TempJob);
        }

        private NativeArray<uint> GetComputeDataAsync()
        {
            var request = AsyncGPUReadback.Request(sunlightSumBuffer);

            request.WaitForCompletion();
            if (!request.done)
            {
                Debug.LogError("gpu request not completed");
            }

            return request.GetData<uint>();
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
            dependency.Complete();

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight result apply");

            var tmpIdentityStack = new TmpNativeStack<SunlightExposurePreProcessRuleAsHashMap.BranchIdentity>(10, Allocator.TempJob);
            var applyJob = new SunlightExposurePreProcessRuleAsHashMap
            {
                symbols = systemState.currentSymbols.Data,
                organIdCounts = organCounts,
                lastIdentityStack = tmpIdentityStack,

                sunlightPerPixel = sunlightCamera.sunlightPerPixel,
                branchOpen = openBranchSymbol,
                branchClose = closeBranchSymbol,
                customSymbols = customSymbols,
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
