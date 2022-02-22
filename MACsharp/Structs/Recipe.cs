namespace MACsharp.Structs
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Enums;
    using Interfaces;
    using Helpers;
    using System.Diagnostics;

    [DebuggerDisplay("Recipe({Name})")]
    public readonly struct Recipe : IEquatable<Recipe>
    {
        public static Dictionary<string, Recipe> Mapping { get; set; }

        public static IImmutableDictionary<RecipePrerequisiteTuple, string> RecipeAdjacencies;
        public static ILookup<string, (string, string)> MergeableAdjacencies;
        public static Dictionary<IMergeable, HashSet<Recipe>> CanBeCombinedToCache; 

        private static HashSet<(Recipe, Recipe)> _recipeLeadsTo;

        static Recipe()
        {
            CompileRecipes();
            CreateRecipeTree();
        }

        public string Name { get; }
        public IImmutableSet<IMergeable> Prerequisites { get; }
        public IMergeable Product { get; }

        public Recipe(IMergeable product, params IMergeable[] prerequisites)
        {
            Name = product.Name;
            Prerequisites = prerequisites.ToImmutableHashSet();
            Product = product;
        }

        public Recipe(FoodItem product, bool isDelivery = false)
        {
            Name = product.Name;

            if (isDelivery)
            {
                Prerequisites = product.Ingredients.Cast<IMergeable>().Concat(new List<IMergeable> { ActionObject.GetDeliveryStation() }).ToImmutableHashSet();
            }
            else
            {
                Prerequisites = product.Ingredients.Cast<IMergeable>().ToImmutableHashSet();
            }
            Product = product;
        }

        public static bool TryGetRecipe(string name, out Recipe recipe) => Mapping.TryGetValue(name, out recipe);
        public static Recipe GetRecipe(string name) => Mapping[name];
        
        public static bool TryGetRecipe(IMergeable m1, IMergeable m2, out Recipe? recipe)
        {
            if (RecipeAdjacencies.TryGetValue(new RecipePrerequisiteTuple(m1, m2), out var recipeName))
            {
                recipe = GetRecipe(recipeName);
                return true;
            }

            recipe = null;
            return false;
        }

        public static bool TryGetFoodItem(IEnumerable<IMergeable> mergeables, out IMergeable result)
        {
            result = null;

            var mergeableList = mergeables.ToList();
            if (mergeableList.Count() == 1)
            {
                result = mergeableList.First();
                return true;
            }

            Recipe? foundRecipe = null;

            foreach (var recipe in Mapping)
            {
                if (recipe.Value.Prerequisites.SetEquals(mergeableList))
                {
                    foundRecipe = recipe.Value;
                    break;
                }
            }

            if (foundRecipe.HasValue && foundRecipe.Value.Product is FoodItem or Ingredient)
            {
                result = foundRecipe.Value.Product;
                return true;
            }

            return false;
        }

        private static bool TryGetProductInternal(IMergeable input1, IMergeable input2, out IMergeable product, out string recipeName)
        {
            if (input1 is FoodItem fi1 &&
                input2 is FoodItem fi2 &&
                fi1.HasIngredient(IngredientKind.Plate) &&
                fi2.HasIngredient(IngredientKind.Plate) ||
                input1.HasProperty(PropertyKind.IsDelivered) ||
                input2.HasProperty(PropertyKind.IsDelivered))
            {
                product = null;
                recipeName = string.Empty;
                return false;
            }

            var ingredientsList = new List<IMergeable>();

            if (input1 is FoodItem foodItem1)
            {
                ingredientsList.AddRange(foodItem1.Ingredients.Select(x => (IMergeable)x));
            }
            else
            {
                ingredientsList.Add(input1);
            }

            if (input2 is FoodItem foodItem2)
            {
                ingredientsList.AddRange(foodItem2.Ingredients.Select(x => (IMergeable)x));
            }
            else
            {
                ingredientsList.Add(input2);
            }

            if (ingredientsList.Count(x => x is Ingredient i && i.IsKind(IngredientKind.Plate)) > 1)
            {
                product = null;
                recipeName = string.Empty;
                return false;
            }

            ISet<IMergeable> ingredients = new HashSet<IMergeable>(ingredientsList);

            foreach (Recipe recipe in Mapping.Values)
            {
                if (recipe.Prerequisites.SetEquals(ingredients))
                {
                    recipeName = recipe.Name;
                    product = recipe.Product;
                    return true;
                }
            }

            recipeName = null;
            product = null;
            return false;
        }

        public static void CompileRecipes()
        {
            Mapping = new Dictionary<string, Recipe>();

            CompileSingleIngredientRecipes();

            CompileTwoIngredientRecipes();

            CompileThreeIngredientRecipes();

            var deliveryRecipes = new List<Recipe>();

            foreach (var recipe in Mapping.Values)
            {
                var product = recipe.Product;
                if (product is FoodItem foodItem && foodItem.Properties.HasFlag(PropertyKind.Deliverable))
                {
                    var name = "Delivered" + recipe.Name;
                    var deliveredFoodItem = new FoodItem(name, foodItem.Properties, foodItem.Ingredients.ToArray());
                    deliveredFoodItem.Properties |= Property.IsDelivered;
                    deliveredFoodItem.Properties &= ~Property.IsDeliverable;

                    var delivery = new Recipe(deliveredFoodItem);
                    deliveryRecipes.Add(delivery);
                }
            }

            deliveryRecipes.ForEach(x => Mapping.Add(x.Name, x));

            Mapping["SimpleTomato"] = new Recipe(CopyWithNewName((FoodItem)Mapping["DeliveredPlatedCutTomato"].Product, "SimpleTomato"), isDelivery: true);
            Mapping.Remove("DeliveredPlatedCutTomato");

            Mapping["SimpleLettuce"] = new Recipe(CopyWithNewName((FoodItem)Mapping["DeliveredPlatedCutLettuce"].Product, "SimpleLettuce"), isDelivery: true);
            Mapping.Remove("DeliveredPlatedCutLettuce");

            Mapping["Salad"] = new Recipe(CopyWithNewName((FoodItem)Mapping["DeliveredPlatedCutTomatoLettuce"].Product, "Salad"), isDelivery: true);
            Mapping.Remove("DeliveredPlatedCutTomatoLettuce");

            Mapping["OnionSalad"] = new Recipe(CopyWithNewName((FoodItem)Mapping["DeliveredPlatedCutTomatoLettuceOnion"].Product, "OnionSalad"), isDelivery: true);
            Mapping.Remove("DeliveredPlatedCutTomatoLettuceOnion");
        }

        private static void CompileSingleIngredientRecipes()
        {
            var cutTomato = new Recipe(Ingredient.GetCutTomato(), Ingredient.GetTomato(), ActionObject.GetCuttingStation());
            var cutLettuce = new Recipe(Ingredient.GetCutLettuce(), Ingredient.GetLettuce(), ActionObject.GetCuttingStation());
            var cutOnion = new Recipe(Ingredient.GetCutOnion(), Ingredient.GetOnion(), ActionObject.GetCuttingStation());

            Mapping.Add(cutTomato.Name, cutTomato);
            Mapping.Add(cutLettuce.Name, cutLettuce);
            Mapping.Add(cutOnion.Name, cutOnion);

            var platedCutTomato = new Recipe(new FoodItem("PlatedCutTomato", Property.IsDeliverable, Ingredient.GetCutTomato(), Ingredient.GetPlate()));
            var platedCutLettuce = new Recipe(new FoodItem("PlatedCutLettuce", Property.IsDeliverable, Ingredient.GetCutLettuce(), Ingredient.GetPlate()));
            var platedCutOnion = new Recipe(new FoodItem("PlatedCutOnion", Ingredient.GetCutOnion(), Ingredient.GetPlate()));

            Mapping.Add(platedCutTomato.Name, platedCutTomato);
            Mapping.Add(platedCutLettuce.Name, platedCutLettuce);
            Mapping.Add(platedCutOnion.Name, platedCutOnion);
        }

        private static void CompileTwoIngredientRecipes()
        {
            var cutTomatoLettuce =
                new Recipe(new FoodItem("CutTomatoLettuce", Ingredient.GetCutTomato(), Ingredient.GetCutLettuce()));
            var cutTomatoOnion =
                new Recipe(new FoodItem("CutTomatoOnion", Ingredient.GetCutTomato(), Ingredient.GetCutOnion()));
            var cutLettuceOnion =
                new Recipe(new FoodItem("CutLettuceOnion", Ingredient.GetCutLettuce(), Ingredient.GetCutOnion()));

            Mapping.Add(cutTomatoLettuce.Name, cutTomatoLettuce);
            Mapping.Add(cutTomatoOnion.Name, cutTomatoOnion);
            Mapping.Add(cutLettuceOnion.Name, cutLettuceOnion);

            var platedCutTomatoLettuce = new Recipe(new FoodItem("PlatedCutTomatoLettuce", Property.IsDeliverable, Ingredient.GetCutTomato(),
                Ingredient.GetCutLettuce(), Ingredient.GetPlate()));
            var platedCutTomatoOnion = new Recipe(new FoodItem("PlatedCutTomatoOnion", Ingredient.GetCutTomato(),
                Ingredient.GetCutOnion(), Ingredient.GetPlate()));
            var platedCutLettuceOnion = new Recipe(new FoodItem("PlatedCutLettuceOnion", Ingredient.GetCutLettuce(),
                Ingredient.GetCutOnion(), Ingredient.GetPlate()));

            Mapping.Add(platedCutTomatoLettuce.Name, platedCutTomatoLettuce);
            Mapping.Add(platedCutTomatoOnion.Name, platedCutTomatoOnion);
            Mapping.Add(platedCutLettuceOnion.Name, platedCutLettuceOnion);
        }

        private static void CompileThreeIngredientRecipes()
        {
            var cutTomatoLettuceOnion = new Recipe(new FoodItem("CutTomatoLettuceOnion", Ingredient.GetCutTomato(),
                Ingredient.GetCutLettuce(), Ingredient.GetCutOnion()));

            Mapping.Add(cutTomatoLettuceOnion.Name, cutTomatoLettuceOnion);

            var platedCutTomatoLettuceOnion = new Recipe(new FoodItem("PlatedCutTomatoLettuceOnion", Property.IsDeliverable, Ingredient.GetCutTomato(),
                Ingredient.GetCutLettuce(), Ingredient.GetCutOnion(), Ingredient.GetPlate()));

            Mapping.Add(platedCutTomatoLettuceOnion.Name, platedCutTomatoLettuceOnion);
        }

        private static FoodItem CopyWithNewName(FoodItem m, string newName)
        {
            return new FoodItem(newName, m.Properties, m.Ingredients.ToArray());
        }

        public static bool TryGetFoodItemOptions(Recipe recipe,
            IEnumerable<IMergeable> existingFoodItems, out IList<(IMergeable, IMergeable)> result)
        {
            result = new List<(IMergeable, IMergeable)>();
            var recipeContent = recipe.Prerequisites;
            var options = EnumerableHelpers.GetOptions(recipeContent, existingFoodItems);

            foreach (var (ingredients1, ingredients2) in options)
            {
                if ((!TryGetFoodItem(ingredients1, out var fi1) ||
                     !TryGetFoodItem(ingredients2, out var fi2)) ||
                    !TryGetProductInternal(fi1, fi2, out _, out _))
                {
                    continue;
                }

                result.Add((fi1, fi2));
            }

            result = result.Distinct().ToList();

            return result.Count > 0;
        }

        public static void CreateRecipeTree()
        {
            _recipeLeadsTo = new();
            var adjacencies = new Dictionary<RecipePrerequisiteTuple, string>();
            var mergeableAdjacencies = new List<(string, string, string)>();

            foreach (var kv in Mapping)
            {
                var recipe = kv.Value;
                var recipeName = kv.Key;
                if (!TryGetFoodItemOptions(recipe, recipe.Prerequisites, out var options))
                {
                    throw new Exception();
                }

                foreach (var (m1, m2) in options)
                {
                    adjacencies.TryAdd(new RecipePrerequisiteTuple(m1, m2), recipeName);
                    adjacencies.TryAdd(new RecipePrerequisiteTuple(m2, m1), recipeName);
                    mergeableAdjacencies.Add((recipeName, m1.Name, m2.Name));
                }
            }

            (RecipeAdjacencies, MergeableAdjacencies) = (adjacencies.ToImmutableDictionary(), mergeableAdjacencies.ToLookup(x => x.Item1, x => (x.Item2, x.Item3)));
        }

        public static void PruneWithState(State state)
        {
            var gameObjects = state.GameObjects;
            var stateIngredients = gameObjects
                .Where(x => x is Ingredient)
                .Cast<Ingredient>()
                .ToList();
            var recipeAdjacencies = new Dictionary<RecipePrerequisiteTuple, string>();
            var mergeableAdjacencies = new List<(string, string, string)>();
            var mapping = new Dictionary<string, Recipe>();
            foreach (var recipeAdjacency in RecipeAdjacencies)
            {
                var part1 = gameObjects.Any(x => x.Equals(recipeAdjacency.Key.Part1)) ||
                            recipeAdjacency.Key.Part1 is Ingredient i1 &&
                            stateIngredients.Any(x => x.Kind == i1.Kind) ||
                            recipeAdjacency.Key.Part1 is FoodItem f1 &&
                            f1.Ingredients.All(i => stateIngredients.Any(x => x.Kind == i.Kind));

                var part2 = gameObjects.Any(x => x.Equals(recipeAdjacency.Key.Part2)) ||
                            recipeAdjacency.Key.Part2 is Ingredient i2 &&
                            stateIngredients.Any(x => x.Kind == i2.Kind) ||
                            recipeAdjacency.Key.Part2 is FoodItem f2 &&
                            f2.Ingredients.All(i => stateIngredients.Any(x => x.Kind == i.Kind));


                if (part1 && part2)
                {
                    recipeAdjacencies.Add(recipeAdjacency.Key, recipeAdjacency.Value);
                }
            }

            foreach (var mergeableAdjacency in MergeableAdjacencies)
            {
                var recipe = GetRecipe(mergeableAdjacency.Key);
                if (recipe.Prerequisites.All(x =>
                {
                    return gameObjects.Any(y => y.Equals(x)) ||
                           x is Ingredient i1 &&
                           stateIngredients.Any(x => x.Kind == i1.Kind) ||
                           x is FoodItem f1 &&
                           f1.Ingredients.All(i => stateIngredients.Any(x => x.Kind == i.Kind));
                }))
                {
                    foreach ((string item1, string item2) in mergeableAdjacency)
                    {
                        mergeableAdjacencies.Add((mergeableAdjacency.Key, item1, item2));
                    }
                }
            }

            foreach (var recipe in Mapping)
            {
                if (recipe.Value.Prerequisites.All(x =>
                {
                    return gameObjects.Any(y => y.Equals(x)) ||
                           x is Ingredient i1 &&
                           stateIngredients.Any(x => x.Kind == i1.Kind) ||
                           x is FoodItem f1 &&
                           f1.Ingredients.All(i => stateIngredients.Any(x => x.Kind == i.Kind));
                }))
                {
                    mapping.Add(recipe.Key, recipe.Value);
                }
            }

            Mapping = mapping;
            RecipeAdjacencies = recipeAdjacencies.ToImmutableDictionary();
            MergeableAdjacencies = mergeableAdjacencies.ToLookup(x => x.Item1, x => (x.Item2, x.Item3));
        }

        public static bool CanBeCombinedTo(IMergeable m, Recipe recipe)
        {
            if (CanBeCombinedToCache.TryGetValue(m, out var recipes) && recipes.Contains(recipe))
            {
                return true;
            }

            foreach (var recipeAdjacency in RecipeAdjacencies)
            {
                if (recipeAdjacency.Value == recipe.Name &&
                    (recipeAdjacency.Key.Part1.Equals(m) || recipeAdjacency.Key.Part2.Equals(m)))
                {
                    if (CanBeCombinedToCache.ContainsKey(m))
                    {
                        CanBeCombinedToCache[m].Add(recipe);
                    }
                    else
                    {
                        CanBeCombinedToCache[m] = new HashSet<Recipe> { recipe };
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool CanBeCombinedTo(IMergeable m1, IMergeable m2, Recipe recipe)
        {
            return RecipeAdjacencies.TryGetValue(new RecipePrerequisiteTuple(m1, m2), out var recipeName) && recipe.Name == recipeName;
        }

        public bool LeadsTo(Recipe recipe)
        {
            if (_recipeLeadsTo.Contains((this, recipe)))
            {
                return true;
            }

            if (this.Equals(recipe))
            {
                _recipeLeadsTo.Add((this, recipe));
                return true;
            }

            var product = this.Product;

            var recipesThatUsesRecipe = RecipeAdjacencies.Where(x =>
                x.Key.Part1.Equals(product) ||
                x.Key.Part2.Equals(product))
                .GroupBy(x => x.Value)
                .Select(x => x.First())
                .Select(x => GetRecipe(x.Value));

            foreach (var newRecipe in recipesThatUsesRecipe)
            {
                if (newRecipe.LeadsTo(recipe))
                {
                    _recipeLeadsTo.Add((newRecipe, recipe));
                    return true;
                }
            }

            return false;
        }

        public bool Equals(Recipe other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is Recipe other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}