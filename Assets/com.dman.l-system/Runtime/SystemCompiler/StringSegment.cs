namespace Dman.LSystem.SystemCompiler
{
    public struct StringSegment
    {
        public CompilerContext context;
        public string value;

        public StringSegment(string val, CompilerContext context)
        {
            value = val;
            this.context = context;
        }

        public static StringSegment MergeConsecutive(StringSegment first, StringSegment second)
        {
            return new StringSegment(first.value + second.value, new CompilerContext(first.context, second.context));
        }

        public override string ToString()
        {
            return value;
        }
    }
}
