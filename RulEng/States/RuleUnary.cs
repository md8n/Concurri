using System;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RulEng.States
{
    public class RuleUnary : IRulePrescription
    {
        /// <inheritdoc />
        /// <summary>
        /// The Id of the RuleResult that will receive the result of the Unary Rule
        /// </summary>
        public Guid RuleResultId { get; set; }

        public int MinEntitiesRequired => 1;

        /// <inheritdoc />
        /// <summary>
        /// The Ids of all of the RuleResults that will be used in calculating the result of the Unary Rule
        /// </summary>
        public ImmutableList<ITypeKey> EntityIds { get; set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString(Formatting.None);
        }
    }
}
