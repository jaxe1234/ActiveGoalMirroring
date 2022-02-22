namespace MACsharpTest
{
    using MACsharp.Enums;
    using MACsharp.Structs;
    using TestUtil;
    using Xunit;
    
    public class RecipeTest : MACsharpTestBase
    {
        [Fact]
        public void TestRecipeLeadsTo()
        {
            var cutTomato = Recipe.GetRecipe("TomatoMovableIsCut");
            var platedCutTomato = Recipe.GetRecipe("PlatedCutTomato");
            var cutTomatoLettuce = Recipe.GetRecipe("CutTomatoLettuce");
            var simpleTomato = Recipe.GetRecipe("SimpleTomato");

            Assert.True(platedCutTomato.LeadsTo(simpleTomato));

            Assert.True(cutTomato.LeadsTo(platedCutTomato));
            Assert.False(platedCutTomato.LeadsTo(cutTomato));

            Assert.True(cutTomato.LeadsTo(cutTomatoLettuce));
            Assert.False(cutTomatoLettuce.LeadsTo(cutTomato));

            Assert.False(platedCutTomato.LeadsTo(cutTomatoLettuce));
            Assert.False(cutTomatoLettuce.LeadsTo(platedCutTomato));
        }

        [Fact]
        public void TestRecipePrerequisites()
        {
            var m1 = new RecipePrerequisiteTuple(new FoodItem("PlatedCutTomato",
                    PropertyKind.Movable | PropertyKind.Deliverable, 
                    Ingredient.GetCutTomato(), 
                    Ingredient.GetPlate()), 
                    ActionObject.GetDeliveryStation());
            var m2 = new RecipePrerequisiteTuple(new FoodItem("PlatedCutTomato",
                    PropertyKind.Movable | PropertyKind.Deliverable,
                    Ingredient.GetCutTomato(),
                    Ingredient.GetPlate()),
                    ActionObject.GetDeliveryStation());

            Assert.Equal(m1, m2);
            Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
        }
    }
}
