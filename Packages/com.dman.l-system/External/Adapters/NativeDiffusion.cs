using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Dman.LSystem.Extern.Adapters
{
    public static class NativeDiffusion
    {
        public static bool DiffuseBetween(
            NativeArray<DiffusionEdge> edges,
            NativeArray<DiffusionNode> nodes,
            NativeArray<float> capacities,
            NativeArray<float> amountListA,
            NativeArray<float> amountListB,
            int steps,
            float globalMultiplier)
        {
            unsafe
            {
                
                return SystemRuntimeRust.diffuse_between(
                    (DiffusionEdge*)edges.GetUnsafeReadOnlyPtr(),
                    edges.Length,
                    (DiffusionNode*)nodes.GetUnsafeReadOnlyPtr(),
                    (float*)capacities.GetUnsafeReadOnlyPtr(),
                    (float*)amountListA.GetUnsafeReadOnlyPtr(),
                    (float*)amountListB.GetUnsafeReadOnlyPtr(),
                    nodes.Length,
                    steps,
                    globalMultiplier
                    );
            }
        }
    }
}