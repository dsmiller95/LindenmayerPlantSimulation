using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.SystemCompiler
{
    public struct CompilerContext
    {
        public int originalIndex;
        public int originalEndIndex;

        public CompilerContext(int index): this(index, index + 1)
        {
        }
        public CompilerContext(int index, int endIndex)
        {
            this.originalIndex = index;
            this.originalEndIndex = endIndex;
        }

        public CompilerContext(CompilerContext first, CompilerContext last)
        {
            this.originalIndex = first.originalIndex;
            this.originalEndIndex = last.originalEndIndex;
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
