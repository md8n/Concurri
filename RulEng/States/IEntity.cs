using System;

namespace RulEng.States
{
    public interface IEntity
    {
        Guid EntityId { get; set; }

        EntityType EntType { get; }

        DateTime LastChanged { get; set; }
    }
}
