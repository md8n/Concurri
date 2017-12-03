using System;

namespace RulEng.States
{
    public interface ITypeKey
    {
        EntityType EntityType { get; set; }
        Guid EntityId { get; set; }
    }
}
