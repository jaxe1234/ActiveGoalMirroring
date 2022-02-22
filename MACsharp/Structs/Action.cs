namespace MACsharp.Structs
{
    using Enums;
    public struct Action
    {
        public AgentActionKind Kind;

        public Action(AgentActionKind agentActionKind)
        {
            Kind = agentActionKind;
        }

        public Action(Position actionDirection)
        {
            Kind = AgentActionKindHelper.GetAgentActionKind(actionDirection);
        }

        public override string ToString()
        {
            return Kind.GetDirection().ToString();
        }
    }
}