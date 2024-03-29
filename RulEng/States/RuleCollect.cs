﻿using System;
using System.Collections.Immutable;
using System.Text.Json;

namespace RulEng.States;

public class RuleCollect : IRulePrescription
{
    /// <inheritdoc />
    /// <summary>
    /// The Id of the RuleResult that will receive the result of the Collection Rule
    /// </summary>
    public Guid RuleResultId { get; set; }

    public int MinEntitiesRequired => 2;

    /// <inheritdoc />
    /// <summary>
    /// The Ids of all of the RuleResults that will be used in calculating the result of the Collection Rule
    /// </summary>
    public ImmutableList<IEntity> EntityIds { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
