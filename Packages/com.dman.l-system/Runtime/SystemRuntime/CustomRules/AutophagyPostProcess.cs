using Dman.LSystem.SystemRuntime.NativeCollections;
using Unity.Burst;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.CustomRules
{
    /// <summary>
    /// will make modifications to the symbols themselves.
    /// 
    /// replaces every symbol following the autophagy indicator with the dead symbol, to be removed on the next update.
    /// requires exclusive access to the Target symbols
    /// </summary>
    [BurstCompile]
    struct AutophagyPostProcess : IJob
    {
        public SymbolString<float> symbols;
        public TmpNativeStack<BranchIdentity> lastIdentityStack;

        public int branchOpen;
        public int branchClose;
        public CustomRuleSymbols customSymbols;

        public void Execute()
        {
            var branchIdentity = new BranchIdentity
            {
                isDead = false
            };
            for (int symbolIndex = 0; symbolIndex < symbols.Length; symbolIndex++)
            {
                var symbol = symbols[symbolIndex];

                if (symbol == branchOpen)
                {
                    lastIdentityStack.Push(branchIdentity);
                }
                else if (symbol == branchClose)
                {
                    branchIdentity = lastIdentityStack.Pop();
                }
                else if (symbol == customSymbols.autophagicSymbol)
                {
                    branchIdentity.isDead = true;
                }
                if (branchIdentity.isDead)
                {
                    symbols[symbolIndex] = customSymbols.deadSymbol;
                    symbols.parameters[symbolIndex] = new JaggedIndexing
                    {
                        index = symbols.parameters[symbolIndex].index,
                        length = 0
                    };
                }
            }
        }

        public struct BranchIdentity
        {
            public bool isDead;
            public BranchIdentity(bool isDead)
            {
                this.isDead = isDead;
            }
        }
    }

}
