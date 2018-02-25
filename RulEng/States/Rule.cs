using System;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulEng.Helpers;

namespace RulEng.States
{
    public class Rule : IEquatable<Rule>, IEntity, IAltHash
    {
        public Guid RuleId { get; set; }

        public Guid EntityId { get => RuleId; set => RuleId = value; }

        public EntityType EntType => EntityType.Rule;

        public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

        public string RuleName { get; set; }

        public RuleType RuleType { get; set; }

        public bool NegateResult { get; set; }

        public IRulePrescription ReferenceValues { get; set; }

        /// <summary>
        /// The last time this rule was executed
        /// </summary>
        public DateTime LastExecuted { get; set; } = DefaultHelpers.DefDate();

        public string GetAltHashCode => RuleId.ToString();

        public Rule()
        {
            LastExecuted = DefaultHelpers.DefDate();
        }

        public override int GetHashCode()
        {
            return RuleId.GetHashCode();
        }

        /// <summary>
        /// Shallow equality - only compares the Id
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is Rule a && Equals(a);
        }

        /// <summary>
        /// Shallow equality - only compares the Id
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Rule other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        /// Returns a non-indented JSON version of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JObject.FromObject(this).ToString(Formatting.None);
        }

        public static implicit operator TypeKey(Rule rule)
        {
            return new TypeKey { EntityId = rule.RuleId, EntType = EntityType.Rule, LastChanged = rule.LastChanged };
        }
    }
}
