namespace MACsharp
{
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Interfaces;
    using Structs;
    using System.Text;

    public class State : IEquatable<State>
    {
        public static readonly DictionaryComparer<Position, IMergeable> ObjectPositionsComparer = new();
        public static readonly DictionaryComparer<AgentId, Position> AgentPositionsComparer = new();

        public readonly int TimeStep;
        public readonly State ParentState;
        public CellKind[,] Cells { get; set; }
        public readonly ISet<Recipe> GoalRecipes = new HashSet<Recipe>();

        // GameObjectPositions contains Ingredients, FoodItems, and ActionObjects
        // InnerGameObjectPositions does not contain ActionObjects
        public IList<(Position Position, IMergeable Mergeable)> GameObjectPositions;
        public List<IMergeable> GameObjects;
        public readonly Dictionary<Position, IMergeable> InnerGameObjectPositions;
        public readonly Dictionary<AgentId, Position> AgentPositions;
        public readonly Dictionary<AgentId, HashSet<Position>> Reachability = new();
        public readonly ISet<Recipe> FulfilledRecipes = new HashSet<Recipe>();
        public readonly ILookup<CellKind, Position> CellKindPositions;
        public int? LockedUntilTimeStep;

        public State(
            int timeStep,
            State parentState,
            CellKind[,] cells,
            Dictionary<Position, IMergeable> gameObjectPositions,
            Dictionary<AgentId, Position> agentPositions,
            Dictionary<AgentId, HashSet<Position>> reachability,
            ISet<Recipe> fulfilledRecipes,
            ISet<Recipe> goalRecipes,
            ILookup<CellKind, Position> cellKindPositions,
            int? lockedUntilTimeStep)
        {
            TimeStep = timeStep;
            ParentState = parentState;
            Cells = cells;
            InnerGameObjectPositions = gameObjectPositions;
            AgentPositions = agentPositions;
            Reachability = reachability;
            FulfilledRecipes = fulfilledRecipes;
            GoalRecipes = goalRecipes;
            CellKindPositions = cellKindPositions;
            LockedUntilTimeStep = lockedUntilTimeStep;
            GameObjectPositions = GetGameObjects();
        }

        public CellKind GetCellKind(Position p) => Cells[p.X, p.Y];

        public bool TryGetHeldItem(AgentId agentId, out IMergeable heldItem)
        {
            var agentPosition = AgentPositions[agentId];
            return InnerGameObjectPositions.TryGetValue(agentPosition, out heldItem);
        }

        public IEnumerable<Position> GetPositionsOfType(CellKind cellKind)
        {
            return CellKindPositions[cellKind];
        }

        public void SetFulfilledRecipe(Recipe recipe)
        {
            FulfilledRecipes.Add(recipe);
        }

        public State DeepCopy()
        {
            return new State(TimeStep,
                ParentState,
                Cells,
                new (InnerGameObjectPositions),
                new (AgentPositions),
                Reachability,
                FulfilledRecipes.ToHashSet(),
                GoalRecipes,
                CellKindPositions,
                LockedUntilTimeStep);
        }

        public State ChildState()
        {
            return new State(TimeStep + 1,
                this,
                Cells,
                new (InnerGameObjectPositions),
                new (AgentPositions),
                Reachability,
                FulfilledRecipes.ToHashSet(),
                GoalRecipes,
                CellKindPositions,
                LockedUntilTimeStep);
        }

        public IList<(Position Position, IMergeable Mergeable)> GetGameObjects()
        {
            var cuttingStations = GetPositionsOfType(CellKind.CuttingStation).ToList();
            var deliveryStations = GetPositionsOfType(CellKind.DeliveryStation).ToList();

            var gameObjectPositions = new List<(Position, IMergeable)>(InnerGameObjectPositions.Count + cuttingStations.Count + deliveryStations.Count);
            var gameObjects = new List<IMergeable>(InnerGameObjectPositions.Count + cuttingStations.Count + deliveryStations.Count);

            foreach (var kv in InnerGameObjectPositions)
            {
                gameObjectPositions.Add((kv.Key, kv.Value));
                gameObjects.Add(kv.Value);
            }

            var cuttingStation = ActionObject.GetCuttingStation();
            foreach (Position position in cuttingStations)
            {
                gameObjectPositions.Add((position, cuttingStation));
                gameObjects.Add(cuttingStation);
            }

            var deliveryStation = ActionObject.GetDeliveryStation();
            foreach (Position position in deliveryStations)
            {
                gameObjectPositions.Add((position, deliveryStation));
                gameObjects.Add(deliveryStation);
            }

            GameObjects = gameObjects;
            return gameObjectPositions;
        }

        public bool IsRecipeFulfilled(Recipe recipe)
        {
            return GameObjects.Contains(recipe.Product) || FulfilledRecipes.Contains(recipe);
        }

        public bool GoalState()
        {
            var result = true;

            foreach (var goalRecipe in GoalRecipes)
            {
                result = result && IsRecipeFulfilled(goalRecipe);
            }

            return result;
        }

        public IEnumerable<Ingredient> GetIngredients() => InnerGameObjectPositions.Values.OfType<Ingredient>();

        public void RemoveItem(Position position)
        {
            InnerGameObjectPositions.Remove(position);
            GameObjectPositions = GetGameObjects();
        }

        public bool IsAgentHoldingItem(AgentId agent, out IMergeable item) =>
            InnerGameObjectPositions.TryGetValue(AgentPositions[agent], out item);

        public bool Equals(State other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return /*TimeStep == other.TimeStep && */InnerGameObjectPositions.DictionaryEquals(other.InnerGameObjectPositions) && AgentPositions.DictionaryEquals(other.AgentPositions);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((State)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(/*TimeStep,*/ ObjectPositionsComparer.GetHashCode(InnerGameObjectPositions), AgentPositionsComparer.GetHashCode(AgentPositions));
        }

        public override string ToString()
        {
            var xMax = Cells.GetLength(0);
            var yMax = Cells.GetLength(1);

            var chars = new char[xMax, yMax];
            var foodItems = new Dictionary<(int x, int y), (string, string)>();

            var foodItemPlaceholder = 'A';

            for (int i = 0; i < yMax; i++)
            {
                for (int j = 0; j < xMax; j++)
                {
                    chars[j, i] = Cells[j, i].ToRepresentation();

                    if (InnerGameObjectPositions.TryGetValue(new Position(j, i), out var mergeable))
                    {
                        if (mergeable is FoodItem fi)
                        {
                            foodItems.Add((j,i), (foodItemPlaceholder.ToString(), fi.Name));
                            chars[j, i] = foodItemPlaceholder++;

                        }
                        else if (mergeable is Ingredient ingredient)
                        {

                            var c = ingredient.Kind switch
                            {
                                IngredientKind.Plate => 'p',
                                IngredientKind.Tomato => 't',
                                IngredientKind.Onion => 'o',
                                IngredientKind.Lettuce => 'l',
                                _ => throw new ArgumentOutOfRangeException(),
                            };

                            chars[j, i] = ingredient.Properties.HasFlag(Property.IsCut) ? char.ToUpper(c) : c;
                        }
                    }
                }
            }

            foreach (var (agent, position) in AgentPositions)
            {
                chars[position.X, position.Y] = agent.Id.ToString()[0];
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Time Step: {TimeStep}");

            foreach (var (position, ingredient) in InnerGameObjectPositions.Where(x => x.Value is Ingredient))
            {
                if (AgentPositions.ContainsValue(position))
                {
                    var agent = chars[position.X, position.Y];
                    sb.AppendLine($"{agent} is holding {ingredient.Name}");
                }
            }

            foreach (var ((x,y), (placeholder,foodItemName)) in foodItems)
            {
                if (AgentPositions.ContainsValue(new Position(x, y)))
                {
                    sb.AppendLine($"{chars[x, y]} is holding {foodItemName}");
                }
                else
                {
                    sb.AppendLine($"{placeholder} is {foodItemName}");
                }
            }

            sb.Append(' ');
            sb.AppendLine(string.Join("", Enumerable.Range(0, xMax)));

            for (int i = 0; i < yMax; i++)
            {
                sb.Append(i);

                for (int j = 0; j < xMax; j++)
                {
                    sb.Append(chars[j, i]);
                }

                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}