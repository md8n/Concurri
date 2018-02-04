using System;
using GraphQL.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RulEng.States
{
    public class Request : IEquatable<Request>, IEntity, IAltHash
    {
        public Guid RequestId { get; set; }

        public Guid EntityId { get => RequestId; set => RequestId = value; }

        public EntityType Type => EntityType.Request;

        public DateTime LastChanged { get; set; } = new DateTime(1980, 1, 1);

        public Guid RuleResultId { get; set; }

        public JTokenType ValueType { get; set; }

        public IObjectGraphType Query { get; set; }

        public Request()
        {
            // TODO:
            //if (ReferenceValueIds.IsDefault)
            //{
            //    ReferenceValueIds = ImmutableArray<ImmutableArray<Guid>>.Empty;
            //}
        }

        public string GetAltHashCode => RequestId.ToString();

        public override int GetHashCode()
        {
            return RequestId.GetHashCode();
        }

        /// <summary>
        /// Shallow equality - only compares the Id
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is Operation a && Equals(a);
        }

        /// <summary>
        /// Shallow equality - only compares the Id
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Request other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        /// Returns a non-indented JSON version of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var serl = new JsonSerializer {ReferenceLoopHandling = ReferenceLoopHandling.Ignore};

            return JObject.FromObject(this, serl).ToString(Formatting.None);
        }

        public static implicit operator TypeKey(Request request)
        {
            return new TypeKey { EntityId = request.RequestId, EntityType = EntityType.Request, LastChanged = request.LastChanged };
        }
    }
}
