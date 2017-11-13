using System.Collections.Immutable;
using System.Text;
using RulEng.Helpers;

namespace RulEng.States
{
    public class RulEngStore : IRulEngStore
    {
        public ImmutableHashSet<Rule> Rules { get; set; }

        public ImmutableHashSet<RuleResult> RuleResults { get; set; }

        public ImmutableHashSet<Operation> Operations { get; set; }

        public ImmutableHashSet<Request> Requests { get; set; }

        public ImmutableHashSet<Value> Values { get; set; }

        public RulEngStore()
        {
            Rules = ImmutableHashSet.Create<Rule>();
            RuleResults = ImmutableHashSet.Create<RuleResult>();
            Operations = ImmutableHashSet.Create<Operation>();
            Requests = ImmutableHashSet.Create<Request>();
            Values = ImmutableHashSet.Create<Value>();
        }

        /// <summary>
        /// Returns a semi-indented JSON version of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var jBlocks = new string[5];

            jBlocks[0] = Rules.ToJson("rules");
            jBlocks[1] = RuleResults.ToJson("ruleResults");
            jBlocks[2] = Operations.ToJson("operations");
            jBlocks[3] = Requests.ToJson("requests");
            jBlocks[4] = Values.ToJson("values");

            var jObj = new StringBuilder();
            jObj.AppendLine("{");
            jObj.Append(string.Join(",", jBlocks));
            jObj.AppendLine("}");

            return jObj.ToString();
        }
    }
}
