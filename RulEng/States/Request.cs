using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using GraphQL.Types;

using RulEng.Helpers;

namespace RulEng.States;

public class Request : BaseExecutableEntity, IEquatable<Request>, IEntity, IAltHash
{
    public Guid RequestId { get; set; }

    [JsonIgnore]
    public Guid EntityId { get => RequestId; set => RequestId = value; }

    public EntityType EntType => EntityType.Request;

    public List<string> EntTags { get; set; }

    public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

    public Guid RuleResultId { get; set; }

    public JsonValueKind ValueKind { get; set; }

    public IObjectGraphType Query { get; set; }

    public Request()
    {
        // TODO:
        //if (ReferenceValueIds.IsDefault)
        //{
        //    ReferenceValueIds = ImmutableArray<ImmutableArray<Guid>>.Empty;
        //}
    }

    [JsonIgnore]
    public string GetAltHashCode => RequestId.ToString();

    public override int GetHashCode()
    {
        return RequestId.GetHashCode();
    }

    /// <summary>
    /// Shallow equality - only compares the Id
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        return obj is Operation a && Equals(a);
    }

    /// <inheritdoc />
    /// <summary>
    /// Shallow equality - only compares the Id
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Request other)
    {
        return GetHashCode() == other.GetHashCode();
    }

    /// <summary>
    /// Returns a non-indented JSON version of this object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, refLoopOptions);
    }

    public static implicit operator TypeKey(Request request)
    {
        return new TypeKey { EntityId = request.RequestId, EntType = EntityType.Request, EntTags = request.EntTags, LastChanged = request.LastChanged };
    }

    private static readonly JsonSerializerOptions refLoopOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
}
