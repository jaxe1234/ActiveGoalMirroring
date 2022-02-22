namespace MACsharp.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Interfaces;
    using Search;
    using Structs;
    using System.Diagnostics;
    using Action = Structs.Action;

    public static class StateHelpers
    {
        public static IEnumerable<SearchNode> ExpandWithJointActions(this SearchNode node)
        {
            var legalActions = node.ResultState.GetAllActions();
            var actionPermutations = GetLegalActionPermutations(legalActions);

            foreach (var actions in actionPermutations)
            {
                var actionList = actions.ToList();
                var actionAssignment = new Dictionary<AgentId, Action>();

                for (int i = 0; i < node.ResultState.AgentPositions.Count; i++)
                {
                    var agent = node.ResultState.AgentPositions.Keys.ElementAt(i);
                    actionAssignment[agent] = actionList.ElementAt(i);
                }

                if (node.ResultState.TryPerform(actionAssignment, out var newState, false, false))
                {
                    yield return new SearchNode(node, newState, actionAssignment);
                }
            }
        }

        private static IEnumerable<IEnumerable<Action>> GetLegalActionPermutations(List<AgentPossibleActions> legalActions)
        {
            return legalActions.CartesianProduct();
        }

        public static List<SearchNode> ExtractPlan(this SearchNode node)
        {
            var current = node;
            var plan = new List<SearchNode> { current };
            while (current.Parent != null)
            {
                plan.Add(current.Parent);
                current = current.Parent;
            }

            plan.Reverse();
            return plan;
        }

        private static List<AgentPossibleActions> GetAllActions(this State state)
        {
            var legalAgentActions = new List<AgentPossibleActions>(state.AgentPositions.Keys.Count);

            foreach (var agent in state.AgentPositions.Keys)
            {
                legalAgentActions.Add(new AgentPossibleActions(state.GetActions(agent), agent));
            }

            return legalAgentActions;
        }

        private static AgentPossibleActions GetActions(this State state, AgentId agentId) =>
            new(AgentActionKindHelper.GetActionKinds().Shuffle().Select(k => new Action(k)), agentId);

        public static bool TryPerform(this State state, Dictionary<AgentId, Action> actionAssignment, out State newState, bool print = false, bool humpingLegal = false)
        {
            if (state.HasCollisions(actionAssignment, print))
            {
                newState = null;
                return false;
            }

            return TryPerformInternal(state, actionAssignment, out newState, print, humpingLegal);
        }

        private static bool HasCollisions(this State state, Dictionary<AgentId, Action> actionAssignment, bool print = false)
        {
            var targets = new HashSet<Position>();

            foreach (var (agent, action) in actionAssignment)
            {
                var actionDirection = action.Kind.GetDirection();
                var oldAgentPosition = state.AgentPositions[agent];
                var newPosition = oldAgentPosition + actionDirection;

                // Vertex conflict
                if (!targets.Add(newPosition))
                {
                    if (print)
                    {
                        Console.WriteLine($"Vertex conflict at {newPosition}");
                    }
                    return true;
                }

                // Vertex conflict
                foreach (var agentPosition in state.AgentPositions)
                {
                    if (!agentPosition.Key.Equals(agent) && agentPosition.Value == newPosition)
                    {
                        if (print)
                        {
                            Console.WriteLine($"Vertex conflict at {newPosition}");
                        }
                        return true;
                    }
                }
            }

            // Check edge conflicts

            foreach (var (agent1, action1) in actionAssignment)
            {
                foreach (var (agent2, action2) in actionAssignment)
                {
                    if (agent1.Equals(agent2)) continue;

                    var actionDirection1 = action1.Kind.GetDirection();
                    var oldAgentPosition1 = state.AgentPositions[agent1];
                    var newPosition1 = oldAgentPosition1 + actionDirection1;

                    var actionDirection2 = action2.Kind.GetDirection();
                    var oldAgentPosition2 = state.AgentPositions[agent2];
                    var newPosition2 = oldAgentPosition2 + actionDirection2;

                    // Edge conflict
                    if (newPosition1 == oldAgentPosition2 && newPosition2 == oldAgentPosition1)
                    {
                        if (print)
                        {
                            Console.WriteLine($"Edge conflict between {oldAgentPosition1} -> {newPosition1} and {oldAgentPosition2} -> {newPosition2}");
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        public static HashSet<AgentId> GetAllCollisions(this State state, Dictionary<AgentId, Action> actionAssignment)
        {
            var agentCollidors = new HashSet<AgentId>();

            foreach (var (agent1, action1) in actionAssignment)
            {
                foreach (var (agent2, action2) in actionAssignment)
                {
                    if (agent1.Equals(agent2))
                    {
                        continue;
                    }

                    var actionDirection = action1.Kind.GetDirection();
                    var oldAgent1Position = state.AgentPositions[agent1];
                    var newPosition1 = oldAgent1Position + actionDirection;

                    actionDirection = action2.Kind.GetDirection();
                    var oldAgent2Position = state.AgentPositions[agent2];
                    var newPosition2 = oldAgent2Position + actionDirection;

                    if (newPosition1.Equals(newPosition2))
                    {
                        agentCollidors.Add(agent1);
                        agentCollidors.Add(agent2);
                    }
                    else if (newPosition1 == oldAgent2Position && newPosition2 == oldAgent1Position)
                    {
                        agentCollidors.Add(agent1);
                        agentCollidors.Add(agent2);
                    }
                }
            }

            return agentCollidors;
        }

        public static HashSet<AgentIdPair> GetAllAgentCollisions(this State state, Dictionary<AgentId, Action> actionAssignment)
        {
            var agentCollidors = new HashSet<AgentIdPair>();

            foreach (var (agent1, action1) in actionAssignment)
            {
                foreach (var (agent2, action2) in actionAssignment)
                {
                    if (agent1.Equals(agent2))
                    {
                        continue;
                    }

                    var actionDirection = action1.Kind.GetDirection();
                    var oldAgent1Position = state.AgentPositions[agent1];
                    var newPosition1 = oldAgent1Position + actionDirection;

                    actionDirection = action2.Kind.GetDirection();
                    var oldAgent2Position = state.AgentPositions[agent2];
                    var newPosition2 = oldAgent2Position + actionDirection;

                    if (newPosition1.Equals(newPosition2))
                    {
                        agentCollidors.Add(new AgentIdPair(agent1, agent2));
                    }
                    else if (newPosition1 == oldAgent2Position && newPosition2 == oldAgent1Position)
                    {
                        agentCollidors.Add(new AgentIdPair(agent1, agent2));
                    }
                }
            }

            return agentCollidors;
        }

        private static bool TryPerformInternal(this State state, Dictionary<AgentId, Action> actionAssignment, out State newState, bool print = false, bool humpingLegal = false)
        {
            newState = state.ChildState();

            foreach (var (agent, action) in actionAssignment)
            {
                var actionDirection = action.Kind.GetDirection();
                var oldAgentPosition = state.AgentPositions[agent];
                var newPosition = oldAgentPosition + actionDirection;

                if (actionDirection == (0, 0))
                {
                    continue;
                }

                if (newState.GetCellKind(newPosition) != CellKind.Empty)
                {
                    // Merging stuff
                    if (newState.InnerGameObjectPositions.TryGetValue(newPosition, out var counterItem) || TryGetActionObject(newState, newPosition, out counterItem))
                    {
                        if (newState.TryGetHeldItem(agent, out var heldItem))
                        {
                            if (!TryMerge(newState, counterItem, heldItem, oldAgentPosition, newPosition, print, humpingLegal)) return false;
                        }
                        else
                        {
                            // Agent pickup
                            if (!TryAgentPickup(newState, counterItem, newPosition, oldAgentPosition, print, humpingLegal)) return false;
                        }
                    }
                    else if (newState.TryGetHeldItem(agent, out var heldItem))
                    {
                        // Agent Drop-off
                        newState.InnerGameObjectPositions.Remove(oldAgentPosition);
                        newState.InnerGameObjectPositions.Add(newPosition, heldItem);
                    }
                    else
                    {
                        var targetCell = newState.Cells[newPosition.X, newPosition.Y];
                        if (targetCell == CellKind.Counter)
                        {
                            return humpingLegal;
                        }

                        return false;
                    }
                }
                else
                {
                    // Moving
                    if (!TryMove(newState, newPosition, agent, oldAgentPosition, print)) return false;
                }
            }

            newState.GameObjectPositions = newState.GetGameObjects();
            return true;
        }

        private static bool TryGetActionObject(State state, Position position, out IMergeable actionObject)
        {
            var kind = state.GetCellKind(position);
            switch (kind)
            {
                case CellKind.Empty:
                    actionObject = new ActionObject();
                    return false;
                case CellKind.Counter:
                    actionObject = new ActionObject();
                    return false;
                case CellKind.CuttingStation:
                    actionObject = ActionObject.GetCuttingStation();
                    return true;
                case CellKind.DeliveryStation:
                    actionObject = ActionObject.GetDeliveryStation();
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool TryAgentPickup(State newState, IMergeable counterItem, Position newPosition, Position oldAgentPosition, bool print = false, bool humpingLegal = false)
        {
            if (counterItem.HasProperty(PropertyKind.Movable))
            {
                newState.InnerGameObjectPositions.Remove(newPosition);
                newState.InnerGameObjectPositions.Add(oldAgentPosition, counterItem);

                return true;
            }

            return humpingLegal;
        }

        private static bool TryMove(State newState, Position newPosition, AgentId agentId, Position agentPosition, bool print = false)
        {
            //Also move held item
            if (newState.TryGetHeldItem(agentId, out var heldItem))
            {
                newState.InnerGameObjectPositions.Remove(agentPosition);
                newState.InnerGameObjectPositions.Add(newPosition, heldItem);
            }

            newState.AgentPositions[agentId] = newPosition;

            return true;
        }

        public static bool TryMerge(State newState, IMergeable counterItem, IMergeable heldItem, Position oldAgentPosition,
            Position targetPosition, bool print = false, bool humpingLegal = false)
        {
            // Merge items
            if (Recipe.TryGetRecipe(counterItem, heldItem, out var recipe))
            {
                var product = recipe!.Value.Product;
                if (counterItem.HasProperty(PropertyKind.Movable))
                {
                    newState.InnerGameObjectPositions.Remove(oldAgentPosition);
                    newState.InnerGameObjectPositions.Remove(targetPosition);
                    newState.InnerGameObjectPositions.Add(oldAgentPosition, product);

                }
                else if (counterItem is ActionObject actionObject1 && actionObject1.Kind == ActionObjectKind.DeliveryStation)
                {
                    newState.InnerGameObjectPositions.Remove(oldAgentPosition);
                    newState.SetFulfilledRecipe(recipe!.Value);

                }
                else if (counterItem is ActionObject actionObject2 && actionObject2.Kind == ActionObjectKind.CuttingStation)
                {
                    newState.InnerGameObjectPositions[oldAgentPosition] = product;

                }
                else
                {
                    throw new Exception();
                }

                newState.LockedUntilTimeStep = newState.TimeStep;
            }
            else if (humpingLegal && counterItem is ActionObject { Kind: ActionObjectKind.CuttingStation })
            {
                // Agent Drop-off
                newState.InnerGameObjectPositions.Remove(oldAgentPosition);
                newState.InnerGameObjectPositions.Add(targetPosition, heldItem);
            }
            else if (humpingLegal && counterItem is ActionObject { Kind: ActionObjectKind.DeliveryStation })
            {
                //Ignored
            }
            else
            {
                if (print)
                {
                    Console.WriteLine($"Failed to merge {counterItem.Name} and {heldItem.Name} because no recipe was found.");
                }
                return false;
            }

            return true;
        }

        public static bool CanAnyAgentReach(this State state, string part)
        {
            return state.GameObjectPositions.Where(x => x.Mergeable.Name == part)
                .Select(x => x.Position)
                .Any(position => state.AgentPositions.Keys
                    .Any(agent => state.Reachability[agent]
                        .Contains(position)));
        }

        public static bool CanAgentReach(this State state, string part, AgentId agent)
        {
            return state.GameObjectPositions.Where(x => x.Mergeable.Name == part)
                .Select(x => x.Position)
                .Any(p => state.Reachability[agent].Contains(p));
        }

        public static State StateCopyWithoutAgent(this State state, AgentId agent, bool removeHeldItem)
        {
            var stateCopy = state.DeepCopy();

            if (removeHeldItem)
            {
                stateCopy.RemoveItem(stateCopy.AgentPositions[agent]);
            }

            stateCopy.AgentPositions.Remove(agent);

            return stateCopy;
        }

        public static bool RecipeIsPossible(this State state, Recipe recipe, List<IMergeable> gameObjects = null)
        {
            if (state.IsRecipeFulfilled(recipe)) return true;

            gameObjects ??= state.GameObjectPositions.Select(x => x.Item2).ToList();

            var prereqCombinations = Recipe.MergeableAdjacencies[recipe.Name];

            if (gameObjects.Contains(recipe.Product))
                return true;

            foreach (var (p1, p2) in prereqCombinations)
            {
                var p1Exists = gameObjects.Any(x => x.Name == p1)
                               || (Recipe.TryGetRecipe(p1, out var recipe1) && state.RecipeIsPossible(recipe1));

                var p2Exists = new Lazy<bool>(() => gameObjects.Any(x => x.Name == p2)
                                                    || (Recipe.TryGetRecipe(p2, out var recipe2) && state.RecipeIsPossible(recipe2)));

                if (p1Exists && p2Exists.Value)
                    return true;
            }

            return false;
        }
    }
}
