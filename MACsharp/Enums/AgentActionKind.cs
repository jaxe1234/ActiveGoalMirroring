namespace MACsharp.Enums
{
    using System.Linq;
    using System;
    using System.Collections.Generic;
    using Structs;

    public enum AgentActionKind
    {
        NoOp,
        MoveRight,
        MoveUp,
        MoveLeft,
        MoveDown,
    }

    public static class AgentActionKindHelper
    {
        private static readonly Dictionary<AgentActionKind, Position> ActionKindDirectionMap = new();

        private static readonly AgentActionKind[] Actions = Enum.GetValues<AgentActionKind>();

        static AgentActionKindHelper()
        {
            ActionKindDirectionMap.Add(AgentActionKind.NoOp, (0, 0));
            ActionKindDirectionMap.Add(AgentActionKind.MoveUp, (0, -1));
            ActionKindDirectionMap.Add(AgentActionKind.MoveDown, (0, 1));
            ActionKindDirectionMap.Add(AgentActionKind.MoveLeft, (-1, 0));
            ActionKindDirectionMap.Add(AgentActionKind.MoveRight, (1, 0));
        }

        public static Position GetDirection(this AgentActionKind actionKind)
        {
            return ActionKindDirectionMap[actionKind];
        }

        public static AgentActionKind GetAgentActionKind(Position direction)
        {
            return ActionKindDirectionMap.First(x => x.Value.Equals(direction)).Key;
        }

        public static AgentActionKind[] GetActionKinds()
        {
            return Actions;
        }
    }
}