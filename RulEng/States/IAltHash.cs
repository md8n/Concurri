using System.Text.Json.Serialization;

namespace RulEng.States;

public interface IAltHash
{
    [JsonIgnore]
    string GetAltHashCode { get; }
}
