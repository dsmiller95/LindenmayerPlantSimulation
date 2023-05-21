using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Dman.LSystem.Extern.Adapters
{
    public static class NativeDiffusion
    {
        public static bool ParallelDiffusion(
            SymbolStringInterop sourceData,
            SymbolStringInteropMut targetData,
            NativeArray<LSystemSingleSymbolMatchData> matchSingletonData,
            int diffusion_node_symbol, int diffusion_amount_symbol, int branch_open_symbol, int branch_close_symbol,
            int diffusion_steps, float _diffusion_global_multiplier
        )
        {
            unsafe
            {
                using var sourceDataInterop =
                    new NativeArray<SymbolStringInterop>(1, Allocator.Temp, NativeArrayOptions.ClearMemory)
                    {
                        [0] = sourceData
                    };
                var sourceDataPointer = (SymbolStringInterop*)sourceDataInterop.GetUnsafePtr();

                using var targetDataInterop =
                    new NativeArray<SymbolStringInteropMut>(1, Allocator.Temp, NativeArrayOptions.ClearMemory)
                    {
                        [0] = targetData
                    };
                var targetDataPointer = (SymbolStringInteropMut*)targetDataInterop.GetUnsafePtr();

                using var singletonInterops =
                    new NativeArray<NativeArrayInteropLSystemSingleSymbolMatchData>(1, Allocator.Temp,
                        NativeArrayOptions.ClearMemory)
                    {
                        [0] = new NativeArrayInteropLSystemSingleSymbolMatchData(matchSingletonData)
                    };
                var singletonDataPointer = (NativeArrayInteropLSystemSingleSymbolMatchData*)singletonInterops.GetUnsafePtr();
                
                var result = SystemRuntimeRust.perform_parallel_diffusion(
                    sourceDataPointer,
                    targetDataPointer,
                    singletonDataPointer,
                    diffusion_node_symbol,
                    diffusion_amount_symbol,
                    branch_open_symbol,
                    branch_close_symbol,
                    diffusion_steps,
                    _diffusion_global_multiplier
                );

                return result;
            }
        }
        public static bool InPlaceDiffusion(
            SymbolStringInteropMut sourceData,
            int diffusion_node_symbol, int diffusion_amount_symbol, int branch_open_symbol, int branch_close_symbol,
            int diffusion_steps, float _diffusion_global_multiplier
        )
        {
            unsafe
            {
                using var sourceDataInterop =
                    new NativeArray<SymbolStringInteropMut>(1, Allocator.Temp, NativeArrayOptions.ClearMemory)
                    {
                        [0] = sourceData
                    };
                var sourceDataPointer = (SymbolStringInteropMut*)sourceDataInterop.GetUnsafePtr();

                var result = SystemRuntimeRust.perform_in_place_diffusion(
                    sourceDataPointer,
                    diffusion_node_symbol,
                    diffusion_amount_symbol,
                    branch_open_symbol,
                    branch_close_symbol,
                    diffusion_steps,
                    _diffusion_global_multiplier
                );

                return result;
            }
        }
    }
}