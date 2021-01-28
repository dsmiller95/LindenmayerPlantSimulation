using Dman.LSystem;
using NUnit.Framework;
using System;

public class RuleParserTests
{
    [Test]
    public void ParsedRuleParsesStringDefinition()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> AB");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("AB", ruleFromString.replacementSymbols.ToStringFromChars());
    }
    [Test]
    public void ParsedRuleParsesStringDefinitionWithNovelCharacters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> F-[[X]+X]+F[+FX]-X");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("F-[[X]+X]+F[+FX]-X", ruleFromString.replacementSymbols.ToStringFromChars());
    }
    [Test]
    public void ParsesRuleWithMultipleCharacterMatch()
    {
        var ruleFromString = ParsedRule.ParseToRule("AB -> A");

        Assert.AreEqual("AB", ruleFromString.TargetSymbolString());
        Assert.AreEqual("A", ruleFromString.replacementSymbols.ToStringFromChars());
    }
    [Test]
    public void ParsesRuleWithInputParameters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A(x, y) -> B");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual("B", ruleFromString.replacementSymbols.ToStringFromChars());
    }
    [Test]
    public void ParsesRuleWithMultipleCharactersAndParameters()
    {
        var ruleFromString = ParsedRule.ParseToRule("B(x)A(x, y) -> B");

        Assert.AreEqual("B(x)A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual("B", ruleFromString.replacementSymbols.ToStringFromChars());
    }
}
