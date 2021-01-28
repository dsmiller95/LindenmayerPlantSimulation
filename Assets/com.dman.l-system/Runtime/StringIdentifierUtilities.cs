using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dman.LSystem
{
    public static class StringIdentifierUtilities
    {
        public static int[] ToIntArray(this string self)
        {
            return self.Select(x => (int)x).ToArray();
        }
        public static string ToStringFromChars(this IEnumerable<int> self)
        {
            return new string(self.Select(x => (char)x).ToArray());
        }
    }
}
