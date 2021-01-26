using Dman.LSystem;
using NUnit.Framework;

public class LSystemTests
{
    [Test]
    public void LSystemParsesStringAxiom()
    {
        var basicLSystem = new LSystem<float>("B", new IRule<float>[0], 0);

        Assert.AreEqual("B".ToIntArray(), basicLSystem.currentSymbols.symbols);
        Assert.AreEqual(new float[1][], basicLSystem.currentSymbols.parameters);
    }
    [Test]
    public void LSystemAppliesBasicRules()
    {
        var basicLSystem = new LSystem<float>("B", ParsedRule.CompileRules(new string[] {
            "A -> AB",
            "B -> A"
        }), 0);

        Assert.AreEqual("B".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("A".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("AB".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("ABA".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("ABAAB".ToIntArray(), basicLSystem.currentSymbols.symbols);
    }

    [Test]
    public void LSystemAppliesMultiMatchRules()
    {
        var basicLSystem = new LSystem<float>("B", ParsedRule.CompileRules(new string[] {
            "A -> AB",
            "B -> A",
            "AA -> B"
        }), 0);

        Assert.AreEqual("B", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("A", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("AB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABAAB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABABA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABAABAAB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABABABA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
    }

    [Test]
    public void LSystemAssumesIdentityReplacementWithMultiMatchRules()
    {
        var basicLSystem = new LSystem<float>("B", ParsedRule.CompileRules(new string[] {
            "B -> ABA",
            "AA -> B"
        }), 0);

        Assert.AreEqual("B", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("AABAA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("BABAB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABAAABAAABA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
    }

    [Test]
    public void LSystemAssumesIdentityRule()
    {
        var basicLSystem = new LSystem<float>("B", ParsedRule.CompileRules(new string[] {
            "A -> ACB",
            "B -> A"
        }), 0);

        Assert.AreEqual("B".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("A".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("ACB".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("ACBCA".ToIntArray(), basicLSystem.currentSymbols.symbols);
        basicLSystem.StepSystem();
        Assert.AreEqual("ACBCACACB".ToIntArray(), basicLSystem.currentSymbols.symbols);
    }

    [Test]
    public void LSystemAppliesStochasticRule()
    {
        var basicLSystem = new LSystem<float>("C", ParsedRule.CompileRules(new string[] {
            "A -> AC",
            "C (P0.5)-> A",
            "C (P0.5)-> AB"
        }), 0);

        Assert.AreEqual("C", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("AB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACABB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACABACBB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACABACBACABB", basicLSystem.currentSymbols.symbols.ToStringFromChars());
    }
    [Test]
    public void LSystemAppliesStochasticRuleDifferently()
    {
        var basicLSystem = new LSystem<float>("C", ParsedRule.CompileRules(new string[] {
            "A -> AC",
            "C (P0.9)-> A",
            "C (P0.1)-> AB"
        }), 0);

        Assert.AreEqual("C", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("A", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("AC", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACAAC", basicLSystem.currentSymbols.symbols.ToStringFromChars());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACAACACA", basicLSystem.currentSymbols.symbols.ToStringFromChars());
    }
}
