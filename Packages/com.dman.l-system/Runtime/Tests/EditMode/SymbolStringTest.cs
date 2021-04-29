using Dman.LSystem;
using NUnit.Framework;

public class SymbolStringTest
{
    [Test]
    public void SymbolStringConstructorConvertsToInts()
    {
        var symbolFromString = SymbolString<float>.FromString("AABA");
        var convertedSymbols = symbolFromString.symbols;

        Assert.AreEqual(convertedSymbols[0], 65);
        Assert.AreEqual(convertedSymbols[1], 65);
        Assert.AreEqual(convertedSymbols[2], 66);
        Assert.AreEqual(convertedSymbols[3], 65);
        symbolFromString.Dispose();
    }
}
