using RulEng.Helpers;
using System;
using System.Collections.Immutable;

namespace RulEng.States
{
    public class OperandKey : ITypeKey
    {
        /// <summary>
        /// An array of source Value Ids that will be used for performing this Operation
        /// </summary>
        public ImmutableArray<Guid> SourceValueIds { get; set; }

        /// <summary>
        /// The type of Entity the result will be written to
        /// </summary>
        public EntityType EntityType { get; set; }

        /// <summary>
        /// The Id of the Entity the result will be written to.
        /// If the Entity does not exist it will be created with this Id
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Date this OperandKey was created or last changed
        /// </summary>
        public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();
    }
}
