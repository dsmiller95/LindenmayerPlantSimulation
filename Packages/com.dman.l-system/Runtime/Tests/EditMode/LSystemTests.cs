using Dman.LSystem;
using Dman.LSystem.SystemCompiler;
using NUnit.Framework;

public class LSystemTests
{
    [Test]
    public void SymbolStringParsesStringAxiom()
    {
        var systemState = new DefaultLSystemState("B");

        Assert.AreEqual("B", systemState.currentSymbols.ToString());
        Assert.AreEqual(new float[][]{
            new float[0]
            }, systemState.currentSymbols.parameters);
    }
    [Test]
    public void LSystemAppliesBasicRules()
    {
        var state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.DoubleSystem(
            new string[] {
            "A -> AB",
            "B -> A"
            });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABA", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAAB", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesMultiMatchRules()
    {
        var state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A -> AB",
            "B -> A",
            "AA -> B"
        });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABA", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAAB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABABA", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAABAAB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABABABA", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAssumesIdentityReplacementWithMultiMatchRules()
    {
        var state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "B -> ABA",
            "AA -> B"
        });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABA", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("AABAA", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("BABAB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAAABAAABA", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAssumesIdentityRule()
    {
        var state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A -> ACB",
            "B -> A"
        });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACBCA", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACBCACACB", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesStochasticRule()
    {
        var state = new DefaultLSystemState("C");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A -> AC",
            "P(0.5) C -> A",
            "P(0.5) C -> AB"
        });

        Assert.AreEqual("C", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACABB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACABACBB", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACABACBACABB", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemAppliesStochasticRuleDifferently()
    {
        var state = new DefaultLSystemState("C");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A -> AC",
            "P(0.9) C -> A",
            "P(0.1) C -> AB"
        });

        Assert.AreEqual("C", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("AC", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACA", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACAAC", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("ACAACACA", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterMatches()
    {
        var state = new DefaultLSystemState("A(1)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x) -> A(x + 1)",
        });

        Assert.AreEqual("A(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(4)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesComplexParameterEquations()
    {
        var state = new DefaultLSystemState("A(1, 1)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x, y) -> A(x + y, x * y)",
        });

        Assert.AreEqual("A(1, 1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2, 1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3, 2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5, 6)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(11, 30)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterEquationsWhenMultiMatch()
    {
        var state = new DefaultLSystemState("A(1, 1)B(0)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x, y)B(z) -> A(x + z, y + z)B(y)",
        });

        Assert.AreEqual("A(1, 1)B(0)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(1, 1)B(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2, 2)B(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3, 3)B(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5, 5)B(3)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemDoesAFibbonachi()
    {
        var state = new DefaultLSystemState("A(1)B(1)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x)B(y) -> A(x + y)B(x)",
        });

        Assert.AreEqual("A(1)B(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2)B(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3)B(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5)B(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(8)B(5)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(13)B(8)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterMatchesAndTerminatesAtCondition()
    {
        var state = new DefaultLSystemState("A(2)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x) : x < 6 -> A(x + 1)",
        });

        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(4)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(6)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state);
        Assert.AreEqual("A(6)", state.currentSymbols.ToString());
    }


    [Test]
    public void LSystemAppliesReplacementBasedOnGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        var state = new DefaultLSystemState("A(1, 1)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x, y) -> A((x + y) - global, x * y + global)",
        }, globalParameters);

        var defaultGlobalParams = new double[] { 5 };

        Assert.AreEqual("A(1, 1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-3, 6)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-2, -13)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-20, 31)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(6, -615)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-614, -3685)", state.currentSymbols.ToString());
    }


    [Test]
    public void LSystemChecksContidionalBasedOnGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        var state = new DefaultLSystemState("A(0)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x) : x < global -> A(x + 1)",
        }, globalParameters);

        var defaultGlobalParams = new double[] { 3 };

        Assert.AreEqual("A(0)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());

        var nextGlobalParams = new double[] { 4 };
        basicLSystem.StepSystem(state, nextGlobalParams);
        Assert.AreEqual("A(4)", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemSelectsRuleToApplyBasedOnConditional()
    {
        var globalParameters = new string[] { "global" };

        var state = new DefaultLSystemState("A(0)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "A(x) : x < global -> A(x + 1)",
            "A(x) : x >= global -> A(x - 1)",
        }, globalParameters);

        var defaultGlobalParams = new double[] { 3 };

        Assert.AreEqual("A(0)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemSelectsStochasticRuleToApplyBasedOnConditional()
    {
        var globalParameters = new string[] { "global" };

        var state = new DefaultLSystemState("A(0)");
        var basicLSystem = LSystemBuilder.DoubleSystem(new string[] {
            "P(0.5) A(x) : x < global -> A(x + 1)",
            "P(0.5) A(x) : x < global -> A(x + 0.5)",
            "P(0.5) A(x) : x >= global -> A(x - 1)",
            "P(0.5) A(x) : x >= global -> A(x - 0.5)",
        }, globalParameters);

        var defaultGlobalParams = new double[] { 3 };

        Assert.AreEqual("A(0)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(0.5)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(1)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(1.5)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2.5)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2.5)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3.5)", state.currentSymbols.ToString());
        basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2.5)", state.currentSymbols.ToString());
    }
    [Test]
    public void RuleCompilationFailsWhenConflictingRules()
    {
        Assert.Throws<System.Exception>(() =>
        {
            var compiledRules = ParsedRule.CompileRules(new string[] {
                "A -> AB",
                "A -> CA",
            });
        });
    }
}
