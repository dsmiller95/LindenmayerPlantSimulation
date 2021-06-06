using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemCompiler.Linker;
using NUnit.Framework;
using System.Linq;

public class LinkImportExportTests
{
    [Test]
    public void LinksSimpleDependency()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XYZ

#include lib.lsyslib (Exported->X)
Y -> ZX
"); 
        
        fileSystem.RegisterFileWithIdentifier("lib.lsyslib", @"
#symbols AB
#export Exported B

B -> AB
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        linker.LinkFiles();

        var rootFile = linker.allFilesByFullIdentifier["root.lsystem"];
        Assert.AreEqual(3, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(3, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XYZ").Count());

        var libFile = linker.allFilesByFullIdentifier["lib.lsyslib"];
        Assert.AreEqual(2, libFile.allSymbolAssignments.Count);
        Assert.AreEqual(2, libFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB").Count());

        var importedInRoot = rootFile.GetSymbolInFile('X');
        var exportedInLib = libFile.GetSymbolInFile('B');
        Assert.AreEqual(exportedInLib, importedInRoot);
    }
    [Test]
    public void LinksDependencyWithoutRemapping()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XYZ

#include lib.lsyslib
Y -> ZX
");

        fileSystem.RegisterFileWithIdentifier("lib.lsyslib", @"
#symbols AB
#export Exported B

B -> AB
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        linker.LinkFiles();

        var rootFile = linker.allFilesByFullIdentifier["root.lsystem"];
        Assert.AreEqual(3, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(3, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XYZ").Count());

        var libFile = linker.allFilesByFullIdentifier["lib.lsyslib"];
        Assert.AreEqual(2, libFile.allSymbolAssignments.Count);
        Assert.AreEqual(2, libFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB").Count());

        var exportedInLib = libFile.GetSymbolInFile('B');
        Assert.IsFalse(rootFile.allSymbolAssignments.Any(x => x.remappedSymbol == exportedInLib));
    }

    [Test]
    public void LinksChainedDependency()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XY

#include lib0.lsyslib (Exported->X)
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#include lib1.lsyslib (Exported->A)
#export Exported A
");
        fileSystem.RegisterFileWithIdentifier("lib1.lsyslib", @"
#symbols CD
#export Exported C
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        linker.LinkFiles();

        var rootFile = linker.allFilesByFullIdentifier["root.lsystem"];
        Assert.AreEqual(2, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(2, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XY").Count());

        var lib0File = linker.allFilesByFullIdentifier["lib0.lsyslib"];
        Assert.AreEqual(2, lib0File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib0File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB").Count());

        var lib1File = linker.allFilesByFullIdentifier["lib1.lsyslib"];
        Assert.AreEqual(2, lib1File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib1File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("CD").Count());

        var importedInRoot = rootFile.GetSymbolInFile('X');
        var exportedInLib0 = lib0File.GetSymbolInFile('A');
        var exportedInLib1 = lib1File.GetSymbolInFile('C');
        Assert.AreEqual(exportedInLib1, exportedInLib0);
        Assert.AreEqual(exportedInLib1, importedInRoot);
    }
    [Test]
    public void LinksForkedChainDependency()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XY

#include lib0.lsyslib (Exported->X)
#include lib1.lsyslib (Exported->Y)
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#include lib2.lsyslib (Exported1->A)
#export Exported A
");
        fileSystem.RegisterFileWithIdentifier("lib1.lsyslib", @"
#symbols CD
#include lib2.lsyslib (Exported2->C)
#export Exported C
");
        fileSystem.RegisterFileWithIdentifier("lib2.lsyslib", @"
#symbols EF
#export Exported1 E
#export Exported2 F
");

        var linker = new FileLinker(fileSystem, "root.lsystem");
        linker.LinkFiles();

        var rootFile = linker.allFilesByFullIdentifier["root.lsystem"];
        Assert.AreEqual(2, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(2, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XY").Count());

        var lib0File = linker.allFilesByFullIdentifier["lib0.lsyslib"];
        Assert.AreEqual(2, lib0File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib0File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB").Count());

        var lib1File = linker.allFilesByFullIdentifier["lib1.lsyslib"];
        Assert.AreEqual(2, lib1File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib1File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("CD").Count());

        var lib2File = linker.allFilesByFullIdentifier["lib2.lsyslib"];
        Assert.AreEqual(2, lib2File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib2File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("EF").Count());

        var importedInRoot = rootFile.GetSymbolInFile('X');
        var exportedInLib0 = lib0File.GetSymbolInFile('A');
        var exportedInLib2 = lib2File.GetSymbolInFile('E');
        Assert.AreEqual(exportedInLib2, exportedInLib0);
        Assert.AreEqual(exportedInLib2, importedInRoot);


        importedInRoot = rootFile.GetSymbolInFile('Y');
        var exportedInLib1 = lib1File.GetSymbolInFile('C');
        exportedInLib2 = lib2File.GetSymbolInFile('F');
        Assert.AreEqual(exportedInLib2, exportedInLib1);
        Assert.AreEqual(exportedInLib2, importedInRoot);
    }

    [Test]
    public void AllowsImportCollisionToPreventDissonance()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XY

#include lib0.lsyslib (Exported->X)
#include lib1.lsyslib (Exported->X)
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#include lib2.lsyslib (Exported->A)
#export Exported A
");
        fileSystem.RegisterFileWithIdentifier("lib1.lsyslib", @"
#symbols CD
#include lib2.lsyslib (Exported->C)
#export Exported C
");
        fileSystem.RegisterFileWithIdentifier("lib2.lsyslib", @"
#symbols EF
#export Exported E
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        linker.LinkFiles();

        var rootFile = linker.allFilesByFullIdentifier["root.lsystem"];
        Assert.AreEqual(2, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(2, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XY").Count());

        var lib0File = linker.allFilesByFullIdentifier["lib0.lsyslib"];
        Assert.AreEqual(2, lib0File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib0File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB").Count());

        var lib1File = linker.allFilesByFullIdentifier["lib1.lsyslib"];
        Assert.AreEqual(2, lib1File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib1File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("CD").Count());

        var lib2File = linker.allFilesByFullIdentifier["lib2.lsyslib"];
        Assert.AreEqual(2, lib2File.allSymbolAssignments.Count);
        Assert.AreEqual(2, lib2File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("EF").Count());

        var importedInRoot = rootFile.GetSymbolInFile('X');
        var exportedInLib0 = lib0File.GetSymbolInFile('A');
        var exportedInLib1 = lib1File.GetSymbolInFile('C');
        var exportedInLib2 = lib2File.GetSymbolInFile('E');
        Assert.AreEqual(exportedInLib2, exportedInLib1);
        Assert.AreEqual(exportedInLib2, exportedInLib0);
        Assert.AreEqual(exportedInLib2, importedInRoot);
    }

    #region Nice Exceptions
    [Test]
    public void CatchesSimpleCycle()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XYZ

#include lib0.lsyslib (Exported->X)
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#include lib1.lsyslib (Exported->B)
#export Exported A
");
        fileSystem.RegisterFileWithIdentifier("lib1.lsyslib", @"
#symbols CD
#include lib2.lsyslib (Exported->D)
#export Exported C
"); ;
        fileSystem.RegisterFileWithIdentifier("lib2.lsyslib", @"
#symbols EF
#include lib0.lsyslib (Exported->F)
#export Exported E
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        try
        {
            linker.LinkFiles();
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.CYCLIC_DEPENDENCY, e.exceptionType);
            return;
        }
        Assert.Fail("linker must prevent cyclic dependencies");
    }

    [Test]
    public void CatchesMissingInclude()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XYZ

#include missinglib.lsyslib
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        try
        {
            linker.LinkFiles();
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.MISSING_FILE, e.exceptionType);
            return;
        }
        Assert.Fail("linker must gracefully fail when it cannot find a file");
    }

    [Test]
    public void CatchesMissingExport()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XYZ

#include lib0.lsyslib (NotExported->X)
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#export Exported A
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        try
        {
            linker.LinkFiles();
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.MISSING_EXPORT, e.exceptionType);
            return;
        }
        Assert.Fail("linker must prevent importing symbols which are not exported");
    }


    [Test]
    public void CatchesSimpleImportCollision()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XY

#include lib0.lsyslib (Exported->X)
#include lib1.lsyslib (Exported->X)
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#export Exported A
");
        fileSystem.RegisterFileWithIdentifier("lib1.lsyslib", @"
#symbols CD
#export Exported C
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        try
        {
            linker.LinkFiles();
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.IMPORT_COLLISION, e.exceptionType);
            return;
        }
        Assert.Fail("linker must prevent importing symbols in such a way that it attempts to represent multiple symbols with the same character");
    }

    [Test]
    public void CatchesSimpleImportDissonance()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XY

#include lib0.lsyslib (Exported->X)
#include lib1.lsyslib (Exported->Y)
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#include lib2.lsyslib (Exported->A)
#export Exported A
");
        fileSystem.RegisterFileWithIdentifier("lib1.lsyslib", @"
#symbols CD
#include lib2.lsyslib (Exported->C)
#export Exported C
");
        fileSystem.RegisterFileWithIdentifier("lib2.lsyslib", @"
#symbols EF
#export Exported E
");
        var linker = new FileLinker(fileSystem, "root.lsystem");
        try
        {
            linker.LinkFiles();
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.IMPORT_DISSONANCE, e.exceptionType);
            return;
        }
        Assert.Fail("linker must prevent importing symbols in such a way that would cause a single symbol to be represented as multiple character");
    }
    #endregion
}
