namespace MACsharp.Helpers
{
    using System.Collections.Generic;

    public class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>>
    {
        public bool Equals(IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            if (first == second) return true;
            if ((first == null) || (second == null)) return false;
            if (first.Count != second.Count) return false;

            var valueComparer = EqualityComparer<TValue>.Default;

            foreach (var (key, value) in first)
            {
                if (!second.TryGetValue(key, out var secondValue)) return false;
                if (!valueComparer.Equals(value, secondValue)) return false;
            }
            return true;
        }

        public int GetHashCode(IDictionary<TKey, TValue> obj)
        {
            unchecked
            {
                int hash = 0;
                int count = 0;
                foreach (var (key, value) in obj)
                {
                    hash += key.GetHashCode();
                    hash += value.GetHashCode();
                    count++;
                }
                return 31 * hash + count.GetHashCode();
            }
        }
    }
}