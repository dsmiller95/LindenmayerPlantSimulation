namespace Dman.LSystem
{
    public static class BitMixer
    {
        public static uint Mix(uint number)
        {
            uint onePer4 = 0x88888888;
            var b1 = number & onePer4;
            var b2 = Rotate(number & (onePer4 >> 1), 8 + 2);
            var b3 = Rotate(number & (onePer4 >> 2), 16);
            var b4 = Rotate(number & (onePer4 >> 3), 24 - 2);

            return b1 | b2 | b3 | b4;
        }
        public static uint UnMix(uint number)
        {
            uint onePer4 = 0x88888888;
            var b1 = number & onePer4;
            var b2 = Rotate(number & (onePer4 >> 3), 32 - (8 + 2));
            var b3 = Rotate(number & (onePer4 >> 2), 32 - (16));
            var b4 = Rotate(number & (onePer4 >> 1), 32 - (24 - 2));

            return b1 | b2 | b3 | b4;
        }
        private static uint Rotate(uint value, int amount)
        {
            return (value << amount) | (value >> (32 - amount));
        }
    }
}
