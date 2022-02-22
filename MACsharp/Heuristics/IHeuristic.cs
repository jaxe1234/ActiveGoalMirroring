namespace MACsharp.Heuristics
{
    using Structs;

    public interface IHeuristic<TState>
    {
        long Estimate(TState node, params Recipe[] goals);
    }
}
