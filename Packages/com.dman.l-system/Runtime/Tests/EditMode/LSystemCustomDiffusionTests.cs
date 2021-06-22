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
            diffusionAmount = 'a'
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
            diffusionAmount = 'a'
        };
        using var basicLSystem = BuildSystem(
            new string[] { },
            customSymbols: customSymbols);

        Assert.AreEqual("n(0.5, 4, 10)Fa(2)Fn(0.5, 0, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 4, 10)FFn(0.5, 2, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 10)FFn(0.5, 3, 10)", state.currentSymbols.Data.ToString());
        state = basicLSystem.StepSystem(state);
        Assert.AreEqual("n(0.5, 3, 10)FFn(0.5, 3, 10)", state.currentSymbols.Data.ToString());

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
            diffusionAmount = 'a'
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
            diffusionAmount = 'a'
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
            diffusionAmount = 'a'
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
    public void LSystemAppliesCapToDiffusion()
    {
        LSystemState<float> state = new DefaultLSystemState("n(0.5, 0, 5)Fn(0.5, 0, 5)Fn(0.5, 20, 20)");
        var customSymbols = new CustomRuleSymbols
        {
            hasDiffusion = true,
            diffusionNode = 'n',
            diffusionAmount = 'a'
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
            diffusionAmount = 'a'
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
