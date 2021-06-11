using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemCompiler.Linker;
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
}
