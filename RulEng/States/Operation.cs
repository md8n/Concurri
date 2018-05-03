using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulEng.Helpers;

namespace RulEng.States
{
    public class Operation : BaseExecutableEntity, IEquatable<Operation>, IEntity, IAltHash
    {
        public Guid OperationId { get; set; }

        [JsonIgnore]
        public Guid EntityId { get => OperationId; set => OperationId = value; }

        public EntityType EntType => EntityType.Operation;

        public List<string> EntTags { get; set; }

        public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

        public Guid RuleResultId { get; set; }

        public ImmutableArray<OperandKey> Operands { get; set; }

        public OperationType OperationType { get; set; } = OperationType.CreateUpdate;

        public string OperationTemplate { get; set; }

        public Operation ()
        {
            CleanOperands();
        }

        [JsonIgnore]
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

            CleanOperands();

            return JObject.FromObject(this, serl).ToString(Formatting.None);
        }

        public static implicit operator TypeKey(Operation operation)
        {
            return new TypeKey { EntityId = operation.OperationId, EntType = EntityType.Operation, EntTags = operation.EntTags, LastChanged = operation.LastChanged };
        }

        private void CleanOperands()
        {
            if (Operands.IsDefault)
            {
                Operands = ImmutableArray<OperandKey>.Empty;
            }
        }
    }
}
