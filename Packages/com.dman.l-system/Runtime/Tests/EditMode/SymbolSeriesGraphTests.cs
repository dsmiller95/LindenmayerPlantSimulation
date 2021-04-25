using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System.Linq;

public class SymbolSeriesGraphTests
{
    public static SymbolSeriesSuffixMatcher GenerateSingleMatcher(
        string matchPattern,
        out SymbolSeriesMatcherNativeDataArray nativeData)
    {
        var builder = SymbolSeriesSuffixBuilder.Parse(matchPattern);
        builder.BuildGraphIndexes('[', ']');

        nativeData = new SymbolSeriesMatcherNativeDataArray(new[] { builder });
        var writer = new SymbolSeriesMatcherNativeDataWriter();
        return builder.BuildIntoManagedMemory(nativeData, writer);
    }


    [Test]
    public void ComputesSeriesGraph()
    {
        var seriesMatcher = GenerateSingleMatcher("ABC", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        Assert.AreEqual(3, nativeData.childrenDataArray.Length);
        Assert.AreEqual(1, seriesMatcher.childrenOfRoot.length);
        Assert.AreEqual(0, seriesMatcher.childrenOfRoot.index);
        Assert.AreEqual(0, nativeData.childrenDataArray[0]);

        Assert.AreEqual(1, nativeData.graphNodeData[0].childrenIndexing.length);
        Assert.AreEqual(1, nativeData.graphNodeData[0].childrenIndexing.index);
        Assert.AreEqual(1, nativeData.childrenDataArray[1]);

        Assert.AreEqual(1, nativeData.graphNodeData[1].childrenIndexing.length);
        Assert.AreEqual(2, nativeData.graphNodeData[1].childrenIndexing.index);
        Assert.AreEqual(2, nativeData.childrenDataArray[2]);

        Assert.AreEqual(0, nativeData.graphNodeData[2].childrenIndexing.length);
        Assert.AreEqual(3, nativeData.graphNodeData[2].childrenIndexing.index);
    }
    [Test]
    public void ComputesGraphWithBranch()
    {
        var seriesMatcher = GenerateSingleMatcher("A[B]C", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, 0, -2, 0 }));
    }
    [Test]
    public void ComputesGraphWithSeveralBranches()
    {
        var seriesMatcher = GenerateSingleMatcher("A[B[E]][C]", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, 0, -2, 2, -2, -2, -2, 0, -2 }));
    }
    [Test]
    public void ComputesGraphWithBranchesAtRoot()
    {
        var seriesMatcher = GenerateSingleMatcher("[B[E]][C]", out var tmpNativeData);
        using var nativeData = tmpNativeData;
        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -2, -1, -2, 1, -2, -2, -2, -1, -2 }));
    }
    [Test]
    public void SimplifiesSimpleDeeplyNestedBranches()
    {
        var seriesMatcher = GenerateSingleMatcher("A[[E]]", out var tmpNativeData);
        using var nativeData = tmpNativeData;
        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, -2, 0, -2, -2 }));
    }

    [Test]
    public void SimplifiesComplexDeeplyNestedBranches()
    {
        var seriesMatcher = GenerateSingleMatcher("A[[E][B]]", out var tmpNativeData);
        using var nativeData = tmpNativeData;
        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, -2, 0, -2, -2, 0, -2, -2 }));
    }
    [Test]
    public void SimplifiesComplexDeeplyNestedBranchesWithNoInitial()
    {;
        var seriesMatcher = GenerateSingleMatcher("[[E][B]]", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -2, -2, -1, -2, -2, -1, -2, -2 }));
    }
    [Test]
    public void SimplifiesOtherNestedBranches()
    {
        var seriesMatcher = GenerateSingleMatcher("A[[E]B]", out var tmpNativeData);
        using var nativeData = tmpNativeData;
        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, -2, 0, -2, 0, -2 }));
    }
    [Test]
    public void PreservesNestedWithIntermediateSymbol()
    {
        var seriesMatcher = GenerateSingleMatcher("A[B[E]]", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        Assert.IsTrue(nativeData.graphNodeData.Select(x => x.parentIndex).SequenceEqual(new int[] { -1, -2, 0, -2, 2, -2, -2 }));
    }
    [Test]
    public void CorrectlyLabelsIndexesInParentChildren()
    {
        var seriesMatcher = GenerateSingleMatcher("[B[E]][C]D[[E]F]", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        for (int rootChild = 0; rootChild < seriesMatcher.childrenOfRoot.length; rootChild++)
        {
            var childIndex = nativeData.childrenDataArray[seriesMatcher.childrenOfRoot.index + rootChild];
            var childNode = nativeData.graphNodeData[childIndex];
            Assert.AreEqual(rootChild, childNode.myIndexInParentChildren, "Child's index in parent children list mismatch");
        }

        for (int nodeIndex = 0; nodeIndex < seriesMatcher.graphNodeMemSpace.length; nodeIndex++)
        {
            var node = nativeData.graphNodeData[nodeIndex];
            if(node.childrenIndexing.length > 0)
            {
                for (int child = 0; child < node.childrenIndexing.length; child++)
                {
                    var childIndex = nativeData.childrenDataArray[node.childrenIndexing.index + child];
                    var childNode = nativeData.graphNodeData[childIndex];
                    Assert.AreEqual(child, childNode.myIndexInParentChildren, "Child's index in parent children list mismatch");
                }
            }
        }
    }
    [Test]
    public void DepthFirstTraversYieldsInCorrectOrder()
    {
        var seriesMatcher = GenerateSingleMatcher("A[B[E]][C]D[[E]F]", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        var resultCharaterString =
            seriesMatcher
            .GetDepthFirstEnumerator(nativeData)
            .Select(x => nativeData.graphNodeData[x].mySymbol.targetSymbol)
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
        var seriesMatcher = GenerateSingleMatcher("[B[E]][C]D[[E]F]", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        var result =
            seriesMatcher
            .GetDepthFirstEnumerator(nativeData)
            .Select(x => nativeData.graphNodeData[x].mySymbol.targetSymbol)
            .Take(15) // infinite loop protection
            .ToArray();
        Assert.AreEqual(
            "BECDEF".Select(x => (int)x).ToArray(),
            result
            );

        var dfsIterator = seriesMatcher.GetImmutableDepthFirstIterationState(nativeData);

        var partial = dfsIterator.TakeNNext(10).ToArray();
        Assert.AreEqual(
            "BECDEF".Select(x => (int)x).ToArray(),
            partial.Select(x => nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNPrevious(10).ToArray();
        Assert.AreEqual(
            "EDCEB\0".Select(x => (int)x).ToArray(),
            partial.Select(x => x.currentIndex == -1 ? 0 : nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );
    }
    [Test]
    public void DepthFirstTraverseForwardAndReverse()
    {
        var seriesMatcher = GenerateSingleMatcher("A[B[C][D[E][F]][G[H][I]][J[K][L]]", out var tmpNativeData);
        using var nativeData = tmpNativeData;

        Assert.AreEqual(
            "ABCDEFGHIJKL".Select(x => (int)x).ToArray(),
            seriesMatcher
            .GetDepthFirstEnumerator(nativeData)
            .Select(x => nativeData.graphNodeData[x].mySymbol.targetSymbol)
            .Take(15) // infinite loop protection
            .ToArray()
            );

        var dfsIterator = seriesMatcher.GetImmutableDepthFirstIterationState(nativeData);

        var partial = dfsIterator.TakeNNext(8).ToArray();
        Assert.AreEqual(
            "ABCDEFGH".Select(x => (int)x).ToArray(),
            partial.Select(x => nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNPrevious(6).ToArray();
        Assert.AreEqual(
            "GFEDCB".Select(x => (int)x).ToArray(),
            partial.Select(x => nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );

        partial = partial[partial.Length - 1].TakeNPrevious(100).ToArray();
        Assert.AreEqual(
            "A\0".Select(x => (int)x).ToArray(),
            partial.Select(x => x.currentIndex == -1 ? 0 : nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNNext(20).ToArray();
        Assert.AreEqual(
            "ABCDEFGHIJKL".Select(x => (int)x).ToArray(),
            partial.Select(x => nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );

        partial = partial[partial.Length - 1].TakeNPrevious(6).ToArray();
        Assert.AreEqual(
            "KJIHGF".Select(x => (int)x).ToArray(),
            partial.Select(x => nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );
        partial = partial[partial.Length - 1].TakeNNext(6).ToArray();
        Assert.AreEqual(
            "GHIJKL".Select(x => (int)x).ToArray(),
            partial.Select(x => nativeData.graphNodeData[x.currentIndex].mySymbol.targetSymbol).ToArray()
            );
    }

}
