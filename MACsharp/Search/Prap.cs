namespace MACsharp.Search
{
    using Enums;
    using Helpers;
    using System.Linq;
    using System.Collections.Generic;
    using Structs;
    using Action = Structs.Action;
    using System;
    using Interfaces;
    using System.Diagnostics;

    public class Prap : AgentSolver
    {
        private static readonly Dictionary<string, long> _optimalRecipePlanCosts = new();
        private readonly Dictionary<AgentId, Queue<PossibleSubgoal>> _subGoalAgentAllocation = new();
        private readonly Dictionary<AgentId, Action> _agentActions = new();
        private readonly Dictionary<AgentId, PossibleSubgoal> _agentSubgoals = new();
        private int _consecutiveNoop = 0;

        public Prap(AgentId id, State initialState) : base(id, initialState)
        {
            var initialQueue = new Queue<PossibleSubgoal>();

            if (GlobalConfig.UseInitialQueue)
            {
                var bestInitialSubgoal = BestInitialSubgoal();
                for (var i = 0; i < GlobalConfig.PrapHistorySize / GlobalConfig.NumberOfAgents; i++)
                {
                    initialQueue.Enqueue(bestInitialSubgoal);
                }
            }

            _agentSubgoals.Add(AgentId, new PossibleSubgoal());
            _agentActions.Add(AgentId, new Action(AgentActionKind.NoOp));
            _subGoalAgentAllocation.Add(AgentId, new Queue<PossibleSubgoal>(initialQueue));

            foreach (var agent in OtherAgents)
            {
                _agentSubgoals.Add(agent, new PossibleSubgoal());
                _agentActions.Add(agent, new Action(AgentActionKind.NoOp));
                _subGoalAgentAllocation.Add(agent, new Queue<PossibleSubgoal>());
            }
        }

        private PossibleSubgoal BestInitialSubgoal()
        {
            var possibleDirectSubGoals = GetDirectlyAchievableSubGoals(CurrentState).ToList();

            var bestSubgoal = possibleDirectSubGoals.First();
            var bestValue = long.MaxValue;

            foreach (var subgoal in possibleDirectSubGoals)
            {
                var planCost = GetOptimalCostForRecipe(Recipe.GetRecipe(subgoal.RecipeName));

                if (planCost <= bestValue)
                {
                    bestSubgoal = subgoal;
                    bestValue = planCost;
                }
            }

            return bestSubgoal;
        }

        private void UpdateSubgoalAgentAllocation(AgentId agent, PossibleSubgoal subgoal)
        {
            if (_subGoalAgentAllocation[agent].Count == GlobalConfig.PrapHistorySize)
            {
                _subGoalAgentAllocation[agent].Dequeue();
            }

            _subGoalAgentAllocation[agent].Enqueue(subgoal);
        }

        private void RemoveFromSubgoalInertiaQueue(AgentId agent, List<PossibleSubgoal> possibleSubgoals)
        {
            var old = new Queue<PossibleSubgoal>(_subGoalAgentAllocation[agent]);
            _subGoalAgentAllocation[agent] = new Queue<PossibleSubgoal>();

            while (old.TryDequeue(out var oldGoal))
            {
                if (possibleSubgoals.Contains(oldGoal))
                {
                    _subGoalAgentAllocation[agent].Enqueue(oldGoal);
                }
            }
        }

        private PossibleSubgoal? GetMostLikelySubgoalsForAgent(AgentId agent, List<PossibleSubgoal> possibleSubgoals)
        {
            if (!_subGoalAgentAllocation[agent].Any())
            {
                return null;
            }

            var possible = _subGoalAgentAllocation[agent].Mode();

            return possible[GlobalConfig.Rng.Next(possible.Count)];
        }

        public override Action GetNextAction()
        {
            if (GlobalConfig.Rng.Next(0, 100) < _consecutiveNoop - GlobalConfig.NumberOfAgents)
            {
                return GetRandomAction(AgentId);
            }

            var possibleDirectSubGoals = GetDirectlyAchievableSubGoals(CurrentState).ToList();

            Debug.Assert(possibleDirectSubGoals.Count > 0);

            RemoveFromSubgoalInertiaQueue(AgentId, possibleDirectSubGoals);

            foreach (var otherAgent in OtherAgents)
            {
                RemoveFromSubgoalInertiaQueue(otherAgent, possibleDirectSubGoals);
                var foundSubGoal = GetBestFittingSubGoal(
                    otherAgent,
                    possibleDirectSubGoals,
                    AgentObservationsList,
                    out var foundPlan,
                    out _);

                if (foundSubGoal.HasValue)
                {
                    _agentSubgoals[otherAgent] = foundSubGoal.Value;
                    _agentActions[otherAgent] = foundPlan!.FirstOrDefault(x => x.ResultState.TimeStep == CurrentState.TimeStep + 1)?.ActionAssignment[otherAgent] ?? new Action(AgentActionKind.NoOp);
                    UpdateSubgoalAgentAllocation(otherAgent, foundSubGoal.Value);
                }
            }

            if (GlobalConfig.Print)
            {
                Console.Write($"Agent {AgentId.Id} believes: ");
                Console.Write(string.Join(",",
                    _subGoalAgentAllocation.Where(x => !x.Key.Equals(AgentId)).Select(x => $"{x.Key.Id} follows {GetMostLikelySubgoalsForAgent(x.Key, possibleDirectSubGoals)?.ToString() ?? "None"}")));
                Console.WriteLine();
            }

            var agentRequiredPlans = new List<(PossibleSubgoal subgoal, List<SearchNode> plan)>();

            foreach (var subgoalAgentAllocation in _subGoalAgentAllocation)
            {
                var likelySubgoal = GetMostLikelySubgoalsForAgent(subgoalAgentAllocation.Key, possibleDirectSubGoals);
                if (likelySubgoal.HasValue &&
                    IsAgentNeeded(likelySubgoal.Value, AgentObservationsList, out var plan) &&
                    plan.Last().ResultState.GoalRecipes.All(x => plan.Last().ResultState.RecipeIsPossible(x)))
                {
                    agentRequiredPlans.Add((likelySubgoal.Value, plan));
                }
            }
            
            PossibleSubgoal? bestSubGoal = null;
            List<SearchNode> bestPlan = null;
            bool nothingToDo = false;

            if (agentRequiredPlans.Any())
            {
                (bestSubGoal, bestPlan) = agentRequiredPlans.OrderBy(x => x.plan.Count).First();
            }
            else
            {
                var subgoalsWithoutAgents = possibleDirectSubGoals
                    .Except(_subGoalAgentAllocation.Where(x => !x.Key.Equals(AgentId))
                        .Select(x => GetMostLikelySubgoalsForAgent(x.Key, possibleDirectSubGoals))
                        .Where(x => x.HasValue).Select(x => x.Value))
                    .ToList();

                if (subgoalsWithoutAgents.Any())
                {
                    var bestCost = long.MaxValue;
                    foreach (var subgoalWithoutAgents in subgoalsWithoutAgents)
                    {
                        var plan = CurrentState.HierarchicalSearch(null,
                            Recipe.GetRecipe(subgoalWithoutAgents.RecipeName), AgentObservationsList);

                        if (plan == null)
                        {
                            continue;
                        }

                        var planResultState = plan.Last().ResultState;

                        var planCost = plan.Count -
                                       _subGoalAgentAllocation[AgentId].Count(x => x.Equals(subgoalWithoutAgents));

                        if (planCost < bestCost && planResultState.GoalRecipes.All(x => planResultState.RecipeIsPossible(x)))
                        {
                            bestCost = planCost;
                            bestPlan = plan;
                            bestSubGoal = subgoalWithoutAgents;
                        }
                    }
                }
                else
                {
                    nothingToDo = true;
                }
            }

            if (!nothingToDo && bestPlan == null || (bestPlan == null && !_subGoalAgentAllocation[AgentId].Any()))
            {
                bestSubGoal = possibleDirectSubGoals[GlobalConfig.Rng.Next(possibleDirectSubGoals.Count)];
                bestPlan = CurrentState.HierarchicalSearch(null, bestSubGoal.Value.RecipeName, AgentObservationsList);
            }

            if (GlobalConfig.Print)
            {
                Console.WriteLine($"Prap agent {AgentId.Id} chose subgoal {bestSubGoal?.ToString() ?? "None"}");
            }

            if (bestSubGoal != null)
            {
                _agentSubgoals[AgentId] = bestSubGoal.Value;
                UpdateSubgoalAgentAllocation(AgentId, bestSubGoal!.Value);
            }

            var action = bestPlan == null ?
                GetRandomAction(AgentId) :
                bestPlan.FirstOrDefault(x => x.ResultState.TimeStep == CurrentState.TimeStep + 1)?.ActionAssignment[AgentId];

            _agentActions[AgentId] = action ?? GetRandomAction(AgentId);

            ResolveConflicts(possibleDirectSubGoals);

            if (!IsActionLegalInStateForAgent(_agentActions[AgentId], CurrentState, AgentId))
            {
                _agentActions[AgentId] = GetRandomAction(AgentId);
            }

            return _agentActions[AgentId];
        }

        private void ResolveConflicts(List<PossibleSubgoal> possibleDirectSubGoals)
        {
            var collisions = CurrentState.GetAllAgentCollisions(_agentActions);
            var collisionsWithAgent = collisions.Where(x => x.Agent1.Equals(AgentId) || x.Agent2.Equals(AgentId)).ToList();
            if (collisionsWithAgent.Any())
            {
                foreach (var conflict in collisions)
                {
                    var random = GlobalConfig.Rng.NextDouble();
                    var replanAgent = random >= 0.5 ? conflict.Agent1 : conflict.Agent2;
                    var otherAgent = random >= 0.5 ? conflict.Agent2 : conflict.Agent1;

                    var mockObservations = AgentObservationsList.Select(x => new AgentObservations(x.ObservedAgentId, x.ToList())).ToList();
                    mockObservations[otherAgent.Id].Add(_agentActions[otherAgent]);

                    if (!_agentSubgoals.TryGetValue(replanAgent, out var subgoal) || subgoal.RecipeName == null)
                    {
                        subgoal = possibleDirectSubGoals[GlobalConfig.Rng.Next(possibleDirectSubGoals.Count)];
                    }

                    var plan = CurrentState.HierarchicalSearch(AllAgents.Where(x => !x.Equals(replanAgent)).ToList(),
                        Recipe.GetRecipe(subgoal.RecipeName), mockObservations);

                    if (plan != null && plan.Count > 0)
                    {
                        _agentActions[replanAgent] = plan.First(x => x.ResultState.TimeStep == CurrentState.TimeStep + 1).ActionAssignment[replanAgent];
                    }
                    else
                    {
                        _agentActions[replanAgent] = GetRandomAction(replanAgent);
                    }
                }
            }
        }

        private bool IsActionLegalInStateForAgent(Action action, State state, AgentId agent)
        {
            var agentActions = new Dictionary<AgentId, Action>();
            agentActions[agent] = action;
            return state.TryPerform(agentActions, out _);
        }

        private Action GetRandomAction(AgentId agent)
        {
            var actions = AgentActionKindHelper.GetActionKinds();
            var chosenAction = new Action(actions[GlobalConfig.Rng.Next(actions.Length)]);

            if (IsActionLegalInStateForAgent(chosenAction, CurrentState, agent))
            {
                return chosenAction;
            }

            return new Action(AgentActionKind.NoOp);
        }

        private bool IsAgentNeeded(PossibleSubgoal subgoal, List<AgentObservations> agentObservations, out List<SearchNode> plan)
        {
            var planWithAgent = CurrentState.HierarchicalSearch(null, subgoal.RecipeName);

            if (planWithAgent == null)
            {
                plan = null;
                return false;
            }

            var removeHeldItem = false;

            if (CurrentState.IsAgentHoldingItem(this.AgentId, out var item))
            {
                if (subgoal.Parts.Contains(item.Name))
                {
                    plan = planWithAgent;
                    return true;
                }
                else
                {
                    removeHeldItem = true;
                }
            }

            var stateCopy = CurrentState.StateCopyWithoutAgent(AgentId, removeHeldItem);

            var goalIsReachable = true;

            foreach (var part in subgoal.Parts)
            {
                goalIsReachable = goalIsReachable && stateCopy.CanAnyAgentReach(part);
            }

            plan = planWithAgent;

            if (!goalIsReachable)
                return true;

            var planWithoutAgent = stateCopy.HierarchicalSearch(null, subgoal.RecipeName);

            plan = planWithAgent;

            return planWithoutAgent == null || planWithAgent.Last().ResultState.TimeStep < planWithoutAgent.Last().ResultState.TimeStep;
        }

        private PossibleSubgoal? GetBestFittingSubGoal(AgentId agent, List<PossibleSubgoal> possibleDirectSubGoals, List<AgentObservations> agentObservationsList, out List<SearchNode> bestPlan, out long bestCost, bool tryAgain = true)
        {
            bestPlan = null;
            bestCost = long.MaxValue;
            PossibleSubgoal? bestSubGoal = null;

            foreach (var possibleSubGoal in possibleDirectSubGoals)
            {
                var goalIsReachable = false;

                foreach (var part in possibleSubGoal.Parts)
                {
                    goalIsReachable = goalIsReachable || CurrentState.CanAgentReach(part, agent);
                }

                if (!goalIsReachable)
                {
                    break;
                }

                var recipe = Recipe.GetRecipe(possibleSubGoal.RecipeName);

                var optimalCost = GetOptimalCostForRecipe(recipe);

                var observationBoundAgents = new List<AgentId> { agent };

                var observedPlan = InitialState.HierarchicalSearch(observationBoundAgents, recipe, agentObservationsList);
                if (observedPlan == null)
                {
                    continue;
                }

                var costDelta = observedPlan.Count - optimalCost;

                if (costDelta < bestCost)
                {
                    bestPlan = observedPlan;
                    bestCost = costDelta;
                    bestSubGoal = possibleSubGoal;
                }
            }

            if (!bestSubGoal.HasValue && tryAgain)
            {
                return GetBestFittingSubGoal(agent, possibleDirectSubGoals, agentObservationsList, out bestPlan,
                    out bestCost, false);
            }

            return bestSubGoal;
        }

        private long GetOptimalCostForRecipe(Recipe recipe)
        {
            long optimalCost;
            if (_optimalRecipePlanCosts.ContainsKey(recipe.Name))
            {
                optimalCost = _optimalRecipePlanCosts[recipe.Name];
            }
            else
            {
                var optimalPlan = CurrentState.HierarchicalSearch(null, recipe);
                optimalCost = optimalPlan.Count;
                _optimalRecipePlanCosts.Add(recipe.Name, optimalCost);
            }

            return optimalCost;
        }

        private IEnumerable<PossibleSubgoal> GetDirectlyAchievableSubGoals(State state, List<IMergeable> gameObjects = null)
        {
            IEnumerable<PossibleSubgoal> PossibleSubgoals()
            {
                // Gets which subgoals are directly achievable in the current state
                // this can be used to infer which limited list of possible final goals are likely
                gameObjects ??= CurrentState.GameObjects;

                foreach (var gameObject in gameObjects)
                {
                    foreach (var otherGameObject in gameObjects)
                    {
                        if (Recipe.TryGetRecipe(gameObject, otherGameObject, out var recipe))
                        {
                            var useful = false;
                            foreach (Recipe goalRecipe in state.GoalRecipes)
                            {
                                useful = useful || (recipe!.Value.LeadsTo(goalRecipe) &&
                                                    CurrentState.RecipeIsPossible(goalRecipe));
                            }

                            if (!useful) continue;

                            yield return new PossibleSubgoal(recipe!.Value.Name, (gameObject.Name, otherGameObject.Name));
                        }
                    }
                }
            }

            return PossibleSubgoals().Distinct();
        }

        public override void UpdateState(Dictionary<AgentId, Action> agentActionAssignment)
        {
            base.UpdateState(agentActionAssignment);
            if (agentActionAssignment[AgentId].Kind == AgentActionKind.NoOp)
            {
                _consecutiveNoop++;
            }
            else
            {
                _consecutiveNoop = 0;
            }
        }
    }
}
