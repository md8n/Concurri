using System.Collections.Immutable;

namespace RulEng.States
{
    public interface IRulEngStore
    {
        ImmutableHashSet<Rule> Rules { get; set; }

        ImmutableHashSet<RuleResult> RuleResults { get; set; }

        ImmutableHashSet<Operation> Operations { get; set; }

        ImmutableHashSet<Request> Requests { get; set; }

        ImmutableHashSet<Value> Values { get; set; }
    }
}
