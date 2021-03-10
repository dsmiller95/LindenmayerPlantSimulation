using Dman.LSystem.SystemRuntime;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public class ContextualMatcherTests
{
    private void AssertForwardsMatch(
        string target,
        string matcher,
        bool shouldMatch = true,
        int indexInTarget = 0,
        string message = null,
        IEnumerable<(int, int)> expectedMatchToTargetMapping = null,
        bool? testOrderingAgnostict = null,
        bool testOrderingInvariant = true,
        IEnumerable<int> ignoreSymbols = null)
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse(matcher);
        var targetString = new SymbolString<double>(target);
        var branchingCache = new SymbolStringBranchingCache('[', ']', ignoreSymbols == null ? new HashSet<int>() : new HashSet<int>(ignoreSymbols));
        branchingCache.SetTargetSymbolString(targetString);

        if (testOrderingInvariant && shouldMatch && !testOrderingAgnostict.HasValue)
        {
            // if an invariant matches, agnostic should match too.
            testOrderingAgnostict = true;
        }

        if (testOrderingAgnostict.HasValue && testOrderingAgnostict.Value)
        {
            var matchPairings = branchingCache.MatchesForward(indexInTarget, seriesMatcher, true);
            var matches = matchPairings != null;
            if (shouldMatch != matches)
            {
                Assert.Fail($"Expected '{matcher}' to {(shouldMatch ? "" : "not ")}match forwards ignoring order from {indexInTarget} in '{target}'{(message == null ? "" : '\n' + message)}");
            }
            if (shouldMatch && expectedMatchToTargetMapping != null)
            {
                foreach (var expectedMatch in expectedMatchToTargetMapping)
                {
                    Assert.IsTrue(matchPairings.ContainsKey(expectedMatch.Item1), $"Expected symbol at {expectedMatch.Item1} to be mapped to the target string");
                    Assert.AreEqual(expectedMatch.Item2, matchPairings[expectedMatch.Item1], $"Expected symbol at {expectedMatch.Item1} to be mapped to {expectedMatch.Item2} in target string, but was {matchPairings[expectedMatch.Item1]}");
                }
            }
        }
        if (testOrderingInvariant)
        {
            var matchPairings = branchingCache.MatchesForward(indexInTarget, seriesMatcher, false);
            var matches = matchPairings != null;
            if (shouldMatch != matches)
            {
                Assert.Fail($"Expected '{matcher}' to {(shouldMatch ? "" : "not ")}match forwards retaining order from {indexInTarget} in '{target}'{(message == null ? "" : '\n' + message)}");
            }
            if (shouldMatch && expectedMatchToTargetMapping != null)
            {
                foreach (var expectedMatch in expectedMatchToTargetMapping)
                {
                    Assert.IsTrue(matchPairings.ContainsKey(expectedMatch.Item1), $"Expected symbol at {expectedMatch.Item1} to be mapped to the target string");
                    Assert.AreEqual(expectedMatch.Item2, matchPairings[expectedMatch.Item1], $"Expected symbol at {expectedMatch.Item1} to be mapped to {expectedMatch.Item2} in target string, but was {matchPairings[expectedMatch.Item1]}");
                }
            }
        }
    }


    #region backwards matching
    private void AssertBackwardsMatch(
        string target,
        string matcher,
        bool shouldMatch,
        IEnumerable<(int, int)> expectedMatchToTargetMapping = null,
        int indexInTarget = -1,
        IEnumerable<int> ignoreSymbols = null)
    {
        var seriesMatcher = SymbolSeriesMatcher.Parse(matcher);
        var targetString = new SymbolString<double>(target);
        var branchingCache = new SymbolStringBranchingCache('[', ']', ignoreSymbols == null ? new HashSet<int>() : new HashSet<int>(ignoreSymbols));
        branchingCache.SetTargetSymbolString(targetString);

        var realIndex = indexInTarget < 0 ? indexInTarget + targetString.Length : indexInTarget;
        var matchPairings = branchingCache.MatchesBackwards(realIndex, seriesMatcher);
        var matches = matchPairings != null;
        if (shouldMatch != matches)
        {
            Assert.Fail($"Expected '{matcher}' to {(shouldMatch ? "" : "not ")}match backwards from {indexInTarget} in '{target}'");
        }
        if (shouldMatch && expectedMatchToTargetMapping != null)
        {
            foreach (var expectedMatch in expectedMatchToTargetMapping)
            {
                Assert.IsTrue(matchPairings.ContainsKey(expectedMatch.Item1), $"Expected symbol at {expectedMatch.Item1} to be mapped to the target string");
                Assert.AreEqual(expectedMatch.Item2, matchPairings[expectedMatch.Item1], $"Expected symbol at {expectedMatch.Item1} to be mapped to {expectedMatch.Item2} in target string, but was {matchPairings[expectedMatch.Item1]}");
            }
        }
    }
    [Test]
    public void NoBranchingBackwardsMatch()
    {
        var branchingCache = new SymbolStringBranchingCache();
        Assert.IsTrue(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("A")));
        Assert.IsTrue(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("ABC")));
        Assert.IsFalse(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("A[B]")));
        Assert.IsFalse(branchingCache.ValidBackwardsMatch(SymbolSeriesMatcher.Parse("[A]")));
    }
    [Test]
    public void BasicBackwardMatchesOnlyImmediateSiblingNoBranching()
    {
        AssertBackwardsMatch("AD", "A", true, expectedMatchToTargetMapping: new[] { (0, 0) });
        AssertBackwardsMatch("BCA", "A", false);
        AssertBackwardsMatch("ADE", "A", false);
    }
    [Test]
    public void BasicBackwardsSkipBranchMatches()
    {
        AssertBackwardsMatch("[C]AD", "A", true, expectedMatchToTargetMapping: new[] { (0, 3) });
        AssertBackwardsMatch("[C]ADE", "A", false);
        AssertBackwardsMatch("A[D]E", "A", true);
    }
    [Test]
    public void BackwardsMatchFromInsideBranchWithIgnoreFailsGracefull()
    {
        var defaultExclude = "1234567890".Select(x => (int)x).ToArray();
        AssertBackwardsMatch("AB[12E]", "AB", true, indexInTarget: -2, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("B[12E]", "AB", false, indexInTarget: -2, ignoreSymbols: defaultExclude);
    }
    [Test]
    public void BackwardsSkipIgnoredCharacters()
    {
        var defaultExclude = "1234567890".Select(x => (int)x).ToArray();
        AssertBackwardsMatch("[C]A1234567890D", "A", true, expectedMatchToTargetMapping: new[] { (0, 3) }, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("[C]A1233344B2555CD", "ABC", true, expectedMatchToTargetMapping: new[] { (0, 3), (1, 11), (2, 16) }, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("[C]A1234567890", "A", true, expectedMatchToTargetMapping: new[] { (0, 3) }, ignoreSymbols: defaultExclude);
    }
    [Test]
    public void BackwardsMatchNoMatchWhenOverlapCloseToStringOrigin()
    {
        var defaultExclude = "1234567890".Select(x => (int)x).ToArray();
        AssertBackwardsMatch("[A]", "A", false, indexInTarget: 1, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("A4", "A", false, indexInTarget: 0, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("23A4", "A", false, indexInTarget: 2, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("3A4", "A", false, indexInTarget: 1, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("3A4", "A", true, indexInTarget: 2, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("3A4", "A", false, indexInTarget: 0, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("3A4124", "A", true, indexInTarget: -1, ignoreSymbols: defaultExclude);

        AssertBackwardsMatch("BCADE", "A", false, indexInTarget: 2, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("BCADE", "A", true, indexInTarget: 3, ignoreSymbols: defaultExclude);
        AssertBackwardsMatch("BCADE", "A", false, indexInTarget: 4, ignoreSymbols: defaultExclude);
    }
    [Test]
    public void BackwardsMultibranch()
    {
        AssertBackwardsMatch("A[[E]F]", "A", true, indexInTarget: -4, expectedMatchToTargetMapping: new[] { (0, 0) });
        AssertBackwardsMatch("A[D][E]", "A", true, indexInTarget: -2, expectedMatchToTargetMapping: new[] { (0, 0) });
        AssertBackwardsMatch("A[[D][E]]", "A", true, indexInTarget: -3, expectedMatchToTargetMapping: new[] { (0, 0) });
        AssertBackwardsMatch("A[[D][E]]E", "A", true, indexInTarget: -1, expectedMatchToTargetMapping: new[] { (0, 0) });
    }
    [Test]
    public void BackwardsPathMatch()
    {
        AssertBackwardsMatch("A[B]E", "AB", false);
        AssertBackwardsMatch("A[BE]", "AB", true, indexInTarget: -2, expectedMatchToTargetMapping: new[] { (0, 0), (1, 2) });
        AssertBackwardsMatch("ABE", "AB", true, expectedMatchToTargetMapping: new[] { (0, 0), (1, 1) });
        AssertBackwardsMatch("[AB]E", "AB", false);
    }
    [Test]
    public void BackwardsPathMatchManyBranchingSymbols()
    {
        AssertBackwardsMatch("[[ABE]]", "AB", true, indexInTarget: -3);
        AssertBackwardsMatch("[A[BE]]", "AB", true, indexInTarget: -3);
        AssertBackwardsMatch("A[[BE]]", "AB", true, indexInTarget: -3);
        AssertBackwardsMatch("D[[BE]]", "AB", false, indexInTarget: -3);
        AssertBackwardsMatch("[A]BE", "AB", false, indexInTarget: -3);
        AssertBackwardsMatch("[[BE]]", "AB", false, indexInTarget: -3);
    }
    [Test]
    public void BackwardsLongPathMatch()
    {
        AssertBackwardsMatch("AB[JJ][CD[JJ]EF]", "ABCDE", true, indexInTarget: -2, expectedMatchToTargetMapping: new[] { (0, 0), (1, 1), (2, 7), (3, 8), (4, 13) });
    }
    [Test]
    public void BackwardsPathMatchOnlyCorrectParameterSize()
    {
        AssertBackwardsMatch("AB(1)E", "AB(x)", true, expectedMatchToTargetMapping: new[] { (0, 0), (1, 1) });
        AssertBackwardsMatch("ABE", "AB(x)", false);
        AssertBackwardsMatch("A(2)B(1)E", "A(y)B(x)", true, expectedMatchToTargetMapping: new[] { (0, 0), (1, 1) });
    }


    #endregion

    #region branch navigation
    [Test]
    public void FindsOpeningBranchLinks()
    {
        var targetString = new SymbolString<double>("A[AA]AAA[A[AAA[A]]]A");
        var branchingCache = new SymbolStringBranchingCache();
        branchingCache.SetTargetSymbolString(targetString);
        Assert.AreEqual(8, branchingCache.FindOpeningBranchIndex(18));
        Assert.AreEqual(10, branchingCache.FindOpeningBranchIndex(17));
        Assert.AreEqual(1, branchingCache.FindOpeningBranchIndex(4));
    }
    [Test]
    public void FindsClosingBranchLinks()
    {
        var targetString = new SymbolString<double>("A[AA]AAA[A[AAA[A]]]A");
        var branchingCache = new SymbolStringBranchingCache();
        branchingCache.SetTargetSymbolString(targetString);
        Assert.AreEqual(4, branchingCache.FindClosingBranchIndex(1));
        Assert.AreEqual(17, branchingCache.FindClosingBranchIndex(10));
        Assert.AreEqual(18, branchingCache.FindClosingBranchIndex(8));
    }
    [Test]
    public void FindsBranchClosingLinksCorrectlyWhenBranchingAtSameIndexAsCharacterCodeForBranchingSymbol()
    {
        var targetString = new SymbolString<double>("EEEBE[&E][&&E]&EEEE[&[EE]E][&&[EE]E]&EEEE[&[EEEE]EE][&&[EEEE]EE]&EEEA[&[EEEEEE]E[E]E[E]][&&[EEEEEE]E[E]E[E]]");
        var branchingCache = new SymbolStringBranchingCache();
        branchingCache.SetTargetSymbolString(targetString);
        Assert.AreEqual(87, branchingCache.FindClosingBranchIndex(69));
        Assert.AreEqual(98, branchingCache.FindClosingBranchIndex(91));
    }
    [Test]
    public void FindsBranchForwardLinksCorrectlyWhenBranchingAtSameIndexAsCharacterCodeForBranchingSymbol()
    {
        var targetString = new SymbolString<double>("[&E][&&E]&EEEE[&[EE]E][&&[EE]E]&EEEE[&[EEEE]EE][&&[EEEE]EE]&EEEA[&[EEEEEE]E[E]E[E]][&&[EEEEEE]E[E]E[E]]");
        var branchingCache = new SymbolStringBranchingCache();
        branchingCache.SetTargetSymbolString(targetString);
        Assert.AreEqual(64, branchingCache.FindOpeningBranchIndex(82));
        Assert.AreEqual(86, branchingCache.FindOpeningBranchIndex(93));
    }
    #endregion

    #region Ordering-invariant Boolean forward symbol matching
    [Test]
    public void BasicForwardMatchesOnlyImmediateSiblingNoBranching()
    {
        AssertForwardsMatch("CA", "A", true, message: "must match immediate following symbol");
        AssertForwardsMatch("BCA", "A", false, message: "must not match when immediate following symbol is not branch");
        AssertForwardsMatch("ADE", "A", false, message: "must not match when match is self");
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
    public void ForwardBranchUp()
    {
        AssertForwardsMatch("B[C[A]]E", "A", false);
        AssertForwardsMatch("B[C[A]]E", "A", true, indexInTarget: 2);
    }
    [Test]
    public void ForwardBranchWithNoMatchInsideStructureFailsGracefull()
    {
        var defaultExclude = "1234567890".Select(x => (int)x).ToArray();
        AssertForwardsMatch("BE", "A", false);
        AssertForwardsMatch("[BE[A134]144]24", "A", false, indexInTarget: 1);
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
    public void ForwardSkipsOverPartiallyMatchBranchStructures()
    {
        AssertForwardsMatch("EA[BC][BCD][BCDE]", "A[BCDE]", true, expectedMatchToTargetMapping: new[] { (0, 1), (2, 12), (3, 13), (4, 14), (5, 15) });
        AssertForwardsMatch("EA[BC[D][E]][BCD][BCDE]", "A[BCDE]", true, expectedMatchToTargetMapping: new[] { (0, 1), (2, 18), (3, 19), (4, 20), (5, 21) });
    }

    [Test]
    public void ForwardSkipsOverIgnoredCharacters()
    {
        var defaultExclude = "1234567890".Select(x => (int)x).ToArray();
        AssertForwardsMatch("E1[23A4[5B67[890C]]]", "ABC", true, ignoreSymbols: defaultExclude, expectedMatchToTargetMapping: new[] { (0, 5), (1, 9), (2, 16) });
        AssertForwardsMatch("E1A2B3C", "ABC", true, ignoreSymbols: defaultExclude, expectedMatchToTargetMapping: new[] { (0, 2), (1, 4), (2, 6) });
        AssertForwardsMatch("1A2B3C", "ABC", true, ignoreSymbols: defaultExclude, expectedMatchToTargetMapping: new[] { (0, 1), (1, 3), (2, 5) });
        AssertForwardsMatch("1A3[2B]3[C3]3", "A[B][C]", true, ignoreSymbols: defaultExclude, expectedMatchToTargetMapping: new[] { (0, 1), (2, 5), (5, 9) });
    }

    [Test]
    public void ForwardOrderInvariantHandlesOverlapingMatchesPredictably()
    {
        AssertForwardsMatch("EA[BC][BCD]", "A[BCD]", true);
        AssertForwardsMatch("EA[BC][BCD][BCDE]", "A[BC][BCD][BCDE]", true);
        AssertForwardsMatch("EA[BCE][BCD]", "A[BCD]", true);
        AssertForwardsMatch("EA[BCD][BCDE][BC]", "A[BC][BCD][BCDE]", false);
        AssertForwardsMatch("EA[BCD][BCDE][BC][BCD][BCDE]", "A[BC][BCD][BCDE]", true,
            expectedMatchToTargetMapping: new[] { (0, 1), (2, 3), (3, 4), (6, 8), (7, 9), (8, 10), (11, 23), (12, 24), (13, 25), (14, 26) });
        AssertForwardsMatch("EA[BCD][BCDE][BCDE]", "A[BC][BCD][BCDE]", true);
        AssertForwardsMatch("EA[BCD][BCDE][BCD]", "A[BC][BCD][BCDE]", false);
    }
    [Test]
    public void ForwardOrderInvariantHandlesWrongParentingOfLongChain()
    {
        AssertForwardsMatch("EA[BC[BCD]]", "A[BC][BCD]", false);
        AssertForwardsMatch("EA[BC]BCD[BCDE]", "A[BC][BCD][BCDE]", false);
        AssertForwardsMatch("EA[BC[EF]][BCD]", "A[BC[EF]][BCD]", true);
        AssertForwardsMatch("EA[BC[B][EF]][BCD]", "A[BC[EF]][BCD]", true);
        AssertForwardsMatch("EA[BC][EF][BCD]", "A[BC[EF]][BCD]", false);
        AssertForwardsMatch("EA[BC[B]DE]DE", "A[BCDE]", true);
        AssertForwardsMatch("EA[BC[B]]DE", "A[BCDE]", false);
        AssertForwardsMatch("EA[[B]C][CD]", "A[B][CD]", true);
        AssertForwardsMatch("EA[[B]C]CD", "A[B][CD]", true);
        AssertForwardsMatch("EA[[B]CE]CD", "A[B][CD]", true);
        AssertForwardsMatch("EA[[B]CE]D", "A[B][CD]", false);
        AssertForwardsMatch("E[[B]CE]CD", "[B][CD]", true);
        AssertForwardsMatch("EA[[B]CE]CD", "[B][CD]", false);
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
    public void ForwardsDoubleMatchTreeStructureWithSymbolContinuationAndExtraBranchInTarget()
    {
        AssertForwardsMatch("EA[B][C]D[E]F[G]", "A[B][C]DF", true);
        AssertForwardsMatch("EA[B][C][D[E]F]", "A[B][C]DF", true);
        AssertForwardsMatch("EA[DF][B][C]", "A[B][C]DF", false);
        AssertForwardsMatch("EA[D[F]][C][B]", "A[B][C]DF", false);
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
    public void ForwardsDoubleMatchTreeStructureFailsBranchStructureMismatch()
    {
        AssertForwardsMatch("EA[B[C]]", "A[B][C]", false);
        AssertForwardsMatch("EAB[C]", "A[B][C]", false);
    }
    [Test]
    public void ForwardsMatchesTreeStructureWithExtraSymbols()
    {
        AssertForwardsMatch("E[B]A", "A[B]", false);
        AssertForwardsMatch("EA[E][B]", "A[B]", true);
        AssertForwardsMatch("EA[[E]EEE[B]]", "A[B]", false);
        AssertForwardsMatch("EA[[E][[E]B]]", "A[B]", true);
    }

    [Test]
    public void ForwardMatchTreeStructureShuffledMatchAllFailsWhenOrderInvariant()
    {
        AssertForwardsMatch("EA[C][B]", "A[B][C]", false);
        AssertForwardsMatch("EA[C[B]]", "A[B][C]", false);
        AssertForwardsMatch("EA[[C]B]", "A[B][C]", false);
        AssertForwardsMatch("EA[A[C]B]", "A[B][C]", false);
    }
    [Test]
    public void NestedForwardMatchBranches()
    {
        AssertForwardsMatch("EA[[B]C]", "A[[B]C]", true);
        AssertForwardsMatch("EA[B][C]", "A[[B]C]", true);
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
    [Test]
    public void ForwardMatchSelectsForCorrectParameterSize()
    {
        AssertForwardsMatch("EAB", "AB(x)", false, testOrderingAgnostict: true);
        AssertForwardsMatch("EA[B][B(1)]", "AB(x)", true, expectedMatchToTargetMapping: new[] { (0, 1), (1, 6) });
        AssertForwardsMatch("EA[B][B(2)]", "AB(x, y)", false, testOrderingAgnostict: true);
        AssertForwardsMatch("EA[B][B(3)]B(2, 3)", "AB(x, y)", true, expectedMatchToTargetMapping: new[] { (0, 1), (1, 8) });
    }

    #endregion

    #region Ordering-agnosting boolean forward symbol matching

    [Test]
    public void ForwardsDoubleMatchTreeStructureWhenIndividualsNotNestedAndSwapped()
    {
        AssertForwardsMatch("EA[B]C", "A[B][C]", true, testOrderingAgnostict: true, testOrderingInvariant: true);
        AssertForwardsMatch("EA[C]B", "A[B][C]", true, testOrderingAgnostict: true, testOrderingInvariant: false);
    }

    [Test]
    public void ForwardsMatchLongTreeStructureShuffled()
    {
        AssertForwardsMatch("EA[DF][B][C]", "A[B][C]DF", true, testOrderingAgnostict: true, testOrderingInvariant: false);
        AssertForwardsMatch("EA[D[F]][C][B]", "A[B][C]DF", true, testOrderingAgnostict: true, testOrderingInvariant: false);
        AssertForwardsMatch("EA[D[F][C][B]]", "A[B][C]DF", false, testOrderingAgnostict: true, testOrderingInvariant: false);
    }
    [Test]
    public void ForwardsDoubleMatchTreeStructureFailsWhenDisorderedOrderAgnostic()
    {
        AssertForwardsMatch("EA[B[C]]", "A[B][C]", false, testOrderingAgnostict: true, testOrderingInvariant: true);
        AssertForwardsMatch("EAB[C]", "A[B][C]", false, testOrderingAgnostict: true, testOrderingInvariant: true);
        AssertForwardsMatch("EAC[B]", "A[B][C]", false, testOrderingAgnostict: true, testOrderingInvariant: false);
    }
    [Test]
    public void ForwardMatchTreeStructureShuffledMatch()
    {
        AssertForwardsMatch("EA[C][B]", "A[B][C]", true, testOrderingAgnostict: true, testOrderingInvariant: false);
        AssertForwardsMatch("EA[C[B]]", "A[B][C]", false, testOrderingAgnostict: true, testOrderingInvariant: false);
        AssertForwardsMatch("EA[[C]B]", "A[B][C]", true, testOrderingAgnostict: true, testOrderingInvariant: false);
        AssertForwardsMatch("EA[A[C]B]", "A[B][C]", false, testOrderingAgnostict: true, testOrderingInvariant: false);

        AssertForwardsMatch("EA[C][B]", "A[[B]C]", true, testOrderingAgnostict: true, testOrderingInvariant: false);
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
        AssertForwardsMatch("EA[C]B", "A[B][C]", expectedMatchToTargetMapping: new[] { (0, 1), (2, 5), (5, 3) }, testOrderingAgnostict: true, testOrderingInvariant: false);
    }
    [Test]
    public void ForwardBranchMultipleIdenticleBranchesMapped()
    {
        AssertForwardsMatch("EA[BC][B][BCD]", "A[B][B][B]", expectedMatchToTargetMapping: new[] { (0, 1), (2, 3), (5, 7), (8, 10) });
    }
    #endregion
}
