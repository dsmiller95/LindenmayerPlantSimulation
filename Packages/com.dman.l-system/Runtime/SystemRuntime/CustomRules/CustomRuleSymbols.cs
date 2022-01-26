namespace Dman.LSystem.SystemRuntime.CustomRules
{
    public struct CustomRuleSymbols
    {
        public int branchOpenSymbol;
        public int branchCloseSymbol;

        public bool hasDiffusion;
        public bool independentDiffusionUpdate;
        /// <summary>
        /// this value can be modified during runtime to change how quickly things diffuse through the system globally.
        ///     can be useful in combination with other integrations as a way to speed up or slow down an l-system
        /// </summary>
        public float diffusionConstantRuntimeGlobalMultiplier;
        public int diffusionNode;
        public int diffusionAmount;
        public int diffusionStepsPerStep;

        public bool hasIdentifiers;
        public int identifier;

        public bool hasSunlight;
        public int sunlightSymbol;

        public bool hasAutophagy;
        public int autophagicSymbol;
        public int deadSymbol;
    }
}
