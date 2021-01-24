using Dman.LSystem;
using NUnit.Framework;

public class BasicRuleTests
{
    [Test]
    public void BasicRuleParsesStringDefinition()
    {
        var ruleFromString = new BasicRule("A -> AB");

        Assert.AreEqual((int)'A', ruleFromString.TargetSymbol);
        Assert.AreEqual("AB".ToIntArray(), ruleFromString.replacementSymbols);
    }
    [Test]
    public void BasicRuleParsesStringDefinitionWithNovelCharacters()
    {
        var ruleFromString = new BasicRule("A -> F-[[X]+X]+F[+FX]-X");

        Assert.AreEqual((int)'A', ruleFromString.TargetSymbol);
        Assert.AreEqual("F-[[X]+X]+F[+FX]-X".ToIntArray(), ruleFromString.replacementSymbols);
    }
    [Test]
    public void BasicRuleRejectsIfAnyParameters()
    {
        var ruleFromString = new BasicRule("A -> AB");
        Assert.IsNull(ruleFromString.ApplyRule(new float[1]));
        Assert.IsNotNull(ruleFromString.ApplyRule(new float[0]));
        Assert.IsNotNull(ruleFromString.ApplyRule(null));
    }
    [Test]
    public void BasicRuleReplacesSelfWithReplacement()
    {
        var ruleFromString = new BasicRule("A -> AB");

        var replacement = ruleFromString.ApplyRule(null);
        Assert.AreEqual("AB".ToIntArray(), replacement.symbols);
        Assert.AreEqual(new float[2][], replacement.parameters);
    }
}
