using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System;
using System.Collections.Generic;

public class ContextualMatcherTests
{
    private void AssertForwardsMatch(string target, string matcher, bool shouldMatch = true, int indexInTarget = 0, string message = null, IEnumerable<(int, int)> expectedMatchToTargetMapping = null)
    {
        var seriesMatcher = new SymbolSeriesMatcher
        {
            targetSymbolSeries = InputSymbolParser.ParseInputSymbols(matcher)
        };
        var targetString = new SymbolString<double>(target);
        var branchingCache = new SymbolStringBranchingCache('[', ']');
        branchingCache.SetTargetSymbolString(targetString);

        var matchPairings = branchingCache.MatchesForward(indexInTarget, seriesMatcher);
        var matches = matchPairings != null;
        if (shouldMatch != matches)
        {
            Assert.Fail($"Expected '{matcher}' to {(shouldMatch ? "" : "not ")}match forwards from {indexInTarget} in '{target}'{(message == null ? "" : '\n' + message)}");
        }
        if(shouldMatch && expectedMatchToTargetMapping != null)
        {
            foreach (var expectedMatch in expectedMatchToTargetMapping)
            {
                Assert.IsTrue(matchPairings.ContainsKey(expectedMatch.Item1), $"Expected symbol at {expectedMatch.Item1} to be mapped to the target string");
                Assert.AreEqual(expectedMatch.Item2, matchPairings[expectedMatch.Item1], $"Expected symbol at {expectedMatch.Item1} to be mapped to {expectedMatch.Item2} in target string, but was {matchPairings[expectedMatch.Item1]}");
            }
        }
    }
    private void AssertBackwardsMatch(string target, string matcher, bool shouldMatch, string message = "", int indexInTarget = -1)
    {
        var seriesMatcher = new SymbolSeriesMatcher
        {
            targetSymbolSeries = InputSymbolParser.ParseInputSymbols(matcher)
        };
        var targetString = new SymbolString<double>(target);
        var branchingCache = new SymbolStringBranchingCache('[', ']');
        branchingCache.SetTargetSymbolString(targetString);

        var realIndex = indexInTarget < 0 ? indexInTarget + target.Length : indexInTarget;
        var matches = branchingCache.MatchesBackwards(realIndex, seriesMatcher);
        if (shouldMatch != matches)
        {
            Assert.Fail($"Expected '{matcher}' to {(shouldMatch ? "" : "not ")}match backwards from {indexInTarget} in '{target}'");
        }
    }

    [Test]
    public void NoBranchingBackwardsMatch()
    {
        var branchingCache = new SymbolStringBranchingCache('[', ']');
        Assert.IsTrue(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("A")));
        Assert.IsTrue(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("ABC")));
        Assert.IsFalse(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("A[B]")));
        Assert.IsFalse(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("[A]")));
    }

    [Test]
    public void FindsOpeningBranchLinks()
    {
        var targetString = new SymbolString<double>("A[AA]AAA[A[AAA[A]]]A");
        var branchingCache = new SymbolStringBranchingCache('[', ']');
        branchingCache.SetTargetSymbolString(targetString);
        Assert.AreEqual(8, branchingCache.FindOpeningBranchIndex(18));
        Assert.AreEqual(10, branchingCache.FindOpeningBranchIndex(17));
        Assert.AreEqual(1, branchingCache.FindOpeningBranchIndex(4));

    }
    [Test]
    public void FindsClosingBranchLinks()
    {
        var targetString = new SymbolString<double>("A[AA]AAA[A[AAA[A]]]A");
        var branchingCache = new SymbolStringBranchingCache('[', ']');
        branchingCache.SetTargetSymbolString(targetString);
        Assert.AreEqual(4, branchingCache.FindClosingBranchIndex(1));
        Assert.AreEqual(17, branchingCache.FindClosingBranchIndex(10));
        Assert.AreEqual(18, branchingCache.FindClosingBranchIndex(8));

    }

    #region Boolean symbol matching
    [Test]
    public void BasicForwardMatchesOnlyImmediateSiblingNoBranching()
    {
        AssertForwardsMatch("CA", "A", true, message: "must match immediate following symbol");
        AssertForwardsMatch("BCA", "A", false, message: "must not match when immediate following symbol is not branch");
        AssertForwardsMatch("ADE", "A", false, message: "must not match when match is self");
    }
    [Test]
    public void BasicBackwardMatchesOnlyImmediateSiblingNoBranching()
    {
        AssertBackwardsMatch("AD", "A", true, message: "must match when immediate preceding symbol matches");
        AssertBackwardsMatch("BCA", "A", false, message: "must not match when immediate preceding symbol is not branch");
        AssertBackwardsMatch("ADE", "A", false, message: "must not match when immediate preceding symbol is not branch");
    }

    [Test]
    public void BasicForwardSkipBranchMatchesDoesntSkipDown()
    {
        AssertForwardsMatch("B[C]A", "A", true);
        AssertForwardsMatch("[C]AD", "A", false, indexInTarget: 1);
    }
    [Test]
    public void MultiForwardSkipBranchMatchesDoesntSkipDown()
    {
        AssertForwardsMatch("BA[C]B", "AB", true);
        AssertForwardsMatch("[C]ABD", "AB", false, indexInTarget: 1);
    }

    [Test]
    public void BasicBackwardsSkipBranchMatches()
    {
        AssertBackwardsMatch("[C]AD", "A", true);
        AssertBackwardsMatch("[C]ADE", "A", false);
        AssertBackwardsMatch("A[D]E", "A", true);
    }

    [Test]
    public void ForwardBranchUp()
    {
        AssertForwardsMatch("B[C[A]]E", "A", false);
        AssertForwardsMatch("B[C[A]]E", "A", true, indexInTarget: 2);
    }
    [Test]
    public void ForwardBranchUpDoesntBranchBackDown()
    {
        AssertForwardsMatch("E[A[B[C]]]", "ABC", true);
        AssertForwardsMatch("E[AB[C]]", "ABC", true);
        AssertForwardsMatch("E[A[B][C]]", "ABC", false);
        AssertForwardsMatch("E[A]BC", "ABC", false);
    }

    [Test]
    public void BackwardsMultibranch()
    {
        AssertBackwardsMatch("A[[E]F]", "A", true, indexInTarget: -4);
        AssertBackwardsMatch("A[D][E]", "A", true, indexInTarget: -2);
        AssertBackwardsMatch("A[[D][E]]", "A", true, indexInTarget: -3);
        AssertBackwardsMatch("A[[D][E]]E", "A", true, indexInTarget: -1);
    }

    [Test]
    public void ForwardsMatchesSimpleTreeStructure()
    {
        AssertForwardsMatch("EA[B]", "A[B]", true);
        AssertForwardsMatch("EA[BC]", "A[BC]", true);
        AssertForwardsMatch("EA[B][C]", "A[B][C]", true);
        AssertForwardsMatch("EA[[B]C]", "A[B][C]", true);
    }
    [Test]
    public void ForwardsDoubleMatchTreeStructure()
    {
        AssertForwardsMatch("EA[B][C]", "A[B][C]", true);
        AssertForwardsMatch("EA[[B]C]", "A[B][C]", true);
        AssertForwardsMatch("EA[B]C", "A[B][C]", true);
        AssertForwardsMatch("EAC[B]", "A[B][C]", false);
    }
    [Test]
    public void ForwardsDoubleMatchTreeStructureWhenIndividualsNotNested()
    {
        AssertForwardsMatch("EA[B]C", "A[B][C]", true);
        AssertForwardsMatch("EA[C]B", "A[B][C]", true);
    }
    [Test]
    public void ForwardsDoubleMatchTreeStructureWithSymbolContinuationAndExtraBranchInTarget()
    {
        AssertForwardsMatch("EA[B][C]D[E]F[G]", "A[B][C]DF", true);
        AssertForwardsMatch("EA[B][C][D[E]F]", "A[B][C]DF", true);
        AssertForwardsMatch("EA[DF][B][C]", "A[B][C]DF", true);
        AssertForwardsMatch("EA[D[F]][C][B]", "A[B][C]DF", true);
        AssertForwardsMatch("EA[D[F][C][B]]", "A[B][C]DF", false);
    }
    [Test]
    public void ForwardsMatchesExtraNestedTreeStructure()
    {
        AssertForwardsMatch("EA[[B]EE]", "A[B]", true);
        AssertForwardsMatch("E[A[BEEE]]", "A[B]", true);
        AssertForwardsMatch("EC[A[B]]", "A[B]", false);
        AssertForwardsMatch("E[[C]A[B]]", "A[B]", true);
        AssertForwardsMatch("EA[[B][C]]", "A[B][C]", true);
    }
    [Test]
    public void ForwardsDoubleMatchTreeStructureFailsWhenDisordered()
    {
        AssertForwardsMatch("EA[B[C]]", "A[B][C]", false);
        AssertForwardsMatch("EAB[C]", "A[B][C]", false);
        AssertForwardsMatch("EAC[B]", "A[B][C]", false);
    }

    [Test]
    public void ForwardsMatchesTreeStructureOutOfPerfectOrder()
    {
        AssertForwardsMatch("E[B]A", "A[B]", false);
        AssertForwardsMatch("EA[E][B]", "A[B]", true);
        AssertForwardsMatch("EA[[E]EEE[B]]", "A[B]", false);
        AssertForwardsMatch("EA[[E][[E]B]]", "A[B]", true);
    }
    [Test]
    public void BackwardsPathMatch()
    {
        AssertBackwardsMatch("A[B]E", "AB", false);
        AssertBackwardsMatch("A[BE]", "AB", true, indexInTarget: -2);
        AssertBackwardsMatch("ABE", "AB", true);
        AssertBackwardsMatch("[AB]E", "AB", false);
    }
    [Test]
    public void ForwardMatchTreeStructureShuffledMatch()
    {
        AssertForwardsMatch("EA[C][B]", "A[B][C]", true);
        AssertForwardsMatch("EA[C[B]]", "A[B][C]", false);
        AssertForwardsMatch("EA[[C]B]", "A[B][C]", true);
        AssertForwardsMatch("EA[A[C]B]", "A[B][C]", false);
    }
    [Test]
    public void NestedForwardMatchBranches()
    {
        AssertForwardsMatch("EA[[B]C]", "A[[B]C]", true);
        AssertForwardsMatch("EA[C][B]", "A[[B]C]", true);
        AssertForwardsMatch("EA[[B]C]", "A[[B]C]", true);
        AssertForwardsMatch("EA[BC]", "A[[B]C]", false);
    }
    [Test]
    public void ForwardBranchMustRemainSingle()
    {
        AssertForwardsMatch("EA[B[C][D]]", "A[B[C][D]]", true);
        AssertForwardsMatch("EA[B[C]][B[D]]", "A[B[C][D]]", false);
    }
    [Test]
    public void ForwardBranchMultipleIdenticleBranchesRequired()
    {
        AssertForwardsMatch("EA[BC][B][BCD]", "A[B][B][B]", true);
        AssertForwardsMatch("EA[B][B][B]", "A[B][B][B]", true);
        AssertForwardsMatch("EA[[B]B][B]", "A[B][B][B]", true);
        AssertForwardsMatch("EA[B][B]", "A[B][B][B]", false);
        AssertForwardsMatch("EA[BC][BD]", "A[B][B][B]", false);
    }

    /// <summary>
    ///TODO: partial child matches will fail inconsistently based on ordering of children.
    ///  should be sufficient for most systems. must keep in mind that order matters,
    ///  under very specific circumstances
    /// Perhaps a completely seperate matching approach, which consolidates the matching pattern sections together,
    ///  would solve this problem.
    /// </summary>
    [Test, Ignore("Disordered matching restriction accepted")]
    public void ForwardBranchHandlesSubsetsInMatchString()
    {
        AssertForwardsMatch("EA[BC][B][BCD]", "A[BCD][BC][B]", true);
        AssertForwardsMatch("EA[BC][B][BCD]", "A[B][BC][BCD]", true);
        AssertForwardsMatch("EA[BC][BCD]", "A[B][BC][BCD]", false);
        AssertForwardsMatch("EA[BC][[B]BCD]", "A[B][BC][BCD]", true);
        AssertForwardsMatch("EA[B][BCD]", "A[B][BC][BCD]", false);
    }
    [Test, Ignore("Disordered matching restriction accepted")]
    public void ForwardBranchHandlesEqualComplexityMultipleMatches()
    {
        AssertForwardsMatch("EA[B[D][C]][B[D][C]]", "A[BC][BD]", true);
        AssertForwardsMatch("EA[B[D][C]][B[D]]", "A[BC][BD]", true);
        AssertForwardsMatch("EA[B[D][C]][B[C]]", "A[BC][BD]", true);
    }
    #endregion

    #region Symbol Mapping

    [Test]
    public void ForwardMatchBasicSymbolMatch()
    {
        AssertForwardsMatch("CA", "A", expectedMatchToTargetMapping: new[] { (0, 1) });
        AssertForwardsMatch("CAB", "A", expectedMatchToTargetMapping: new[] { (0, 1) });
        AssertForwardsMatch("CACE", "A", expectedMatchToTargetMapping: new[] { (0, 1) });
    }
    [Test]
    public void ForwardMatchTreeStructureMapping()
    {
        AssertForwardsMatch("EA[B][C]", "A[B][C]", expectedMatchToTargetMapping: new[] { (0, 1), (2, 3), (5, 6) });
        AssertForwardsMatch("EA[[B]C]", "A[B][C]", expectedMatchToTargetMapping: new[] { (0, 1), (2, 4), (5, 6) });
        AssertForwardsMatch("EA[C]B", "A[B][C]", expectedMatchToTargetMapping: new[] { (0, 1), (2, 5), (5, 3) });
    }
    [Test]
    public void ForwardBranchMultipleIdenticleBranchesMapped()
    {
        AssertForwardsMatch("EA[BC][B][BCD]", "A[B][B][B]", expectedMatchToTargetMapping: new[] { (0, 1), (2, 3), (5, 7), (8, 10) });
    }
    #endregion
}
