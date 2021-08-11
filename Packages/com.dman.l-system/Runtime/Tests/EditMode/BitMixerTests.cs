using Dman.LSystem;
using NUnit.Framework;
using System;

public class BitMixerTests
{
    [Test]
    public void MixesAndUnmixesBits()
    {
        var numbers = new uint[]
        {
            0,
            1,
            0x10000000,
            0xFFFFFFFF,
            0xF0F0F0F0,
            0x00FF0F00,
            993,
            1002,
            3,
            16
        };

        foreach (var number in numbers)
        {
            var mixed = BitMixer.Mix(number);
            var unmixed = BitMixer.UnMix(mixed);
            Assert.AreEqual(number, unmixed, $"expected: {Convert.ToString(number, 2).PadLeft(32, '0')} actual: {Convert.ToString(unmixed, 2).PadLeft(32, '0')} mixed: {Convert.ToString(mixed, 2).PadLeft(32, '0')}");
        }
    }
}
