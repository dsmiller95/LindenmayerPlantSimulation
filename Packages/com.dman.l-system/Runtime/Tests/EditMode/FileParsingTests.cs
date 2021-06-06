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

#global ZXC
#ignore QWE

#define compileTime ABC
#runtime runTimeParam 92.4

## comment
##line2

#include ./std.lsyslib (Key->K) (Segment->L) (Signal->x)

A -> AB";

        var parsed = new ParsedFile(fullTopLevelFile, false);

        Assert.AreEqual("/(20)S(1)R(0)", parsed.axiom);
        Assert.AreEqual(1000, parsed.iterations);
        Assert.AreEqual("ZXC", parsed.globalCharacters);
        Assert.AreEqual("QWE", parsed.ignoredCharacters);

        Assert.AreEqual(1, parsed.globalCompileTimeParameters.Count);
        Assert.AreEqual("compileTime", parsed.globalCompileTimeParameters.First().name);
        Assert.AreEqual("ABC", parsed.globalCompileTimeParameters.First().replacement);

        Assert.AreEqual(1, parsed.globalRuntimeParameters.Count);
        Assert.AreEqual("runTimeParam", parsed.globalRuntimeParameters.First().name);
        Assert.AreEqual(92.4f, parsed.globalRuntimeParameters.First().defaultValue);

        Assert.AreEqual(1, parsed.links.Count);
        var link = parsed.links.First();
        Assert.AreEqual("./std.lsyslib", link.relativeImportPath);

        Assert.AreEqual(3, link.importedSymbols.Count);
        Assert.AreEqual(new SymbolRemap
        {
            importName = "Key",
            remappedSymbol = 'K'
        }, link.importedSymbols[0]);
        Assert.AreEqual(new SymbolRemap
        {
            importName = "Segment",
            remappedSymbol = 'L'
        }, link.importedSymbols[1]);
        Assert.AreEqual(new SymbolRemap
        {
            importName = "Signal",
            remappedSymbol = 'x'
        }, link.importedSymbols[2]);


        Assert.AreEqual(1, parsed.ruleLines.Count);
        Assert.AreEqual("A -> AB", parsed.ruleLines.First());
    }
    [Test]
    public void ParsesLibraryFile()
    {
        var fullTopLevelFile = @"
#export Key W
#export Signal O

#global ZXC
#ignore QWE

#define compileTime ABC
#runtime runTimeParam 92.4

## comment
##line2

#include ./std.lsyslib (Key->K) (Segment->L) (Signal->x)

A -> AB";

        var parsed = new ParsedFile(fullTopLevelFile, true);

        Assert.AreEqual("ZXC", parsed.globalCharacters);
        Assert.AreEqual("QWE", parsed.ignoredCharacters);

        Assert.AreEqual(1, parsed.globalCompileTimeParameters.Count);
        Assert.AreEqual("compileTime", parsed.globalCompileTimeParameters.First().name);
        Assert.AreEqual("ABC", parsed.globalCompileTimeParameters.First().replacement);

        Assert.AreEqual(1, parsed.globalRuntimeParameters.Count);
        Assert.AreEqual("runTimeParam", parsed.globalRuntimeParameters.First().name);
        Assert.AreEqual(92.4f, parsed.globalRuntimeParameters.First().defaultValue);

        Assert.AreEqual(1, parsed.links.Count);
        var link = parsed.links.First();
        Assert.AreEqual("./std.lsyslib", link.relativeImportPath);

        Assert.AreEqual(3, link.importedSymbols.Count);
        Assert.AreEqual(new SymbolRemap
        {
            importName = "Key",
            remappedSymbol = 'K'
        }, link.importedSymbols[0]);
        Assert.AreEqual(new SymbolRemap
        {
            importName = "Segment",
            remappedSymbol = 'L'
        }, link.importedSymbols[1]);
        Assert.AreEqual(new SymbolRemap
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
