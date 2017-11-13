using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RulEng.States
{
    public class RuleCompare : IRuleCompare
    {
        public Guid ValueAId { get; set; }
        public Guid ValueBId { get; set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString(Formatting.None);
        }
    }
}
