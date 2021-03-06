using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System.Linq;

public class SymbolSeriesGraphTests
{
    [Test]
    public void ComputesSeriesGraph()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("ABC");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(4, seriesMatcher.graphChildPointers.Length);
        Assert.AreEqual(1, seriesMatcher.graphChildPointers[0][0]);
        Assert.AreEqual(2, seriesMatcher.graphChildPointers[1][0]);
        Assert.AreEqual(3, seriesMatcher.graphChildPointers[2][0]);
        Assert.AreEqual(0, seriesMatcher.graphChildPointers[3].Length);
    }
    [Test]
    public void ComputesGraphWithBranch()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B]C");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -1, -2, 0, -2, 0 }));
    }
    [Test]
    public void ComputesGraphWithSeveralBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -1, -2, 0, -2, 2, -2, -2, -2, 0, -2 }));
    }
    [Test]
    public void ComputesGraphWithBranchesAtRoot()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -2, -1, -2, 1, -2, -2, -2, -1, -2 }));
    }
    [Test]
    public void SimplifiesSimpleDeeplyNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -1, -2, -2, 0, -2, -2 }));
    }

    [Test]
    public void SimplifiesComplexDeeplyNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E][B]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -1, -2, -2, 0, -2, -2, 0, -2, -2 }));
    }
    [Test]
    public void SimplifiesComplexDeeplyNestedBranchesWithNoInitial()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[[E][B]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -2, -2, -1, -2, -2, -1, -2, -2 }));
    }
    [Test]
    public void SimplifiesOtherNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E]B]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -1, -2, -2, 0, -2, 0, -2 }));
    }
    [Test]
    public void PreservesNestedWithIntermediateSymbol()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.graphParentPointers.SequenceEqual(new int[] { -1, -2, 0, -2, 2, -2, -2 }));
    }

}
