using System.Collections.Generic;
using RulEng.States;

namespace RulEng.ProcessingState
{
    public class ProcessingRuleOperationValueStore
    {
        public HashSet<Rule> Rules { get; set; }

        public HashSet<Operation> Operations { get; set; }

        public HashSet<Value> Values { get; set; }
    }
}
