using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System;
using System.Linq;

public class BasicRuleTests
{
    [Test]
    public void BasicRuleRejectsApplicationIfAnyParameters()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A -> AB"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][]{ null });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        Assert.IsNotNull(ruleFromString.ApplyRule(branchCache, symbols, 0, ref random));
        symbols.parameters[0] = new double[0];
        Assert.IsNotNull(ruleFromString.ApplyRule(branchCache, symbols, 0, ref random));
        symbols.parameters[0] = new double[] { 1 };
        Assert.IsNull(ruleFromString.ApplyRule(branchCache, symbols, 0, ref random));
    }
    [Test]
    public void BasicRuleReplacesSelfWithReplacement()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A -> AB"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { null });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
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
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
        Assert.AreEqual("B(21)C(20)A(1, 20)", replacement.ToString());
    }
    [Test]
    public void BasicRuleDifferentParametersNoMatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x, y) -> B(y + x)C(x)A(y, x)"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 20 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ParametricConditionalNoMatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x) : x < 10 -> A(x + 1)"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 20 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ParametricConditionalMatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x) : x < 10 -> A(x + 1)"));
        var symbols = new SymbolString<double>(new int[] { 'A' }, new double[][] { new double[] { 6 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
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
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, 
            symbols, 0,
            ref random,
            new double[] { 7d });
        Assert.AreEqual("B(27)C(1)", replacement.ToString());
    }
    [Test]
    public void ContextualRuleRequiresContextToMatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("B < A > B -> B"));
        var symbols = new SymbolString<double>("BAABAB".Select(x => (int)x).ToArray(), new double[][] { new double[] { 20 }, new double[0] { }, new double[0] { }, new double[0] { }, new double[0] { }, new double[0] { } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 1, ref random);
        Assert.IsNull(replacement);
        replacement = ruleFromString.ApplyRule(branchCache, symbols, 4, ref random);
        Assert.IsNotNull(replacement);
        Assert.AreEqual("B", replacement.ToString());
    }
    [Test]
    public void ContextualRuleCapturesParametersFromSuffix()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x) > B(y) -> B(x - y)"));
        var symbols = new SymbolString<double>("AB".Select(x => (int)x).ToArray(), new double[][] { new[] { 20.0 }, new[] { 3.1 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
        Assert.IsNotNull(replacement);
        Assert.AreEqual("B(16.9)", replacement.ToString());
    }
    [Test]
    public void ContextualRuleCapturesParametersFromPrefix()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("B(x) < A(y) -> B(x - y)"));
        var symbols = new SymbolString<double>("BA".Select(x => (int)x).ToArray(), new double[][] { new[] { 20.0 }, new[] { 3.1 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 1, ref random);
        Assert.IsNotNull(replacement);
        Assert.AreEqual("B(16.9)", replacement.ToString());
    }
    [Test]
    public void ContextualRuleCapturesParametersFromPrefixAndSuffix()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("B(x) < A(y) > B(z) -> B((x - y)/z)"));
        var symbols = new SymbolString<double>("BAB".Select(x => (int)x).ToArray(), new double[][] { new[] { 20.0 }, new[] { 3.1 }, new[] { 2.0 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 1, ref random);
        Assert.IsNotNull(replacement);
        Assert.AreEqual("B(8.45)", replacement.ToString());
    }

    [Test]
    public void ContextualRulePrefixNoMatchWhenParameterMismatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("B(x) < A(y) -> B(x - y)"));
        var symbols = new SymbolString<double>("BA".Select(x => (int)x).ToArray(), new double[][] { new double[0], new[] { 3.1 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 1, ref random);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ContextualRuleSuffixNoMatchWhenParameterMismatch()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x) > B(y) -> B(x - y)"));
        var symbols = new SymbolString<double>("AB".Select(x => (int)x).ToArray(), new double[][] { new[] { 3.1 }, new double[0] });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
        Assert.IsNull(replacement);
    }
    [Test]
    public void ContextualRuleSuffixFindsMatchBySkipIfPossible()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A(x) > B(y) -> B(x - y)"));
        var symbols = new SymbolString<double>("A[B]B".Select(x => (int)x).ToArray(), new double[][] { new[] { 3.1 }, new double[0], new double[0], new double[0], new[] { 20.0 } });
        var branchCache = new SymbolStringBranchingCache();
        branchCache.SetTargetSymbolString(symbols);

        var random = new Unity.Mathematics.Random();
        var replacement = ruleFromString.ApplyRule(branchCache, symbols, 0, ref random);
        Assert.IsNotNull(replacement);
        Assert.AreEqual("B(-16.9)", replacement.ToString());
    }
}
