namespace MACsharpRunner
{
    using MACsharp;
    using MACsharp.Enums;
    using MACsharp.Helpers;
    using MACsharp.Structs;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Action = MACsharp.Structs.Action;

    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && bool.TryParse(args[0], out var print))
            {
                GlobalConfig.Print = print;
            }

            GlobalConfig.Seed = 1;

            //MainLoop("full-divider_onionsalad", 3);

            //PerformSimpleBenchmark(10, 2, "open-divider_tomato", "open-divider_tl", "open-divider_salad", "open-divider_onionsalad");
            //PerformSimpleBenchmark(10, 2, "full-divider_tomato", "full-divider_tl", "full-divider_salad", "full-divider_onionsalad");
            PerformSimpleBenchmark(5, 2,
                "full-divider_tomato", "full-divider_tl", "full-divider_salad", "full-divider_onionsalad",
                "open-divider_tomato", "open-divider_tl", "open-divider_salad", "open-divider_onionsalad",
                "partial-divider_tomato", "partial-divider_tl", "partial-divider_salad", "partial-divider_onionsalad");
        }

        public static int MainLoop(string level, int noOfAgents)
        {
            var agents = new string[noOfAgents];
            for (int i = 0; i < noOfAgents; i++)
            {
                agents[i] = "prap";
            }

            return MainLoop(level, agents);
        }

        public static int MainLoop(string level, params string[] agents)
        {
            var interfacer = new MACsharpInterfacer();
            interfacer.LoadLevel(level, agents.Length);

            for (var i = 0; i < agents.Length; i++)
            {
                interfacer.InitializeAgent(i, agents[i]);
            }

            var actions = new Dictionary<int, Dictionary<AgentId, Action>>();
            var timestep = 0;
            while (timestep < 100)
            {
                if (GlobalConfig.Print)
                {
                    Console.WriteLine(interfacer.ToString());
                }

                if (interfacer.GoalState())
                {
                    if (GlobalConfig.Print)
                    {
                        Console.WriteLine("Goal state reached.");
                    }

                    break;
                }

                // get next actions for agents
                var actionAssignment = new Dictionary<AgentId, Action>();
                for (var i = 0; i < agents.Length; i++)
                {
                    var action = new Action(AgentActionKindHelper.GetAgentActionKind(interfacer.GetNextAction(i)));
                    actionAssignment.Add(new AgentId(i), action);
                }

                // resolve potential conflicts
                var collisions = interfacer.CurrentState.GetAllCollisions(actionAssignment);
                if (collisions.Any())
                {
                    foreach (var agent in collisions)
                    {
                        if (GlobalConfig.Print)
                        {
                            Console.WriteLine($"Agent{agent.Id} was causing a collision and has been adjusted to NoOp.");
                        }
                        actionAssignment[agent] = new Action(AgentActionKind.NoOp);
                    }
                }

                // update state
                for (var i = 0; i < agents.Length; i++)
                {
                    var actionDirection = actionAssignment[new AgentId(i)].Kind.GetDirection();
                    interfacer.Update(i, actionDirection.X, actionDirection.Y);
                }
                actions.Add(timestep, actionAssignment);


                timestep++;
            }

            return timestep;
        }

        private static void PerformSimpleBenchmark(int iterations, int noOfAgents, params string[] levels)
        {
            GlobalConfig.Print = false;
            GlobalConfig.PrintError = true;
            var rng = new Random(0);

            Console.WriteLine($"Running benchmarks. Config: {iterations} iterations, {noOfAgents} agents, {levels.Length} levels.");
            Console.WriteLine("Level; Completion Rate; Average Timesteps; Min Timesteps; Max Timesteps; Avg. ComputationTime [ms]");

            foreach (var level in levels)
            {
                var timestepHistory = new List<int>();
                decimal sum = 0;
                decimal successfulRuns = 0;
                var stopwatch = new Stopwatch();
                int timesteps;
                stopwatch.Start();
                for (var i = 0; i < iterations; i++)
                {
                    try
                    {
                        Console.Write(i + " ");
                        timesteps = MainLoop(level, noOfAgents);
                        sum += timesteps;
                        timestepHistory.Add(timesteps);
                        GlobalConfig.Seed = rng.Next();
                        successfulRuns++;
                    }
                    catch (Exception e)
                    {
                        if (GlobalConfig.PrintError)
                        {
                            Console.WriteLine(e);
                        }
                        // Ignored
                    }
                }
                Console.WriteLine();
                stopwatch.Stop();

                var completionRate = successfulRuns / iterations;
                var averageTimesteps = sum / successfulRuns;
                var averageComputationTime = (stopwatch.ElapsedMilliseconds / successfulRuns);

                Console.WriteLine($"{level}; {completionRate:P1}; {averageTimesteps:F3}; {timestepHistory.Min():F3}; {timestepHistory.Max():F3}; {averageComputationTime:F3}");
            }
        }

        private static void StupidCodeGenerator()
        {
            var o = new[] { "Tomato", "Lettuce", "Onion" };
            var cut = @$"var cut{{0}}{{1}}{{2}} = new Recipe(new FoodItem(""Cut{{0}}{{1}}{{2}}"", Ingredient.GetCut{{0}}(), Ingredient.GetCut{{1}}(), Ingredient.GetCut{{2}}()));";
            var plated = @$"var platedCut{{0}}{{1}}{{2}} = new Recipe(new FoodItem(""PlatedCut{{0}}{{1}}{{2}}"", Ingredient.GetCut{{0}}(), Ingredient.GetCut{{1}}(), Ingredient.GetCut{{2}}(), Ingredient.GetPlate()));";
            var mappingCut = @$"Mapping.Add(cut{{0}}{{1}}{{2}}.Name, cut{{0}}{{1}}{{2}});";
            var mappingPlated = @$"Mapping.Add(platedCut{{0}}{{1}}{{2}}.Name, platedCut{{0}}{{1}}{{2}});";
            var p = o.Combinations(4);

            foreach (var s in p)
            {
                Console.WriteLine(cut, s.ToArray());
            }
            Console.WriteLine();
            foreach (var s in p)
            {
                Console.WriteLine(mappingCut, s.ToArray());
            }
            Console.WriteLine();
            foreach (var s in p)
            {
                Console.WriteLine(plated, s.ToArray());
            }
            Console.WriteLine();
            foreach (var s in p)
            {
                Console.WriteLine(mappingPlated, s.ToArray());
            }
        }
    }
}
