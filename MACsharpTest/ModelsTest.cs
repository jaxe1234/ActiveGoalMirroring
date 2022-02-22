namespace MACsharpTest
{
    using MACsharp.Enums;
    using MACsharp.Helpers;
    using MACsharp.Structs;
    using System.Collections.Generic;
    using System.Linq;
    using TestUtil;
    using Xunit;

    public class ModelsTest : MACsharpTestBase
    {
        [Fact]
        public void TestStateComparison()
        {
            var state1 = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithIngredient("Lettuce", location: (0, 0))
                .WithAgent(0, location: (0, 1))
                .WithAgent(1, location: (1, 0)));

            var state2 = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithIngredient("Lettuce", location: (0, 0))
                .WithAgent(0, location: (0, 1))
                .WithAgent(1, location: (1, 0)));

            Assert.False(ReferenceEquals(state1, state2));
            Assert.Equal(state1, state2);
            Assert.Equal(state1.GetHashCode(), state2.GetHashCode());

            Assert.Equal(state1, state1.DeepCopy());
            Assert.Equal(state1.GetHashCode(), state1.DeepCopy().GetHashCode());
        }

        [Fact]
        public void PositionHashCodeTest()
        {
            var hashes = new List<int>();

            for (int i = 0; i < 10000; i++)
            {
                for (int j = 0; j < 1000; j++)
                {
                    hashes.Add(new Position(i, j).GetHashCode());
                }
            }

            var distinctHashes = hashes.Distinct().ToList();

            Assert.Equal(hashes.Count, distinctHashes.Count);
        }

        [Fact]
        public void TestGetFlags()
        {
            var properties = PropertyKind.Movable | PropertyKind.Cuttable | PropertyKind.Deliverable;

            var expected = new List<PropertyKind>()
            {
                PropertyKind.Movable, PropertyKind.Cuttable, PropertyKind.Deliverable
            };

            var actual = properties.GetFlags().ToList();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetFlags2()
        {
            var properties = PropertyKind.Movable | PropertyKind.IsDelivered;

            var expected = new List<PropertyKind>()
            {
                PropertyKind.Movable, PropertyKind.IsDelivered
            };

            var actual = properties.GetFlags().ToList();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestStateToString_TwoAgents()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithAgent(0, location: (2, 1))
                .WithAgent(1, location: (4, 1)));

            string expected = "Time Step: 0" + "\r\n"
                            + " 0123456" + "\r\n"
                            + "0-------" + "\r\n"
                            + "1/ 0-1 -" + "\r\n"
                            + "2/  -  -" + "\r\n"
                            + "3*  -  -" + "\r\n"
                            + "4-  -  -" + "\r\n"
                            + "5-  -  -" + "\r\n"
                            + "6-------" + "\r\n";
            var actual = state.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestStateToString_WithFoodItem()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithAgent(0, (2, 1))
                .WithFoodItem("CutTomatoLettuceOnion", location: (3, 3)));

            string expected = "Time Step: 0" + "\r\n"
                              + "A is CutTomatoLettuceOnion" + "\r\n"
                              + " 0123456" + "\r\n"
                              + "0-------" + "\r\n"
                              + "1/ 0-  -" + "\r\n"
                              + "2/  -  -" + "\r\n"
                              + "3*  A  -" + "\r\n"
                              + "4-  -  -" + "\r\n"
                              + "5-  -  -" + "\r\n"
                              + "6-------" + "\r\n";
            var actual = state.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestStateToString_WithHeldIngredient()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithAgentHeldIngredient(0, "CutTomato", (2, 1)));

            string expected = "Time Step: 0" + "\r\n"
                              + "0 is holding TomatoMovableIsCut" + "\r\n"
                              + " 0123456" + "\r\n"
                              + "0-------" + "\r\n"
                              + "1/ 0-  -" + "\r\n"
                              + "2/  -  -" + "\r\n"
                              + "3*  -  -" + "\r\n"
                              + "4-  -  -" + "\r\n"
                              + "5-  -  -" + "\r\n"
                              + "6-------" + "\r\n";
            var actual = state.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestStateToString_WithHeldFoodItem()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithAgentHeldFoodItem(0, "CutTomatoLettuceOnion", location: (2, 1)));

            string expected = "Time Step: 0" + "\r\n"
                             + "0 is holding CutTomatoLettuceOnion" + "\r\n"
                             + " 0123456" + "\r\n"
                             + "0-------" + "\r\n"
                             + "1/ 0-  -" + "\r\n"
                             + "2/  -  -" + "\r\n"
                             + "3*  -  -" + "\r\n"
                             + "4-  -  -" + "\r\n"
                             + "5-  -  -" + "\r\n"
                             + "6-------" + "\r\n";
            var actual = state.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestStateToString_WithHeldFoodItemAndFoodItemOnCounter()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithAgentHeldFoodItem(0, "CutTomatoLettuce", location: (2, 1))
                .WithFoodItem("PlatedCutOnion", location: (3, 3)));

            string expected = "Time Step: 0" + "\r\n"
                              + "0 is holding CutTomatoLettuce" + "\r\n"
                              + "B is PlatedCutOnion" + "\r\n"
                              + " 0123456" + "\r\n"
                              + "0-------" + "\r\n"
                              + "1/ 0-  -" + "\r\n"
                              + "2/  -  -" + "\r\n"
                              + "3*  B  -" + "\r\n"
                              + "4-  -  -" + "\r\n"
                              + "5-  -  -" + "\r\n"
                              + "6-------" + "\r\n";
            var actual = state.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestStateToString_Complex()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithAgentHeldIngredient(0, "CutOnion", (1, 2))
                .WithAgent(1, (4, 2))
                .WithAgent(2, (4, 4))
                .WithIngredient("Plate", (5, 6))
                .WithFoodItem("PlatedCutTomatoLettuce", (2, 6))
                .SetTimeStep(43)
                .AddGoal("OnionSalad"));

            var expectedString = @"Time Step: 43
0 is holding OnionMovableIsCut
A is PlatedCutTomatoLettuce
 0123456
0-------
1/  -  -
2/0 -1 -
3*  -  -
4-  -2 -
5-  -  -
6--A--p-
";

            Assert.Equal(expectedString, state.ToString());
        }

        [Theory]
        [InlineData("PlatedCutTomato", "SimpleTomato")]
        [InlineData("PlatedCutLettuce", "SimpleLettuce")]
        [InlineData("PlatedCutTomatoLettuce", "Salad")]
        [InlineData("PlatedCutTomatoLettuceOnion", "OnionSalad")]
        public void TestRecipeIsPossible_PredecessorFoodItem(string foodItemName, string recipeName)
        {
            var state = CreateState(x =>
                x.OnMap(PredefinedGrid.FullDivider)
                    .WithAgentHeldFoodItem(0, foodItemName, (1, 3))
                    .AddGoal(recipeName));

            Assert.True(state.RecipeIsPossible(Recipe.GetRecipe(recipeName)));
        }

        [Theory]
        [InlineData("OnionSalad", "CutTomatoLettuce", "PlatedCutOnion")]
        [InlineData("OnionSalad", "CutTomatoOnion", "PlatedCutLettuce")]
        [InlineData("OnionSalad", "CutLettuceOnion", "PlatedCutTomato")]
        public void TestRecipeIsPossible_BasicFoodItem(string recipeName, string foodItem1, string foodItem2)
        {
            var state = CreateState(x =>
                x.OnMap(PredefinedGrid.FullDivider)
                    .WithAgentHeldFoodItem(0, foodItem1, (1, 2))
                    .WithAgentHeldFoodItem(1, foodItem2, (1, 3))
                    .AddGoal(recipeName));

            Assert.True(state.RecipeIsPossible(Recipe.GetRecipe(recipeName)));
        }

        [Theory]
        [InlineData("OnionSalad", "PlatedCutTomatoLettuce", "CutOnion")]
        [InlineData("OnionSalad", "PlatedCutTomatoOnion", "CutLettuce")]
        [InlineData("OnionSalad", "PlatedCutLettuceOnion", "CutTomato")]
        public void TestRecipeIsPossible_BasicFoodItemAndIngredient(string recipeName, string foodItem, string ingredient)
        {
            var state = CreateState(x =>
                x.OnMap(PredefinedGrid.FullDivider)
                    .WithAgentHeldFoodItem(0, foodItem, (1, 2))
                    .WithAgentHeldIngredient(1, ingredient, (1, 3))
                    .AddGoal(recipeName));

            Assert.True(state.RecipeIsPossible(Recipe.GetRecipe(recipeName)));
        }

        [Theory]
        [InlineData("Salad", "PlatedCutLettuce", "PlatedCutTomato")]
        public void TestRecipeIsPossible_BasicFoodItemAndIngredient_ReturnsFalse(string recipeName, string foodItem1, string foodItem2)
        {
            var state = CreateState(x =>
                x.OnMap(PredefinedGrid.FullDivider)
                    .WithAgentHeldFoodItem(0, foodItem1, (1, 2))
                    .WithAgentHeldFoodItem(1, foodItem2, (1, 3))
                    .AddGoal(recipeName));

            Assert.False(state.RecipeIsPossible(Recipe.GetRecipe(recipeName)));
        }
    }
}
