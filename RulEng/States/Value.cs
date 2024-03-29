using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using RulEng.Helpers;

namespace RulEng.States;

public class Value : IEquatable<Value>, IEntity, IAltHash
{
    public Guid ValueId { get; set; }

    [JsonIgnore]
    public Guid EntityId { get => ValueId; set => ValueId = value; }

    public EntityType EntType => EntityType.Value;

    public List<string> EntTags { get; set; }

    public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

    public JsonNode Detail { get; set; }

    public Value() : this(null, null)
    {
    }

    public Value(List<string> entTags) : this(null, entTags)
    {
    }

    public Value(JsonNode jToken, List<string> entTags = null) {
        ValueId = GuidHelpers.NewTimeUuid();
        EntTags = entTags;
        Detail = jToken.DeepClone();
    }

    //public Value(object jToken, List<string> entTags = null)
    //{
    //    ValueId = GuidHelpers.NewTimeUuid();
    //    EntTags = entTags;
    //    try
    //    {
    //        Detail = JsonNode.Parse(jToken.ToString());
    //    }
    //    catch
    //    {
    //        //Detail = new JsonValue();
    //    }
    //}

    public Value(bool jToken, List<string> entTags = null)
    {
        ValueId = GuidHelpers.NewTimeUuid();
        EntTags = entTags;
        try
        {
            Detail = JsonNode.Parse(jToken.ToString());
        }
        catch
        {
            //Detail = new JsonValue();
        }
    }

    public Value(int jToken, List<string> entTags = null)
    {
        ValueId = GuidHelpers.NewTimeUuid();
        EntTags = entTags;
        try
        {
            Detail = JsonNode.Parse(jToken.ToString());
        }
        catch
        {
            //Detail = new JsonValue();
        }
    }

    public Value(string jToken, List<string> entTags = null)
    {
        ValueId = GuidHelpers.NewTimeUuid();
        EntTags = entTags;
        try
        {
            Detail = JsonNode.Parse(jToken);
        }
        catch
        {
            //Detail = new JsonValue();
        }
    }

    [JsonIgnore]
    public string GetAltHashCode => ValueId.ToString();

    public override int GetHashCode()
    {
        return ValueId.GetHashCode();
    }

    /// <summary>
    /// Shallow equality - only compares the Id
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        return obj is Value a && Equals(a);
    }

    /// <summary>
    /// Shallow equality - only compares the Id
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Value other)
    {
        return GetHashCode() == other.GetHashCode();
    }

    /// <summary>
    /// Returns a non-indented JSON version of this object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public static implicit operator TypeKey (Value value)
    {
        return new TypeKey { EntityId = value.ValueId, EntType = EntityType.Value, EntTags = value.EntTags, LastChanged = value.LastChanged };
    }
}
