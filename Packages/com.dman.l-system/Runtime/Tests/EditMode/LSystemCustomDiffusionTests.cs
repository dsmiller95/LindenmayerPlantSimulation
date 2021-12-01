using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
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
            includedContextualCharactersByRuleIndex: new[] { new HashSet<int>(includedCharacters.Select(x => (int)x)) },
            customSymbols: customSymbols
        );
    }

    public void TestLSystemFile(
        string fileText,
        IEnumerable<string> expectedSteps)
    {
        var fileSystem = new InMemoryFileProvider();
        fileSystem.RegisterFileWithIdentifier("root.lsystem", fileText);

        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");
        using var system = linkedFiles.CompileSystem();
        LSystemState<float> state = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var symbolStringMapping = linkedFiles.allSymbolDefinitionsLeafFirst
            .Where(x => x.sourceFileDefinition == "root.lsystem")
            .ToDictionary(x => x.actualSymbol, x => x.characterInSourceFile);

        using var axiom = linkedFiles.GetAxiom();
        Assert.AreEqual(axiom.ToString(symbolStringMapping), state.currentSymbols.Data.ToString(symbolStringMapping));
        try
        {
            foreach (var expectedStep in expectedSteps)
            {
                state = system.StepSystem(state);
                Assert.AreEqual(expectedStep, state.currentSymbols.Data.ToString(symbolStringMapping));
            }
        }
        catch(System.Exception e)
        {
            UnityEngine.Debug.LogException(e);
            UnityEngine.Debug.Log("actual result:");
            state.currentSymbols.DisposeImmediate();
            state = new DefaultLSystemState(
                linkedFiles.GetAxiom(),
                (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            for (int i = 0; i < expectedSteps.Count(); i++)
            {
                state = system.StepSystem(state);
                UnityEngine.Debug.Log(state.currentSymbols.Data.ToString(symbolStringMapping));
            }
            throw;
        }
        finally
        {
            state.currentSymbols.DisposeImmediate();
        }
    }

    public void TestDiffusionParallelAndIndependent(
        string fileSource,
        IEnumerable<string> expectedSteps)
    {
        TestLSystemFile(fileSource, expectedSteps);
        fileSource += "\n#define independentDiffusionStep true";
        TestLSystemFile(fileSource, expectedSteps);
    }
    [Test]
    public void LSystemAppliesTwoNodeDiffusion()
    {
        var testFile = @"
#axiom n(0.5, 4, 10)Fn(0.5, 0, 10)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 2, 10)Fn(0.5, 2, 10)",
                "n(0.5, 2, 10)Fn(0.5, 2, 10)"
            });
    }
    [Test]
    public void LSystemAppliesTwoNodeDiffusionAndAddsResource()
    {
        var testFile = @"
#axiom n(0.5, 4, 10)Fa(2)Fn(0.5, 0, 10)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 3, 10)FaFn(0.5, 3, 10)",
                "n(0.5, 3, 10)FFn(0.5, 3, 10)"
            });
    }
    [Test]
    public void LSystemAppliesThreeNodeDiffusionAndAddsResource()
    {
        var testFile = @"
#axiom n(0.5, 4, 10)a(2)n(0.5, 0, 10)n(0.5, 0, 10)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 3, 10)an(0.5, 3, 10)n(0.5, 0, 10)",
                "n(0.5, 3, 10)n(0.5, 1.5, 10)n(0.5, 1.5, 10)",
                "n(0.5, 2.25, 10)n(0.5, 2.25, 10)n(0.5, 1.5, 10)",
                "n(0.5, 2.25, 10)n(0.5, 1.875, 10)n(0.5, 1.875, 10)",
            });
    }

    [Test]
    public void LSystemAppliesSingleLeafSourceTreeBasedDiffusion()
    {
        var testFile = @"
#axiom n(0.5, 0, 20)F[n(0.5, 0, 20)][n(0.5, 12, 20)]
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 6, 20)F[n(0.5, 0, 20)][n(0.5, 6, 20)]",
                "n(0.5, 3, 20)F[n(0.5, 3, 20)][n(0.5, 6, 20)]",
                "n(0.5, 4.5, 20)F[n(0.5, 3, 20)][n(0.5, 4.5, 20)]",
                "n(0.5, 3.75, 20)F[n(0.5, 3.75, 20)][n(0.5, 4.5, 20)]",
                "n(0.5, 4.125, 20)F[n(0.5, 3.75, 20)][n(0.5, 4.125, 20)]",
                "n(0.5, 3.9375, 20)F[n(0.5, 3.9375, 20)][n(0.5, 4.125, 20)]"
            });
    }
    [Test]
    public void LSystemAppliesSingleRootSourceTreeBasedDiffusion()
    {
        var testFile = @"
#axiom n(0.5, 12, 20)F[n(0.5, 0, 20)][n(0.5, 0, 20)]
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 0, 20)F[n(0.5, 6, 20)][n(0.5, 6, 20)]",
                "n(0.5, 6, 20)F[n(0.5, 3, 20)][n(0.5, 3, 20)]",
                "n(0.5, 3, 20)F[n(0.5, 4.5, 20)][n(0.5, 4.5, 20)]",
                "n(0.5, 4.5, 20)F[n(0.5, 3.75, 20)][n(0.5, 3.75, 20)]"
            });

    }

    [Test]
    public void LSystemDiffusesThroughChain()
    {
        var testFile = @"
#axiom n(0.5, 18, 10)Fn(0.5, 0, 10)Fn(0.5, 0, 10)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 9, 10)Fn(0.5, 9, 10)Fn(0.5, 0, 10)",
                "n(0.5, 9, 10)Fn(0.5, 4.5, 10)Fn(0.5, 4.5, 10)",
                "n(0.5, 6.75, 10)Fn(0.5, 6.75, 10)Fn(0.5, 4.5, 10)",
                "n(0.5, 6.75, 10)Fn(0.5, 5.625, 10)Fn(0.5, 5.625, 10)",
                "n(0.5, 6.1875, 10)Fn(0.5, 6.1875, 10)Fn(0.5, 5.625, 10)",
                "n(0.5, 6.1875, 10)Fn(0.5, 5.90625, 10)Fn(0.5, 5.90625, 10)"
            });
    }
    [Test]
    public void LSystemDiffusesThroughChainSmootherWithMoreDiffusionSteps()
    {
        var testFile = @"
#axiom n(0.25, 18, 20)Fn(0.25, 0, 20)Fn(0.25, 0, 20)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 2
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.25, 11.25, 20)Fn(0.25, 5.625, 20)Fn(0.25, 1.125, 20)",
                "n(0.25, 8.859375, 20)Fn(0.25, 5.976563, 20)Fn(0.25, 3.164063, 20)",
                "n(0.25, 7.602539, 20)Fn(0.25, 5.998535, 20)Fn(0.25, 4.398926, 20)",
                "n(0.25, 6.901062, 20)Fn(0.25, 5.999908, 20)Fn(0.25, 5.09903, 20)",
                "n(0.25, 6.506824, 20)Fn(0.25, 5.999994, 20)Fn(0.25, 5.493181, 20)",
                "n(0.25, 6.285088, 20)Fn(0.25, 6, 20)Fn(0.25, 5.714913, 20)",
                "n(0.25, 6.160362, 20)Fn(0.25, 6, 20)Fn(0.25, 5.839638, 20)",
                "n(0.25, 6.090203, 20)Fn(0.25, 6, 20)Fn(0.25, 5.909797, 20)",
            });
    }

    [Test]
    public void LSystemAppliesCapToDiffusion()
    {
        var testFile = @"
#axiom n(0.5, 0, 5)Fn(0.5, 0, 5)Fn(0.5, 20, 20)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 0, 5)Fn(0.5, 10, 5)Fn(0.5, 10, 20)",
                "n(0.5, 5, 5)Fn(0.5, 5, 5)Fn(0.5, 10, 20)",
                "n(0.5, 5, 5)Fn(0.5, 5, 5)Fn(0.5, 10, 20)",
            });
    }
    [Test]
    public void LSystemAppliesDiffusionRatesToDiffusion()
    {
        var testFile = @"
#axiom n(0.5, 0, 10)Fn(0.1, 0, 10)Fn(0.5, 8, 10)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 0, 10)Fn(0.1, 2.4, 10)Fn(0.5, 5.6, 10)",
                "n(0.5, 0.72, 10)Fn(0.1, 2.64, 10)Fn(0.5, 4.64, 10)",
            });
    }

    [Test]
    public void DiffusionRateCanChangeAtRuntime()
    {
        var testFile = @"
#axiom n(0.5, 0, 10)Fn(0.5, 0, 10)Fn(0.5, 8, 10)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";


        var fileSystem = new InMemoryFileProvider();
        fileSystem.RegisterFileWithIdentifier("root.lsystem", testFile);
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");
        using var system = linkedFiles.CompileSystem();

        LSystemState<float> state = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var symbolStringMapping = linkedFiles.allSymbolDefinitionsLeafFirst
            .Where(x => x.sourceFileDefinition == "root.lsystem")
            .ToDictionary(x => x.actualSymbol, x => x.characterInSourceFile);

        using var axiom = linkedFiles.GetAxiom();
        Assert.AreEqual(axiom.ToString(symbolStringMapping), state.currentSymbols.Data.ToString(symbolStringMapping));
        try
        {
            state = system.StepSystem(state);
            Assert.AreEqual("n(0.5, 0, 10)Fn(0.5, 4, 10)Fn(0.5, 4, 10)", state.currentSymbols.Data.ToString(symbolStringMapping));

            system.customSymbols.diffusionConstantRuntimeGlobalMultiplier = 0.5f;
            state = system.StepSystem(state);
            Assert.AreEqual("n(0.5, 1, 10)Fn(0.5, 3, 10)Fn(0.5, 4, 10)", state.currentSymbols.Data.ToString(symbolStringMapping));

            state = system.StepSystem(state);
            Assert.AreEqual("n(0.5, 1.5, 10)Fn(0.5, 2.75, 10)Fn(0.5, 3.75, 10)", state.currentSymbols.Data.ToString(symbolStringMapping));

            system.customSymbols.diffusionConstantRuntimeGlobalMultiplier = 1f;
            state = system.StepSystem(state);
            Assert.AreEqual("n(0.5, 2.125, 10)Fn(0.5, 2.625, 10)Fn(0.5, 3.25, 10)", state.currentSymbols.Data.ToString(symbolStringMapping));
        }
        finally
        {
            state.currentSymbols.DisposeImmediate();
        }

    }
    [Test]
    public void LSystemOnlyCountsAmountsOnce()
    {
        var testFile = @"
#axiom n(0.5, 0, 10)a(1)Fn(0.5, 0, 10)Fn(0.5, 0, 10)a(1)
#iterations 10
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
";
        TestDiffusionParallelAndIndependent(testFile,
            new[]
            {
                "n(0.5, 0.5, 10)aFn(0.5, 1, 10)Fn(0.5, 0.5, 10)a",
                "n(0.5, 0.75, 10)Fn(0.5, 0.5, 10)Fn(0.5, 0.75, 10)",
            });
    }

    [Test]
    public void IndependentDiffusionStepFasterFeedbackInSystem()
    {
        var testFile = @"
#axiom n(0.5, 0, 10)S(1)Fn(0.5, 0, 10)A(0)
#iterations 20
#symbols Fna
#define diffusionStepsPerStep 1
#include diffusion (Node->n) (Amount->a)
#matches n

#symbols A
n(a, resource, b) < A(progress) : progress < 3 && resource > 0.1 -> a(-resource/2)A(progress + resource / 2)
n(a, resource, b) < A(progress) : progress >= 3 -> FA(progress - 3)

#symbols S
S(x) -> a(x)S(x)

";
        TestLSystemFile(testFile,
            new[]
            {
                "n(0.5, 0, 10)a(1)S(1)Fn(0.5, 0, 10)A(0)",
                "n(0.5, 0.5, 10)aa(1)S(1)Fn(0.5, 0.5, 10)A(0)",
                "n(0.5, 1, 10)aa(1)S(1)Fn(0.5, 1, 10)a(-0.25)A(0.25)",
                "n(0.5, 1.375, 10)aa(1)S(1)Fn(0.5, 1.375, 10)aa(-0.5)A(0.75)",
                "n(0.5, 1.625, 10)aa(1)S(1)Fn(0.5, 1.625, 10)aa(-0.6875)A(1.4375)",
                "n(0.5, 1.78125, 10)aa(1)S(1)Fn(0.5, 1.78125, 10)aa(-0.8125)A(2.25)",
                "n(0.5, 1.875, 10)aa(1)S(1)Fn(0.5, 1.875, 10)aa(-0.890625)A(3.140625)",
                "n(0.5, 1.929688, 10)aa(1)S(1)Fn(0.5, 1.929688, 10)aFA(0.140625)",
                "n(0.5, 2.429688, 10)aa(1)S(1)Fn(0.5, 2.429688, 10)Fa(-0.9648438)A(1.105469)",
                "n(0.5, 2.447266, 10)aa(1)S(1)Fn(0.5, 2.447266, 10)Faa(-1.214844)A(2.320313)",
                "n(0.5, 2.339844, 10)aa(1)S(1)Fn(0.5, 2.339844, 10)Faa(-1.223633)A(3.543945)",
                "n(0.5, 2.228027, 10)aa(1)S(1)Fn(0.5, 2.228027, 10)FaFA(0.5439453)",
                "n(0.5, 2.728027, 10)aa(1)S(1)Fn(0.5, 2.728027, 10)FFa(-1.114014)A(1.657959)",
                "n(0.5, 2.671021, 10)aa(1)S(1)Fn(0.5, 2.671021, 10)FFaa(-1.364014)A(3.021973)",
                "n(0.5, 2.489014, 10)aa(1)S(1)Fn(0.5, 2.489014, 10)FFaFA(0.02197266)",
            });

        testFile += "\n#define independentDiffusionStep true";
        TestLSystemFile(testFile,
            new[]
            {
                "n(0.5, 0.5, 10)aS(1)Fn(0.5, 0.5, 10)A(0)",
                "n(0.5, 0.875, 10)aS(1)Fn(0.5, 0.875, 10)aA(0.25)",
                "n(0.5, 1.15625, 10)aS(1)Fn(0.5, 1.15625, 10)aA(0.6875)",
                "n(0.5, 1.367188, 10)aS(1)Fn(0.5, 1.367188, 10)aA(1.265625)",
                "n(0.5, 1.525391, 10)aS(1)Fn(0.5, 1.525391, 10)aA(1.949219)",
                "n(0.5, 1.644043, 10)aS(1)Fn(0.5, 1.644043, 10)aA(2.711914)",
                "n(0.5, 1.733032, 10)aS(1)Fn(0.5, 1.733032, 10)aA(3.533936)",
                "n(0.5, 2.233032, 10)aS(1)Fn(0.5, 2.233032, 10)FA(0.5339355)",
                "n(0.5, 2.174774, 10)aS(1)Fn(0.5, 2.174774, 10)FaA(1.650452)",
                "n(0.5, 2.131081, 10)aS(1)Fn(0.5, 2.131081, 10)FaA(2.737839)",
                "n(0.5, 2.09831, 10)aS(1)Fn(0.5, 2.09831, 10)FaA(3.803379)",
                "n(0.5, 2.59831, 10)aS(1)Fn(0.5, 2.59831, 10)FFA(0.8033791)",
                "n(0.5, 2.448733, 10)aS(1)Fn(0.5, 2.448733, 10)FFaA(2.102534)",
                "n(0.5, 2.33655, 10)aS(1)Fn(0.5, 2.33655, 10)FFaA(3.326901)",
                "n(0.5, 2.83655, 10)aS(1)Fn(0.5, 2.83655, 10)FFFA(0.3269007)",
            });
    }

}
