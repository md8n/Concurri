using System;
using System.Collections.Generic;
using System.Linq;

namespace RulEng.States;

/// <summary>
/// A simple class used when Entities need to be matched by their type and Id
/// </summary>
public class EntityMatch : IEntityMatch {
    /// <summary>
    /// Id of the Entity
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// EntityType of the Entity
    /// </summary>
    public EntityType EntType { get; set; }
}

public static class EntitiesMatch {
    /// <summary>
    /// In the supplied list of potential entity matches, are there any matches to the supplied entity?
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="ent"></param>
    /// <returns></returns>
    public static bool Any(this List<EntityMatch> entities, IEntityMatch ent) {
        return entities.Any(ad => ad.EntType == ent.EntType && ad.EntityId == ent.EntityId);
    }

    /// <summary>
    /// In the supplied list of potential entity matches, is there no match to the supplied entity?
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="ent"></param>
    /// <returns></returns>
    public static bool None(this List<EntityMatch> entities, IEntityMatch ent) {
        return !entities.Any(ent);
    }
}
