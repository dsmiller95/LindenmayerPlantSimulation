using Dman.LSystem;
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
        Assert.AreEqual("AB".ToIntArray(), replacement.symbols);
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
        Assert.AreEqual("BCA".ToIntArray(), replacement.symbols);
        var expectedParameters = new double[][]
        {
            new double[]{ 21},
            new double[]{ 20},
            new double[]{ 1, 20}
        };
        Assert.AreEqual(expectedParameters, replacement.parameters);
    }
}
