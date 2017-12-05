using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RulEng.States
{
    public class RuleResult : IEquatable<RuleResult>, IEntity, IAltHash
    {
        public Guid RuleResultId { get; set; }

        public Guid RuleId { get; set; }

        public Guid EntityId { get => RuleResultId; set => RuleResultId = value; }

        public EntityType Type { get => EntityType.RuleResult; }

        public DateTime LastChanged { get; set; } = new DateTime(1980, 1, 1);

        public bool Detail { get; set; }

        public RuleResult() : this(false)
        {
        }

        public RuleResult(bool jToken)
        {
            RuleResultId = Guid.NewGuid();
            Detail = jToken;
        }

        public RuleResult(Rule rule)
        {
            RuleResultId = Guid.NewGuid();
            RuleId = rule.RuleId;
            Detail = false;
        }

        public string GetAltHashCode => RuleResultId.ToString();

        public override int GetHashCode()
        {
            return RuleResultId.GetHashCode();
        }

        /// <summary>
        /// Shallow equality - only compares the Id
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var a = obj as Value;

            return a != null && Equals(a);
        }

        /// <summary>
        /// Shallow equality - only compares the Id
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(RuleResult other)
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
    }
}
