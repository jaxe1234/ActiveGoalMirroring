namespace MACsharp.Search
{
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HierarchicalPlannerSearchNode : IEquatable<HierarchicalPlannerSearchNode>
    {
        public List<SearchNode> Plan { get; }
        public State State { get; }
        public List<string> SubgoalOrder { get; }

        public HierarchicalPlannerSearchNode(List<SearchNode> plan, State state, List<string> subgoalOrder)
        {
            Plan = plan;
            State = state;
            SubgoalOrder = subgoalOrder;
        }
        
        public bool Equals(HierarchicalPlannerSearchNode other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return SubgoalOrder.SequenceEqual(other.SubgoalOrder);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((HierarchicalPlannerSearchNode)obj);
        }

        public override int GetHashCode() => SubgoalOrder.GetSequenceHashCode();
    }
}
