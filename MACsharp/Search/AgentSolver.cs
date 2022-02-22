namespace MACsharp.Search
{
    using System;
    using System.Collections.Generic;
    using Structs;
    using Helpers;
    using System.Linq;
    using Action = Structs.Action;

    public abstract class AgentSolver
    {
        protected AgentId AgentId { get; set; }
        protected List<AgentId> AllAgents { get; }
        protected State CurrentState { get; private set; }
        protected State InitialState { get; private set; }
        protected readonly List<AgentObservations> AgentObservationsList = new();
        protected readonly List<AgentId> OtherAgents = new();

        protected AgentSolver(AgentId id, State initialState)
        {
            AgentId = id;
            InitialState = initialState.DeepCopy();
            CurrentState = initialState.DeepCopy();
            InitializeObservations();
            InitializeOtherAgents();
            AllAgents = InitialState.AgentPositions.Select(x => x.Key).ToList();
        }

        private void InitializeOtherAgents()
        {
            foreach (var agentId in InitialState.AgentPositions.Keys)
            {
                if(agentId.Equals(AgentId)) continue;
                OtherAgents.Add(agentId);
            }
        }

        private void InitializeObservations()
        {
            for (var i = 0; i < CurrentState.AgentPositions.Count; i++)
            {
                AgentObservationsList.Add(new AgentObservations(i));
            }
        }

        public abstract Action GetNextAction();

        public virtual void UpdateState(Dictionary<AgentId, Action> agentActionAssignment)
        {
            if (CurrentState.TryPerform(agentActionAssignment, out var newState, GlobalConfig.Print, true))
            {
                CurrentState = newState.DeepCopy();
            }
            else
            {
                throw new Exception("Failed to update state.");
            }

            foreach (var (agent, action) in agentActionAssignment){

                AgentObservationsList[agent.Id].Add(action);
            }
        }
    }
}
