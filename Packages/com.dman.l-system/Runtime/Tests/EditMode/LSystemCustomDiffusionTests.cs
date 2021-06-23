using Dman.LSystem.Packages.Tests.EditMode;
using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class LSystemCustomDiffusionTests
{
    public static LSystemStepper BuildSystem(
       IEnumerable<string> rules,
       string[] globalParameters = null,
       string includedCharacters = "[]ABCDEFDna",
       int branchOpenSymbol = '[',
       int branchCloseSymbol = ']',
       CustomRuleSymbols customSymbols = default)
    {
        var compiledRules = RuleParser.CompileRules(
                    rules,
                    out var nativeRuleData,
                    branchOpenSymbol, branchCloseSymbol,
                    globalParameters
                    );

        return new LSystemStepper(
            compiledRules,
            nativeRuleData,
            branchOpenSymbol, branchCloseSymbol,
            globalParameters?.Length ?? 0,
            includedCharactersByRuleIndex: new[] { new HashSet<int>(includedCharacters.Select(x => (int)x)) },
            customSymbols: customSymbols
        );
    }

    [Test]
    public void LSystemAppliesTwoNodeDiffusion()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 4, 10)Fn(0.5, 0, 10)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 4, 10)Fn(0.5, 0, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2, 10)Fn(0.5, 2, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2, 10)Fn(0.5, 2, 10)", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }
    [Test]
    public void LSystemAppliesTwoNodeDiffusionAndAddsResource()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 4, 10)Fa(2)Fn(0.5, 0, 10)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 4, 10)Fa(2)Fn(0.5, 0, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 10)FFn(0.5, 3, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 10)FFn(0.5, 3, 10)", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }
    [Test]
    public void LSystemAppliesThreeNodeDiffusionAndAddsResource()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 4, 10)a(2)n(0.5, 0, 10)n(0.5, 0, 10)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 4, 10)a(2)n(0.5, 0, 10)n(0.5, 0, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 10)n(0.5, 3, 10)n(0.5, 0, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 10)n(0.5, 1.5, 10)n(0.5, 1.5, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2.25, 10)n(0.5, 2.25, 10)n(0.5, 1.5, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2.25, 10)n(0.5, 1.875, 10)n(0.5, 1.875, 10)", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }

    [Test]
    public void LSystemAppliesSingleLeafSourceTreeBasedDiffusion()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 0, 20)F[n(0.5, 0, 20)][n(0.5, 12, 20)]");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 0, 20)F[n(0.5, 0, 20)][n(0.5, 12, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 6, 20)F[n(0.5, 0, 20)][n(0.5, 6, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 20)F[n(0.5, 3, 20)][n(0.5, 6, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 4.5, 20)F[n(0.5, 3, 20)][n(0.5, 4.5, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3.75, 20)F[n(0.5, 3.75, 20)][n(0.5, 4.5, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 4.125, 20)F[n(0.5, 3.75, 20)][n(0.5, 4.125, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3.9375, 20)F[n(0.5, 3.9375, 20)][n(0.5, 4.125, 20)]", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }
    [Test]
    public void LSystemAppliesSingleRootSourceTreeBasedDiffusion()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 12, 20)F[n(0.5, 0, 20)][n(0.5, 0, 20)]");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 12, 20)F[n(0.5, 0, 20)][n(0.5, 0, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 0, 20)F[n(0.5, 6, 20)][n(0.5, 6, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 6, 20)F[n(0.5, 3, 20)][n(0.5, 3, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 20)F[n(0.5, 4.5, 20)][n(0.5, 4.5, 20)]", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 4.5, 20)F[n(0.5, 3.75, 20)][n(0.5, 3.75, 20)]", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }

    [Test]
    public void LSystemDiffusesThroughChain()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 0, 10)Fn(0.5, 0, 10)Fn(0.5, 8, 10)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 0, 10)Fn(0.5, 0, 10)Fn(0.5, 8, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 0, 10)Fn(0.5, 4, 10)Fn(0.5, 4, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2, 10)Fn(0.5, 2, 10)Fn(0.5, 4, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2, 10)Fn(0.5, 3, 10)Fn(0.5, 3, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2.5, 10)Fn(0.5, 2.5, 10)Fn(0.5, 3, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2.5, 10)Fn(0.5, 2.75, 10)Fn(0.5, 2.75, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 2.625, 10)Fn(0.5, 2.625, 10)Fn(0.5, 2.75, 10)", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }
    [Test]
    public void LSystemDiffusesThroughChainSmootherWithMoreDiffusionSteps()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.25, 18, 20)Fn(0.25, 0, 20)Fn(0.25, 0, 20)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 2
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.25, 18, 20)Fn(0.25, 0, 20)Fn(0.25, 0, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 11.25, 20)Fn(0.25, 5.625, 20)Fn(0.25, 1.125, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 8.859375, 20)Fn(0.25, 5.976563, 20)Fn(0.25, 3.164063, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 7.602539, 20)Fn(0.25, 5.998535, 20)Fn(0.25, 4.398926, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 6.901062, 20)Fn(0.25, 5.999908, 20)Fn(0.25, 5.09903, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 6.506824, 20)Fn(0.25, 5.999994, 20)Fn(0.25, 5.493181, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 6.285088, 20)Fn(0.25, 6, 20)Fn(0.25, 5.714913, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 6.160362, 20)Fn(0.25, 6, 20)Fn(0.25, 5.839638, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.25, 6.090203, 20)Fn(0.25, 6, 20)Fn(0.25, 5.909797, 20)", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }

    [Test]
    public void LSystemAppliesCapToDiffusion()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 0, 5)Fn(0.5, 0, 5)Fn(0.5, 20, 20)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 0, 5)Fn(0.5, 0, 5)Fn(0.5, 20, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 0, 5)Fn(0.5, 10, 5)Fn(0.5, 10, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 5, 5)Fn(0.5, 5, 5)Fn(0.5, 10, 20)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 5, 5)Fn(0.5, 5, 5)Fn(0.5, 10, 20)", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }
    [Test]
    public void LSystemAppliesDiffusionRatesToDiffusion()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 0, 10)Fn(0.1, 0, 10)Fn(0.5, 8, 10)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a',
            diffusionStepsPerStep = 1
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 0, 10)Fn(0.1, 0, 10)Fn(0.5, 8, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 0, 10)Fn(0.1, 2.4, 10)Fn(0.5, 5.6, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 0.72, 10)Fn(0.1, 2.64, 10)Fn(0.5, 4.64, 10)", state.currentSymbols.Data.ToString());

        state.currentSymbols.Dispose();
    }


}
