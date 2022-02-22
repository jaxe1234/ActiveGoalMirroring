namespace MACsharp.Heuristics
{
    using Enums;
    using Search;
    using System.Collections.Generic;
    using System.Linq;
    using Structs;
    using System;

    public class SubGoalCountHeuristic : IHeuristic<SearchNode>
    {
        public long Estimate(SearchNode node, params Recipe[] goals)
        {
            long estimate = 0;

            if (GlobalConfig.UsePropertyCountHeuristic)
            {
                var goalIngredients = goals.Select(r => r.Product).OfType<Ingredient>().ToList();
                var goalFoodItems = goals.Select(r => r.Product).OfType<FoodItem>();

                goalIngredients = goalIngredients.Concat(goalFoodItems.SelectMany(f => f.Ingredients)).ToList();

                long countTotalIngredientProperties = goalIngredients.Sum(i =>
                    Property.GetSetBitCount(i.Properties));

                estimate += countTotalIngredientProperties;

                var cost = CountNumberOfIngredientCorrectProperties(goalIngredients, node.ResultState.GetIngredients().ToList());
                
                estimate -= cost;
            }

            foreach (Recipe recipe in goals)
            {
                if (node.ContainsRecipe(recipe))
                {
                    continue;
                }

                estimate += DiscourageHeldItems(recipe, node.ResultState);
                estimate += GetIngredientDistanceFromTargetProperties(recipe, node.ResultState);
            }
            
            estimate += node.ResultState.TimeStep;

            return estimate;
        }

        private static long DiscourageHeldItems(Recipe recipe, State state)
        {
            var estimate = 0L;

            if (state.IsRecipeFulfilled(recipe))
            {
                return estimate;
            }

            foreach (var agent in state.AgentPositions)
            {
                if (state.TryGetHeldItem(agent.Key, out var heldItem) && !Recipe.CanBeCombinedTo(heldItem, recipe))
                {
                    FindClosestEmptyCounter(state, agent.Value, out long cost);
                    estimate += cost * GlobalConfig.NumberOfAgents;
                }
            }

            return estimate;
        }
        
        private long CountNumberOfIngredientCorrectProperties(IEnumerable<Ingredient> goalIngredients, List<Ingredient> stateIngredients)
        {
            var count = 0;
            foreach (var prereq in goalIngredients)
            {
                foreach (var ingredient in stateIngredients.Where(i => i.IsKind(prereq.Kind)))
                {
                    count += Property.GetSetBitCount(prereq.Properties & ingredient.Properties);
                }
            }

            return count;
        }
        
        private long GetIngredientDistanceFromTargetProperties(Recipe recipe, State state)
        {
            if (state.IsRecipeFulfilled(recipe))
            {
                return 0;
            }

            var cost = 0L;
            
            foreach (var gameObjectPosition1 in state.GameObjectPositions)
            {
                foreach (var gameObjectPosition2 in state.GameObjectPositions)
                {
                    if (!Recipe.CanBeCombinedTo(gameObjectPosition1.Mergeable, gameObjectPosition2.Mergeable, recipe))
                    {
                        continue;
                    }

                    var position = FindClosestEmptyCounter(state, gameObjectPosition1.Position, gameObjectPosition2.Position, out var counterCost);
                    cost += counterCost;

                    foreach (var (agent, agentPosition) in state.AgentPositions)
                    {
                        if (!state.TryGetHeldItem(agent, out _))
                        {
                            cost += GetManhattanDistance(position, agentPosition) / GlobalConfig.NumberOfAgents;
                        }
                    }

                    return cost;
                }
            }

            return cost;
        }

        private static Position FindClosestEmptyCounter(State state, Position position, out long bestCost)
        {
            var counterPositions = state.GetPositionsOfType(CellKind.Counter);

            Position closestCounter = new(0, 0);
            bestCost = long.MaxValue;

            foreach (Position counterPosition in counterPositions)
            {
                if (state.InnerGameObjectPositions.TryGetValue(counterPosition, out _))
                {
                    continue;
                }

                var cost = 0L;
                cost += GetManhattanDistance(position, counterPosition);

                if (cost <= bestCost)
                {
                    closestCounter = counterPosition;
                    bestCost = cost;
                }
            }

            return closestCounter;
        }
        
        private static Position FindClosestEmptyCounter(State state, Position p1, Position p2, out long bestCost)
        {
            var counterPositions = state.GetPositionsOfType(CellKind.Counter);

            Position closestCounter = new (0, 0);
            bestCost = long.MaxValue;

            foreach (Position counterPosition in counterPositions)
            {
                if (!counterPosition.Equals(p1) && !counterPosition.Equals(p2) &&
                    state.InnerGameObjectPositions.TryGetValue(counterPosition, out _))
                {
                    continue;
                }

                var cost = 0L;
                cost += GetManhattanDistance(p1, counterPosition);
                cost += GetManhattanDistance(p2, counterPosition);

                if (cost <= bestCost)
                {
                    closestCounter = counterPosition; 
                    bestCost = cost;
                }
            }

            return closestCounter;
        }

        private static long GetManhattanDistance(Position position1, Position position2)
        {
            return Math.Abs(position1.X - position2.X) + Math.Abs(position1.Y - position2.Y);
        }
    }
}
