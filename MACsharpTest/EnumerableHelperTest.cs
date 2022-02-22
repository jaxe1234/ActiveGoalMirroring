namespace MACsharpTest
{
    using MACsharp.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TestUtil;
    using Xunit;
    
    public class EnumerableHelperTest : MACsharpTestBase
    {
        [Fact]
        public void TestCartesianProduct()
        {
            var lists = new List<List<string>> {
                new () {"a","b"},
                new () {"d","e"},
                new () {"g","h"}
            };

            var permutations = lists.CartesianProduct().ToList();

            var expected = new List<List<string>>()
            {
                new () { "a", "d", "g" },
                new () { "a", "d", "h" },
                new () { "a", "e", "g" },
                new () { "a", "e", "h" },

                new () { "b", "d", "g" },
                new () { "b", "d", "h" },
                new () { "b", "e", "g" },
                new () { "b", "e", "h" },
            };

            Assert.Equal(expected.Count, permutations.Count);

            AssertSequenceEqual(permutations, expected);
        }

        [Fact]
        public void TestSlices()
        {
            var unitaryList = new List<string>() { "a", "b" };
            var expectedUnitary = new List<(IEnumerable<string>, IEnumerable<string>)>()
            {
                (new List<string> { "a" }, new List<string> { "b" })
            };

            TestSlicesCore(unitaryList, expectedUnitary, 1, 1);

            var list2 = new List<int> { 1, 2, 3, 4 };
            var expected2 = new List<(IEnumerable<int>, IEnumerable<int>)>()
            {
                (new List<int> { 1 }, new List<int> { 2, 3, 4 }),
                (new List<int> { 1, 2 }, new List<int> { 3, 4 }),
                (new List<int> { 1, 2, 3 }, new List<int> { 4 })
            };

            TestSlicesCore(list2, expected2, 1, 3);
            
            var list3 = list2;
            var expected3 = new List<(IEnumerable<int>, IEnumerable<int>)>()
            {
                (new List<int> { 1 }, new List<int> { 2, 3 }),
                (new List<int> { 1, 2 }, new List<int> { 3 }),
            };

            TestSlicesCore(list3, expected3, 1, 2);
            
            var list4 = new List<int> { 1, 2, 3, 4, 5 };

            // throws because minLength > maxLength
            Assert.Throws<ArgumentException>(() => list4.Slices(2, 1));

            // throws because minLength + maxLength > list.Count
            Assert.Throws<ArgumentException>(() => list4.Slices(1, list4.Count + 1));
        }

        private void TestSlicesCore<T>(IEnumerable<T> enumerable,
            IEnumerable<(IEnumerable<T>, IEnumerable<T>)> expectedSlices, int minLength, int maxLength)
        {
            IEnumerable<T> enumerableAsArray = enumerable as T[] ?? enumerable.ToArray();

            var actual = enumerableAsArray.Slices(minLength, maxLength);

            IEnumerable<(IEnumerable<T>, IEnumerable<T>)> actualSlicesAsArray = actual as (IEnumerable<T>, IEnumerable<T>)[] ?? actual.ToArray();

            Assert.NotNull(actual);

            if (enumerableAsArray.Any())
            {
                Assert.NotEmpty(actualSlicesAsArray);
            }

            AssertSequenceEqual(actualSlicesAsArray, expectedSlices);
        }

        private static void AssertSequenceEqual<T>(IEnumerable<IEnumerable<T>> actual, IEnumerable<IEnumerable<T>> expected)
        {
            var actualAsList = actual.Select(a => a.ToList()).ToList();
            var expectedAsList = expected.Select(e => e.ToList()).ToList();

            for (int i = 0; i < actualAsList.Count; i++)
            {
                Assert.True(expectedAsList[i].SequenceEqual(actualAsList[i]));
            }
        }

        private static void AssertSequenceEqual<T>(IEnumerable<(IEnumerable<T>, IEnumerable<T>)> actual, IEnumerable<(IEnumerable<T>, IEnumerable<T>)> expected)
        {
            var actualAsList = actual.Select(a => (a.Item1.ToList(), a.Item2.ToList())).ToList();
            var expectedAsList = expected.Select(e => (e.Item1.ToList(), e.Item2.ToList())).ToList();

            for (int i = 0; i < actualAsList.Count; i++)
            {
                Assert.True(expectedAsList[i].Item1.SequenceEqual(actualAsList[i].Item1));
                Assert.True(expectedAsList[i].Item2.SequenceEqual(actualAsList[i].Item2));
            }
        }
    }
}
