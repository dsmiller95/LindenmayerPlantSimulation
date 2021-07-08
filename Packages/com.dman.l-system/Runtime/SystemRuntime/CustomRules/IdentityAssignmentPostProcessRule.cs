using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using System.Runtime.InteropServices;
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
        public uint originOfUniqueIndexes;

        public void Execute()
        {
            if (!customSymbols.hasIdentifiers)
            {
                maxIdentityId[0] = 0;
                return;
            }
            
            var persistentOrganIdentityIndex = new UIntFloatColor32
            {
                UIntValue = originOfUniqueIndexes
            };

            for (int symbolIndex = 0; symbolIndex < targetData.Length; symbolIndex++)
            {
                var symbol = targetData[symbolIndex];
                if (symbol == customSymbols.identifier)
                {
                    targetData.parameters[symbolIndex, 0] = persistentOrganIdentityIndex.FloatValue;
                    persistentOrganIdentityIndex.UIntValue++;
                }
            }
            maxIdentityId[0] = persistentOrganIdentityIndex.UIntValue - originOfUniqueIndexes;
        }
    }

}
