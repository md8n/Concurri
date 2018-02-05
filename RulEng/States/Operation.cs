using System;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulEng.Helpers;

namespace RulEng.States
{
    public enum OperationType
    {
        Error = -1,
        Unknown = 0,
        CreateUpdate,
        Delete
    }

    public class Operation : IEquatable<Operation>, IEntity, IAltHash
    {
        public Guid OperationId { get; set; }

        public Guid EntityId { get => OperationId; set => OperationId = value; }

        public EntityType Type => EntityType.Operation;

        public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

        public Guid RuleResultId { get; set; }

        public ImmutableArray<OperandKey> Operands { get; set; }

        public OperationType OperationType { get; set; } = OperationType.CreateUpdate;

        public Operation ()
        {
            if (Operands.IsDefault)
            {
                Operands = ImmutableArray<OperandKey>.Empty;
            }
        }

        public string GetAltHashCode => OperationId.ToString();

        public override int GetHashCode()
        {
            return OperationId.GetHashCode();
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
        public bool Equals(Operation other)
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

        public static implicit operator TypeKey(Operation operation)
        {
            return new TypeKey { EntityId = operation.OperationId, EntityType = EntityType.Operation, LastChanged = operation.LastChanged };
        }
    }
}
