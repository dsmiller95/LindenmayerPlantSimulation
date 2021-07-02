//using Dman.LSystem.SystemRuntime.NativeCollections;
//using Dman.LSystem.SystemRuntime.Turtle;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;

//namespace Dman.LSystem.SystemRuntime.CustomRules
//{
//    /// <summary>
//    /// will make modifications to the symbols themselves.
//    /// 
//    /// replaces every symbol following the autophagy indicator with the dead symbol, to be removed on the next update.
//    /// requires exclusive access to the Target symbols
//    /// </summary>
//    [BurstCompile]
//    struct AutophagyPostProcess : IJob
//    {
//        public SymbolString<float> symbols;
//        [ReadOnly]
//        public NativeHashMap<uint, uint> organIdCounts;
//        public TmpNativeStack<BranchIdentity> lastIdentityStack;

//        public int branchOpen;
//        public int branchClose;
//        public CustomRuleSymbols customSymbols;

//        public void Execute()
//        {
//            var branchIdentity = new BranchIdentity
//            {
//                identity = 0,
//                appliedSunlight = false
//            };
//            for (int symbolIndex = 0; symbolIndex < symbols.Length; symbolIndex++)
//            {
//                var symbol = symbols[symbolIndex];
//                if (symbol == customSymbols.identifier)
//                {
//                    var identityBits = new UIntFloatColor32(symbols.parameters[symbolIndex, 0]);
//                    branchIdentity = new BranchIdentity(identityBits.UIntValue);
//                }
//                else if (symbol == branchOpen)
//                {
//                    lastIdentityStack.Push(branchIdentity);
//                }
//                else if (symbol == branchClose)
//                {
//                    branchIdentity = lastIdentityStack.Pop();
//                }
//                else if (symbol == customSymbols.sunlightSymbol)
//                {
//                    if (branchIdentity.identity <= 0)
//                    {
//                        continue;
//                    }
//                    var mixedIdentity = BitMixer.Mix(branchIdentity.identity);
//                    if (!organIdCounts.TryGetValue(mixedIdentity, out var pixelCount))
//                    {
//                        pixelCount = 0;
//                    }
//                    var sunlightAmount = sunlightPerPixel * pixelCount;
//                    symbols.parameters[symbolIndex, 0] = sunlightAmount;
//                    branchIdentity.appliedSunlight = true;
//                }
//            }
//        }

//        public struct BranchIdentity
//        {
//            public uint identity;
//            public bool appliedSunlight;

//            public BranchIdentity(uint identity)
//            {
//                this.identity = identity;
//                appliedSunlight = false;
//            }
//        }
//    }

//}
