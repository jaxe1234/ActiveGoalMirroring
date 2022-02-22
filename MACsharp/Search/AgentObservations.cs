namespace MACsharp.Search
{
    using System.Collections.Generic;
    using Structs;

    public class AgentObservations : List<Action>
    {
        public AgentId ObservedAgentId { get; set; }

        public AgentObservations(int agentId)
        {
            ObservedAgentId = new AgentId(agentId);
        }

        public AgentObservations(AgentId observedAgentId)
        {
            ObservedAgentId = observedAgentId;
        }

        public AgentObservations(AgentId observedAgentId, IEnumerable<Action> source) : base(source)
        {
            ObservedAgentId = observedAgentId;
        }

        public AgentObservations() { }
    }
}