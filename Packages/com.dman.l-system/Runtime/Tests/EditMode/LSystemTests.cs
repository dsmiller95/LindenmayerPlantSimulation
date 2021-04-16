using Dman.LSystem;
using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
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
        LSystemState<float> state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.FloatSystem(
            new string[] {
            "A -> AB",
            "B -> A"
            });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAAB", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesContextualRulesWithUniqueOrigins()
    {
        LSystemState<float> state = new DefaultLSystemState("A");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A -> AB",
            "B -> CDC",
            "D > C -> A",
            "D < C > D -> B"
        });

        Assert.AreEqual("A", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABCDC", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABCDCCAC", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABCDCCACCABC", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABCDCCACCABCCABCDCC", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemAppliesFlatContextualRules()
    {
        LSystemState<float> state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A -> AB",
            "B -> A",
            "A > A ->",
            "A < A -> B"
        });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAAB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABABA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAABAAB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABABABA", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAssumesIdentityReplacementWithContextRules()
    {
        LSystemState<float> state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "B -> ABA",
            "A > A ->",
            "A < A -> B"
        });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("AABAA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("BABAB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAAABAAABA", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAssumesIdentityRule()
    {
        LSystemState<float> state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A -> ACB",
            "B -> A"
        });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACBCA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACBCACACB", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesStochasticRule()
    {
        LSystemState<float> state = new DefaultLSystemState("C");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A -> AC",
            "P(0.5) | C -> A",
            "P(0.5) | C -> AB"
        });

        Assert.AreEqual("C", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACAB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACABACB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACAACBACABB", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemAppliesStochasticRuleDifferently()
    {
        LSystemState<float> state = new DefaultLSystemState("C");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A -> AC",
            "P(0.9) | C -> A",
            "P(0.1) | C -> AB"
        });

        Assert.AreEqual("C", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("AC", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACAAC", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ACAACACA", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterMatches()
    {
        LSystemState<float> state = new DefaultLSystemState("A(1)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A(x) -> A(x + 1)",
        });

        Assert.AreEqual("A(1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(4)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesComplexParameterEquations()
    {
        LSystemState<float> state = new DefaultLSystemState("A(1, 1)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A(x, y) -> A(x + y, x * y)",
        });

        Assert.AreEqual("A(1, 1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2, 1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3, 2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5, 6)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(11, 30)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterEquationsWhenContextMatch()
    {
        LSystemState<float> state = new DefaultLSystemState("A(1, 1)B(0)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "          A(x, y) > B(z) -> A(x + z, y + z)",
            "A(x, y) < B(z)           -> B(y)",
        });

        Assert.AreEqual("A(1, 1)B(0)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(1, 1)B(1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2, 2)B(1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3, 3)B(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5, 5)B(3)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemDoesAFibbonachi()
    {
        LSystemState<float> state = new DefaultLSystemState("A(1)B(1)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "       A(x) > B(y) -> A(x + y)",
            "A(x) < B(y)        -> B(x)",
        });

        Assert.AreEqual("A(1)B(1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(2)B(1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3)B(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5)B(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(8)B(5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(13)B(8)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemAppliesParameterMatchesAndTerminatesAtCondition()
    {
        LSystemState<float> state = new DefaultLSystemState("A(2)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A(x) : x < 6 -> A(x + 1)",
        });

        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(4)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(6)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A(6)", state.currentSymbols.ToString());
    }


    [Test]
    public void LSystemAppliesReplacementBasedOnGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        LSystemState<float> state = new DefaultLSystemState("A(1, 1)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A(x, y) -> A((x + y) - global, x * y + global)",
        }, globalParameters);

        var defaultGlobalParams = new float[] { 5 };

        Assert.AreEqual("A(1, 1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-3, 6)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-2, -13)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-20, 31)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(6, -615)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(-614, -3685)", state.currentSymbols.ToString());
    }


    [Test]
    public void LSystemChecksContidionalBasedOnGlobalParameters()
    {
        var globalParameters = new string[] { "global" };

        LSystemState<float> state = new DefaultLSystemState("A(0)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A(x) : x < global -> A(x + 1)",
        }, globalParameters);

        var defaultGlobalParams = new float[] { 3 };

        Assert.AreEqual("A(0)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());

        var nextGlobalParams = new float[] { 4 };
        state = basicLSystem.StepSystem(state, nextGlobalParams);
        Assert.AreEqual("A(4)", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemSelectsRuleToApplyBasedOnConditional()
    {
        var globalParameters = new string[] { "global" };

        LSystemState<float> state = new DefaultLSystemState("A(0)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A(x) : x < global -> A(x + 1)",
            "A(x) : x >= global -> A(x - 1)",
        }, globalParameters);

        var defaultGlobalParams = new float[] { 3 };

        Assert.AreEqual("A(0)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(1)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemSelectsStochasticRuleToApplyBasedOnConditional()
    {
        var globalParameters = new string[] { "global" };

        LSystemState<float> state = new DefaultLSystemState("A(0)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "P(0.5) | A(x) : x < global -> A(x + 1)",
            "P(0.5) | A(x) : x < global -> A(x + 0.5)",
            "P(0.5) | A(x) : x >= global -> A(x - 1)",
            "P(0.5) | A(x) : x >= global -> A(x - 0.5)",
        }, globalParameters);

        var defaultGlobalParams = new float[] { 3 };

        Assert.AreEqual("A(0)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(0.5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(1.5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2.5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2.5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3.5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(2.5)", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state, defaultGlobalParams);
        Assert.AreEqual("A(3.5)", state.currentSymbols.ToString());
    }

    [Test]
    public void LSystemReproducesStochasticResultFromReplicatedState()
    {
        var globalParameters = new string[] { "global" };

        LSystemState<float> state = new DefaultLSystemState("A(0)");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "P(0.5) | A(x) : x < global -> A(x + 1)",
            "P(0.5) | A(x) : x < global -> A(x + 0.5)",
            "P(0.5) | A(x) : x >= global -> A(x - 1)",
            "P(0.5) | A(x) : x >= global -> A(x - 0.5)",
        }, globalParameters);

        var defaultGlobalParams = new float[] { 3 };

        var expectedResultSequence = new string[]
        {
            "A(0)",
            "A(0.5)",
            "A(1.5)",
            "A(2.5)",
            "A(3)",
            "A(2)",
            "A(3)",
            "A(2)",
            "A(2.5)",
            "A(3.5)",
            "A(2.5)",
            "A(3.5)",
        };
        Assert.AreEqual(expectedResultSequence[0], state.currentSymbols.ToString());

        LSystemState<float> systemCopyAt5 = null;
        for (int i = 1; i < expectedResultSequence.Length; i++)
        {
            if (i == 5)
            {
                systemCopyAt5 = state;
            }
            state = basicLSystem.StepSystem(state, defaultGlobalParams);
            Assert.AreEqual(expectedResultSequence[i], state.currentSymbols.ToString(), $"Index {i}");
        }

        for (int i = 5; i < expectedResultSequence.Length; i++)
        {
            systemCopyAt5 = basicLSystem.StepSystem(systemCopyAt5, defaultGlobalParams);
            Assert.AreEqual(expectedResultSequence[i], systemCopyAt5.currentSymbols.ToString(), $"Index {i}");
        }
    }

    [Test]
    public void LSystemPrioritizesContextualRulesBySize()
    {
        LSystemState<float> state = new DefaultLSystemState("AABCD");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A -> B",
            "A > A -> C",
            "A > ABCD -> F",
            "A > ABC -> E",
            "A > AB -> D",
        });

        Assert.AreEqual("AABCD", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("FBBCD", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemAppliesContextualRulesStochasticly()
    {
        LSystemState<float> state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A -> AB",
            "B -> A",
            "P(0.5) | A > A ->",
            "P(0.5) | A > A -> A",
            "A < A -> B"
        });

        Assert.AreEqual("B", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("A", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("AB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAAB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABABA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABAABAAB", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("ABABABA", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemAppliesContextualRulesOfEqualComplexityInDefinitionOrder()
    {
        LSystemState<float> state = new DefaultLSystemState("AAA");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "    A > A -> B",
            "A < A     -> C"
        });
        Assert.AreEqual("AAA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("BBC", state.currentSymbols.ToString());

        state = new DefaultLSystemState("AAA");
        basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "A < A     -> C",
            "    A > A -> B"
        });
        Assert.AreEqual("AAA", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("BCC", state.currentSymbols.ToString());
    }
    [Test]
    public void LSystemIgnoresIgnoredCharachters()
    {
        LSystemState<float> state = new DefaultLSystemState("B");
        var basicLSystem = LSystemBuilder.FloatSystem(new string[] {
            "    A     -> A1B2",
            "    B     -> 3A4",
            "    A > A -> 5",
            "A < A     -> 6B7"
        }, ignoredCharacters: "1234567");

        Assert.AreEqual("B", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("3A4", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("3A1B24", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("3A1B213A424", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("3A1B213A4213A1B2424", state.currentSymbols.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("3A1B213A4213542136B713A42424", state.currentSymbols.ToString());
    }

    [Test]
    public void RuleCompilationFailsWhenConflictingRules()
    {
        Assert.Throws<LSystemRuntimeException>(() =>
        {
            var compiledRules = RuleParser.CompileRules(new string[] {
                "A -> AB",
                "A -> CA",
            });
        });
    }
    [Test]
    public void RuleCompilationFailsWhenContextMatchesOfDifferentTypesTryToShareProbability()
    {
        Assert.Throws<LSystemRuntimeException>(() =>
        {
            var compiledRules = RuleParser.CompileRules(new string[] {
                "P(0.5) | A > B -> AB",
                "P(0.5) | A > BC -> CA",
            });
        });
    }
    [Test]
    public void RuleCompilationFailsWhenContextMatchesOfDifferentTypesMatchSamePattern()
    {
        Assert.Throws<LSystemRuntimeException>(() =>
        {
            var compiledRules = RuleParser.CompileRules(new string[] {
                "C < A > B[C][D] -> AB",
                "C < A > B[C][D] -> CA",
            });
        });
    }
    [Test, Ignore("future feature will be to compare rules semantically instead of literally")]
    public void RuleCompilationFailsWhenSuffixContextsMatchSemanticallyButNotLiterally()
    {
        Assert.Throws<LSystemRuntimeException>(() =>
        {
            var compiledRules = RuleParser.CompileRules(new string[] {
                "A > B[C]D -> AB",
                "A > B[C][D] -> CA",
            });
        });
    }
}
