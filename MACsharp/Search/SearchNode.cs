namespace MACsharp.Search
{
    using System;
    using Structs;
    using System.Collections.Generic;
    using System.Text;
    using Action = Structs.Action;

    /// <summary>
    /// Result state is the parent's state after performing the joint action
    /// </summary>
    public class SearchNode : IEquatable<SearchNode>
    {
        public readonly SearchNode Parent;
        public readonly State ResultState;
        public readonly Dictionary<AgentId, Action> ActionAssignment;
        public long? Cost = null;

        public SearchNode(SearchNode parent, State resultState, Dictionary<AgentId, Action> actionAssignment)
        {
            Parent = parent;
            ResultState = resultState;
            ActionAssignment = actionAssignment;
        }
        
        public bool ContainsRecipe(Recipe recipe) => ResultState.IsRecipeFulfilled(recipe);

        public bool Equals(SearchNode other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ResultState, other.ResultState);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SearchNode)obj);
        }

        public override int GetHashCode()
        {
            return ResultState.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var (agent, action) in ActionAssignment)
            {
                sb.AppendFormat("Agent{0}: {1}", agent.Id, action.Kind.ToString());
            }

            sb.Append(ResultState);

            return sb.ToString();
        }
    }
}