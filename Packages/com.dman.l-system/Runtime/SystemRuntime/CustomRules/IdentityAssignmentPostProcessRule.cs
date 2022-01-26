using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules
{
    /// <summary>
    /// Runs after all replacment has taken place on the target symbols
    /// </summary>
    [BurstCompile]
    struct IdentityAssignmentPostProcessRule : IJob
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction] // disable all safety to allow parallel writes
        public SymbolString<float> targetData;

        public NativeArray<uint> maxIdentityId;

        public CustomRuleSymbols customSymbols;
        public uint lastMaxIdReached;
        public uint uniquePlantId;
        public uint originOfUniqueIndexes;

        public void Execute()
        {
            if (!customSymbols.hasIdentifiers)
            {
                maxIdentityId[0] = 0;
                return;
            }

            for (int symbolIndex = 0; symbolIndex < targetData.Length; symbolIndex++)
            {
                var symbol = targetData[symbolIndex];
                if (symbol == customSymbols.identifier)
                {
                    uint currentId = (uint)targetData.parameters[symbolIndex, 1];
                    if (currentId == 0)
                    {
                        lastMaxIdReached++;
                        currentId = lastMaxIdReached;
                        targetData.parameters[symbolIndex, 1] = (float)currentId;
                        targetData.parameters[symbolIndex, 2] = uniquePlantId;
                    }
                    var updatedOrganId = new UIntFloatColor32(currentId + originOfUniqueIndexes);
                    targetData.parameters[symbolIndex, 0] = updatedOrganId.FloatValue;
                }
            }
            maxIdentityId[0] = lastMaxIdReached;
        }
    }

}
