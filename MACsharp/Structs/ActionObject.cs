namespace MACsharp.Structs
{
    using Enums;
    using Interfaces;
    using System.Diagnostics;

    [DebuggerDisplay("{Name}")]
    public readonly struct ActionObject : IMergeable
    {
        public readonly ActionObjectKind Kind;
        public PropertyKind Properties { get; }
        public string Name { get; }

        public ActionObject(ActionObjectKind kind, PropertyKind properties)
        {
            Kind = kind;
            Properties = properties;
            Name = Kind.ToString();
        }

        public static ActionObject GetCuttingStation() =>
            new(ActionObjectKind.CuttingStation, Property.None);
        public static ActionObject GetDeliveryStation() =>
            new(ActionObjectKind.DeliveryStation, Property.None);

        public bool Equals(ActionObject other) => Kind == other.Kind && Properties.Equals(other.Properties);
        public bool Equals(IMergeable obj) => obj is ActionObject other && Equals(other);
        public override bool Equals(object obj) => obj is ActionObject other && Equals(other);
        public override int GetHashCode() => (int) Kind + (int) Properties;
    }
}