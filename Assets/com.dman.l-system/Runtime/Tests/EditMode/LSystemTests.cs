using Dman.LSystem;
using NUnit.Framework;

public class LSystemTests
{
    [Test]
    public void LSystemParsesStringAxiom()
    {
        var basicLSystem = new LSystem<double>("B", new IRule<double>[0], 0);

        Assert.AreEqual("B".ToIntArray(), basicLSystem.currentSymbols.symbols);
        Assert.AreEqual(new float[][]{
            new float[0]
            }, basicLSystem.currentSymbols.parameters);
    }
    [Test]
    public void LSystemAppliesBasicRules()
    {
        var basicLSystem = new LSystem<double>("B", ParsedRule.CompileRules(new string[] {
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
        var basicLSystem = new LSystem<double>("B", ParsedRule.CompileRules(new string[] {
            "A -> AB",
            "B -> A",
            "AA -> B"
        }), 0);

        Assert.AreEqual("B", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("AB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABA", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABAAB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABABA", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABAABAAB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABABABA", basicLSystem.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAssumesIdentityReplacementWithMultiMatchRules()
    {
        var basicLSystem = new LSystem<double>("B", ParsedRule.CompileRules(new string[] {
            "B -> ABA",
            "AA -> B"
        }), 0);

        Assert.AreEqual("B", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABA", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("AABAA", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("BABAB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ABAAABAAABA", basicLSystem.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAssumesIdentityRule()
    {
        var basicLSystem = new LSystem<double>("B", ParsedRule.CompileRules(new string[] {
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
        var basicLSystem = new LSystem<double>("C", ParsedRule.CompileRules(new string[] {
            "A -> AC",
            "(P0.5) C -> A",
            "(P0.5) C -> AB"
        }), 0);

        Assert.AreEqual("C", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("AB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACABB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACABACBB", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACABACBACABB", basicLSystem.currentSymbols.ToString());
    }
    [Test]
    public void LSystemAppliesStochasticRuleDifferently()
    {
        var basicLSystem = new LSystem<double>("C", ParsedRule.CompileRules(new string[] {
            "A -> AC",
            "(P0.9) C -> A",
            "(P0.1) C -> AB"
        }), 0);

        Assert.AreEqual("C", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("AC", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACA", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACAAC", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("ACAACACA", basicLSystem.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterMatches()
    {
        var basicLSystem = new LSystem<double>("A(1)", ParsedRule.CompileRules(new string[] {
            "A(x) -> A(x + 1)",
        }), 0);

        Assert.AreEqual("A(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(4)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(5)", basicLSystem.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesComplexParameterEquations()
    {
        var basicLSystem = new LSystem<double>("A(1, 1)", ParsedRule.CompileRules(new string[] {
            "A(x, y) -> A(x + y, x * y)",
        }), 0);

        Assert.AreEqual("A(1, 1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(2, 1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(3, 2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(5, 6)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(11, 30)", basicLSystem.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterEquationsWhenMultiMatch()
    {
        var basicLSystem = new LSystem<double>("A(1, 1)B(0)", ParsedRule.CompileRules(new string[] {
            "A(x, y)B(z) -> A(x + z, y + z)B(y)",
        }), 0);

        Assert.AreEqual("A(1, 1)B(0)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(1, 1)B(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(2, 2)B(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(3, 3)B(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(5, 5)B(3)", basicLSystem.currentSymbols.ToString());
    }

    [Test]
    public void LSystemDoesAFibbonachi()
    {
        var basicLSystem = new LSystem<double>("A(1)B(1)", ParsedRule.CompileRules(new string[] {
            "A(x)B(y) -> A(x + y)B(x)",
        }), 0);

        Assert.AreEqual("A(1)B(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(2)B(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(3)B(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(5)B(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(8)B(5)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(13)B(8)", basicLSystem.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterMatchesAndTerminatesAtCondition()
    {
        var basicLSystem = new LSystem<double>("A(2)", ParsedRule.CompileRules(new string[] {
            "A(x) : x < 6 -> A(x + 1)",
        }), 0);

        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(4)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(5)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(6)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem();
        Assert.AreEqual("A(6)", basicLSystem.currentSymbols.ToString());
    }


    [Test]
    public void LSystemAppliesReplacementBasedOnGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        var basicLSystem = new LSystem<double>("A(1, 1)", ParsedRule.CompileRules(new string[] {
            "A(x, y) -> A((x + y) - global, x * y + global)",
        }, globalParameters), 0);

        var defaultGlobalParams = new double[] { 5 };

        Assert.AreEqual("A(1, 1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(-3, 6)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(-2, -13)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(-20, 31)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(6, -615)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(-614, -3685)", basicLSystem.currentSymbols.ToString());
    }


    [Test]
    public void LSystemChecksContidionalBasedOnGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        var basicLSystem = new LSystem<double>("A(0)", ParsedRule.CompileRules(new string[] {
            "A(x) : x < global -> A(x + 1)",
        }, globalParameters), 0);

        var defaultGlobalParams = new double[] { 3 };

        Assert.AreEqual("A(0)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());

        var nextGlobalParams = new double[] { 4 };
        basicLSystem.StepSystem(nextGlobalParams);
        Assert.AreEqual("A(4)", basicLSystem.currentSymbols.ToString());
    }
    [Test]
    public void LSystemSelectsRuleToApplyBasedOnConditional()
    {
        var globalParameters = new string[] { "global" };

        var basicLSystem = new LSystem<double>("A(0)", ParsedRule.CompileRules(new string[] {
            "A(x) : x < global -> A(x + 1)",
            "A(x) : x >= global -> A(x - 1)",
        }, globalParameters), 0);

        var defaultGlobalParams = new double[] { 3 };

        Assert.AreEqual("A(0)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
    }
    [Test]
    public void LSystemSelectsStochasticRuleToApplyBasedOnConditional()
    {
        var globalParameters = new string[] { "global" };

        var basicLSystem = new LSystem<double>("A(0)", ParsedRule.CompileRules(new string[] {
            "(P0.5) A(x) : x < global -> A(x + 1)",
            "(P0.5) A(x) : x < global -> A(x + 0.5)",
            "(P0.5) A(x) : x >= global -> A(x - 1)",
            "(P0.5) A(x) : x >= global -> A(x - 0.5)",
        }, globalParameters), 0);

        var defaultGlobalParams = new double[] { 3 };

        Assert.AreEqual("A(0)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(1)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(2)", basicLSystem.currentSymbols.ToString());
        basicLSystem.StepSystem(defaultGlobalParams);
        Assert.AreEqual("A(3)", basicLSystem.currentSymbols.ToString());
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
