using Dman.LSystem;
using NUnit.Framework;

public class LSystemTests
{
    [Test]
    public void LSystemParsesStringAxiom()
    {
        var basicLSystem = new LSystem("B", new IRule[0], 0);

        Assert.AreEqual("B".ToIntArray(), basicLSystem.currentSymbols.symbols);
        Assert.AreEqual(new float[1][], basicLSystem.currentSymbols.parameters);
    }
    [Test]
    public void LSystemAppliesBasicRules()
    {
        var basicLSystem = new LSystem("B", ParsedRule.CompileRules(new string[] {
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
    public void LSystemAssumesIdentityRule()
    {
        var basicLSystem = new LSystem("B", ParsedRule.CompileRules(new string[] {
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
        var basicLSystem = new LSystem("C", ParsedRule.CompileRules(new string[] {
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
        var basicLSystem = new LSystem("C", ParsedRule.CompileRules(new string[] {
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
