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
        Assert.AreEqual(4, seriesMatcher.graphPointers.Length);
        Assert.AreEqual(1, seriesMatcher.graphPointers[0][0]);
        Assert.AreEqual(2, seriesMatcher.graphPointers[1][0]);
        Assert.AreEqual(3, seriesMatcher.graphPointers[2][0]);
        Assert.AreEqual(0, seriesMatcher.graphPointers[3].Length);
    }
    [Test]
    public void ComputesGraphWithBranch()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B]C");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(6, seriesMatcher.graphPointers.Length);
        Assert.IsTrue(seriesMatcher.graphPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphPointers[1].SequenceEqual(new int[] { 2, 5 }));
        Assert.IsTrue(seriesMatcher.graphPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.AreEqual(0, seriesMatcher.graphPointers[3].Length);
        Assert.AreEqual(0, seriesMatcher.graphPointers[4].Length);
        Assert.AreEqual(0, seriesMatcher.graphPointers[5].Length);
    }
    [Test]
    public void ComputesGraphWithSeveralBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(11, seriesMatcher.graphPointers.Length);
        Assert.IsTrue(seriesMatcher.graphPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphPointers[1].SequenceEqual(new int[] { 2, 8 }));
        Assert.IsTrue(seriesMatcher.graphPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphPointers[4].SequenceEqual(new int[] { 5 }));
        Assert.IsTrue(seriesMatcher.graphPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[6].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[7].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[8].SequenceEqual(new int[] { 9 }));
        Assert.IsTrue(seriesMatcher.graphPointers[9].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[10].SequenceEqual(new int[] { }));
    }
    [Test]
    public void ComputesGraphWithBranchesAtRoot()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(10, seriesMatcher.graphPointers.Length);
        Assert.IsTrue(seriesMatcher.graphPointers[0].SequenceEqual(new int[] { 1, 7 }));
        Assert.IsTrue(seriesMatcher.graphPointers[1].SequenceEqual(new int[] { 2 }));
        Assert.IsTrue(seriesMatcher.graphPointers[2].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphPointers[4].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[6].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[7].SequenceEqual(new int[] { 8 }));
        Assert.IsTrue(seriesMatcher.graphPointers[8].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[9].SequenceEqual(new int[] { }));
    }
    [Test]
    public void SimplifiesSimpleDeeplyNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(7, seriesMatcher.graphPointers.Length);
        Assert.IsTrue(seriesMatcher.graphPointers[0].SequenceEqual(new int[] { 1 }));
        Assert.IsTrue(seriesMatcher.graphPointers[1].SequenceEqual(new int[] { 3 }));
        Assert.IsTrue(seriesMatcher.graphPointers[2].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[3].SequenceEqual(new int[] { 4 }));
        Assert.IsTrue(seriesMatcher.graphPointers[4].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[5].SequenceEqual(new int[] { }));
        Assert.IsTrue(seriesMatcher.graphPointers[5].SequenceEqual(new int[] { }));
    }

}
