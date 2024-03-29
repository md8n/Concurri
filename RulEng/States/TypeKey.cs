using System;
using System.Collections.Generic;
using System.Text.Json;

using RulEng.Helpers;

namespace RulEng.States;

/// <summary>
/// An instantiable implementation of the IEntity interface
/// </summary>
public class TypeKey : IEntity
{
    public EntityType EntType { get; set; }

    public List<string> EntTags { get; set; }

    public Guid EntityId { get; set; }

    public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
