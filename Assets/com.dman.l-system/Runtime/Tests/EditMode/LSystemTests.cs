using Dman.LSystem;
using NUnit.Framework;

public class LSystemTests
{
    [Test]
    public void LSystemParsesStringAxiom()
    {
        var basicLSystem = new LSystem("B", new IRule[0]);

        Assert.AreEqual("B".ToIntArray(), basicLSystem.currentSymbols.symbols);
        Assert.AreEqual(new float[1][], basicLSystem.currentSymbols.parameters);
    }
    [Test]
    public void LSystemAppliesBasicRules()
    {
        var basicLSystem = new LSystem("B", new IRule[] {
            new BasicRule("A -> AB"),
            new BasicRule("B -> A")
            });

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
        var basicLSystem = new LSystem("B", new IRule[] {
            new BasicRule("A -> ACB"),
            new BasicRule("B -> A")
            });

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
}
