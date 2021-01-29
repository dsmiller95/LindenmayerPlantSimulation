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
}
