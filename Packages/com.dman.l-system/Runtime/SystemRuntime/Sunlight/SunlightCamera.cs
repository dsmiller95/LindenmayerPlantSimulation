using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    [RequireComponent(typeof(Camera))]
    public class SunlightCamera : MonoBehaviour
    {
        public float sunlightPerSquareUnit = 1;

        public float totalSunlight => Mathf.Pow(GetComponent<Camera>().orthographicSize, 2) * sunlightPerSquareUnit;
        public float sunlightPerPixel => totalSunlight / (sunlightTexture.width * sunlightTexture.height);
        public RenderTexture sunlightTexture => GetComponent<Camera>().targetTexture;


        private int frameOfLastUpdate = 0;
        public NativeDisposableHotSwap<NativeArrayNativeDisposableAdapter<uint>> uniqueSunlightAssignments;


        public ComputeShader uniqueSummationShader;
        public int uniqueOrgansInitialAllocation = 4096;

        [Tooltip("The multiplier used to increase the size of the compute buffer on each resize event")]
        public float computeBufferResizeMultiplier = 2;
        [Tooltip("The usage percentage of the current compute buffer which will trigger a buffer resize")]
        [Range(0, 1)]
        public float computeBufferResizeThreshold = 0.9f;

        private AsyncGPUReadbackRequest? readbackRequest;
        private ComputeBuffer sunlightSumBuffer;
        private int handleInitialize;
        private int handleMain;

        private void Start()
        {
            handleInitialize = uniqueSummationShader.FindKernel("SunlightInitialize");
            handleMain = uniqueSummationShader.FindKernel("SunlightMain");

            sunlightSumBuffer = new ComputeBuffer(uniqueOrgansInitialAllocation, sizeof(uint));

            if (handleInitialize < 0 || handleMain < 0 ||
               sunlightSumBuffer == null)
            {
                Debug.Log("Initialization failed.");
                throw new System.Exception("Could not initialize sunlight camera");
            }

            uniqueSummationShader.SetTexture(handleMain, "InputTexture", sunlightTexture, 0, UnityEngine.Rendering.RenderTextureSubElement.Color);
            uniqueSummationShader.SetBuffer(handleMain, "IdResultBuffer", sunlightSumBuffer);
            uniqueSummationShader.SetBuffer(handleInitialize, "IdResultBuffer", sunlightSumBuffer);

            uniqueSunlightAssignments = new NativeDisposableHotSwap<NativeArrayNativeDisposableAdapter<uint>>();
        }

        private void Update()
        {
            LazyEnsureUpdatedReadback();
        }

        private void LateUpdate()
        {
            LazyEnsureUpdatedReadback();
        }

        private void OnDestroy()
        {
            readbackRequest.Value.WaitForCompletion();
            sunlightSumBuffer.Dispose();
            uniqueSunlightAssignments.Dispose();
        }

        private void LazyEnsureUpdatedReadback()
        {
            if (readbackRequest.HasValue && readbackRequest.Value.done)
            {
                CompleteGPUReadback();
                readbackRequest = null;
            }
            if (readbackRequest == null)
            {
                ReallocateIdResultBufferIfNecessary();
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
            uniqueSummationShader.Dispatch(handleInitialize, (sunlightSumBuffer.count + 63) / 64, 1, 1);

            // divided by 8 in x and y because of [numthreads(8,8,1)] in the compute shader code
            uniqueSummationShader.Dispatch(handleMain, (sunlightTexture.width + 7) / 8, (sunlightTexture.height + 7) / 8, 1);

            readbackRequest = AsyncGPUReadback.Request(sunlightSumBuffer);
            UnityEngine.Profiling.Profiler.EndSample();
        }

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

            var reAllocatedNativeData = new NativeArray<uint>(nativeIdData, Allocator.TempJob);

            uniqueSunlightAssignments.AssignPending(reAllocatedNativeData);
            uniqueSunlightAssignments.HotSwapToPending();
            UnityEngine.Profiling.Profiler.EndSample();
        }


        /// <summary>
        /// assumes that the compute shader is complete and there are no pending readback requests
        /// </summary>
        private void ReallocateIdResultBufferIfNecessary()
        {
            // inspect the data needs from all l-systems. if it will need above the compute resize threshold,
            //  indicate a need to resize the buffer after this update completes
            var newSize = GlobalLSystemCoordinator.instance.ResizeLSystemReservations();
            var nextAllocationSize = this.sunlightSumBuffer.count;
            while(newSize > nextAllocationSize * computeBufferResizeThreshold)
            {
                nextAllocationSize = (int)(nextAllocationSize * computeBufferResizeMultiplier);
            }
            if (nextAllocationSize == sunlightSumBuffer.count)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight buffer resize");
            Debug.Log($"Resizing sunlight buffer from {sunlightSumBuffer.count} to {nextAllocationSize}");

            sunlightSumBuffer = new ComputeBuffer(nextAllocationSize, sizeof(uint));
            uniqueSummationShader.SetBuffer(handleMain, "IdResultBuffer", sunlightSumBuffer);
            uniqueSummationShader.SetBuffer(handleInitialize, "IdResultBuffer", sunlightSumBuffer);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public JobHandle ApplySunlightToSymbols(
            LSystemState<float> systemState,
            CustomRuleSymbols customSymbols,
            int openBranchSymbol,
            int closeBranchSymbol)
        {

            var idsNativeArray = uniqueSunlightAssignments.ActiveData;
            if (idsNativeArray == null)
            {
                Debug.LogError("no sunlight data available");
                return default;
            }
            if (idsNativeArray.IsDisposed)
            {
                Debug.LogError("sunlight data has been disposed already");
                return default;
            }

            UnityEngine.Profiling.Profiler.BeginSample("Sunlight result apply");

            var tmpIdentityStack = new TmpNativeStack<SunlightExposurePreProcessRule.BranchIdentity>(10, Allocator.TempJob);
            var applyJob = new SunlightExposurePreProcessRule
            {
                symbols = systemState.currentSymbols.Data,
                organCountsByIndex = idsNativeArray.Data,
                lastIdentityStack = tmpIdentityStack,

                sunlightPerPixel = sunlightPerPixel,
                branchOpen = openBranchSymbol,
                branchClose = closeBranchSymbol,
                customSymbols = customSymbols,
            };

            var dependency = applyJob.Schedule();
            systemState.currentSymbols.RegisterDependencyOnData(dependency);
            idsNativeArray.RegisterDependencyOnData(dependency);

            dependency = tmpIdentityStack.Dispose(dependency);

            UnityEngine.Profiling.Profiler.EndSample();
            return dependency;
        }
    }
}
