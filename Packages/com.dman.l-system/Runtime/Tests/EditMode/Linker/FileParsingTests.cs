using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemCompiler.Linker;
using NUnit.Framework;
using System.Linq;

public class FileParsingTests
{
    [Test]
    public void ParsesTopLevelFile()
    {
        var fullTopLevelFile = @"
#axiom /(20)S(1)R(0)
#iterations 1000

#symbols ZXCQWE/SRABCK
#symbols Lx

#global ZXC
#ignore QWE

#define compileTime ABC
#runtime runTimeParam 92.4

## comment
##line2

#include std.lsyslib (Key->K) (Segment->L) (Signal->x)
#include stdNoRemap.lsyslib

A -> AB";

        var parsed = new ParsedFile("root.lsystem", fullTopLevelFile, isLibrary: false);

        Assert.AreEqual("/(20)S(1)R(0)", parsed.axiom);
        Assert.AreEqual(1000, parsed.iterations);
        Assert.AreEqual("[]ZXC", parsed.globalCharacters);
        Assert.AreEqual("QWE", parsed.ignoredCharacters);

        Assert.AreEqual(1, parsed.delaredInFileCompileTimeParameters.Count);
        Assert.AreEqual("compileTime", parsed.delaredInFileCompileTimeParameters.First().name);
        Assert.AreEqual("ABC", parsed.delaredInFileCompileTimeParameters.First().replacement);

        Assert.AreEqual(1, parsed.declaredInFileRuntimeParameters.Count);
        Assert.AreEqual("runTimeParam", parsed.declaredInFileRuntimeParameters.First().name);
        Assert.AreEqual(92.4f, parsed.declaredInFileRuntimeParameters.First().defaultValue);

        Assert.AreEqual(2, parsed.links.Count);
        var standardLink = parsed.links[0];
        Assert.AreEqual("std.lsyslib", standardLink.fullImportIdentifier);

        Assert.AreEqual(3, standardLink.importedSymbols.Count);
        Assert.AreEqual(new IncludeImportRemap
        {
            importName = "Key",
            remappedSymbol = 'K'
        }, standardLink.importedSymbols[0]);
        Assert.AreEqual(new IncludeImportRemap
        {
            importName = "Segment",
            remappedSymbol = 'L'
        }, standardLink.importedSymbols[1]);
        Assert.AreEqual(new IncludeImportRemap
        {
            importName = "Signal",
            remappedSymbol = 'x'
        }, standardLink.importedSymbols[2]);

        var noRemapLink = parsed.links[1];
        Assert.AreEqual("stdNoRemap.lsyslib", noRemapLink.fullImportIdentifier);
        Assert.AreEqual(0, noRemapLink.importedSymbols.Count);

        Assert.AreEqual(1, parsed.ruleLines.Count);
        Assert.AreEqual("A -> AB", parsed.ruleLines.First());
    }
    [Test]
    public void ParsesLibraryFile()
    {
        var fullTopLevelFile = @"
#export Key W
#export Signal O

#symbols ZXCQWEABCKLxO

#global ZXC
#ignore QWE

#define compileTime ABC
#runtime runTimeParam 92.4

## comment
##line2

#include std.lsyslib (Key->K) (Segment->L) (Signal->x)

A -> AB";

        var parsed = new ParsedFile("root.lsyslib", fullTopLevelFile, isLibrary: true);

        Assert.AreEqual("[]ZXC", parsed.globalCharacters);
        Assert.AreEqual("QWE", parsed.ignoredCharacters);

        Assert.AreEqual(1, parsed.delaredInFileCompileTimeParameters.Count);
        Assert.AreEqual("compileTime", parsed.delaredInFileCompileTimeParameters.First().name);
        Assert.AreEqual("ABC", parsed.delaredInFileCompileTimeParameters.First().replacement);

        Assert.AreEqual(1, parsed.declaredInFileRuntimeParameters.Count);
        Assert.AreEqual("runTimeParam", parsed.declaredInFileRuntimeParameters.First().name);
        Assert.AreEqual(92.4f, parsed.declaredInFileRuntimeParameters.First().defaultValue);

        Assert.AreEqual(1, parsed.links.Count);
        var link = parsed.links.First();
        Assert.AreEqual("std.lsyslib", link.fullImportIdentifier);

        Assert.AreEqual(3, link.importedSymbols.Count);
        Assert.AreEqual(new IncludeImportRemap
        {
            importName = "Key",
            remappedSymbol = 'K'
        }, link.importedSymbols[0]);
        Assert.AreEqual(new IncludeImportRemap
        {
            importName = "Segment",
            remappedSymbol = 'L'
        }, link.importedSymbols[1]);
        Assert.AreEqual(new IncludeImportRemap
        {
            importName = "Signal",
            remappedSymbol = 'x'
        }, link.importedSymbols[2]);


        Assert.AreEqual(1, parsed.ruleLines.Count);
        Assert.AreEqual("A -> AB", parsed.ruleLines.First());

        Assert.AreEqual(2, parsed.exports.Count);
        Assert.AreEqual(new ExportDirective
        {
            name = "Key",
            exportedSymbol = 'W'
        }, parsed.exports[0]);
        Assert.AreEqual(new ExportDirective
        {
            name = "Signal",
            exportedSymbol = 'O'
        }, parsed.exports[1]);
    }
}
