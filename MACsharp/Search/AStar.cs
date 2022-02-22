namespace MACsharp.Search
{
    using Helpers;
    using Structs;
    using System.Collections.Generic;
    using DataStructures;
    using System.Diagnostics;
    using System.Linq;

    public static class AStar
    {
        public static (List<SearchNode> plan, State resultingState) AStarSearch(this State initialState,
            List<AgentId> observationBoundAgents,
            Recipe customGoal,
            List<AgentObservations> observations = null)
        {
            var frontier = new PriorityQueue<long, SearchNode>();
            var explored = new HashSet<SearchNode>();
            frontier.Enqueue(0, new SearchNode(null, initialState, null));

            while (!frontier.IsEmpty)
            {
                var node = frontier.DequeueValue();

                if (node.ContainsRecipe(customGoal))
                {
                    var path = node.ExtractPlan();
                    return (path, node.ResultState);
                }

                if (IsStuck(node, 100))
                {
                    return (null, null);
                }

                explored.Add(node);

                var children = node.ExpandWithJointActions();

                if (observations != null)
                {
                    children = children.Where(x =>
                        ConformsToObservations(observationBoundAgents, x, observations,
                            initialState.LockedUntilTimeStep));
                }

                var childList = children.ToList();

                foreach (var child in childList)
                {
                    if (explored.Contains(child) || frontier.Contains(child))
                    {
                        continue;
                    }

                    if (!child.ResultState.RecipeIsPossible(customGoal))
                    {
                        explored.Add(child);
                        continue;
                    }

                    var priority = GlobalConfig.Heuristic.Estimate(child, customGoal);
                    child.Cost = priority;
                    Debug.Assert(priority >= 0);
                    frontier.Enqueue(priority, child);
                }
            }

            return (null, null);
        }

        private static bool ConformsToObservations(List<AgentId> observationBoundAgents, SearchNode child,
            List<AgentObservations> observationList, int? initialStateLockedUntilTimeStep)
        {
            var conformsToObservations = true;

            bool allMustConform = initialStateLockedUntilTimeStep.HasValue &&
                                  initialStateLockedUntilTimeStep <= child.ResultState.TimeStep;

            foreach (var agentObservations in observationList)
            {
                if (observationBoundAgents != null && !allMustConform && !observationBoundAgents.Contains(agentObservations.ObservedAgentId))
                {
                    continue;
                }

                if (child.ResultState.TimeStep > agentObservations.Count)
                {
                    // No more observations
                    continue;
                }

                var observedAction = agentObservations[child.ResultState.TimeStep - 1];
                var actualAction = child.ActionAssignment[agentObservations.ObservedAgentId];

                if (!observedAction.Equals(actualAction))
                {
                    conformsToObservations = false;
                    break;
                }
            }

            return conformsToObservations;
        }

        private static bool IsStuck(SearchNode node, int depth)
        {
            return node.ResultState.TimeStep > depth;
        }
    }
}
