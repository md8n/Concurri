using System;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RulEng.States
{
    public class Rule : IEquatable<Rule>, IEntity, IAltHash
    {
        public Guid RuleId { get; set; }

        public Guid EntityId { get => RuleId; set => RuleId = value; }

        public string RuleName { get; set; }

        public RuleType RuleType { get; set; }

        public bool NegateResult { get; set; }

        public ImmutableArray<ITypeKey> ReferenceValues { get; set; } = new ImmutableArray<ITypeKey>();

        /// <summary>
        /// The last time this rule was executed
        /// </summary>
        public DateTime LastExecuted { get; set; } = new DateTime(1980, 1, 1);

        public string GetAltHashCode => RuleId.ToString();

        public Rule()
        {
            LastExecuted = new DateTime(1980, 1, 1);
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
            var a = obj as Rule;

            return a != null && Equals(a);
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
            return new TypeKey() { EntityId = rule.RuleId, EntityType = EntityType.Rule };
        }
    }
}
