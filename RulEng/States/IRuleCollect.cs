using System;
using System.Collections.Immutable;

namespace RulEng.States
{
    public interface IRuleCollect
    {
        ImmutableList<Guid> RuleResultIds { get; set; }
    }
}
