namespace MACsharp.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableHelpers
    {
        public static bool DictionaryEquals<TKey, TValue>(this IDictionary<TKey, TValue> first,
            IDictionary<TKey, TValue> second)
        {
            return new DictionaryComparer<TKey, TValue>().Equals(first, second);
        }
        
        public static int GetSequenceHashCode<T>(this IEnumerable<T> list) where T : IEquatable<T>
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

        public static IEnumerable<IEnumerable<T>> Permutations<T>(this IEnumerable<T> values, int ofLength)
            where T : IEquatable<T>
        {
            var set = values.Permutations().Select(perm => perm.Take(ofLength)).ToList();

            return set.Distinct(new EnumerableComparer<T>());
        }

        public static IEnumerable<IEnumerable<T>> Permutations<T>(this IEnumerable<T> values) where T : IEquatable<T>
        {
            var myVals = values.ToList();
            if (myVals.Count == 1)
                return new[] { myVals };
            return myVals.SelectMany(v => Permutations(myVals.Where(x => !x.Equals(v))), (v, p) => p.Prepend(v));
        }

        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> elements, int ofLength)
        {
            IEnumerable<T> enumerable = elements as T[] ?? elements.ToArray();
            return ofLength == 0 ? new[] { Array.Empty<T>() } :
                enumerable.SelectMany((e, i) =>
                    enumerable.Skip(i + 1).Combinations(ofLength - 1).Select(c => (new[] { e }).Concat(c)));
        }


        public static IEnumerable<(IEnumerable<T>, IEnumerable<T>)> GetOptions<T>(IEnumerable<T> productItems,
            IEnumerable<T> items) where T : IEquatable<T>
        {
            var productItemList = productItems.ToList();

            var minLength = 1;
            var maxLength = productItemList.Count - minLength;

            var permutations = items.Permutations(productItemList.Count).ToList();

            var options = new List<(IEnumerable<T>, IEnumerable<T>)>(permutations.Count / 2);

            foreach (var perm in permutations)
            {
                var slices = perm.Slices(minLength, maxLength);
                foreach (var slice in slices)
                {
                    if (IsSatisfiedBy(productItemList, slice.Item1, slice.Item2))
                    {
                        options.Add(slice);
                    }
                }
            }

            return options;
        }

        public static bool IsSatisfiedBy<T>(IEnumerable<T> result, IEnumerable<T> first, IEnumerable<T> second)
            where T : IEquatable<T>
        {
            IEnumerable<T> firstAsArray = first as T[] ?? first.ToArray();
            IEnumerable<T> secondAsArray = second as T[] ?? second.ToArray();
            IEnumerable<T> resultAsArray = result as T[] ?? result.ToArray();

            return firstAsArray.Count() + secondAsArray.Count() == resultAsArray.Count()
                   && firstAsArray.Concat(secondAsArray).ToList().ToHashSet().SetEquals(resultAsArray);
        }

        public static IEnumerable<(IEnumerable<T>, IEnumerable<T>)> Slices<T>(this IEnumerable<T> enumerable,
            int minLength, int maxLength)
        {
            if (minLength > maxLength)
                throw new ArgumentException($"'{nameof(minLength)}' cannot be larger than '{nameof(maxLength)}'.");

            IEnumerable<T> enumerated = enumerable.ToList();
            if (minLength + maxLength > enumerated.Count())
                throw new ArgumentException(
                    $"Sum of '{nameof(minLength)}' and '{nameof(maxLength)}' cannot be larger than length of '{nameof(enumerable)}'.");

            var slices = new List<(IEnumerable<T>, IEnumerable<T>)>();

            for (var i = minLength; i <= maxLength; i++)
            {
                var first = enumerated.Take(i);
                var second = enumerated.Skip(i).Take(maxLength - i + 1);
                slices.Add((first, second));
            }

            return slices;
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };

            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item }));
        }

        public static T[] Shuffle<T>(this T[] array)
        {
            int n = array.Length;

            while (n > 1)
            {
                int k = GlobalConfig.Rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }

            return array;
        }

        public static List<T> Mode<T>(this IEnumerable<T> collection) where T : IEquatable<T>
        {
            var groupings = collection.GroupBy(value => value).OrderByDescending(group => group.Count()).ToList();

            return groupings.Where(group => group.Count() == groupings.ElementAt(0).Count()).Select(group => group.Key)
                .ToList();
        }
    }
}