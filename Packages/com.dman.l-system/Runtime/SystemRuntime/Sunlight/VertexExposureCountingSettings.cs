using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Sunlight
{
    [CreateAssetMenu(fileName = "VertexExposureCountingSettings", menuName = "LSystem/Config/VertexExposureCountingSettings")]
    public class VertexExposureCountingSettings : ScriptableObject
    {
        public ComputeShader uniqueSummationShader;
        [Tooltip("The usage percentage of the current compute buffer which will trigger a buffer resize")]
        [Range(0, 1)]
        [SerializeField]
        public float computerBufferResizeThreshold = 0.9f;
        [Tooltip("The multiplier used to increase the size of the compute buffer on each resize event")]
        [SerializeField]
        public float computeBufferResizeMultiplier = 2f;
        public int defaultUniqueAllocationSize = 4096;
    }
}
