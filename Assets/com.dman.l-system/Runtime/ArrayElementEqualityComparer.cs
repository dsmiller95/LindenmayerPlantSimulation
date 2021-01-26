using System.Collections.Generic;

namespace Dman.LSystem
{
    public class ArrayElementEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private IEqualityComparer<T> elementComparer;
        public ArrayElementEqualityComparer() : this(EqualityComparer<T>.Default)
        {
        }
        public ArrayElementEqualityComparer(IEqualityComparer<T> comparer)
        {
            elementComparer = comparer;
        }

        public bool Equals(T[] x, T[] y)
        {
            if (x == y)
            {
                return true;
            }
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (!elementComparer.Equals(x[i], y[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(T[] obj)
        {
            int hashCode = 0;
            for (int i = 0; i < obj.Length; i++)
            {
                hashCode ^= obj[i].GetHashCode();
            }
            return hashCode;
        }
    }
}
