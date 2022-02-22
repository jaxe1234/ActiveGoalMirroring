namespace MACsharp
{
    using System.Collections.Generic;
    using Structs;

    public class AgentPossibleActions : List<Action>
    {
        public AgentId Id { get; set; }

        public AgentPossibleActions() { }

        public AgentPossibleActions(IEnumerable<Action> actions, AgentId id) : base(actions)
        {
            Id = id;
        }
    }
}
