namespace MACsharp.Structs
{
    using System;

    public readonly struct AgentId : IEquatable<AgentId>
    {
        public readonly int Id;

        public AgentId(int id)
        {
            Id = id;
        }
        
        public bool Equals(AgentId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is AgentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}