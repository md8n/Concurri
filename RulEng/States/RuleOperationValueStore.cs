using System.Collections.Immutable;
using System.Text;
using RulEng.Helpers;

namespace RulEng.States
{
    public class RuleOperationValueStore
    {
        public ImmutableHashSet<Rule> Rules { get; set; }

        public ImmutableHashSet<Operation> Operations { get; set; }

        public ImmutableHashSet<Value> Values { get; set; }

        public RuleOperationValueStore()
        {
            Rules = ImmutableHashSet.Create<Rule>();
            Operations = ImmutableHashSet.Create<Operation>();
            Values = ImmutableHashSet.Create<Value>();
        }

        /// <summary>
        /// Returns a semi-indented JSON version of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var jBlocks = new string[3];

            jBlocks[0] = Rules.ToJson("rules");
            jBlocks[1] = Operations.ToJson("operations");
            jBlocks[2] = Values.ToJson("values");

            var jObj = new StringBuilder();
            jObj.AppendLine("{");
            jObj.Append(string.Join(",", jBlocks));
            jObj.AppendLine("}");

            return jObj.ToString();
        }
    }
}
