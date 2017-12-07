using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RulEng.States
{
    public class Value : IEquatable<Value>, IEntity, IAltHash
    {
        public Guid ValueId { get; set; }

        public Guid EntityId { get => ValueId; set => ValueId = value; }

        public EntityType Type { get => EntityType.Value; }

        public DateTime LastChanged { get; set; } = new DateTime(1980, 1, 1);

        public JToken Detail { get; set; }

        public Value() : this(null)
        {
        }

        public Value(object jToken)
        {
            ValueId = Guid.NewGuid();
            try
            {
                Detail = JToken.Parse(jToken.ToString());
            }
            catch
            {
                Detail = new JObject();
            }
        }

        public Value(bool jToken)
        {
            ValueId = Guid.NewGuid();
            try
            {
                Detail = JToken.Parse(jToken.ToString());
            }
            catch
            {
                Detail = new JObject();
            }
        }

        public Value(int jToken)
        {
            ValueId = Guid.NewGuid();
            try
            {
                Detail = JToken.Parse(jToken.ToString());
            }
            catch
            {
                Detail = new JObject();
            }
        }

        public Value(JToken jToken)
        {
            ValueId = Guid.NewGuid();
            Detail = jToken;
        }

        public Value(string jToken)
        {
            ValueId = Guid.NewGuid();
            try
            {
                Detail = JToken.Parse(jToken);
            }
            catch
            {
                Detail = new JObject();
            }
        }

        public string GetAltHashCode => ValueId.ToString();

        public override int GetHashCode()
        {
            return ValueId.GetHashCode();
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
        public bool Equals(Value other)
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

        public static implicit operator TypeKey (Value value)
        {
            return new TypeKey() { EntityId = value.ValueId, EntityType = EntityType.Value, LastChanged = value.LastChanged };
        }
    }
}
