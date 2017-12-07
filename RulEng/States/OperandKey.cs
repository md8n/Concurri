using System;
using System.Collections.Immutable;

namespace RulEng.States
{
    public class OperandKey : ITypeKey
    {
        public ImmutableArray<Guid> SourceValueIds { get; set; }
        public EntityType EntityType { get; set; }
        public Guid EntityId { get; set; }
        public DateTime LastChanged { get; set; } = new DateTime(1980, 1, 1);
    }
}
