namespace MACsharpTest
{
    using MACsharp.Enums;
    using MACsharp.Helpers;
    using MACsharp.Structs;
    using System.Collections.Generic;
    using TestUtil;
    using Xunit;
    using Action = MACsharp.Structs.Action;

    public class StateTests : MACsharpTestBase
    {
        [Fact]
        public void TestState1()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.FullDivider)
                .WithAgent(2, (2, 1))
                .WithAgentHeldIngredient(1, "Lettuce", (4, 1))
            );

            var actions = new Dictionary<AgentId, Action>();
            actions.Add(new AgentId(2), new Action(AgentActionKind.MoveRight));
            actions.Add(new AgentId(1), new Action(AgentActionKind.MoveLeft));

            Assert.False(state.TryPerform(actions, out _));
        }

        [Fact]
        public void TestState2()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.OpenDivider)
                .WithAgent(1, (1, 1))
                .WithAgentHeldFoodItem(2, "PlatedCutTomato", (3,4))
            );

            var actions = new Dictionary<AgentId, Action>();
            actions.Add(new AgentId(1), new Action(AgentActionKind.MoveLeft));

            Assert.False(state.TryPerform(actions, out _));
        }

        [Fact]
        public void TestState3()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.OpenDivider)
                .WithAgentHeldFoodItem(1, "PlatedCutTomato", (1, 1))
            );

            var actions = new Dictionary<AgentId, Action>();
            actions.Add(new AgentId(1), new Action(AgentActionKind.MoveLeft));

            Assert.True(state.TryPerform(actions, out var newState, false, true));
            Assert.False(state.TryPerform(actions, out _, false, false));

            actions = new Dictionary<AgentId, Action>();
            actions.Add(new AgentId(1), new Action(AgentActionKind.MoveLeft));

            Assert.True(newState.TryPerform(actions, out newState));
            Assert.True(newState.IsAgentHoldingItem(new AgentId(1), out var item) && item.Name == "PlatedCutTomato");
        }

        [Fact]
        public void TestState4()
        {
            var state = CreateState(x => x.OnMap(PredefinedGrid.OpenDivider)
                .WithAgentHeldIngredient(1, "Tomato", (1, 1))
            );

            var actions = new Dictionary<AgentId, Action>();
            actions.Add(new AgentId(1), new Action(AgentActionKind.MoveLeft));

            Assert.True(state.TryPerform(actions, out var newState));
            Assert.True(newState.IsAgentHoldingItem(new AgentId(1), out var item));
            Assert.True(item.Name == "TomatoMovableIsCut");
        }
    }
}
