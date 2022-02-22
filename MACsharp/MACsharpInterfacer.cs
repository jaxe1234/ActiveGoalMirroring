namespace MACsharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Enums;
    using Helpers;
    using Search;
    using Structs;
    using Action = Structs.Action;

    public class MACsharpInterfacer
    {
        private State InitialState { get; set; }
        public  State CurrentState { get; private set; }

        private Dictionary<AgentId, AgentSolver> AgentSolvers { get; } = new();

        private Dictionary<AgentId, Action> _actionCache;

        private int _numberOfAgents;

        public void LoadLevel(string levelName, int numberOfAgents)
        {
            if (GlobalConfig.Print)
            {
                Console.WriteLine("Seed: " + GlobalConfig.Seed);
            }

            InitialState = LevelReader.Read(Path.Combine(Directory.GetCurrentDirectory(), "utils", "levels", levelName + ".txt"), numberOfAgents);
            CurrentState = InitialState.DeepCopy();
            _numberOfAgents = numberOfAgents;
            _actionCache = new(_numberOfAgents);
            GlobalConfig.NumberOfAgents = _numberOfAgents;
        }

        public bool GoalState() => CurrentState.GoalState();

        public void SetSeed(int seed)
        {
            GlobalConfig.Seed = seed;
        }

        public void SetUsePropertyCountHeuristic(bool use)
        {
            GlobalConfig.UsePropertyCountHeuristic = use;
        }

        public void SetPrint(bool print)
        {
            GlobalConfig.Print = print;
        }

        public void SetPrintError(bool printError)
        {
            GlobalConfig.PrintError = printError;
        }

        public void SetPrapHistorySize(int size)
        {
            GlobalConfig.PrapHistorySize = size;
        }

        public void InitializeAgent(int id, string type)
        {
            var agentId = new AgentId(id);
            AgentSolver agentSolver = type switch
            {
                "prap" => new Prap(agentId, InitialState.DeepCopy()),
                _ => throw new ArgumentException(nameof(type))
            };

            if (GlobalConfig.Print)
            {
                Console.WriteLine($"{type} playing as agent {id + 1}");
            }

            AgentSolvers.Add(agentId, agentSolver);
        }

        public (int x, int y) GetNextAction(int id) => AgentSolvers[new AgentId(id)].GetNextAction().Kind.GetDirection().GetTuple();

        public void Update(int id, int x, int y)
        {
            if (!AgentSolvers.Any())
            {
                return;
            }

            var action = new Action((x, y));
            
            _actionCache[new AgentId(id)] = action;

            if (_actionCache.Count < _numberOfAgents)
            {
                return;
            }

            foreach (var agentSolver in AgentSolvers.Values)
            {
                agentSolver.UpdateState(_actionCache);
            }

            CurrentState.TryPerform(_actionCache, out State newState, GlobalConfig.Print, true);
            CurrentState = newState;

            if (!CurrentState.GoalRecipes.All(recipe => CurrentState.RecipeIsPossible(recipe)))
            {
                throw new Exception("The level is not solvable from the current state. Aborting...");
            }

            _actionCache.Clear();
        }

        public override string ToString() => CurrentState.ToString();
    }
}
