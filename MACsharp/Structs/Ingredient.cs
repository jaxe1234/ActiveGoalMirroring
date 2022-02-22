namespace MACsharp.Structs
{
    using Enums;
    using Interfaces;
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerDisplay("Ingredient({Name})")]
    public readonly struct Ingredient : IMergeable
    {
        private static Dictionary<PropertyKind, string> PropertyKindNameMap = new();
        private static Dictionary<IngredientKind, string> IngredientKindNameMap = new();
        public readonly IngredientKind Kind;
        public PropertyKind Properties { get; }
        public string Name { get; }

        private Ingredient(IngredientKind kind, PropertyKind properties)
        {
            Kind = kind;
            Properties = properties;

            if (!PropertyKindNameMap.TryGetValue(properties, out string propertyName))
            {
                propertyName = properties.ToString().Replace(", ", "");

                PropertyKindNameMap.Add(properties, propertyName);
            }

            if (!IngredientKindNameMap.TryGetValue(kind, out string ingredientName))
            {
                ingredientName = kind.ToString();

                IngredientKindNameMap.Add(kind, ingredientName);
            }

            Name = ingredientName + propertyName;
        }

        public bool IsKind(IngredientKind kind) => Kind == kind;

        public static Ingredient GetPlate() => new(IngredientKind.Plate, Property.Movable);
        public static Ingredient GetTomato() => new(IngredientKind.Tomato, Property.Movable | Property.Cuttable);
        public static Ingredient GetLettuce() => new(IngredientKind.Lettuce, Property.Movable | Property.Cuttable);
        public static Ingredient GetOnion() => new(IngredientKind.Onion, Property.Movable | Property.Cuttable);
        //public static Ingredient GetCucumber() => new(IngredientKind.Cucumber, Property.Movable | Property.Cuttable);

        public static Ingredient GetCutTomato() => new(IngredientKind.Tomato, Property.Movable | Property.IsCut);
        public static Ingredient GetCutLettuce() => new(IngredientKind.Lettuce, Property.Movable | Property.IsCut);
        public static Ingredient GetCutOnion() => new(IngredientKind.Onion, Property.Movable | Property.IsCut);
        //public static Ingredient GetCutCucumber() => new(IngredientKind.Cucumber, Property.Movable | Property.IsCut);

        public bool Equals(Ingredient other) => Kind == other.Kind && Properties.Equals(other.Properties);
        public bool Equals(IMergeable obj) => obj is Ingredient other && Equals(other);
        public override bool Equals(object obj) => obj is Ingredient other && Equals(other);

        public override int GetHashCode() => (int)Kind + (int) Properties;

        public static bool operator ==(Ingredient left, Ingredient right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Ingredient left, Ingredient right)
        {
            return !(left == right);
        }
    }
}