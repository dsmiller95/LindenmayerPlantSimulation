using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    public class VertexColorExposureCounter : IDisposable
    {
        public RenderTexture exposureTexture { get; private set; }

        private int frameOfLastUpdate = 0;
        private NativeDisposableHotSwap<NativeArrayNativeDisposableAdapter<uint>> uniqueSunlightAssignments;

        private readonly ComputeShader uniqueSummationShader;
        private int uniqueOrgansInitialAllocation { get; set; }

        [Tooltip("The multiplier used to increase the size of the compute buffer on each resize event")]
        private readonly float computeBufferResizeMultiplier = 2;
        [Tooltip("The usage percentage of the current compute buffer which will trigger a buffer resize")]
        [Range(0, 1)]
        private readonly float computeBufferResizeThreshold = 0.9f;

        private AsyncGPUReadbackRequest? readbackRequest;
        private ComputeBuffer sunlightSumBuffer;
        private int initializeKernalId;
        private int mainKernalId;

        public VertexColorExposureCounter(
            RenderTexture sunlightTexture,
            VertexExposureCountingSettings overrides = null)
        {
            var defaultSettings = Resources.Load<VertexExposureCountingSettings>("defaultVertexCountingSettings");
            this.exposureTexture = sunlightTexture;

            if (overrides == null)
                overrides = null;
            if (overrides?.uniqueSummationShader != null)
                this.uniqueSummationShader = UnityEngine.Object.Instantiate(overrides.uniqueSummationShader);
            else
                this.uniqueSummationShader = UnityEngine.Object.Instantiate(defaultSettings.uniqueSummationShader);

            if ((overrides?.computerBufferResizeThreshold ?? -1) > 0)
                this.computeBufferResizeThreshold = overrides.computerBufferResizeThreshold;
            else
                this.computeBufferResizeThreshold = defaultSettings.computerBufferResizeThreshold;

            if ((overrides?.computeBufferResizeMultiplier ?? -1) > 1)
                this.computeBufferResizeMultiplier = overrides.computeBufferResizeMultiplier;
            else
                this.computeBufferResizeMultiplier = defaultSettings.computeBufferResizeMultiplier;

            if ((overrides?.defaultUniqueAllocationSize ?? -1) > 1)
                this.uniqueOrgansInitialAllocation = overrides.defaultUniqueAllocationSize;
            else
                this.uniqueOrgansInitialAllocation = defaultSettings.defaultUniqueAllocationSize;
        }

        public void Initialize(uint? uniqueOrganIdSpaceOverride = null)
        {
            if(uniqueOrganIdSpaceOverride.HasValue && uniqueOrganIdSpaceOverride.Value > 0)
            {
                this.uniqueOrgansInitialAllocation = (int)uniqueOrganIdSpaceOverride.Value;
            }
            this.InitializeBuffers();
        }

        public void RefreshExposureData(uint nextExposureBufferSize)
        {
            LazyEnsureUpdatedReadback(nextExposureBufferSize);
        }

        public DependencyTracker<NativeArrayNativeDisposableAdapter<uint>> GetReadAccessSunlightExposure()
        {
            return uniqueSunlightAssignments.ActiveData;
        }

        public void Dispose()
        {
            readbackRequest.Value.WaitForCompletion();
            sunlightSumBuffer.Dispose();
            uniqueSunlightAssignments.Dispose();
        }

        private void InitializeBuffers()
        {
            initializeKernalId = uniqueSummationShader.FindKernel("SunlightInitialize");
            mainKernalId = uniqueSummationShader.FindKernel("SunlightMain");

            sunlightSumBuffer?.Dispose();
            sunlightSumBuffer = new ComputeBuffer(uniqueOrgansInitialAllocation, sizeof(uint));

            if (initializeKernalId < 0 || mainKernalId < 0 ||
               sunlightSumBuffer == null)
            {
                Debug.Log("Initialization failed.");
                throw new System.Exception("Could not initialize sunlight camera");
            }

            uniqueSummationShader.SetTexture(mainKernalId, "InputTexture", exposureTexture, 0, UnityEngine.Rendering.RenderTextureSubElement.Color);
            uniqueSummationShader.SetBuffer(mainKernalId, "IdResultBuffer", sunlightSumBuffer);
            uniqueSummationShader.SetBuffer(initializeKernalId, "IdResultBuffer", sunlightSumBuffer);

            uniqueSunlightAssignments = new NativeDisposableHotSwap<NativeArrayNativeDisposableAdapter<uint>>();
        }

        private void LazyEnsureUpdatedReadback(uint nextExposureBufferSize)
        {
            if (readbackRequest.HasValue && readbackRequest.Value.done)
            {
                CompleteGPUReadback();
                readbackRequest = null;
            }
            if (readbackRequest == null)
            {
                // only resize the buffer when the data readback completes
                ResizeSumBuffer(nextExposureBufferSize);
            }
            if (frameOfLastUpdate < Time.frameCount && readbackRequest == null)
            {
                TriggerGPUReadback();
                frameOfLastUpdate = Time.frameCount;
            }
        }

        private void TriggerGPUReadback()
        {
            UnityEngine.Profiling.Profiler.BeginSample("Sunlight compute shader");
            // divided by 64 in x because of [numthreads(64,1,1)] in the compute shader code
            //  add 63 to ensure rounding up
            //uniqueSummationShader.Dispatch(handleInitialize, sunlightSumBuffer.count / 64, 1, 1);
            uniqueSummationShader.Dispatch(initializeKernalId, (sunlightSumBuffer.count + 63) / 64, 1, 1);

            // divided by 8 in x and y because of [numthreads(8,8,1)] in the compute shader code
            uniqueSummationShader.Dispatch(mainKernalId, (exposureTexture.width + 7) / 8, (exposureTexture.height + 7) / 8, 1);

            readbackRequest = AsyncGPUReadback.Request(sunlightSumBuffer);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private int lastReadbackFrame;

        private void CompleteGPUReadback()
        {
            UnityEngine.Profiling.Profiler.BeginSample("Sunlight data readback");
            if (!readbackRequest.HasValue)
            {
                Debug.LogError("gpu request not available");
            }
            readbackRequest.Value.WaitForCompletion();
            if (!readbackRequest.Value.done)
            {
                Debug.LogError("gpu request not completed");
            }
            using var nativeIdData = readbackRequest.Value.GetData<uint>();
            var reAllocatedNativeData = new NativeArray<uint>(nativeIdData, Allocator.Persistent);
            var dependencyTracker = uniqueSunlightAssignments.AssignPending(reAllocatedNativeData);

#if UNITY_EDITOR
            dependencyTracker.underlyingAllocator = Allocator.Persistent;

            if (Time.frameCount > lastReadbackFrame + 4)
            {
                Debug.LogWarning("sunlight not refreshing fast enough. temp alloc will have expired.");
            }
            lastReadbackFrame = Time.frameCount;
#endif
            uniqueSunlightAssignments.HotSwapToPending();
            UnityEngine.Profiling.Profiler.EndSample();
        }


        /// <summary>
        /// assumes that the compute shader is complete and there are no pending readback requests
        /// </summary>
        private void ResizeSumBuffer(uint newBufferSize)
        {
            var nextAllocationSize = sunlightSumBuffer.count;
            while (newBufferSize > nextAllocationSize * computeBufferResizeThreshold)
            {
                nextAllocationSize = (int)(nextAllocationSize * computeBufferResizeMultiplier);
            }
            if (nextAllocationSize <= sunlightSumBuffer.count)
            {
                return;
            }

            Debug.Log($"Resizing buffer from {sunlightSumBuffer.count} to {nextAllocationSize}");

            sunlightSumBuffer?.Dispose();
            sunlightSumBuffer = new ComputeBuffer(nextAllocationSize, sizeof(uint));
            uniqueSummationShader.SetBuffer(mainKernalId, "IdResultBuffer", sunlightSumBuffer);
            uniqueSummationShader.SetBuffer(initializeKernalId, "IdResultBuffer", sunlightSumBuffer);
        }
    }
}
