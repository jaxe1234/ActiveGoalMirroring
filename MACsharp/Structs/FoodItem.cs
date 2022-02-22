namespace MACsharp.Structs
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Enums;
    using Interfaces;
    using System.Diagnostics;

    [DebuggerDisplay("FoodItem({Name})")]
    public struct FoodItem : IMergeable
    {
        public IImmutableSet<Ingredient> Ingredients { get; }
        public PropertyKind Properties { get; set; }
        public string Name { get; }

        public FoodItem(string name, PropertyKind properties, params Ingredient[] ingredients)
        {
            Properties = properties | PropertyKind.Movable;
            Ingredients = ingredients.ToImmutableHashSet();
            Name = name;
        }

        public FoodItem(string name, params Ingredient[] ingredients) : this(name, PropertyKind.None, ingredients)
        {
        }

        public bool HasIngredient(IngredientKind kind)
        {
            return Ingredients.Any(i => i.Kind == kind);
        }

        public bool Equals(FoodItem other) => Properties.Equals(other.Properties) && Ingredients.SetEquals(other.Ingredients) && Name == other.Name;
        bool IEquatable<IMergeable>.Equals(IMergeable obj) => obj is FoodItem other && Equals(other);
        public override bool Equals(object obj) => obj is FoodItem other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(GetIngredientsSetHash(), Name) + (int) Properties;
        
        public static bool operator ==(FoodItem left, FoodItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FoodItem left, FoodItem right)
        {
            return !(left == right);
        }

        public long GetIngredientsSetHash()
        {
            HashCode hash = new();
            foreach (var item in Ingredients)
            {
                hash.Add(item.GetHashCode());
            }

            return hash.ToHashCode();
        }
    }
}
