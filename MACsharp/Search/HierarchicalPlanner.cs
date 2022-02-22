namespace MACsharp.Search
{
    using DataStructures;
    using Helpers;
    using Structs;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Action = Structs.Action;

    public static class HierarchicalPlanner
    {
        public static List<SearchNode> HierarchicalSearch(this State initialState, List<AgentId> observationBoundAgents,
            string goal, List<AgentObservations> observations = null)
        {
            return initialState.HierarchicalSearch(observationBoundAgents, Recipe.GetRecipe(goal), observations);
        }

        public static List<SearchNode> HierarchicalSearch(this State initialState,
            List<AgentId> observationBoundAgents,
            Recipe goal,
            List<AgentObservations> observations = null)
        {
            var frontier = new PriorityQueue<long, HierarchicalPlannerSearchNode>();
            var explored = new HashSet<HierarchicalPlannerSearchNode>();
            frontier.Enqueue(0, ApplyObservations(observationBoundAgents, new HierarchicalPlannerSearchNode(new List<SearchNode>(), initialState, new List<string>()), observations));

            while (!frontier.IsEmpty)
            {
                var node = frontier.DequeueValue();

                if (node.State.IsRecipeFulfilled(goal))
                {
                    return node.Plan;
                }

                explored.Add(node);

                var children = GetChildren(observationBoundAgents, node, goal, observations).ToList();

                foreach (var child in children)
                {
                    if (explored.Contains(child) || frontier.Contains(child))
                    {
                        continue;
                    }

                    var priority = child.Plan.Count;
                    Debug.Assert(priority >= 0);
                    frontier.Enqueue(priority, child);
                }
            }
            
            return null;
        }

        private static IEnumerable<HierarchicalPlannerSearchNode> GetChildren(List<AgentId> observationBoundAgents,
            HierarchicalPlannerSearchNode node,
            Recipe recipe,
            List<AgentObservations> observations = null)
        {
            var workNode = node;
            var subgoals = GetDirectlyAchievableSubgoals(workNode.State, recipe);
            foreach (var subgoal in subgoals)
            {
                var currentState = workNode.State.DeepCopy();
                var plan = new List<SearchNode>(workNode.Plan ?? new List<SearchNode>());
                var subGoalOrder = new List<string>(workNode.SubgoalOrder ?? new List<string>());

                if (!currentState.RecipeIsPossible(recipe))
                {
                    continue;
                }

                if (!currentState.GetDirectlyAchievableSubgoals(recipe).Select(x => x.RecipeName)
                    .Contains(subgoal.RecipeName))
                {
                    continue;
                }

                var result = currentState.AStarSearch(observationBoundAgents, Recipe.GetRecipe(subgoal.RecipeName), observations);
                if (result.resultingState == null)
                {
                    continue;
                }

                currentState = result.resultingState;
                result.plan.RemoveAt(0);
                plan.AddRange(result.plan);
                subGoalOrder.Add(subgoal.RecipeName);

                if (!currentState.RecipeIsPossible(recipe))
                {
                    continue;
                }

                yield return new HierarchicalPlannerSearchNode(plan, currentState, subGoalOrder);
            }
        }

        private static HierarchicalPlannerSearchNode ApplyObservations(List<AgentId> observationBoundAgents, HierarchicalPlannerSearchNode node, List<AgentObservations> observations = null)
        {
            if (observations == null)
            {
                return node;
            }

            if (observationBoundAgents != null && node.State.LockedUntilTimeStep == null)
            {
                return node;
            }

            var observationLength = node.State.LockedUntilTimeStep ?? observations.First().Count;

            var currentState = node.State;
            var plan = new List<SearchNode>(node.Plan);
            var previousSearchNode = plan.LastOrDefault();
            for (int i = node.State.TimeStep; i < observationLength; i++)
            {
                var actions = new Dictionary<AgentId, Action>();
                foreach(var agentObservation in observations)
                {
                    actions.Add(agentObservation.ObservedAgentId, agentObservation.ElementAt(i));
                }

                currentState.TryPerform(actions, out currentState);
                plan.Add(new SearchNode(previousSearchNode, currentState, actions));
                previousSearchNode = plan.Last();
            }

            return new HierarchicalPlannerSearchNode(plan, currentState, node.SubgoalOrder);
        }

        public static IEnumerable<PossibleSubgoal> GetDirectlyAchievableSubgoals(this State state, Recipe recipe)
        {
            IEnumerable<PossibleSubgoal> PossibleSubgoals(State innerState)
            {
                // Gets which subgoals are directly achievable in the current innerState
                // this can be used to infer which limited list of possible final goals are likely
                var gameObjects = innerState.GameObjects;

                foreach (var gameObject in gameObjects)
                {
                    foreach (var otherGameObject in gameObjects)
                    {
                        if (Recipe.TryGetRecipe(gameObject, otherGameObject, out var foundRecipe) && foundRecipe!.Value.LeadsTo(recipe))
                        {
                            yield return new PossibleSubgoal(foundRecipe!.Value.Name, (gameObject.Name, otherGameObject.Name));
                        }
                    }
                }
            }

            return PossibleSubgoals(state).Distinct();
        }
    }
}
