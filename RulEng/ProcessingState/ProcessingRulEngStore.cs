using System.Collections.Generic;
using RulEng.States;

namespace RulEng.ProcessingState
{
    public class ProcessingRulEngStore
    {
        public HashSet<Rule> Rules { get; set; }

        public HashSet<RuleResult> RuleResults { get; set; }

        public HashSet<Operation> Operations { get; set; }

        public HashSet<Request> Requests { get; set; }

        public HashSet<Value> Values { get; set; }
    }
}
