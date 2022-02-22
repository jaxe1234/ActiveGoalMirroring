namespace MACsharpTest
{
    using MACsharp;
    using MACsharp.Enums;
    using MACsharp.Structs;
    using System.Linq;
    using TestUtil;
    using Xunit;
    
    public class LevelReaderTest : MACsharpTestBase
    {
        /// <summary>
        /// This test that the level is being read correctly. It assures;
        ///     1. The grid world is correctly set up
        ///     2. The initial state contains the correct movable objects and number of agents
        ///     3. The exact correct goal recipes
        /// </summary>
        /// <param name="levelName"></param>
        /// <param name="containsOnion"></param>
        /// <param name="gridString"></param>
        /// <param name="goalRecipes"></param>
        [Theory]
        [InlineData("full-divider_onionsalad", true, FullDivider, "OnionSalad")]
        [InlineData("full-divider_salad", false, FullDivider, "Salad")]
        [InlineData("full-divider_tl", false, FullDivider, "SimpleTomato", "SimpleLettuce")]
        [InlineData("full-divider_tomato", false, FullDivider, "SimpleTomato")]
        [InlineData("partial-divider_onionsalad", true, PartialDivider, "OnionSalad")]
        [InlineData("partial-divider_salad", false, PartialDivider, "Salad")]
        [InlineData("partial-divider_tl", false, PartialDivider, "SimpleTomato", "SimpleLettuce")]
        [InlineData("partial-divider_tomato", false, PartialDivider, "SimpleTomato")]
        [InlineData("open-divider_onionsalad", true, OpenDivider, "OnionSalad")]
        [InlineData("open-divider_salad", false, OpenDivider, "Salad")]
        [InlineData("open-divider_tl", false, OpenDivider, "SimpleTomato", "SimpleLettuce")]
        [InlineData("open-divider_tomato", false, OpenDivider, "SimpleTomato")]
        public void TestReadLevelFromFile(string levelName, bool containsOnion, string gridString, params string[] goalRecipes)
        {
            for (int i = 1; i <= 4; i++)
            {
                var state = LevelReader.Read($"utils/levels/{levelName}.txt", i);

                Assert.Equal(i, state.AgentPositions.Count);

                Assert.Equal(gridString, state.Cells.ToRepresentation());

                Assert.Contains(state.GameObjectPositions.Select(x => x.Mergeable).OfType<ActionObject>(), x => x.Kind == ActionObjectKind.CuttingStation);
                Assert.Contains(state.GameObjectPositions.Select(x => x.Mergeable).OfType<ActionObject>(), x => x.Kind == ActionObjectKind.DeliveryStation);

                Assert.Equal(2, state.GameObjectPositions.Select(x => x.Mergeable).OfType<Ingredient>().Count(x => x.Kind == IngredientKind.Plate));
                Assert.Contains(state.GameObjectPositions.Select(x => x.Mergeable).OfType<Ingredient>(), x => x.Kind == IngredientKind.Lettuce);
                Assert.Contains(state.GameObjectPositions.Select(x => x.Mergeable).OfType<Ingredient>(), x => x.Kind == IngredientKind.Tomato);
                Assert.Equal(containsOnion, state.GameObjectPositions.Select(x => x.Mergeable).OfType<Ingredient>().Any(x => x.Kind == IngredientKind.Onion));

                Assert.Equal(goalRecipes.Length, state.GoalRecipes.Count);

                foreach (var goal in goalRecipes)
                {
                    Assert.Contains(goal, state.GoalRecipes.Select(x => x.Name));
                }
            }
        }
    }
}
