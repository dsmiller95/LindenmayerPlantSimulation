using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemCompiler.Linker.Builtin;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class LinkCompilingTests
{
    [Test]
    public void LinksSimpleDependencyAndExecutes()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 10
#symbols XY

#include lib.lsyslib (Exported->X)
Y -> YX
");

        fileSystem.RegisterFileWithIdentifier("lib.lsyslib", @"
#symbols AB
#export Exported B

B -> AB
");
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");

        using var system = linkedFiles.CompileSystem();
        LSystemState<float> currentState = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var X = linkedFiles.GetSymbol("root.lsystem", 'X');
        var Y = linkedFiles.GetSymbol("root.lsystem", 'Y');
        var A = linkedFiles.GetSymbol("lib.lsyslib", 'A');
        var B = linkedFiles.GetSymbol("lib.lsyslib", 'B');

        Assert.AreEqual(B, X);

        var symbolStringMapping = new Dictionary<int, char>()
        {
            {X, 'X' },
            {Y, 'Y' },
            {A, 'A' }
        };
        /**
         * system equivalent with remapped symbol names:
         * Y -> YX
         * X -> AX
         */

        Assert.AreEqual("Y", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("YX", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("YXAX", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("YXAXAAX", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("YXAXAAXAAAX", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState.currentSymbols.DisposeImmediate();
    }

    [Test]
    public void LinksDependencyWithBranchingSymbolsAndExecutes()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 10
#symbols XY

#include lib.lsyslib (Exported->X)
Y -> Y[X]
");

        fileSystem.RegisterFileWithIdentifier("lib.lsyslib", @"
#symbols AB
#export Exported B

B -> [A]B
");
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");

        using var system = linkedFiles.CompileSystem();
        LSystemState<float> currentState = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var lBracket = linkedFiles.GetSymbol("root.lsystem", '[');
        var rBracket = linkedFiles.GetSymbol("root.lsystem", ']');
        var X = linkedFiles.GetSymbol("root.lsystem", 'X');
        var Y = linkedFiles.GetSymbol("root.lsystem", 'Y');
        var A = linkedFiles.GetSymbol("lib.lsyslib", 'A');
        var B = linkedFiles.GetSymbol("lib.lsyslib", 'B');

        Assert.AreEqual(B, X);

        var symbolStringMapping = new Dictionary<int, char>()
        {
            {lBracket, '[' },
            {rBracket, ']' },
            {X, 'X' },
            {Y, 'Y' },
            {A, 'A' }
        };
        /**
         * system equivalent with remapped symbol names:
         * Y -> Y[X]
         * X -> [A]X
         */

        Assert.AreEqual("Y", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y[X]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y[X][[A]X]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y[X][[A]X][[A][A]X]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y[X][[A]X][[A][A]X][[A][A][A]X]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState.currentSymbols.DisposeImmediate();
    }
    [Test]
    public void LinksDependencyWithContextualMatchesAndIgnoresNonImportedSymbols()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y(0)A(3)
#iterations 10
#symbols XYA
#matches XYA

#include nodes.lsyslib (Node->X) (Root->Y)
A(x) : x > 0 -> I(2)[X(x)]A(x - 1)
A(x) : x <= 0 -> J

#symbols IFJ
#matches IJ
I(x) : x > 0 -> I(x - 1)F
J -> 
I(x) > J -> JI(x)
");

        fileSystem.RegisterFileWithIdentifier("nodes.lsyslib", @"
#symbols AB
#matches AB
#export Node B
#export Root A

A(y) < B(x) : x > 0 -> B(x - 1)
       A(x) > [B(y)] : y > 0 -> A(x + 1)
       A(x) > [B(y)][B(z)] : y > 0 && z > 0 -> A(x + 2)
       A(x) > [B(y)][B(z)][B(a)] : y > 0 && z > 0 && a > 0 -> A(x + 3)
       A(x) > [B(y)][B(z)][B(a)][B(b)] : y > 0 && z > 0 && a > 0 && b > 0 -> A(x + 4)

");
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");

        using var system = linkedFiles.CompileSystem();
        LSystemState<float> currentState = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var lBracket = linkedFiles.GetSymbol("root.lsystem", '[');
        var rBracket = linkedFiles.GetSymbol("root.lsystem", ']');
        var XRoot = linkedFiles.GetSymbol("root.lsystem", 'X');
        var YRoot = linkedFiles.GetSymbol("root.lsystem", 'Y');
        var ARoot = linkedFiles.GetSymbol("root.lsystem", 'A');
        var IRoot = linkedFiles.GetSymbol("root.lsystem", 'I');
        var FRoot = linkedFiles.GetSymbol("root.lsystem", 'F');
        var JRoot = linkedFiles.GetSymbol("root.lsystem", 'J');
        var Alib = linkedFiles.GetSymbol("nodes.lsyslib", 'A');
        var Blib = linkedFiles.GetSymbol("nodes.lsyslib", 'B');

        Assert.AreEqual(Alib, YRoot);
        Assert.AreEqual(Blib, XRoot);

        var symbolStringMapping = new Dictionary<int, char>()
        {
            {lBracket, '[' },
            {rBracket, ']' },
            {XRoot, 'X' },
            {YRoot, 'Y' },
            {ARoot, 'A' },
            {IRoot, 'I' },
            {FRoot, 'F' },
            {JRoot, 'J' }
        };

        Assert.AreEqual("Y(0)A(3)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(0)I(2)[X(3)]A(2)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(1)I(1)F[X(2)]I(2)[X(2)]A(1)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(3)I(0)FF[X(1)]I(1)F[X(1)]I(2)[X(1)]A(0)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(6)I(0)FF[X(0)]I(0)FF[X(0)]I(1)F[X(0)]J", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(6)I(0)FF[X(0)]I(0)FF[X(0)]JI(1)F[X(0)]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(6)I(0)FF[X(0)]JI(0)FF[X(0)]I(0)FF[X(0)]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(6)JI(0)FF[X(0)]I(0)FF[X(0)]I(0)FF[X(0)]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("Y(6)I(0)FF[X(0)]I(0)FF[X(0)]I(0)FF[X(0)]", currentState.currentSymbols.Data.ToString(symbolStringMapping));


        currentState.currentSymbols.DisposeImmediate();
    }
    [Test]
    public void LinkCompilesBranchingSymbolWithContextMatches()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom AF[FB]
#iterations 10
#symbols FAB
#matches AB

#symbols CD
#matches CD
    A > [B] -> C
A < B     -> D
");
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");

        using var system = linkedFiles.CompileSystem();
        LSystemState<float> currentState = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var lBracket = linkedFiles.GetSymbol("root.lsystem", '[');
        var rBracket = linkedFiles.GetSymbol("root.lsystem", ']');
        var FRoot = linkedFiles.GetSymbol("root.lsystem", 'F');
        var ARoot = linkedFiles.GetSymbol("root.lsystem", 'A');
        var BRoot = linkedFiles.GetSymbol("root.lsystem", 'B');
        var CRoot = linkedFiles.GetSymbol("root.lsystem", 'C');
        var DRoot = linkedFiles.GetSymbol("root.lsystem", 'D');


        var symbolStringMapping = new Dictionary<int, char>()
        {
            {lBracket, '[' },
            {rBracket, ']' },
            {FRoot, 'F' },
            {ARoot, 'A' },
            {BRoot, 'B' },
            {CRoot, 'C' },
            {DRoot, 'D' }
        };

        Assert.AreEqual("AF[FB]", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("CF[FD]", currentState.currentSymbols.Data.ToString(symbolStringMapping));


        currentState.currentSymbols.DisposeImmediate();
    }

    [Test]
    public void LinksBuiltinLibraryAndExecutes()
    {
        var builtins = new BuiltinLibraries();
        builtins.Add(new DiffusionLibrary());

        var fileSystem = new InMemoryFileProvider(builtins);

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom n(0.5, 0, 10)ST(3)
#iterations 10
#symbols naFTS

#include diffusion (Node->n) (Amount->a)

T(x) : x > 0 -> n(.5, 0, 10)FT(x - 1)
S -> a(3)S
");
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");

        using var system = linkedFiles.CompileSystem();
        LSystemState<float> currentState = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var n = linkedFiles.GetSymbol("root.lsystem", 'n');
        var a = linkedFiles.GetSymbol("root.lsystem", 'a');
        var F = linkedFiles.GetSymbol("root.lsystem", 'F');
        var T = linkedFiles.GetSymbol("root.lsystem", 'T');
        var S = linkedFiles.GetSymbol("root.lsystem", 'S');

        var symbolStringMapping = new Dictionary<int, char>()
        {
            {n, 'n' },
            {a, 'a' },
            {F, 'F' },
            {T, 'T' },
            {S, 'S' },
        };

        Assert.AreEqual("n(0.5, 0, 10)ST(3)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.5, 0, 10)a(3)Sn(0.5, 0, 10)FT(2)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.5, 1.5, 10)a(3)Sn(0.5, 1.5, 10)Fn(0.5, 0, 10)FT(1)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.5, 3, 10)a(3)Sn(0.5, 2.25, 10)Fn(0.5, 0.75, 10)Fn(0.5, 0, 10)FT(0)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.5, 4.125, 10)a(3)Sn(0.5, 3.375, 10)Fn(0.5, 1.125, 10)Fn(0.5, 0.375, 10)FT(0)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.5, 5.25, 10)a(3)Sn(0.5, 4.125, 10)Fn(0.5, 1.875, 10)Fn(0.5, 0.75, 10)FT(0)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.5, 6.1875, 10)a(3)Sn(0.5, 5.0625, 10)Fn(0.5, 2.4375, 10)Fn(0.5, 1.3125, 10)FT(0)", currentState.currentSymbols.Data.ToString(symbolStringMapping));

        currentState.currentSymbols.DisposeImmediate();
    }
    [Test]
    public void LinksBuiltinLibraryWithCustomParameterAndExecutes()
    {
        var builtins = new BuiltinLibraries();
        builtins.Add(new DiffusionLibrary());

        var fileSystem = new InMemoryFileProvider(builtins);

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom n(0.25, 18, 20)Fn(0.25, 0, 20)Fn(0.25, 0, 20)
#iterations 10
#symbols naF

#define diffusionStepsPerStep 2
#include diffusion (Node->n) (Amount->a)

");
        var linker = new FileLinker(fileSystem);
        var linkedFiles = linker.LinkFiles("root.lsystem");

        using var system = linkedFiles.CompileSystem();
        LSystemState<float> currentState = new DefaultLSystemState(
            linkedFiles.GetAxiom(),
            (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        var n = linkedFiles.GetSymbol("root.lsystem", 'n');
        var a = linkedFiles.GetSymbol("root.lsystem", 'a');
        var F = linkedFiles.GetSymbol("root.lsystem", 'F');

        var symbolStringMapping = new Dictionary<int, char>()
        {
            {n, 'n' },
            {a, 'a' },
            {F, 'F' },
        };

        Assert.AreEqual("n(0.25, 18, 20)Fn(0.25, 0, 20)Fn(0.25, 0, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 11.25, 20)Fn(0.25, 5.625, 20)Fn(0.25, 1.125, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 8.859375, 20)Fn(0.25, 5.976563, 20)Fn(0.25, 3.164063, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 7.602539, 20)Fn(0.25, 5.998535, 20)Fn(0.25, 4.398926, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 6.901062, 20)Fn(0.25, 5.999908, 20)Fn(0.25, 5.09903, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 6.506824, 20)Fn(0.25, 5.999994, 20)Fn(0.25, 5.493181, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 6.285088, 20)Fn(0.25, 6, 20)Fn(0.25, 5.714913, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 6.160362, 20)Fn(0.25, 6, 20)Fn(0.25, 5.839638, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));
        currentState = system.StepSystem(currentState);
        Assert.AreEqual("n(0.25, 6.090203, 20)Fn(0.25, 6, 20)Fn(0.25, 5.909797, 20)", currentState.currentSymbols.Data.ToString(symbolStringMapping));

        currentState.currentSymbols.DisposeImmediate();
    }
}
