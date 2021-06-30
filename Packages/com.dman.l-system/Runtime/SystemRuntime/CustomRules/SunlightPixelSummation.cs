using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules
{
    [BurstCompile]
    struct SunlightPixelSummation : IJobParallelFor
    {
        public NativeArray<uint> allOrganIds;
        public NativeHashMap<ThreadTypeId, uint> organIdCountsPerThread;

        [NativeSetThreadIndex]
        int threadIndex;

        public void Execute(int i)
        {
            var organId = BitMixer.UnMix(allOrganIds[i]);

            if(organId == 0)
            {
                return;
            }
            var idOnThread = new ThreadTypeId
            {
                organId = organId,
                threadId = threadIndex
            };
            if(!organIdCountsPerThread.TryGetValue(idOnThread, out var count))
            {
                count = 0;
            }
            organIdCountsPerThread[idOnThread] = count + 1;
        }
    }
    [BurstCompile]
    struct SunlightPixelJobsCombination : IJob
    {
        public NativeHashMap<ThreadTypeId, uint>.Enumerator organIdCountsPerThreadEnumerator;
        public NativeHashMap<uint, uint> organIdCounts;

        public void Execute()
        {
            while (organIdCountsPerThreadEnumerator.MoveNext())
            {
                var entry = organIdCountsPerThreadEnumerator.Current;

                var organId = entry.Key.organId;
                var organCount = entry.Value;

                if (!organIdCounts.TryGetValue(organId, out var count))
                {
                    count = 0;
                }
                organIdCounts[organId] = count + organCount;
            }
            organIdCountsPerThreadEnumerator.Dispose();
        }
    }
    public struct ThreadTypeId : IEquatable<ThreadTypeId>
    {
        public uint organId;
        public int threadId;

        public bool Equals(ThreadTypeId other)
        {
            return other.organId == organId && other.threadId == threadId;
        }

        public override int GetHashCode()
        {
            return (int)organId << 32 ^ threadId;
        }
    }

    [BurstCompile]
    struct SunlightExposureApplyJob: IJob
    {
        public SymbolString<float> symbols;
        [ReadOnly]
        public NativeHashMap<uint, uint> organIdCounts;
        public TmpNativeStack<BranchIdentity> lastIdentityStack;

        public float sunlightPerPixel;
        public int branchOpen;
        public int branchClose;
        public CustomRuleSymbols customSymbols;

        public void Execute()
        {
            var branchIdentity = new BranchIdentity
            {
                identity = 0,
                appliedSunlight = false
            };
            for (int symbolIndex = 0; symbolIndex < symbols.Length; symbolIndex++)
            {
                var symbol = symbols[symbolIndex];
                if (symbol == customSymbols.identifier)
                {
                    var identityBits = new UIntFloatColor32(symbols.parameters[symbolIndex, 0]);
                    branchIdentity = new BranchIdentity(identityBits.UIntValue);
                }
                else if (symbol == branchOpen)
                {
                    lastIdentityStack.Push(branchIdentity);
                }
                else if (symbol == branchClose)
                {
                    branchIdentity = lastIdentityStack.Pop();
                }
                else if (symbol == customSymbols.sunlightSymbol)
                {
                    if (branchIdentity.identity <= 0)
                    {
                        continue;
                    }
                    if (!organIdCounts.TryGetValue(branchIdentity.identity, out var pixelCount))
                    {
                        pixelCount = 0;
                    }
                    var sunlightAmount = sunlightPerPixel * pixelCount;
                    symbols.parameters[symbolIndex, 0] = sunlightAmount;
                    branchIdentity.appliedSunlight = true;
                }
            }
        }

        public struct BranchIdentity
        {
            public uint identity;
            public bool appliedSunlight;

            public BranchIdentity(uint identity)
            {
                this.identity = identity;
                appliedSunlight = false;
            }
        }
    }

}
