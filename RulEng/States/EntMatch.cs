using System;

namespace RulEng.States
{
    /// <summary>
    /// A simple class used when matching Entities by their type and Id is required
    /// </summary>
    public class EntMatch
    {
        public Guid EntityId { get; set; }

        public EntityType EntType { get; set; }
    }
}
