using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System;

public class BasicRuleTests
{
    [Test]
    public void BasicRuleRejectsApplicationIfAnyParameters()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A -> AB"));
        var paramArray = new double[1][];
        paramArray[0] = null;
        Assert.IsNotNull(ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null));
        paramArray[0] = new double[0];
        Assert.IsNotNull(ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null));
        paramArray[0] = new double[] { 1 };
        Assert.IsNull(ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null));
    }
    [Test]
    public void BasicRuleReplacesSelfWithReplacement()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A -> AB"));

        var paramArray = new double[1][];
        paramArray[0] = null;
        var replacement = ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null);
        Assert.AreEqual("AB", replacement.ToString());
        var expectedParameters = new double[][]
        {
            new double[0],
            new double[0]
        };
        Assert.AreEqual(expectedParameters, replacement.parameters);
    }
    [Test]
    public void BasicRuleReplacesParameters()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A(x, y) -> B(y + x)C(x)A(y, x)"));

        var paramArray = new double[][]
        {
            new double[] {20, 1 }
        };
        var replacement = ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null);
        Assert.AreEqual("B(21)C(20)A(1, 20)", replacement.ToString());
    }
    [Test]
    public void BasicRuleDifferentParametersNoMatch()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A(x, y) -> B(y + x)C(x)A(y, x)"));

        var paramArray = new double[][]
        {
            new double[] {20}
        };
        var replacement = ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ParametricConditionalNoMatch()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A(x) : x < 10 -> A(x + 1)"));

        var paramArray = new double[][]
        {
            new double[] {20}
        };
        var replacement = ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ParametricConditionalMatch()
    {
        var ruleFromString = new BasicRule(ParsedRule.ParseToRule("A(x) : x < 10 -> A(x + 1)"));

        var paramArray = new double[][]
        {
            new double[] {6}
        };
        var replacement = ruleFromString.ApplyRule(new ArraySegment<double[]>(paramArray, 0, 1), null);
        Assert.IsNotNull(replacement);
        Assert.AreEqual("A(7)", replacement.ToString());
    }
    [Test]
    public void BasicRuleReplacesParametersAndGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        var ruleFromString = new BasicRule(
            ParsedRule.ParseToRule("A(x, y) -> B(global + x)C(y)",
            globalParameters));
        var paramArray = new double[][]
        {
            new double[] {20, 1 }
        };

        var replacement = ruleFromString.ApplyRule(
            new ArraySegment<double[]>(paramArray, 0, 1),
            null,
            new double[] { 7d });
        Assert.AreEqual("B(27)C(1)", replacement.ToString());
    }
}
