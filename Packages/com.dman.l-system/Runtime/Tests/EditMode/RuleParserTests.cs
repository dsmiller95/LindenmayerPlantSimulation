using Dman.LSystem.SystemCompiler;
using NUnit.Framework;

public class RuleParserTests
{
    [Test]
    public void ParsedRuleParsesStringDefinition()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> AB");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("AB", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsedRuleParsesStringDefinitionWithNovelCharacters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A -> F-[[X]+X]+F[+FX]-X");

        Assert.AreEqual("A", ruleFromString.TargetSymbolString());
        Assert.AreEqual("F-[[X]+X]+F[+FX]-X", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithMultipleCharacterMatch()
    {
        var ruleFromString = ParsedRule.ParseToRule("AB -> A");

        Assert.AreEqual("AB", ruleFromString.TargetSymbolString());
        Assert.AreEqual("A", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithInputParameters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A(x, y) -> B");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual("B", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithMultipleCharactersAndParameters()
    {
        var ruleFromString = ParsedRule.ParseToRule("B(x)A(x, y) -> B");

        Assert.AreEqual("B(x)A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual("B", ruleFromString.ReplacementSymbolString());
    }
    [Test]
    public void ParsesRuleWithParametersAndReplacementParameters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A(x, y) -> B(y)");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual('B', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(7331, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(1337, 7331));
    }
    [Test]
    public void ParsesRuleWithParametersAndReplacementParametersAndComplexExpression()
    {
        var ruleFromString = ParsedRule.ParseToRule("A(x, y) -> B(y + (y - x) * y)");

        Assert.AreEqual("A(x, y)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);
        Assert.AreEqual('B', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(1, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(4 + (4 - 30) * 4, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(30, 4));
    }
    [Test]
    public void ParsesRuleWithParametersAndMultipleReplacementParameters()
    {
        var ruleFromString = ParsedRule.ParseToRule("A(x, y) -> B(y + (y - x) * y)C(x)A(y, x)");

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
        var ruleFromString = ParsedRule.ParseToRule("A(x, y): x < 10 -> A(x + 1, y - x)");

        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(11, 2));
        Assert.AreEqual(false, ruleFromString.conditionalMatch.DynamicInvoke(10, 2));
        Assert.AreEqual(true, ruleFromString.conditionalMatch.DynamicInvoke(9, 202));

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
        var ruleFromString = ParsedRule.ParseToRule("A(x) -> B(x + stretch, stretch)", new string[] { "stretch" });

        Assert.IsNull(ruleFromString.conditionalMatch);

        Assert.AreEqual("A(x)", ruleFromString.TargetSymbolString());
        Assert.AreEqual(1, ruleFromString.replacementSymbols.Length);

        Assert.AreEqual('B', ruleFromString.replacementSymbols[0].targetSymbol);
        Assert.AreEqual(2, ruleFromString.replacementSymbols[0].evaluators.Length);
        Assert.AreEqual(12, ruleFromString.replacementSymbols[0].evaluators[0].DynamicInvoke(10, 2));
        Assert.AreEqual(10, ruleFromString.replacementSymbols[0].evaluators[1].DynamicInvoke(10, 2));
    }
    [Test]
    public void RuleWithMissingParameterThrowsMeaningfulException()
    {
        var ruleString = "A(x) -> B(x, yeet)";
        try
        {
            ParsedRule.ParseToRule(ruleString);
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
    public void RuleWithNoReplacementThrowsMeaninfulException()
    {
        var ruleString = "A(x) ->";
        try
        {
            ParsedRule.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(0, e.errorStartIndex);
            Assert.AreEqual(ruleString.Length, e.errorLength);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
    [Test]
    public void RuleWithOneTooFewParensThrowsMeaninfulException()
    {
        var ruleString = "A(x) -> B(x + (y)";
        try
        {
            ParsedRule.ParseToRule(ruleString);
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
            ParsedRule.ParseToRule(ruleString);
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
            ParsedRule.ParseToRule(ruleString);
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
            ParsedRule.ParseToRule(ruleString);
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
            ParsedRule.ParseToRule(ruleString);
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
            ParsedRule.ParseToRule(ruleString);
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
            ParsedRule.ParseToRule(ruleString);
        }
        catch (SyntaxException e)
        {
            Assert.AreEqual(15, e.errorStartIndex);
            Assert.AreEqual(16, e.ErrorEndIndex);
            Assert.AreEqual(ruleString, e.ruleText);
        }
    }
}
