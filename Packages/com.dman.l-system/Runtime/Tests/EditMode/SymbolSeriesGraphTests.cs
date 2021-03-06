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
        Assert.AreEqual(6, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { 2, 5 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.AreEqual(0, seriesMatcher.graphChildPointers[3].Length);
        Assert.AreEqual(0, seriesMatcher.graphChildPointers[4].Length);
        Assert.AreEqual(0, seriesMatcher.graphChildPointers[5].Length);
    }
    [Test]
    public void ComputesGraphWithSeveralBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(11, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { 2, 8 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[4].SequenceEqual(new int[] { 5 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[6].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[7].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[8].SequenceEqual(new int[] { 9 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[9].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[10].SequenceEqual(new int[] { }));
    }
    [Test]
    public void ComputesGraphWithBranchesAtRoot()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(10, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 1, 7 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { 2 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[4].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[6].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[7].SequenceEqual(new int[] { 8 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[8].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[9].SequenceEqual(new int[] { }));
    }
    [Test]
    public void SimplifiesSimpleDeeplyNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(7, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[4].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[6].SequenceEqual(new int[] { }));
    }

    [Test]
    public void SimplifiesComplexDeeplyNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E][B]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(10, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { 3, 6 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[4].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[6].SequenceEqual(new int[] { 7 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[7].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[8].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[9].SequenceEqual(new int[] { }));
    }
    [Test]
    public void SimplifiesComplexDeeplyNestedBranchesWithNoInitial()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[[E][B]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(9, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 2, 5 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[3].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[4].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[5].SequenceEqual(new int[] { 6 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[6].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[7].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[8].SequenceEqual(new int[] { }));
    }
    [Test]
    public void SimplifiesOtherNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E]B]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(8, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { 2, 3 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { 6 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[4].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[6].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[7].SequenceEqual(new int[] { }));
    }
    [Test]
    public void PreservesNestedWithIntermediateSymbol()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(8, seriesMatcher.graphChildPointers.Length);
        Assert.IsTrue(seriesMatcher.graphChildPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[1].SequenceEqual(new int[] { 2 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[4].SequenceEqual(new int[] { 5 }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[6].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphChildPointers[7].SequenceEqual(new int[] { }));
    }

}
