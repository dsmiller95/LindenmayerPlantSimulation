using Dman.LSystem.SystemCompiler;
using NUnit.Framework;

public class RuleParserTests
{
    [Test]
    public void ParsedRuleParsesStringDefinition()
    {
        var ruleFromString = RuleParser.ParseToRule("A -> AB");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("AB", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsedRuleParsesStringDefinitionWithExoticCharacter()
    {
        var ruleFromString = RuleParser.ParseToRule("- -> AB");

        Assert.AreEqual("-", ruleFromString.TargetSymbolString());
        Assert.AreEqual("AB", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsedRuleParsesStringDefinitionWithNovelCharacters()
    {
        var ruleFromString = RuleParser.ParseToRule("A -> F-[[X]+X]+F[+FX]-X");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("F-[[X]+X]+F[+FX]-X", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithPrefixCharacterMatch()
    {
        var ruleFromString = RuleParser.ParseToRule("A < B -> A");

        Assert.AreEqual(1, ruleFromString.backwardsMatch.Length);
        Assert.AreEqual(0, ruleFromString.forwardsMatch.Length);
        Assert.AreEqual("A < B", ruleFromString.TargetSymbolString());
        Assert.AreEqual("A", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithSuffixCharacterMatch()
    {
        var ruleFromString = RuleParser.ParseToRule("A > B -> A");

        Assert.AreEqual(0, ruleFromString.backwardsMatch.Length);
        Assert.AreEqual(1, ruleFromString.forwardsMatch.Length);
        Assert.AreEqual("A > B", ruleFromString.TargetSymbolString());
        Assert.AreEqual("A", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithFullContextCharacterMatch()
    {
        var ruleFromString = RuleParser.ParseToRule("A < B > C -> A");

        Assert.AreEqual(1, ruleFromString.forwardsMatch.Length);
        Assert.AreEqual(1, ruleFromString.backwardsMatch.Length);
        Assert.AreEqual("A < B > C", ruleFromString.TargetSymbolString());
        Assert.AreEqual("A", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsedRuleProbability()
    {
        var ruleFromString = RuleParser.ParseToRule("P(0.5) | A -> AB");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("AB", ruleFromString.ReplacementSymbolString());

        Assert.IsAssignableFrom<ParsedStochasticRule>(ruleFromString);
        var stochasticRule = ruleFromString as ParsedStochasticRule;
        Assert.AreEqual(0.5, stochasticRule.probability);
    }
    [Test]
    public void ParsedRuleProbabilityFromExpression()
    {
        var ruleFromString = RuleParser.ParseToRule("P(0.5 - 0.3) | A -> AB");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("AB", ruleFromString.ReplacementSymbolString());

        Assert.IsAssignableFrom<ParsedStochasticRule>(ruleFromString);
        var stochasticRule = ruleFromString as ParsedStochasticRule;
        Assert.AreEqual(0.2f, stochasticRule.probability, 1e-6);
    }
    [Test]
    public void ParsesRuleWithInputParameters()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x, y) -> B");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual("B", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithPrefixContextAndParameters()
    {
        var ruleFromString = RuleParser.ParseToRule("B(x) > A(y, z) -> B");

        Assert.AreEqual(1, ruleFromString.forwardsMatch.Length);
        Assert.AreEqual(1, ruleFromString.coreSymbol.parameterLength);
        Assert.AreEqual(2, ruleFromString.forwardsMatch[0].parameterLength);

        Assert.AreEqual("B(x) > A(y, z)", ruleFromString.TargetSymbolString());
        Assert.AreEqual("B", ruleFromString.ReplacementSymbolString());
    }

    [Test]
    public void ParsesRuleWithFullContextParametersInOrder()
    {
        var ruleFromString = RuleParser.ParseToRule("C(x) < K(y) > A(z) -> D((timeToFruit - x) / (y -z))", new string[] { "timeToFruit" });

        Assert.AreEqual("C(x) < K(y) > A(z)", ruleFromString.TargetSymbolString());

        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual('D', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);

        var evaluatorFunction = ruleFromString.replacementSymbols[0].evaluators[0];
        Assert.AreEqual((10.0f - 25.0f) / (4.0f - 8.0f), (float)evaluatorFunction.DynamicInvoke(10, 25, 4, 8), 1e-3f);

        Assert.AreEqual((11.2f - 892) / (6.66f - 1.7f), (float)evaluatorFunction.DynamicInvoke(11.2f, 892, 6.66f, 1.7f), 1e-3f);
    }
    [Test]
    public void ParsesRuleWithParametersAndReplacementParameters()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x, y) -> B(y)");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual('B', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(7331, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(1337, 7331));
    }
    [Test]
    public void ParsesRuleWithParametersAndReplacementParametersAndComplexExpression()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x, y) -> B(y + (y - x) * y)");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual('B', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(4 + (4 - 30) * 4, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(30, 4));
    }
    [Test]
    public void ParsesRuleWithParametersAndMultipleReplacementParameters()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x, y) -> B(y + (y - x) * y)C(x)A(y, x)");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(3, ruleFromString.replacementSymbols.Length);

        Assert.AreEqual('B', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(4 + (4 - 30) * 4, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(30, 4));

        Assert.AreEqual('C', ruleFromString.replacementSymbols[1].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[1].evaluators.Length);
        Assert.AreEqual(30, ruleFromString.replacementSymbols[1].evaluators[0].DynamicInvoke(30, 4));

        Assert.AreEqual('A', ruleFromString.replacementSymbols[2].targetSymbol);
        Assert.AreEqual(2, ruleFromString.replacementSymbols[2].evaluators.Length);
        Assert.AreEqual(4, ruleFromString.replacementSymbols[2].evaluators[0].DynamicInvoke(30, 4));
        Assert.AreEqual(30, ruleFromString.replacementSymbols[2].evaluators[1].DynamicInvoke(30, 4));
    }
    [Test]
    public void ParsesRuleWithParametersAndConditionalMatch()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x, y): x < 10 -> A(x + 1, y - x)");

        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(11, 2) > 0);
        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(10, 2) > 0);
        Assert.AreEqual(true, ruleFromString.conditionalMatch.DynamicInvoke(9, 202) > 0);

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);

        Assert.AreEqual('A', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(2, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(5, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(4, 10));
        Assert.AreEqual(6, ruleFromString.replacementSymbols[0].evaluators[1].DynamicInvoke(4, 10));
    }
    [Test]
    public void ParsesRuleWithGlobalParametersMatch()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x) -> B(x + stretch, stretch)", new string[] { "stretch" });

        Assert.IsNull(ruleFromString.conditionalMatch);

        Assert.AreEqual("A(x)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);

        Assert.AreEqual('B', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(2, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(12, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(10, 2));
        Assert.AreEqual(10, ruleFromString.replacementSymbols[0].evaluators[1].DynamicInvoke(10, 2));
    }
    [Test]
    public void ParsesRuleWithNonAlphaContextMatchWithParameter()
    {
        var ruleFromString = RuleParser.ParseToRule("C(x) < K(y) > `A(z) : x >= timeToFruit -> D(1)", new string[] { "timeToFruit" });

        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(3, 0, 0, 0) > 0);
        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(3, 1, 0, 0) > 0);
        Assert.AreEqual(true, ruleFromString.conditionalMatch.DynamicInvoke(3, 4, 0, 0) > 0);

        Assert.AreEqual("C(x) < K(y) > `A(z)", ruleFromString.TargetSymbolString());

        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual('D', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(10, 10, 10, 10));
    }

    [Test]
    public void RuleWithNoReplacementValidZeroLengthReplacement()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x) ->");

        Assert.AreEqual("A(x)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(0, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual("", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void RuleWithProbabilityConditionalParses()
    {
        var ruleFromString = RuleParser.ParseToRule("P(0.5) | A(x) : x < global -> A(x + 1)", new string[] { "global" });

        Assert.IsInstanceOf<ParsedStochasticRule>(ruleFromString);

        var stochastic = ruleFromString as ParsedStochasticRule;

        Assert.AreEqual(0.5, stochastic.probability);

        Assert.AreEqual("A(x)", stochastic.TargetSymbolString());
        Assert.AreEqual(1, stochastic.replacementSymbols.Length);
        Assert.AreEqual('A', stochastic.replacementSymbols[0].targetSymbol);

        Assert.AreEqual(true, ruleFromString.conditionalMatch.DynamicInvoke(3, 2) > 0);
        Assert.AreEqual(true, ruleFromString.conditionalMatch.DynamicInvoke(2.5f, 2) > 0);
        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(2, 3) > 0);
    }

    [Test]
    public void ParsesRuleWithEverySyntax()
    {
        var ruleFromString = RuleParser.ParseToRule("P(0.8 - (1/2)) | A < B > C(y) : y < global -> A", new string[] { "global" });

        Assert.AreEqual(1, ruleFromString.forwardsMatch.Length);
        Assert.AreEqual(1, ruleFromString.backwardsMatch.Length);
        Assert.AreEqual("A < B > C(y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual("A", ruleFromString.ReplacementSymbolString());

        Assert.IsInstanceOf<ParsedStochasticRule>(ruleFromString);
        var stochastic = ruleFromString as ParsedStochasticRule;

        Assert.AreEqual(0.3f, stochastic.probability, 1e-5);

        Assert.AreEqual(true, ruleFromString.conditionalMatch.DynamicInvoke(3, 2) > 0);
        Assert.AreEqual(true, ruleFromString.conditionalMatch.DynamicInvoke(2.5f, 2) > 0);
        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(2, 3) > 0);
    }


    [Test]
    public void ParsesRuleWithExtraParens()
    {
        var ruleFromString = RuleParser.ParseToRule("A(x) -> A((x + 1))");

        Assert.AreEqual(0, ruleFromString.forwardsMatch.Length);
        Assert.AreEqual(0, ruleFromString.backwardsMatch.Length);
        Assert.AreEqual("A(x)", ruleFromString.TargetSymbolString());


        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual('A', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(2, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(1));
        Assert.AreEqual(11, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(10));
    }

    #region Meaningful Exceptions
    [Test]
    public void RuleWithMissingParameterThrowsMeaningfulException()
    {
        var ruleString = "A(x) -> B(x, yeet)";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(13, e.errorStartIndex);
            Assert.AreEqual(17, e.ErrorEndIndex);
            Assert.AreEqual(ruleString, e.ruleText);
            Assert.IsTrue(e.Message.Contains("\"yeet\""), "Should contain the missing parameter name");
        }
    }
    [Test]
    public void RuleWithOneTooFewParensThrowsMeaninfulException()
    {
        var ruleString = "A(x) -> B(x + (y)";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(17, e.errorStartIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    [Test]
    public void RuleWith3ConsecutiveExpressionOperatorsThrowsMeaninfulException()
    {
        var ruleString = "A(x, y) -> B(x + / * y)";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(19, e.errorStartIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    [Test]
    public void RuleWithInvalidUnaryOperatorThrowsMeaninfulException()
    {
        var ruleString = "A(x, y) -> B(x + / y)";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(17, e.errorStartIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    [Test]
    public void RuleWithStrandedOperatorThrowsMeaninfulException()
    {
        var ruleString = "A(x, y) -> B(+)";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(13, e.errorStartIndex);
            Assert.AreEqual(14, e.ErrorEndIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    [Test]
    public void RuleWithEmptyParensThrowsMeaninfulException()
    {
        var ruleString = "A(x, y) -> B()";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(12, e.errorStartIndex);
            Assert.AreEqual(14, e.ErrorEndIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    [Test]
    public void RuleWithExtraRightParenThrowsMeaninfulException()
    {
        var ruleString = "A(x, y) -> B(x))";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(15, e.errorStartIndex);
            Assert.AreEqual(16, e.ErrorEndIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    [Test]
    public void RuleWithInvalidParameterInConditionalThrowsMeaningfulException()
    {
        var ruleString = "A(x, y) : x >= e -> B(x)";
        try
        {
            RuleParser.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(15, e.errorStartIndex);
            Assert.AreEqual(16, e.ErrorEndIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    #endregion
}
