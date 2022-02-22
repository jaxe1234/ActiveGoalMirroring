namespace MACsharp.Structs
{
    using Helpers;
    using System;
    using System.Collections.Generic;

    public readonly struct PossibleSubgoal : IEquatable<PossibleSubgoal>
    {
        public readonly string RecipeName;
        public readonly ISet<string> Parts;

        private static readonly EnumerableComparer<string> Comparer =
            new EnumerableComparer<string>();

        public PossibleSubgoal(string name, (string part1, string part2) parts)
        {
            RecipeName = name;
            Parts = new HashSet<string>{ parts.part1, parts.part2 };
        }

        public bool Equals(PossibleSubgoal other)
        {
            return RecipeName == other.RecipeName && Parts.SetEquals(other.Parts);
        }

        public override bool Equals(object obj)
        {
            return obj is PossibleSubgoal other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RecipeName, Comparer.GetHashCode(Parts));
        }

        public override string ToString()
        {
            return RecipeName;
        }
    }
}
