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
        //int[] expectedReplacementSymbols = null,
        //float[] expectedReplacementParameters = null,
        //SymbolString<float>.JaggedIndexing[] expectedReplacementIndexing = null
        )
    {
        globalParamNames = globalParamNames ?? new string[0];
        using var symbols = axiom == null ? new SymbolString<float>(sourceSymbols, sourceParameters) : new SymbolString<float>(axiom, Allocator.Persistent);
        using var expectedReplacement = new SymbolString<float>(expectedReplacementText, Allocator.Persistent);

        var ruleFromString = new BasicRule(RuleParser.ParseToRule(ruleText, globalParamNames));
        globalParams = globalParams ?? new float[0];
        using var globalNative = new NativeArray<float>(globalParams, Allocator.Persistent);


        //expectedReplacementSymbols = expectedReplacementSymbols ?? new int[0];
        //expectedReplacementParameters = expectedReplacementParameters ?? new float[0];
        //expectedReplacementIndexing = expectedReplacementIndexing ?? expectedReplacementSymbols.Select(x => new SymbolString<float>.JaggedIndexing
        //{
        //    length = 0,
        //    index = 0
        //}).ToArray();
        var expectedTotalParamReplacement = expectedReplacement.newParameters.data.Length;

        using var paramMemory = new NativeArray<float>(paramTempMemorySize, Allocator.Persistent);
        var branchCache = new SymbolStringBranchingCache();
        branchCache.BuildJumpIndexesFromSymbols(symbols.symbols);
        var random = new Unity.Mathematics.Random();
        var matchSingleData = new LSystemStepMatchIntermediate
        {
            isTrivial = false,
            parametersStartIndex = ruleParamMemoryStartIndex
        };

        var preMatchSuccess = ruleFromString.PreMatchCapturedParameters(
            branchCache,
            symbols,
            matchIndex,
            globalNative,
            paramMemory,
            ref random,
            ref matchSingleData
            );
        Assert.IsTrue(preMatchSuccess);
        Assert.AreEqual(expectedReplacementPatternIndex, matchSingleData.selectedReplacementPattern);
        Assert.AreEqual(paramTempMemorySize, matchSingleData.matchedParametersCount);
        Assert.AreEqual(expectedReplacement.symbols.Length, matchSingleData.replacementSymbolLength);
        Assert.AreEqual(expectedReplacement.newParameters.data.Length, matchSingleData.replacementParameterCount);

        matchSingleData.replacementSymbolStartIndex = 0;
        matchSingleData.replacementParameterStartIndex = 0;

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
    private void AssertRuleDoesNotMatch(
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
        var matchSingleData = new LSystemStepMatchIntermediate
        {
            isTrivial = false,
            parametersStartIndex = ruleParamMemoryStartIndex
        };

        var preMatchSuccess = ruleFromString.PreMatchCapturedParameters(
            branchCache,
            symbols,
            matchIndex,
            globalNative,
            paramMemory,
            ref random,
            ref matchSingleData
            );
        Assert.IsFalse(preMatchSuccess);
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
            var matchSingleData = new LSystemStepMatchIntermediate
            {
                isTrivial = false,
                parametersStartIndex = 0
            };

            var preMatchSuccess = ruleFromString.PreMatchCapturedParameters(
                branchCache,
                symbols,
                0,
                globalNative,
                paramMemory,
                ref random,
                ref matchSingleData
                );
            Assert.IsTrue(preMatchSuccess);
            Assert.AreEqual(0, matchSingleData.selectedReplacementPattern);
            Assert.AreEqual(0, matchSingleData.matchedParametersCount);
            Assert.AreEqual(2, matchSingleData.replacementSymbolLength);
            Assert.AreEqual(0, matchSingleData.replacementParameterCount);

            symbols.newParameters[0] = new JaggedIndexing
            {
                index = 0,
                length = 1
            };

            matchSingleData = new LSystemStepMatchIntermediate
            {
                isTrivial = false,
                parametersStartIndex = 0
            };
            preMatchSuccess = ruleFromString.PreMatchCapturedParameters(
                branchCache,
                symbols,
                0,
                globalNative,
                paramMemory,
                ref random,
                ref matchSingleData
                );
            Assert.IsFalse(preMatchSuccess);


            symbols.newParameters.data.Dispose();
            symbols.newParameters.data = new Unity.Collections.NativeArray<float>(new float[] { 1 }, Unity.Collections.Allocator.Persistent);

            matchSingleData = new LSystemStepMatchIntermediate
            {
                isTrivial = false,
                parametersStartIndex = 0
            };
            preMatchSuccess = ruleFromString.PreMatchCapturedParameters(
                branchCache,
                symbols,
                0,
                globalNative,
                paramMemory,
                ref random,
                ref matchSingleData
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
        var ruleFromString = new BasicRule(RuleParser.ParseToRule("A -> AB"));
        using var symbols = new SymbolString<float>(new int[] { 'A' }, new float[][] { new float[0] });
        var globalParams = new float[0];
        using var globalNative = new NativeArray<float>(globalParams, Allocator.Persistent);
        using var paramMemory = new NativeArray<float>(0, Allocator.Persistent);
        var branchCache = new SymbolStringBranchingCache();
        branchCache.BuildJumpIndexesFromSymbols(symbols.symbols);
        var random = new Unity.Mathematics.Random();
        var matchSingleData = new LSystemStepMatchIntermediate
        {
            isTrivial = false,
            parametersStartIndex = 0
        };

        var preMatchSuccess = ruleFromString.PreMatchCapturedParameters(
            branchCache,
            symbols,
            0,
            globalNative,
            paramMemory,
            ref random,
            ref matchSingleData
            );
        Assert.IsTrue(preMatchSuccess);
        Assert.AreEqual(0, matchSingleData.selectedReplacementPattern);
        Assert.AreEqual(0, matchSingleData.matchedParametersCount);
        Assert.AreEqual(2, matchSingleData.replacementSymbolLength);
        Assert.AreEqual(0, matchSingleData.replacementParameterCount);

        matchSingleData.replacementSymbolStartIndex = 0;
        matchSingleData.replacementParameterStartIndex = 0;

        using var targetSymbols = new SymbolString<float>(2, 0, Allocator.Persistent);

        ruleFromString.WriteReplacementSymbols(
            globalNative,
            paramMemory,
            targetSymbols,
            matchSingleData
            );

        Assert.AreEqual("AB", targetSymbols.ToString());
        Assert.AreEqual(2, targetSymbols.newParameters.indexing.Length);
        Assert.AreEqual(0, targetSymbols.newParameters.data.Length);
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
        AssertRuleDoesNotMatch(
            "A(x, y) -> B(y + x)C(x)A(y, x)",
            new int[] { 'A' },
            new float[][] { new float[] { 20 } }
            );
    }
    [Test]
    public void ParametricConditionalNoMatch()
    {
        AssertRuleDoesNotMatch(
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
        AssertRuleDoesNotMatch(
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
        AssertRuleDoesNotMatch(
            "B(x) < A(y) -> B(x - y)",
            axiom: "BA(3.1)",
            paramTempMemorySize: 2,
            matchIndex: 1
            );
    }
    [Test]
    public void ContextualRuleSuffixNoMatchWhenParameterMismatch()
    {
        AssertRuleDoesNotMatch(
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
