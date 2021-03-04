using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dman.LSystem.SystemCompiler
{
    public static class ArrayStringifyExtensions
    {

        public static string JoinText<T>(this IEnumerable<T> self, string seperator = ", ", Func<T, string> stringify = null)
        {
            if (stringify == null)
                stringify = x => x.ToString();
            var selfEnum = self.GetEnumerator();
            if (!selfEnum.MoveNext())
            {
                return "";
            }
            var result = new StringBuilder(stringify(selfEnum.Current));
            while (selfEnum.MoveNext())
            {
                result.Append(seperator);
                result.Append(stringify(selfEnum.Current));
            }
            return result.ToString();
        }
    }
}
