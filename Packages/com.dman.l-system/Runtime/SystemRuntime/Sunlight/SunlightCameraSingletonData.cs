using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    public class SunlightCameraSingletonData : MonoBehaviour
    {
        private SunlightCamera ActiveCamera => GameObject.FindObjectOfType<SunlightCamera>(false);

        [SerializeField]
        private RenderTexture sunlightTexture;

        [SerializeField]
        private ComputeShader uniqueSummationShader;
        [SerializeField]
        private int uniqueOrgansInitialAllocation = 4096;

        [Tooltip("The multiplier used to increase the size of the compute buffer on each resize event")]
        [SerializeField]
        private float computeBufferResizeMultiplier = 2;
        [Tooltip("The usage percentage of the current compute buffer which will trigger a buffer resize")]
        [Range(0, 1)]
        [SerializeField]
        private float computeBufferResizeThreshold = 0.9f;


        private VertexColorExposureCounter exposureCalculator;

        private void Start()
        {
            exposureCalculator = new VertexColorExposureCounter(
                sunlightTexture,
                uniqueSummationShader,
                computeBufferResizeThreshold,
                computeBufferResizeMultiplier,
                uniqueOrgansInitialAllocation);
            exposureCalculator.Initialize();
        }

        private void Update()
        {
            // don't force the exposure buffer to resize;
            exposureCalculator.RefreshExposureData(0);
        }

        private void LateUpdate()
        {
            var newSize = GlobalLSystemCoordinator.instance.ResizeLSystemReservations();
            exposureCalculator.RefreshExposureData(newSize);
        }

        private void OnDestroy()
        {
            exposureCalculator.Dispose();
        }


        public JobHandle ApplySunlightToSymbols(
            LSystemState<float> systemState,
            CustomRuleSymbols customSymbols)
        {
            var activeSunlightCamera = ActiveCamera;
            if (activeSunlightCamera == null)
            {
                return default(JobHandle);
            }

            var idsNativeArray = exposureCalculator.GetReadAccessSunlightExposure();
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
            var sunlightPerPixel = activeSunlightCamera.sunlightPerPixel;
            var applyJob = new SunlightExposurePreProcessRule
            {
                symbols = systemState.currentSymbols.Data,
                organCountsByIndex = idsNativeArray.Data,
                lastIdentityStack = tmpIdentityStack,

                sunlightPerPixel = sunlightPerPixel,
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
