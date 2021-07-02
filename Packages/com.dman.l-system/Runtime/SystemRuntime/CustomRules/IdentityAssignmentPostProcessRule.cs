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

        public CustomRuleSymbols customSymbols;

        public void Execute()
        {
            if (customSymbols.hasIdentifiers)
            {
                var persistentOrganIdentityIndex = new UIntFloatColor32
                {
                    UIntValue = 1
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
            }
        }
    }

}
