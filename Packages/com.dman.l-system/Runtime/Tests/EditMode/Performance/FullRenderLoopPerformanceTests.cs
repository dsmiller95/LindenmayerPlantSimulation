using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.UnityObjects;
using NUnit.Framework;
using System.Linq;
using Unity.PerformanceTesting;
using UnityEngine;

public class FullRenderLoopPerformanceTests
{
    [Test, Performance]
    public void SimpleLineLSystemStepsAndRenders()
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
        var systemObject = LSystemObject.GetNewLSystemFromFiles(linkedFiles);



    }


}
