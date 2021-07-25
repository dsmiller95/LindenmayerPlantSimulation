namespace Dman.LSystem.SystemRuntime.CustomRules
{
    public struct CustomRuleSymbols
    {
        public bool hasDiffusion;
        public bool independentDiffusionUpdate;
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
