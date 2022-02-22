namespace MACsharp.Enums
{
    using System;
    using System.Text;

    public enum CellKind
    {
        Empty,
        Counter,
        CuttingStation,
        DeliveryStation
    }

    public static class CellKindHelpers
    {
        public static char ToRepresentation(this CellKind kind)
        {
            switch (kind)
            {
                case CellKind.Empty:
                    return ' ';
                case CellKind.Counter:
                    return '-';
                case CellKind.CuttingStation:
                    return '/';
                case CellKind.DeliveryStation:
                    return '*';
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static string ToRepresentation(this CellKind[,] cells)
        {
            var sb = new StringBuilder();

            for (int y = 0; y < cells.GetLength(0); y++)
            {
                for (int x = 0; x < cells.GetLength(1); x++)
                {
                    sb.Append(cells[x, y].ToRepresentation());
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}