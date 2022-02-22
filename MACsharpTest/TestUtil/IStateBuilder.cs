namespace MACsharpTest.TestUtil
{
    using MACsharp;
    using MACsharp.Enums;

    public interface IStateBuilder
    {
        State ToState();
        IStateBuilder AddGoal(string recipeName);
        IStateBuilder OnMap(PredefinedGrid type);
        IStateBuilder OnMap(string gridWorld);
        IStateBuilder SetTimeStep(int timeStep);
        IStateBuilder WithAgent(int id, (int x, int y) location);
        IStateBuilder WithAgentHeldFoodItem(int id, string foodItemName, (int x, int y) location);
        IStateBuilder WithAgentHeldIngredient(int id, string ingredientName, (int x, int y) location);
        IStateBuilder WithFoodItem(string foodItemName, (int x, int y) location);
        IStateBuilder WithIngredient(string ingredientName, (int x, int y) location);
        IStateBuilder WithParentState(State state);
    }
}