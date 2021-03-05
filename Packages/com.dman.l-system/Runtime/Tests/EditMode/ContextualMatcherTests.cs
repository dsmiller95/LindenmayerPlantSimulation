using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System;

public class ContextualMatcherTests
{
    private void AssertForwardsMatch(string target, string matcher, bool shouldMatch, int indexInTarget = 0, string message = null)
    {
        var seriesMatcher = new SymbolSeriesMatcher
        {
            targetSymbolSeries = InputSymbolParser.ParseInputSymbols(matcher)
        };
        var targetString = new SymbolString<double>(target);
        var branchingCache = new SymbolStringBranchingCache('[', ']');
        branchingCache.SetTargetSymbolString(targetString);

        var matches = branchingCache.MatchesForward(indexInTarget, seriesMatcher);
        if (shouldMatch != matches)
        {
            Assert.Fail($"Expected '{matcher}' to {(shouldMatch ? "" : "not ")}match forwards from {indexInTarget} in '{target}'{(message == null ? "" : '\n' + message)}");
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
    public void BasicForwardSkipBranchMatchesDoesntSkipDown ()
    {
        AssertForwardsMatch("B[C]A", "A", true);
        AssertForwardsMatch("[C]AD", "A", false, indexInTarget: 1);
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
    public void BackwardsMultibranch()
    {
        AssertBackwardsMatch("A[[E]F]", "A", true, indexInTarget: -4);
        AssertBackwardsMatch("A[D][E]", "A", true, indexInTarget: -2);
        AssertBackwardsMatch("A[[D][E]]", "A", true, indexInTarget: -3);
        AssertBackwardsMatch("A[[D][E]]E", "A", true, indexInTarget: -1);
    }

    [Test]
    public void ForwardsMatchesTreeStructure()
    {
        AssertForwardsMatch("EA[B]", "A[B]", true);
        AssertForwardsMatch("EA[[B]EE]", "A[B]", true);
        AssertForwardsMatch("E[A[BEEE]]", "A[B]", true);
        AssertForwardsMatch("EC[A[B]]", "A[B]", false);
        AssertForwardsMatch("E[[C]A[B]]", "A[B]", true);
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
        AssertForwardsMatch("A[C][B]", "A[B][C]", true);
        AssertForwardsMatch("A[C[B]]", "A[B][C]", false);
        AssertForwardsMatch("A[[C]B]", "A[B][C]", true);
        AssertForwardsMatch("A[A[C]B]", "A[B][C]", false);
    }
    [Test]
    public void NestedForwardMatchBranches()
    {
        AssertForwardsMatch("A[[B]C]", "A[[B]C]", true);
        AssertForwardsMatch("A[C][B]", "A[[B]C]", true);
        AssertForwardsMatch("A[[B]C]", "A[[B]C]", true);
    }
}
