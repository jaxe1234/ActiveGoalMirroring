namespace MACsharp.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class EnumerableComparer<T> : IEqualityComparer<IEnumerable<T>> where T : IEquatable<T>
    {
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;
            
            //Check whether the products' properties are equal.
            return x.SequenceEqual(y, EqualityComparer<T>.Default);
        }

        public int GetHashCode(IEnumerable<T> list)
        {
            unchecked
            {
                int hash = 0;
                int count = 0;
                foreach (var item in list)
                {
                    hash += item.GetHashCode();
                    count++;
                }
                return 31 * hash + count.GetHashCode();
            }
        }
    }
}
