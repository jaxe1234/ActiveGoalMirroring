namespace MACsharp
{
    using Search;
    using Heuristics;
    using System;

    public static class GlobalConfig
    {
        public static readonly IHeuristic<SearchNode> Heuristic = new SubGoalCountHeuristic();
        private static int _seed;
        public static int Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                Rng = new Random(value);
            }
        }

        public static Random Rng { get; private set; }
        public static int PrapHistorySize { get; set; } = 6;
        public static bool Print { get; set; } = false;
        public static bool PrintError { get; set; } = false;
        public static long NumberOfAgents { get; set; }
        public static bool UsePropertyCountHeuristic { get; set; } = false;
        public static bool UseInitialQueue { get; set; } = true;

        static GlobalConfig()
        {
            Seed = 0;
        }
    }
}
