using RulEng.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace RulEng.States
{
    /// <summary>
    /// Identifies all of the relevant information required by an Operation or Request
    /// </summary>
    public class OperandKey : IEntity
    {
        /// <summary>
        /// An array of source Value Ids that will be used for performing this Operation
        /// </summary>
        /// <remarks>Not required for Search operations</remarks>
        public ImmutableArray<Guid> SourceValueIds { get; set; }

        /// <summary>
        /// The type of Entity the result will be written to
        /// </summary>
        public EntityType EntType { get; set; }

        /// <summary>
        /// The Tags that will be written to the resulting Entity
        /// </summary>
        public List<string> EntTags { get; set; }

        /// <summary>
        /// The Id of the Entity the result will be written to.
        /// If the Entity does not exist it will be created with this Id
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Date this OperandKey was created or last changed
        /// </summary>
        public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

        /// <summary>
        /// Implicitly convert an OperandKey to a TypeKey
        /// </summary>
        /// <param name="ok"></param>
        public static implicit operator TypeKey(OperandKey ok)
        {
            return new TypeKey {EntityId = ok.EntityId, EntType = ok.EntType, EntTags = ok.EntTags, LastChanged = ok.LastChanged};
        }
    }
}
