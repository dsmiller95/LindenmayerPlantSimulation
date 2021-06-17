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
        var linker = new FileLinker(fileSystem);
        var linkedSet = linker.LinkFiles("root.lsystem");

        var rootFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["root.lsystem"]];
        Assert.AreEqual(5, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(5, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XYZ[]").Count());

        var libFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib.lsyslib"]];
        Assert.AreEqual(4, libFile.allSymbolAssignments.Count);
        Assert.AreEqual(4, libFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB[]").Count());

        var importedInRoot = rootFile.GetSymbolInFile('X');
        var exportedInLib = libFile.GetSymbolInFile('B');
        Assert.AreEqual(exportedInLib, importedInRoot);
    }
    [Test]
    public void LinksSimpleDependencyAndAssociatesGlobals()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XYZDE

#global DE

#include lib.lsyslib (Exported->X)
Y -> ZX
");

        fileSystem.RegisterFileWithIdentifier("lib.lsyslib", @"
#symbols ABDE
#export Exported B

#global D

B -> AB
");
        var linker = new FileLinker(fileSystem);
        var linkedSet = linker.LinkFiles("root.lsystem");

        var rootFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["root.lsystem"]];
        Assert.AreEqual(7, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(7, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XYZDE[]").Count());

        var libFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib.lsyslib"]];
        Assert.AreEqual(6, libFile.allSymbolAssignments.Count);
        Assert.AreEqual(6, libFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("ABDE[]").Count());

        var importedInRoot = rootFile.GetSymbolInFile('X');
        var exportedInLib = libFile.GetSymbolInFile('B');
        Assert.AreEqual(exportedInLib, importedInRoot);

        Assert.AreEqual(rootFile.GetSymbolInFile('D'), libFile.GetSymbolInFile('D'));
        Assert.AreNotEqual(rootFile.GetSymbolInFile('E'), libFile.GetSymbolInFile('E'));

        Assert.AreEqual(rootFile.GetSymbolInFile('['), libFile.GetSymbolInFile('['));
        Assert.AreEqual(rootFile.GetSymbolInFile(']'), libFile.GetSymbolInFile(']'));
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
        var linker = new FileLinker(fileSystem);
        var linkedSet = linker.LinkFiles("root.lsystem");

        var rootFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["root.lsystem"]];
        Assert.AreEqual(5, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(5, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XYZ[]").Count());

        var libFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib.lsyslib"]];
        Assert.AreEqual(4, libFile.allSymbolAssignments.Count);
        Assert.AreEqual(4, libFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB[]").Count());

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
        var linker = new FileLinker(fileSystem);
        var linkedSet = linker.LinkFiles("root.lsystem");

        var rootFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["root.lsystem"]];
        Assert.AreEqual(4, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(4, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XY[]").Count());

        var lib0File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib0.lsyslib"]];
        Assert.AreEqual(4, lib0File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib0File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB[]").Count());

        var lib1File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib1.lsyslib"]];
        Assert.AreEqual(4, lib1File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib1File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("CD[]").Count());

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

        var linker = new FileLinker(fileSystem);
        var linkedSet = linker.LinkFiles("root.lsystem");

        var rootFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["root.lsystem"]];
        Assert.AreEqual(4, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(4, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XY[]").Count());

        var lib0File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib0.lsyslib"]];
        Assert.AreEqual(4, lib0File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib0File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB[]").Count());

        var lib1File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib1.lsyslib"]];
        Assert.AreEqual(4, lib1File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib1File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("CD[]").Count());

        var lib2File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib2.lsyslib"]];
        Assert.AreEqual(4, lib2File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib2File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("EF[]").Count());

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
        var linker = new FileLinker(fileSystem);
        var linkedSet = linker.LinkFiles("root.lsystem");

        var rootFile = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["root.lsystem"]];
        Assert.AreEqual(4, rootFile.allSymbolAssignments.Count);
        Assert.AreEqual(4, rootFile.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("XY[]").Count());

        var lib0File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib0.lsyslib"]];
        Assert.AreEqual(4, lib0File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib0File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("AB[]").Count());

        var lib1File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib1.lsyslib"]];
        Assert.AreEqual(4, lib1File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib1File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("CD[]").Count());

        var lib2File = linkedSet.allFiles[linkedSet.fileIndexesByFullIdentifier["lib2.lsyslib"]];
        Assert.AreEqual(4, lib2File.allSymbolAssignments.Count);
        Assert.AreEqual(4, lib2File.allSymbolAssignments.Select(x => x.sourceCharacter).Intersect("EF[]").Count());

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
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsystem");
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
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsystem");
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
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsystem");
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
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsystem");
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
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsystem");
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.IMPORT_DISSONANCE, e.exceptionType);
            return;
        }
        Assert.Fail("linker must prevent importing symbols in such a way that would cause a single symbol to be represented as multiple character");
    }

    [Test]
    public void CatchesCompileTimeVariableCollision()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XY

#define compileTime IEKA
#include lib0.lsyslib
");
        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#define compileTime IEKA
#export Exported A
");
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsystem");
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.GLOBAL_VARIABLE_COLLISION, e.exceptionType);
            return;
        }
        Assert.Fail("linker must prevent declaration of multiple global replacements with the same name");
    }
    [Test]
    public void CatchesRunTimeVariableCollision()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsystem", @"
#axiom Y
#iterations 1000
#symbols XY

#runtime runTime 1992.01
#include lib0.lsyslib
");

        fileSystem.RegisterFileWithIdentifier("lib0.lsyslib", @"
#symbols AB
#runtime runTime 1992.01
#export Exported A
");
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsystem");
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.GLOBAL_VARIABLE_COLLISION, e.exceptionType);
            return;
        }
        Assert.Fail("linker must prevent declaration of multiple global runtime variables with the same name");
    }
    [Test]
    public void CatchesLibraryAsOriginError()
    {
        var fileSystem = new InMemoryFileProvider();

        fileSystem.RegisterFileWithIdentifier("root.lsyslib", @"
#symbols AB
#export Exported A
");
        var linker = new FileLinker(fileSystem);
        try
        {
            linker.LinkFiles("root.lsyslib");
        }
        catch (LinkException e)
        {
            Assert.AreEqual(LinkExceptionType.BASE_FILE_IS_LIBRARY, e.exceptionType);
            return;
        }
        Assert.Fail("linker must not allow the root file to be a library file");
    }
    #endregion
}
