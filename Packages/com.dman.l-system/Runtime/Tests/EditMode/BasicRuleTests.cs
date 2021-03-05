using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System;

public class BasicRuleTests
{
    [Test]
    public void BasicRuleRejectsApplicationIfAnyParameters()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A -> AB"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][]{ null });
        var random = new Unity.Mathematics.Random();
        Assert.IsNotNull(ruleFromString.ApplyRule(symbols, 0, ref random));
        symbols.parameters[0] = new double[0];
        Assert.IsNotNull(ruleFromString.ApplyRule(symbols, 0, ref random));
        symbols.parameters[0] = new double[] { 1 };
        Assert.IsNull(ruleFromString.ApplyRule(symbols, 0, ref random));
    }
    [Test]
    public void BasicRuleReplacesSelfWithReplacement()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A -> AB"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { null });

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(symbols, 0, ref random);
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
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x, y) -> B(y + x)C(x)A(y, x)"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 20, 1 } });

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(symbols, 0, ref random);
        Assert.AreEqual("B(21)C(20)A(1, 20)", replacement.ToString());
    }
    [Test]
    public void BasicRuleDifferentParametersNoMatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x, y) -> B(y + x)C(x)A(y, x)"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 20 } });

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(symbols, 0, ref random);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ParametricConditionalNoMatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x) : x < 10 -> A(x + 1)"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 20 } });

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(symbols, 0, ref random);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ParametricConditionalMatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x) : x < 10 -> A(x + 1)"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 6 } });

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(symbols, 0, ref random);
        Assert.IsNotNull(replacement);
        Assert.AreEqual("A(7)", replacement.ToString());
    }
    [Test]
    public void BasicRuleReplacesParametersAndGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        var ruleFromString = new BasicRule(
            RuleParser.ParseToRule("A(x, y) -> B(global + x)C(y)",
            globalParameters));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 20, 1 } });

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(
            symbols, 0,
            ref random,
            new double[] { 7d });
        Assert.AreEqual("B(27)C(1)", replacement.ToString());
    }
}
