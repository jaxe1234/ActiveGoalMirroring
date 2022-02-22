namespace MACsharp.Interfaces
{
    using System;
    using Enums;

    public interface IMergeable : IEquatable<IMergeable>
    {
        public string Name { get; }
        public PropertyKind Properties { get; }
        public bool HasProperty(PropertyKind kind) => Properties.HasFlag(kind);
    }
}