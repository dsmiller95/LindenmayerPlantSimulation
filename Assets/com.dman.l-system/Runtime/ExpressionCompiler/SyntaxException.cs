using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem.ExpressionCompiler
{
    internal class SyntaxException: System.Exception
    {
        public SyntaxException(string description): base(description)
        {

        }
    }
}
