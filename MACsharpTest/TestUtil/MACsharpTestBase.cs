namespace MACsharpTest.TestUtil
{
    using MACsharp;
    using MACsharp.Enums;
    using MACsharp.Interfaces;
    using MACsharp.Structs;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Xunit;

    [Collection("MACsharpTests")]
    public abstract class MACsharpTestBase
    {
        protected readonly AgentId Agent0 = new(0);
        protected readonly AgentId Agent1 = new(1);
        protected readonly AgentId Agent2 = new(2);
        protected readonly AgentId Agent3 = new(3);

        protected const string FullDivider =
              "-------" + "\r\n"
            + "/  -  -" + "\r\n"
            + "/  -  -" + "\r\n"
            + "*  -  -" + "\r\n"
            + "-  -  -" + "\r\n"
            + "-  -  -" + "\r\n"
            + "-------" + "\r\n";

        protected const string PartialDivider =
              "-------" + "\r\n"
            + "/  -  -" + "\r\n"
            + "/  -  -" + "\r\n"
            + "*  -  -" + "\r\n"
            + "-  -  -" + "\r\n"
            + "-     -" + "\r\n"
            + "-------" + "\r\n";

        protected const string OpenDivider =
              "-------" + "\r\n"
            + "/     -" + "\r\n"
            + "/     -" + "\r\n"
            + "*     -" + "\r\n"
            + "-     -" + "\r\n"
            + "-     -" + "\r\n"
            + "-------" + "\r\n";

        protected static State CreateState(Func<IStateBuilder, IStateBuilder> buildAction)
        {
            return buildAction.Invoke(new StateBuilder()).ToState();
        }

        protected class StateBuilder : IStateBuilder
        {
            private int _timeStep;
            private State _parentState;
            private CellKind[,] _cells;
            private readonly HashSet<Recipe> _goals;
            private readonly Dictionary<Position, IMergeable> _gameObjectPositions;
            private readonly Dictionary<AgentId, Position> _agentPositions;
            private readonly ISet<Recipe> _fulfilledRecipes = new HashSet<Recipe>();

            public StateBuilder()
            {
                _timeStep = 0;
                _parentState = null;
                _goals = new HashSet<Recipe>();
                _gameObjectPositions = new Dictionary<Position, IMergeable>();
                _agentPositions = new Dictionary<AgentId, Position>();
                _fulfilledRecipes = new HashSet<Recipe>();
            }

            public IStateBuilder OnMap(PredefinedGrid type)
            {
                switch (type)
                {
                    case PredefinedGrid.FullDivider:
                        _cells = GetGrid(FullDivider, out _);
                        break;
                    case PredefinedGrid.PartialDivider:
                        _cells = GetGrid(PartialDivider, out _);
                        break;
                    case PredefinedGrid.OpenDivider:
                        _cells = GetGrid(OpenDivider, out _);
                        break;
                }

                return this;
            }

            public IStateBuilder OnMap(string gridWorld)
            {
                _cells = GetGrid(gridWorld, out _);

                return this;
            }

            private static CellKind[,] GetGrid(string grid, out (int x, int y) dimensions)
            {
                var memStream = new MemoryStream();
                var streamWriter = new StreamWriter(memStream);

                streamWriter.Write(grid);
                streamWriter.Flush();
                memStream.Position = 0;

                var streamReader = new StreamReader(memStream);

                return LevelReader.ReadCells(streamReader, out dimensions);
            }

            public IStateBuilder SetTimeStep(int timeStep)
            {
                _timeStep = timeStep;

                return this;
            }

            public IStateBuilder WithParentState(State state)
            {
                _parentState = state;

                return this;
            }

            public IStateBuilder AddGoal(string recipeName)
            {
                _goals.Add(Recipe.GetRecipe(recipeName));

                return this;
            }

            public IStateBuilder WithAgent(int id, (int x, int y) location)
            {
                _agentPositions.Add(new AgentId(id), new Position(location));

                return this;
            }

            public IStateBuilder WithIngredient(string ingredientName, (int x, int y) location)
            {
                var ingredient = (Ingredient)typeof(Ingredient).GetMethod($"Get{ingredientName}").Invoke(null, null);

                _gameObjectPositions.Add(new Position(location), ingredient);

                return this;
            }

            public IStateBuilder WithFoodItem(string foodItemName, (int x, int y) location)
            {
                var foodItem = Recipe.GetRecipe(foodItemName).Product;

                _gameObjectPositions.Add(new Position(location), foodItem);

                return this;
            }

            public IStateBuilder WithAgentHeldIngredient(int id, string ingredientName, (int x, int y) location)
            {
                return WithAgent(id, location).WithIngredient(ingredientName, location);
            }

            public IStateBuilder WithAgentHeldFoodItem(int id, string foodItemName, (int x, int y) location)
            {
                return WithAgent(id, location).WithFoodItem(foodItemName, location);
            }

            public State ToState()
            {
                GlobalConfig.NumberOfAgents = _agentPositions.Count;
                var reachability = LevelReader.PrecomputeReachability(_cells, _agentPositions);
                var cellKindPositions = LevelReader.PrecomputeCellKindPositions(_cells);
                return new State(_timeStep,
                    _parentState,
                    _cells,
                    _gameObjectPositions,
                    _agentPositions,
                    reachability,
                    _fulfilledRecipes,
                    _goals,
                    cellKindPositions,
                    null);
            }
        }
    }
}