using Dman.LSystem.SystemCompiler;
using Dman.LSystem.SystemRuntime;
using LSystem.Runtime.SystemRuntime;
using NUnit.Framework;
using Unity.Collections;

public class BasicRuleTests
{
    private void AssertRuleReplacement(
        string ruleText,
        int[] sourceSymbols = null,
        float[][] sourceParameters = null,
        string expectedReplacementText = null,
        string axiom = null,
        int ruleParamMemoryStartIndex = 0,
        int matchIndex = 0,
        int paramTempMemorySize = 0,
        float[] globalParams = null,
        string[] globalParamNames = null,
        int expectedReplacementPatternIndex = 0
        )
    {
        globalParamNames = globalParamNames ?? new string[0];
        using var symbols = axiom == null ? new SymbolString<float>(sourceSymbols, sourceParameters) : new SymbolString<float>(axiom, Allocator.Persistent);
        using var expectedReplacement = new SymbolString<float>(expectedReplacementText, Allocator.Persistent);

        var ruleFromString = new BasicRule(RuleParser.ParseToRule(ruleText, globalParamNames));
        globalParams = globalParams ?? new float[0];
        using var globalNative = new NativeArray<float>(globalParams, Allocator.Persistent);

        var expectedTotalParamReplacement = expectedReplacement.newParameters.data.Length;

        using var paramMemory = new NativeArray<float>(paramTempMemorySize, Allocator.Persistent);
        var branchCache = new SymbolStringBranchingCache();
        branchCache.BuildJumpIndexesFromSymbols(symbols.symbols);
        var random = new Unity.Mathematics.Random();
        var matchSingleData = new LSystemSingleSymbolMatchData
        {
            isTrivial = false,
            tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(ruleParamMemoryStartIndex)
        };

        var potentialMatch = ruleFromString.AsBlittable().PreMatchCapturedParametersWithoutConditional(
            branchCache,
            symbols,
            matchIndex,
            paramMemory,
            matchSingleData.tmpParameterMemorySpace.index,
            out var specificMatchDetails
            );

        Assert.IsTrue(potentialMatch);

        var postMatchWithConditional = ruleFromString.TryMatchSpecificMatch(
            globalNative,
            paramMemory,
            specificMatchDetails.matchedParameters,
            ref random,
            ref matchSingleData
            );
        Assert.IsTrue(postMatchWithConditional);
        Assert.AreEqual(expectedReplacementPatternIndex, matchSingleData.selectedReplacementPattern);
        Assert.AreEqual(paramTempMemorySize, matchSingleData.tmpParameterMemorySpace.length, "parameter temp memory size mismatch");
        Assert.AreEqual(expectedReplacement.symbols.Length, matchSingleData.replacementSymbolIndexing.length, "replacement symbols size mismatch");
        Assert.AreEqual(expectedReplacement.newParameters.data.Length, matchSingleData.replacementParameterIndexing.length, "replacement parameter size mismatch");

        matchSingleData.replacementSymbolIndexing.index = 0;
        matchSingleData.replacementParameterIndexing.index = 0;

        using var resultSymbols = new SymbolString<float>(
                expectedReplacement.symbols.Length,
                expectedReplacement.newParameters.data.Length,
                Allocator.Persistent);
        ruleFromString.WriteReplacementSymbols(
            globalNative,
            paramMemory,
            resultSymbols,
            matchSingleData
            );

        Assert.AreEqual(expectedReplacementText, resultSymbols.ToString());
        Assert.IsTrue(expectedReplacement.Equals(resultSymbols));
    }
    private void AssertRuleDoesNotMatchCondtitional(
        string ruleText,
        int[] sourceSymbols = null,
        float[][] sourceParameters = null,
        string axiom = null,
        int ruleParamMemoryStartIndex = 0,
        int matchIndex = 0,
        int paramTempMemorySize = 0,
        float[] globalParams = null
        )
    {
        using var symbols = axiom == null ? new SymbolString<float>(sourceSymbols, sourceParameters) : new SymbolString<float>(axiom, Allocator.Persistent);
        var ruleFromString = new BasicRule(RuleParser.ParseToRule(ruleText));
        globalParams = globalParams ?? new float[0];
        using var globalNative = new NativeArray<float>(globalParams, Allocator.Persistent);

        using var paramMemory = new NativeArray<float>(paramTempMemorySize, Allocator.Persistent);
        var branchCache = new SymbolStringBranchingCache();
        branchCache.BuildJumpIndexesFromSymbols(symbols.symbols);
        var random = new Unity.Mathematics.Random();
        var matchSingleData = new LSystemSingleSymbolMatchData
        {
            isTrivial = false,
            tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(ruleParamMemoryStartIndex)
        };

        var potentialMatch = ruleFromString.AsBlittable().PreMatchCapturedParametersWithoutConditional(
            branchCache,
            symbols,
            matchIndex,
            paramMemory,
            matchSingleData.tmpParameterMemorySpace.index,
            out var specificMatchDetails
            );

        Assert.IsTrue(potentialMatch);

        var postMatchWithConditional = ruleFromString.TryMatchSpecificMatch(
            globalNative,
            paramMemory,
            specificMatchDetails.matchedParameters,
            ref random,
            ref matchSingleData
            );
        Assert.IsFalse(postMatchWithConditional);
    }
    private void AssertRuleDoesNotMatchBeforeConditional(
        string ruleText,
        int[] sourceSymbols = null,
        float[][] sourceParameters = null,
        string axiom = null,
        int ruleParamMemoryStartIndex = 0,
        int matchIndex = 0,
        int paramTempMemorySize = 0,
        float[] globalParams = null
        )
    {
        using var symbols = axiom == null ? new SymbolString<float>(sourceSymbols, sourceParameters) : new SymbolString<float>(axiom, Allocator.Persistent);
        var ruleFromString = new BasicRule(RuleParser.ParseToRule(ruleText));
        globalParams = globalParams ?? new float[0];
        using var globalNative = new NativeArray<float>(globalParams, Allocator.Persistent);

        using var paramMemory = new NativeArray<float>(paramTempMemorySize, Allocator.Persistent);
        var branchCache = new SymbolStringBranchingCache();
        branchCache.BuildJumpIndexesFromSymbols(symbols.symbols);
        var matchSingleData = new LSystemSingleSymbolMatchData
        {
            isTrivial = false,
            tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(ruleParamMemoryStartIndex)
        };

        var potentialMatch = ruleFromString.AsBlittable().PreMatchCapturedParametersWithoutConditional(
            branchCache,
            symbols,
            matchIndex,
            paramMemory,
            matchSingleData.tmpParameterMemorySpace.index,
            out var specificMatchDetails
            );

        Assert.IsFalse(potentialMatch);
    }

    [Test]
    public void BasicRuleRejectsApplicationIfAnyParameters()
    {
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A -> AB"));
        var symbols = new SymbolString<float>(new int[] { 'A' }, new float[][] { new float[0] });
        try
        {
            var globalParams = new float[0];
            using var globalNative = new NativeArray<float>(globalParams, Allocator.Persistent);
            using var paramMemory = new NativeArray<float>(0, Allocator.Persistent);
            var branchCache = new SymbolStringBranchingCache();
            branchCache.BuildJumpIndexesFromSymbols(symbols.symbols);
            var random = new Unity.Mathematics.Random();
            var matchSingleData = new LSystemSingleSymbolMatchData
            {
                isTrivial = false,
                tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(0)
            };

            var preMatchSuccess = ruleFromString.AsBlittable().PreMatchCapturedParametersWithoutConditional(
                branchCache,
                symbols,
                0,
                paramMemory,
                matchSingleData.tmpParameterMemorySpace.index,
                out var specificMatchDetails
                );
            Assert.IsTrue(preMatchSuccess);
            var postMatchWithConditional = ruleFromString.TryMatchSpecificMatch(
                globalNative,
                paramMemory,
                specificMatchDetails.matchedParameters,
                ref random,
                ref matchSingleData
                );
            Assert.IsTrue(postMatchWithConditional);
            Assert.AreEqual(0, matchSingleData.selectedReplacementPattern);
            Assert.AreEqual(0, matchSingleData.tmpParameterMemorySpace.length);
            Assert.AreEqual(2, matchSingleData.replacementSymbolIndexing.length);
            Assert.AreEqual(0, matchSingleData.replacementParameterIndexing.length);

            symbols.newParameters[0] = new JaggedIndexing
            {
                index = 0,
                length = 1
            };

            matchSingleData = new LSystemSingleSymbolMatchData
            {
                isTrivial = false,
                tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(0)
            };
            preMatchSuccess = ruleFromString.AsBlittable().PreMatchCapturedParametersWithoutConditional(
                branchCache,
                symbols,
                0,
                paramMemory,
                matchSingleData.tmpParameterMemorySpace.index,
                out specificMatchDetails
                );
            Assert.IsFalse(preMatchSuccess);


            symbols.newParameters.data.Dispose();
            symbols.newParameters.data = new Unity.Collections.NativeArray<float>(new float[] { 1 }, Unity.Collections.Allocator.Persistent);

            matchSingleData = new LSystemSingleSymbolMatchData
            {
                isTrivial = false,
                tmpParameterMemorySpace = JaggedIndexing.GetWithNoLength(0)
            };
            preMatchSuccess = ruleFromString.AsBlittable().PreMatchCapturedParametersWithoutConditional(
                branchCache,
                symbols,
                0,
                paramMemory,
                matchSingleData.tmpParameterMemorySpace.index,
                out specificMatchDetails
                );
            Assert.IsFalse(preMatchSuccess);
        }
        finally
        {
            symbols.Dispose();
        }
    }
    [Test]
    public void BasicRuleReplacesSelfWithReplacement()
    {
        AssertRuleReplacement(
            "A -> AB",
            new int[] { 'A' },
            new float[][] { new float[0] },
            "AB"
            );
    }
    [Test]
    public void BasicRuleReplacesParameters()
    {
        AssertRuleReplacement(
            "A(x, y) -> B(y + x)C(x)A(y, x)",
            new int[] { 'A' },
            new float[][] { new float[] { 20, 1 } },
            "B(21)C(20)A(1, 20)",
            paramTempMemorySize: 2
            );
    }
    [Test]
    public void BasicRuleDifferentParametersNoMatch()
    {
        AssertRuleDoesNotMatchBeforeConditional(
            "A(x, y) -> B(y + x)C(x)A(y, x)",
            new int[] { 'A' },
            new float[][] { new float[] { 20 } }
            );
    }
    [Test]
    public void ParametricConditionalNoMatch()
    {
        AssertRuleDoesNotMatchCondtitional(
            "A(x) : x < 10 -> A(x + 1)",
            new int[] { 'A' },
            new float[][] { new float[] { 20 } },
            paramTempMemorySize: 1
            );
    }
    [Test]
    public void ParametricConditionalMatch()
    {
        AssertRuleReplacement(
            "A(x) : x < 10 -> A(x + 1)",
            new int[] { 'A' },
            new float[][] { new float[] { 6 } },
            "A(7)",
            paramTempMemorySize: 1
            );
    }
    [Test]
    public void BasicRuleReplacesParametersAndGlobalParameters()
    {
        AssertRuleReplacement(
            "A(x, y) -> B(global + x)C(y)",
            new int[] { 'A' },
            new float[][] { new[] { 20f, 1f } },
            "B(27)C(1)",
            paramTempMemorySize: 2,
            globalParamNames: new[] { "global" },
            globalParams: new[] { 7f }
            );
    }
    [Test]
    public void ContextualRuleRequiresContextToMatch()
    {
        AssertRuleDoesNotMatchBeforeConditional(
            "B < A > B -> B",
            axiom: "B(20)AABAB",
            paramTempMemorySize: 0,
            matchIndex: 1
            );

        AssertRuleReplacement(
            "B < A > B -> B",
            axiom: "B(20)AABAB",
            expectedReplacementText: "B",
            paramTempMemorySize: 0,
            matchIndex: 4
            );
    }
    [Test]
    public void ContextualRuleCapturesParametersFromSuffix()
    {
        AssertRuleReplacement(
            "A(x) > B(y) -> B(x - y)",
            axiom: "A(20)B(3.1)",
            expectedReplacementText: "B(16.9)",
            paramTempMemorySize: 2,
            matchIndex: 0
            );
    }
    [Test]
    public void ContextualRuleCapturesParametersFromPrefix()
    {
        AssertRuleReplacement(
            "B(x) < A(y) -> B(x - y)",
            axiom: "B(20)A(3.1)",
            expectedReplacementText: "B(16.9)",
            paramTempMemorySize: 2,
            matchIndex: 1
            );
    }
    [Test]
    public void ContextualRuleCapturesParametersFromPrefixAndSuffix()
    {
        AssertRuleReplacement(
            "B(x) < A(y) > B(z) -> B((x - y)/z)",
            axiom: "B(20)A(3.1)B(2)",
            expectedReplacementText: "B(8.45)",
            paramTempMemorySize: 3,
            matchIndex: 1
            );
    }

    [Test]
    public void ContextualRulePrefixNoMatchWhenParameterMismatch()
    {
        AssertRuleDoesNotMatchBeforeConditional(
            "B(x) < A(y) -> B(x - y)",
            axiom: "BA(3.1)",
            paramTempMemorySize: 2,
            matchIndex: 1
            );
    }
    [Test]
    public void ContextualRuleSuffixNoMatchWhenParameterMismatch()
    {
        AssertRuleDoesNotMatchBeforeConditional(
            "A(x) > B(y) -> B(x - y)",
            axiom: "A(3.1)B",
            paramTempMemorySize: 2,
            matchIndex: 0
            );
    }
    [Test]
    public void ContextualRuleSuffixFindsMatchBySkipIfPossible()
    {
        AssertRuleReplacement(
            "A(x) > B(y) -> B(x - y)",
            axiom: "A(3.1)[B]B(20)",
            expectedReplacementText: "B(-16.9)",
            paramTempMemorySize: 2,
            matchIndex: 0
            );

    }
}
