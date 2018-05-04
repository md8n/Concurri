using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulEng.Helpers;

namespace RulEng.States
{
    public class RuleResult : IEquatable<RuleResult>, IEntity, IAltHash
    {
        public Guid RuleResultId { get; set; }

        public Guid RuleId { get; set; }

        [JsonIgnore]
        public Guid EntityId { get => RuleResultId; set => RuleResultId = value; }

        public EntityType EntType => EntityType.RuleResult;

        public List<string> EntTags { get; set; }

        public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

        public bool Detail { get; set; }

        public RuleResult() : this(false)
        {
        }

        public RuleResult(bool jToken, List<string> entTags = null)
        {
            RuleResultId = Guid.NewGuid();
            EntTags = entTags;
            Detail = jToken;
        }

        public RuleResult(Rule rule, List<string> entTags = null)
        {
            RuleResultId = Guid.NewGuid();
            EntTags = entTags != null && entTags.Any() ? entTags : rule.EntTags;
            RuleId = rule.RuleId;
            Detail = false;
        }

        [JsonIgnore]
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
            return obj is Value a && Equals(a);
        }

        /// <inheritdoc />
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

        public static implicit operator TypeKey(RuleResult ruleResult)
        {
            return new TypeKey { EntityId = ruleResult.RuleResultId, EntType = EntityType.RuleResult, EntTags = ruleResult.EntTags, LastChanged = ruleResult.LastChanged };
        }
    }
}
