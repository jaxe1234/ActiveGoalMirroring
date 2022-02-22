namespace MACsharp.Structs
{
    public readonly struct AgentIdPair
    {
        public readonly AgentId Agent1;
        public readonly AgentId Agent2;

        public AgentIdPair(AgentId agent1, AgentId agent2)
        {
            Agent1 = agent1;
            Agent2 = agent2;
        }

        public bool Equals(AgentIdPair other)
        {
            return Agent1.Equals(other.Agent1) && Agent2.Equals(other.Agent2) ||
                   Agent2.Equals(other.Agent1) && Agent1.Equals(other.Agent2);
        }

        public override bool Equals(object obj)
        {
            return obj is AgentIdPair other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Agent1.GetHashCode() + Agent2.GetHashCode();
        }
    }
}
