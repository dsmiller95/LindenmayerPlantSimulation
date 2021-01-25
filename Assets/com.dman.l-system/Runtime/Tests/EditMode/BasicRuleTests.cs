using Dman.LSystem;
using NUnit.Framework;

public class BasicRuleTests
{
    [Test]
    public void ParsedRuleParsesStringDefinition()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> AB");

        Assert.AreEqual((int)'A', ruleFromString.targetSymbol);
        Assert.AreEqual("AB".ToIntArray(), ruleFromString.replacementSymbols);
    }
    [Test]
    public void ParsedRuleParsesStringDefinitionWithNovelCharacters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> F-[[X]+X]+F[+FX]-X");

        Assert.AreEqual((int)'A', ruleFromString.targetSymbol);
        Assert.AreEqual("F-[[X]+X]+F[+FX]-X".ToIntArray(), ruleFromString.replacementSymbols);
    }
    [Test]
    public void BasicRuleRejectsIfAnyParameters()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A -> AB"));
        Assert.IsNull(ruleFromString.ApplyRule(new float[1], null));
        Assert.IsNotNull(ruleFromString.ApplyRule(new float[0], null));
        Assert.IsNotNull(ruleFromString.ApplyRule(null, null));
    }
    [Test]
    public void BasicRuleReplacesSelfWithReplacement()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A -> AB"));

        var replacement = ruleFromString.ApplyRule(null, null);
        Assert.AreEqual("AB".ToIntArray(), replacement.symbols);
        Assert.AreEqual(new float[2][], replacement.parameters);
    }
}
