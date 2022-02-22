namespace MACsharp.Enums
{
    using System;

    [Flags]
    public enum PropertyKind
    {
        None = 0,
        Movable = 1,
        Cuttable = 2,
        IsCut = 4,
        Deliverable = 8,
        IsDelivered = 16
    }
}