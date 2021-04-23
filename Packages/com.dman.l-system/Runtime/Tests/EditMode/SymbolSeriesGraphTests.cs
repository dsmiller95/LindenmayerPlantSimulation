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
        Assert.AreEqual(1, seriesMatcher.graphChildPointers[0, 0]);
        Assert.AreEqual(2, seriesMatcher.graphChildPointers[1, 0]);
        Assert.AreEqual(3, seriesMatcher.graphChildPointers[2, 0]);
        Assert.AreEqual(0, seriesMatcher.graphChildPointers[3].length);
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
    [Test]
    public void DepthFirstTraversYieldsInCorrectOrder()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]][C]D[[E]F]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(
            new int[] { 'A', 'B', 'E', 'C', 'D', 'E', 'F' },
            seriesMatcher
            .GetDepthFirstEnumerator()
            .Select(x => seriesMatcher.targetSymbolSeries[x].targetSymbol)
            .Take(10) // infinite loop protection
            .ToArray()
            );
    }
    [Test]
    public void DepthFirstTraverseReverseOriginBranchesYieldsInCorrectOrder()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[B[E]][C]D[[E]F]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(
            "BECDEF".Select(x => (int)x).ToArray(),
            seriesMatcher
            .GetDepthFirstEnumerator()
            .Select(x => seriesMatcher.targetSymbolSeries[x].targetSymbol)
            .Take(15) // infinite loop protection
            .ToArray()
            );

        var dfsIterator = seriesMatcher.GetImmutableDepthFirstIterationState();

        var partial = dfsIterator.TakeNNext(10).ToArray();
        Assert.AreEqual(
            "BECDEF".Select(x => (int)x).ToArray(),
            partial.Select(x => seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNPrevious(10).ToArray();
        Assert.AreEqual(
            "EDCEB\0".Select(x => (int)x).ToArray(),
            partial.Select(x => x.currentIndex == -1 ? 0 : seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );
    }
    [Test]
    public void DepthFirstTraverseForwardAndReverse()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[C][D[E][F]][G[H][I]][J[K][L]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.AreEqual(
            "ABCDEFGHIJKL".Select(x => (int)x).ToArray(),
            seriesMatcher
            .GetDepthFirstEnumerator()
            .Select(x => seriesMatcher.targetSymbolSeries[x].targetSymbol)
            .Take(15) // infinite loop protection
            .ToArray()
            );

        var dfsIterator = seriesMatcher.GetImmutableDepthFirstIterationState();

        var partial = dfsIterator.TakeNNext(8).ToArray();
        Assert.AreEqual(
            "ABCDEFGH".Select(x => (int)x).ToArray(),
            partial.Select(x => seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNPrevious(6).ToArray();
        Assert.AreEqual(
            "GFEDCB".Select(x => (int)x).ToArray(),
            partial.Select(x => seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );

        partial = partial[partial.Length - 1].TakeNPrevious(100).ToArray();
        Assert.AreEqual(
            "A\0".Select(x => (int)x).ToArray(),
            partial.Select(x => x.currentIndex == -1 ? 0 : seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNNext(20).ToArray();
        Assert.AreEqual(
            "ABCDEFGHIJKL".Select(x => (int)x).ToArray(),
            partial.Select(x => seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );

        partial = partial[partial.Length - 1].TakeNPrevious(6).ToArray();
        Assert.AreEqual(
            "KJIHGF".Select(x => (int)x).ToArray(),
            partial.Select(x => seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNNext(6).ToArray();
        Assert.AreEqual(
            "GHIJKL".Select(x => (int)x).ToArray(),
            partial.Select(x => seriesMatcher.targetSymbolSeries[x.currentIndex].targetSymbol).ToArray()
            );
    }

}
