using Dman.LSystem;
using NUnit.Framework;
using System;

public class RustExternTests
{
    [Test]
    public void MultipliesNumbers()
    {
        Assert.AreEqual(6, RustExternals.double_input(3));
        Assert.AreEqual(6, RustExternals.triple_input(2));
    }
}
