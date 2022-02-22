namespace DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    public class Collector
    {
        private static string CsvHeader = "Map; Agents; Solved; Average Actions; SD(Actions); Average Runtime; SD(Runtime); Average Efficiency; SD(Efficiency)";

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var path = Path.Combine(Environment.CurrentDirectory, args[0]);

            if (!File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");

                return;
            }
            
            var dataLines = ReadDataLinesFromFile(path);

            // there are no "none" models
            Debug.Assert(!dataLines.SelectMany(x => x.Models).Any(x => string.Equals(x, "none", StringComparison.InvariantCultureIgnoreCase)));

            Console.WriteLine("Cross play scenarios: " + dataLines.Count(d => FilterByScenarioType(d.Models, ScenarioType.CrossPlay)));
            Console.WriteLine("Self play scenarios: " + dataLines.Count(d => FilterByScenarioType(d.Models, ScenarioType.SelfPlay)));

            Console.WriteLine("//-----------AGENT GROUPED SCENARIOS------------//");
            PrintAgentGroupedScenarios(dataLines);
            Console.WriteLine("//-------MAP GROUPED CROSS-PLAY SCENARIOS-------//");
            PrintMapGroupedCrossplayScenarios(dataLines);
            Console.WriteLine("//-------MAP GROUPED SELF-PLAY SCENARIOS--------//");
            PrintMapGroupedSelfplayScenarios(dataLines);
        }

        #region Print Scenario to Output

        private static void PrintAgentGroupedScenarios(List<DataLine> dataLines)
        {
            var agentGroupedScenarios = GetAgentGroupedScenarios(dataLines, ScenarioType.CrossPlay);

            Console.WriteLine("//----------CROSS PLAY---------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in agentGroupedScenarios)
            {
                Console.WriteLine(scenario.ToCsv());
            }

            agentGroupedScenarios = GetAgentGroupedScenarios(dataLines, ScenarioType.SelfPlay);

            Console.WriteLine("//-----------SELF PLAY---------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in agentGroupedScenarios)
            {
                Console.WriteLine(scenario.ToCsv());
            }
        }
        
        private static void PrintMapGroupedCrossplayScenarios(List<DataLine> dataLines)
        {
            var mapGroupedCrossplayScenarios = GetMapGroupedScenarios(dataLines, ScenarioType.CrossPlay)
                .OrderBy(x => x.MapName).ToList(); 


            Console.WriteLine("//----------FULL DIVIDER---------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in mapGroupedCrossplayScenarios.Where(s => s.MapName.Contains("full-divider")))
            {
                Console.WriteLine(scenario.ToCsv());
            }

            Console.WriteLine("//--------PARTIAL DIVIDER--------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in mapGroupedCrossplayScenarios.Where(s => s.MapName.Contains("partial-divider")))
            {
                Console.WriteLine(scenario.ToCsv());
            }

            Console.WriteLine("//----------OPEN DIVIDER---------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in mapGroupedCrossplayScenarios.Where(s => s.MapName.Contains("open-divider")))
            {
                Console.WriteLine(scenario.ToCsv());
            }
        }

        private static void PrintMapGroupedSelfplayScenarios(List<DataLine> dataLines)
        {
            var mapGroupedSelfplayScenarios = GetMapGroupedScenarios(dataLines, ScenarioType.SelfPlay)
                .OrderBy(x => x.MapName).ToList();

            Console.WriteLine("//----------FULL DIVIDER---------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in mapGroupedSelfplayScenarios.Where(s => s.MapName.Contains("full-divider")))
            {
                Console.WriteLine(scenario.ToCsv());
            }

            Console.WriteLine("//--------PARTIAL DIVIDER--------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in mapGroupedSelfplayScenarios.Where(s => s.MapName.Contains("partial-divider")))
            {
                Console.WriteLine(scenario.ToCsv());
            }

            Console.WriteLine("//----------OPEN DIVIDER---------//");
            Console.WriteLine(CsvHeader);
            foreach (var scenario in mapGroupedSelfplayScenarios.Where(s => s.MapName.Contains("open-divider")))
            {
                Console.WriteLine(scenario.ToCsv());
            }
        }

        private static void PrintUngroupedSelfplayAggregates(List<DataLine> dataLines)
        {
            var agm6Aggregate2Agents = GetUngroupedSelfplayAggregate(dataLines, 2, "agm-6");
            var macAggregate2Agents = GetUngroupedSelfplayAggregate(dataLines, 2, "mac");
            var bdAggregate2Agents = GetUngroupedSelfplayAggregate(dataLines, 2, "bd");
            Console.WriteLine(agm6Aggregate2Agents);
            Console.WriteLine(macAggregate2Agents);
            Console.WriteLine(bdAggregate2Agents);

            var agm1Aggregate3Agents = GetUngroupedSelfplayAggregate(dataLines, 3, "agm-1");
            var agm6Aggregate3Agents = GetUngroupedSelfplayAggregate(dataLines, 3, "agm-6");
            var macAggregate3Agents = GetUngroupedSelfplayAggregate(dataLines, 3, "mac");
            var bdAggregate3Agents = GetUngroupedSelfplayAggregate(dataLines, 3, "bd");
            Console.WriteLine(agm1Aggregate3Agents);
            Console.WriteLine(agm6Aggregate3Agents);
            Console.WriteLine(macAggregate3Agents);
            Console.WriteLine(bdAggregate3Agents);
        }

        #endregion
        
        private static ScenarioAggregate GetUngroupedSelfplayAggregate(List<DataLine> dataLines, int numberofAgents, string agentToFilter)
        {
            var filtered = dataLines.Where(x => x.NumberOfAgents == numberofAgents && FilterByAgent(x, agentToFilter, requireAll: true));

            var solved = new ReadOnlyCollection<DataLine>(filtered.Where(x => !x.HasError && x.Solved).ToList());

            return GetAggregates(solved, filtered.Count(), "", new [] { agentToFilter }, numberofAgents);
        }

        #region Grouped Scenario Aggregates

        private static IEnumerable<ScenarioAggregate> GetMapGroupedScenarios(List<DataLine> dataLines, ScenarioType scenarioType)
        {
            var groups = dataLines.GroupBy(
                d => new MapGroupingKey(d.MapName, d.Models, d.NumberOfAgents));

            var filteredGroups = groups.Where(g => FilterByScenarioType(g.Key.Agents, scenarioType));

            return filteredGroups
                .Select(g =>
                {
                    var solved = g.Where(d => d.Solved && !d.HasError).ToList();
                    return GetAggregates(solved, g.Count(), g.Key.MapName, g.Key.Agents, g.Key.NumberOfAgents);
                });
        }
        
        private static IEnumerable<ScenarioAggregate> GetAgentGroupedScenarios(List<DataLine> dataLines, ScenarioType scenarioType)
        {
            var groups = dataLines.GroupBy(d => new AgentGroupingKey(d.Models));

            var filteredGroups = groups.Where(g => FilterByScenarioType(g.Key.Agents, scenarioType));

            return filteredGroups.Select(g =>
            {
                var solved = g.Where(d => d.Solved && !d.HasError).ToList();
                return GetAggregates(solved, g.Count(), "", g.Key.Agents, g.Key.Agents.Length);
            });
        }

        private static ScenarioAggregate GetAggregates(IReadOnlyCollection<DataLine> solved, int scenarioCount, string mapName, string[] models, int agentCount)
        {
            var solveRate = (double)solved.Count / scenarioCount;
            
            var averageActions = solved.Count > 0 ? solved.Average(d => d.Actions) : 0;
            var standardDeviationActions = solved.Count > 0 ? solved.Select(d => d.Actions).StandardDeviation() : 0;

            var averageRuntime = solved.Count > 0 ? solved.Average(d => d.Runtime) : 0;
            var standardDeviationRuntime = solved.Count > 0 ? solved.Select(d => d.Runtime).StandardDeviation() : 0;

            var averageEfficiency = solved.Count > 0 ? solved.Average(d => d.TimeEfficiency) : 0;
            var standardDeviationEfficiency = solved.Count > 0 ? solved.Select(d => d.TimeEfficiency).StandardDeviation() : 0;

            return new ScenarioAggregate
            {
                MapName = mapName,
                Models = models,
                NumberOfAgents = agentCount,
                SolveRate = solveRate,
                AverageActions = averageActions,
                StandardDeviationActions = standardDeviationActions,
                AverageRuntime = averageRuntime,
                StandardDeviationRuntime = standardDeviationRuntime,
                AverageEfficiency = averageEfficiency,
                StandardDeviationEfficiency = standardDeviationEfficiency
            };
        }

        #endregion

        #region Filters
        
        private static bool FilterByAgent(DataLine dataLine, string agentToFilter, bool requireAll = false)
        {
            if (requireAll)
            {
                return dataLine.Models.All(m => string.Equals(m, agentToFilter, StringComparison.InvariantCultureIgnoreCase));
            }
            
            return dataLine.Models.Any(m => string.Equals(m, agentToFilter, StringComparison.InvariantCultureIgnoreCase));
        }
        private static bool FilterByScenarioType(string[] agents, ScenarioType scenarioType)
        {
            return agents.Distinct().Count() == (scenarioType == ScenarioType.SelfPlay ? 1 : agents.Length);
        }

        #endregion
        
        #region File/IO

        private static List<DataLine> ReadDataLinesFromFile(string path)
        {
            var sr = new StreamReader(path);
            var output = new List<DataLine>();

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                var items = line!.Split(';');

                var models = new[] { items[2], items[3], items[4], items[5] };
                models = models.Select(m => string.Equals(m, "agm", StringComparison.InvariantCultureIgnoreCase)
                    ? $"{m}-{items[10]}"
                    : m)
                    .Where(m => !string.Equals(m, "none", StringComparison.InvariantCultureIgnoreCase))
                    .ToArray();

                output.Add(new DataLine()
                {
                    MapName = items[0],
                    NumberOfAgents = int.Parse(items[1]),
                    Models = models,
                    Seed = int.Parse(items[6]),
                    Actions = int.Parse(items[7]),
                    Runtime = double.Parse(items[8]),
                    Solved = bool.Parse(items[9]),
                    AgmHistory = int.Parse(items[10]),
                    UsePropertHeuristic = false,
                    ErrorMessage = items[^2]
                });
            }

            return output;
        }

        #endregion

        #region Internal classes

        private enum ScenarioType
        {
            CrossPlay,
            SelfPlay
        }

        private class AgentGroupingKey : IEquatable<AgentGroupingKey>
        {
            public readonly string[] Agents;

            public AgentGroupingKey(string[] agents)
            {
                Agents = agents;
            }

            public bool Equals(AgentGroupingKey other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return Agents.SequenceEqual(other.Agents);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return Equals((AgentGroupingKey)obj);
            }

            public override int GetHashCode()
            {
                return Agents.Aggregate(Agents.Length, (current, val) => unchecked(current * 314159 + val.GetHashCode()));
            }
        }

        private class MapGroupingKey : IEquatable<MapGroupingKey>
        {
            public readonly string MapName;
            public readonly string[] Agents;
            public readonly int NumberOfAgents;

            public MapGroupingKey(string mapName, string[] agents, int numberOfAgents)
            {
                MapName = mapName;
                Agents = agents;
                NumberOfAgents = numberOfAgents;
            }

            public bool Equals(MapGroupingKey other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return MapName == other.MapName && Agents.SequenceEqual(other.Agents) && NumberOfAgents == other.NumberOfAgents;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return Equals((MapGroupingKey)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(MapName, 
                    NumberOfAgents,
                    Agents.Aggregate(Agents.Length, (current, val) => unchecked(current * 314159 + val.GetHashCode())));
            }
        }

        #endregion
    }
}
