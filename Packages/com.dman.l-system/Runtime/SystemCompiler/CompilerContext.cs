namespace Dman.LSystem.SystemCompiler
{
    public struct CompilerContext
    {
        public int originalIndex;
        public int originalEndIndex;

        public CompilerContext(int index) : this(index, index + 1)
        {
        }
        public CompilerContext(int index, int endIndex)
        {
            originalIndex = index;
            originalEndIndex = endIndex;
        }

        public CompilerContext(CompilerContext first, CompilerContext last)
        {
            originalIndex = first.originalIndex;
            originalEndIndex = last.originalEndIndex;
        }

        public SyntaxException ExceptionHere(string message)
        {
            return new SyntaxException(message,
                originalIndex,
                originalEndIndex - originalIndex);
        }

        public override string ToString()
        {
            return $"{originalIndex}-{originalEndIndex}";
        }
    }
}
