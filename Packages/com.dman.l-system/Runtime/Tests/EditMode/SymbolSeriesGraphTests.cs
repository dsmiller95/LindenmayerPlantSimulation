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
        Assert.AreEqual(3, seriesMatcher.childrenDataArray.Length);
        Assert.AreEqual(1, seriesMatcher.childrenOfRoot.length);
        Assert.AreEqual(0, seriesMatcher.childrenOfRoot.index);
        Assert.AreEqual(0, seriesMatcher.childrenDataArray[0]);

        Assert.AreEqual(1, seriesMatcher.nodes[0].childrenIndexing.length);
        Assert.AreEqual(1, seriesMatcher.nodes[0].childrenIndexing.index);
        Assert.AreEqual(1, seriesMatcher.childrenDataArray[1]);

        Assert.AreEqual(1, seriesMatcher.nodes[1].childrenIndexing.length);
        Assert.AreEqual(2, seriesMatcher.nodes[1].childrenIndexing.index);
        Assert.AreEqual(2, seriesMatcher.childrenDataArray[2]);

        Assert.AreEqual(0, seriesMatcher.nodes[2].childrenIndexing.length);
        Assert.AreEqual(3, seriesMatcher.nodes[2].childrenIndexing.index);
    }
    [Test]
    public void ComputesGraphWithBranch()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B]C");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, 0, -2, 0 }));
    }
    [Test]
    public void ComputesGraphWithSeveralBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, 0, -2, 2, -2, -2, -2, 0, -2 }));
    }
    [Test]
    public void ComputesGraphWithBranchesAtRoot()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[B[E]][C]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -2, -1, -2, 1, -2, -2, -2, -1, -2 }));
    }
    [Test]
    public void SimplifiesSimpleDeeplyNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, -2, 0, -2, -2 }));
    }

    [Test]
    public void SimplifiesComplexDeeplyNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E][B]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, -2, 0, -2, -2, 0, -2, -2 }));
    }
    [Test]
    public void SimplifiesComplexDeeplyNestedBranchesWithNoInitial()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[[E][B]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -2, -2, -1, -2, -2, -1, -2, -2 }));
    }
    [Test]
    public void SimplifiesOtherNestedBranches()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[[E]B]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, -2, 0, -2, 0, -2 }));
    }
    [Test]
    public void PreservesNestedWithIntermediateSymbol()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        Assert.IsTrue(seriesMatcher.nodes.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, 0, -2, 2, -2, -2 }));
    }
    [Test]
    public void CorrectlyLabelsIndexesInParentChildren()
    {
        using var seriesMatcher = SymbolSeriesMatcher.Parse("[B[E]][C]D[[E]F]");
        seriesMatcher.ComputeGraphIndexes('[', ']');


        for (int rootChild = 0; rootChild < seriesMatcher.childrenOfRoot.length; rootChild++)
        {
            var childIndex = seriesMatcher.childrenDataArray[seriesMatcher.childrenOfRoot.index + rootChild];
            var childNode = seriesMatcher.nodes[childIndex];
            Assert.AreEqual(rootChild, childNode.myIndexInParentChildren, "Child's index in parent children list mismatch");
        }

        for (int nodeIndex = 0; nodeIndex < seriesMatcher.nodes.Length; nodeIndex++)
        {
            var node = seriesMatcher.nodes[nodeIndex];
            if(node.childrenIndexing.length > 0)
            {
                for (int child = 0; child < node.childrenIndexing.length; child++)
                {
                    var childIndex = seriesMatcher.childrenDataArray[node.childrenIndexing.index + child];
                    var childNode = seriesMatcher.nodes[childIndex];
                    Assert.AreEqual(child, childNode.myIndexInParentChildren, "Child's index in parent children list mismatch");
                }
            }
        }
    }
    [Test]
    public void DepthFirstTraversYieldsInCorrectOrder()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("A[B[E]][C]D[[E]F]");
        seriesMatcher.ComputeGraphIndexes('[', ']');
        var resultCharaterString =
            seriesMatcher
            .GetDepthFirstEnumerator()
            .Select(x => seriesMatcher.targetSymbolSeries[x].targetSymbol)
            .Take(10) // infinite loop protection
            .ToArray();
        Assert.AreEqual(
            new int[] { 'A', 'B', 'E', 'C', 'D', 'E', 'F' },
            resultCharaterString
            );
    }
    [Test]
    public void DepthFirstTraverseReverseOriginBranchesYieldsInCorrectOrder()
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse("[B[E]][C]D[[E]F]");
        seriesMatcher.ComputeGraphIndexes('[', ']');

        var result =
            seriesMatcher
            .GetDepthFirstEnumerator()
            .Select(x => seriesMatcher.targetSymbolSeries[x].targetSymbol)
            .Take(15) // infinite loop protection
            .ToArray();
        Assert.AreEqual(
            "BECDEF".Select(x => (int)x).ToArray(),
            result
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
