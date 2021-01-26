using Dman.LSystem;
using NUnit.Framework;
using System;

public class BasicRuleTests
{
    [Test]
    public void ParsedRuleParsesStringDefinition()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> AB");

        Assert.AreEqual("A", ruleFromString.targetSymbols.ToStringFromChars());
        Assert.AreEqual("AB".ToIntArray(), ruleFromString.replacementSymbols);
    }
    [Test]
    public void ParsedRuleParsesStringDefinitionWithNovelCharacters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> F-[[X]+X]+F[+FX]-X");

        Assert.AreEqual("A", ruleFromString.targetSymbols.ToStringFromChars());
        Assert.AreEqual("F-[[X]+X]+F[+FX]-X".ToIntArray(), ruleFromString.replacementSymbols);
    }
    [Test]
    public void BasicRuleRejectsIfAnyParameters()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A -> AB"));
        var paramArray = new float[1][];
        paramArray[0] = null;
        Assert.IsNotNull(ruleFromString.ApplyRule(new ArraySegment<float[]>(paramArray, 0, 1), null));
        paramArray[0] = new float[0];
        Assert.IsNotNull(ruleFromString.ApplyRule(new ArraySegment<float[]>(paramArray, 0, 1), null));
        paramArray[0] = new float[] { 1 };
        Assert.IsNull(ruleFromString.ApplyRule(new ArraySegment<float[]>(paramArray, 0, 1), null));
    }
    [Test]
    public void BasicRuleReplacesSelfWithReplacement()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A -> AB"));

        var paramArray = new float[1][];
        paramArray[0] = null;
        var replacement = ruleFromString.ApplyRule(new ArraySegment<float[]>(paramArray, 0, 1), null);
        Assert.AreEqual("AB".ToIntArray(), replacement.symbols);
        Assert.AreEqual(new float[2][], replacement.parameters);
    }
}
