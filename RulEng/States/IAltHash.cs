using Newtonsoft.Json;

namespace RulEng.States
{
    public interface IAltHash
    {
        [JsonIgnore]
        string GetAltHashCode { get; }
    }
}
