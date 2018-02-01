using System;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RulEng.States
{
    public class RuleCompare : IRulePrescription
    {
        /// <summary>
        /// The Id of the RuleResult that will receive the result of the Compare Rule
        /// </summary>
        public Guid RuleResultId { get; set; }

        public int MinEntitiesRequired => 2;

        /// <summary>
        /// The Ids of all of the Values that will be used in calculating the result of the Compare Rule
        /// </summary>
        public ImmutableList<ITypeKey> EntityIds { get; set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString(Formatting.None);
        }
    }
}
