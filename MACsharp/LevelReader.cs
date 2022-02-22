namespace MACsharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Enums;
    using Interfaces;
    using Structs;
    using System.Linq;

    public static class LevelReader
    {
        private static Dictionary<AgentId, Position> AgentPositions { get; set; } = new();
        private static Dictionary<Position, IMergeable> ObjectPositions { get; set; } = new();
        private static ISet<Recipe> GoalRecipes { get; set; } = new HashSet<Recipe>();

        public static State Read(string path, int numberOfAgent)
        {
            Clear();

            var myFile = File.OpenRead(path);

            if (GlobalConfig.Print)
            {
                Console.WriteLine($"Reading level from file: {myFile.Name}");
            }

            CellKind[,] cells;

            using (var sr = new StreamReader(myFile))
            {
                cells = ReadCells(sr, out var dimensions);

                for (var line = sr.ReadLine(); !string.IsNullOrWhiteSpace(line); line = sr.ReadLine())
                {
                    ReadRecipeLine(line);
                }
                var agentCounter = 0;
                for (var line = sr.ReadLine(); !string.IsNullOrWhiteSpace(line); line = sr.ReadLine())
                {
                    if (agentCounter == numberOfAgent) break;
                    ReadAgentLine(line, agentCounter++);
                }
            }

            var state = new State(0,
                null,
                cells,
                ObjectPositions,
                AgentPositions,
                PrecomputeReachability(cells, AgentPositions),
                new HashSet<Recipe>(),
                GoalRecipes,
                PrecomputeCellKindPositions(cells),
                null);
            Recipe.PruneWithState(state);
            Recipe.CanBeCombinedToCache = new Dictionary<IMergeable, HashSet<Recipe>>();
            return state;
        }

        public static ILookup<CellKind, Position> PrecomputeCellKindPositions(CellKind[,] cells)
        {
            var result = new List<(CellKind, Position)>();
            for (int i = 0; i < cells.GetLength(0); i++)
            {
                for (int j = 0; j < cells.GetLength(1); j++)
                {
                    result.Add((cells[i,j], (i,j)));
                }
            }

            return result.ToLookup(x => x.Item1, x => x.Item2);
        }

        public static Dictionary<AgentId, HashSet<Position>> PrecomputeReachability(CellKind[,] cells, Dictionary<AgentId, Position> agentPositions)
        {
            var reachability = new Dictionary<AgentId, HashSet<Position>>();
            foreach (var (agent, position) in agentPositions)
            {
                reachability.Add(agent, new HashSet<Position>());

                var explored = new HashSet<Position>();
                var frontier = new Queue<Position>();

                frontier.Enqueue(position);

                while (frontier.Any())
                {
                    var p = frontier.Dequeue();
                    explored.Add(p);

                    reachability[agent].Add(p);

                    if (cells[p.X, p.Y] != CellKind.Empty) { continue; }

                    foreach (var direction in AgentActionKindHelper.GetActionKinds()
                        .Where(x => x is not AgentActionKind.NoOp).Select(AgentActionKindHelper.GetDirection))
                    {
                        var neighbour = p + direction;
                        if (!explored.Contains(neighbour) && !frontier.Contains(neighbour))
                        {
                            frontier.Enqueue(neighbour);
                        }
                    }
                }
            }

            return reachability;
        }

        public static CellKind[,] ReadCells(StreamReader sr, out (int x, int y) dimensions)
        {
            Clear();

            var tempCells = new List<(int x, int y, CellKind cell)>();

            dimensions = (0, 0);
            for (var line = sr.ReadLine(); !string.IsNullOrWhiteSpace(line); line = sr.ReadLine())
            {
                dimensions.x = ReadLevelLine(tempCells, line, dimensions.y++);
            }

            return ConvertCells(tempCells, dimensions.x, dimensions.y);
        }

        private static int ReadLevelLine(List<(int x, int y, CellKind cell)> tempCells, string line, int lineNumber)
        {
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                switch (c)
                {
                    case ' ':
                        tempCells.Add((i, lineNumber, CellKind.Empty));
                        break;
                    case '/':
                        tempCells.Add((i, lineNumber, CellKind.CuttingStation));
                        break;
                    case '*':
                        tempCells.Add((i, lineNumber, CellKind.DeliveryStation));
                        break;
                    default:
                        if (char.IsLetter(c))
                        {
                            ObjectPositions.Add((i, lineNumber), CreateObject(c));
                        }
                        tempCells.Add((i, lineNumber, CellKind.Counter));
                        break;
                }
            }

            return line.Length;
        }

        private static IMergeable CreateObject(char c)
        {
            return c switch
            {
                'p' => Ingredient.GetPlate(),
                'l' => Ingredient.GetLettuce(),
                'o' => Ingredient.GetOnion(),
                't' => Ingredient.GetTomato(),
                //'c' => Ingredient.GetCucumber(),
                _ => throw new ArgumentOutOfRangeException(nameof(c)),
            };
        }

        private static CellKind[,] ConvertCells(List<(int x, int y, CellKind cell)> tempCells, int x, int y)
        {
            var cells = new CellKind[x, y];
            foreach (var (x1, y1, cell) in tempCells)
            {
                cells[x1, y1] = cell;
            }

            return cells;
        }

        private static void ReadRecipeLine(string line) =>
            GoalRecipes.Add(Recipe.GetRecipe(line));

        private static void ReadAgentLine(string line, int agentCounter)
        {
            var positions = line.Split(" ");
            AgentPositions.Add(new AgentId(agentCounter), (int.Parse(positions[0]), int.Parse(positions[1])));
        }

        private static void Clear()
        {
            AgentPositions = new();
            ObjectPositions = new();
            GoalRecipes = new HashSet<Recipe>();
            Recipe.CompileRecipes();
            Recipe.CreateRecipeTree();
        }
    }
}
