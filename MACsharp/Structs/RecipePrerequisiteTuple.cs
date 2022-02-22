namespace MACsharp.Structs
{
    using Interfaces;
    using System;
    using System.Diagnostics;


    [DebuggerDisplay("({Part1.Name}, {Part2.Name})")]
    public readonly struct RecipePrerequisiteTuple : IEquatable<RecipePrerequisiteTuple>
    {
        public IMergeable Part1 { get; }
        public IMergeable Part2 { get; }

        public RecipePrerequisiteTuple(IMergeable part1, IMergeable part2)
        {
            Part1 = part1;
            Part2 = part2;
        }


        public bool Equals(RecipePrerequisiteTuple other)
        {
            return Equals(Part1, other.Part1) && Equals(Part2, other.Part2);
        }

        public override bool Equals(object obj)
        {
            return obj is RecipePrerequisiteTuple other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Part1.GetHashCode(), Part2.GetHashCode());
        }
    }
}