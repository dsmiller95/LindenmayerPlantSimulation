using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Collections;
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
               null == sunlightSumBuffer)
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
            uniqueSummationShader.Dispatch(handleInitialize, sunlightSumBuffer.count / 64, 1, 1);

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

    }
}
