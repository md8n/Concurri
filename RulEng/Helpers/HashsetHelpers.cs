using RulEng.States;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RulEng.Helpers
{
    public static class HashSetHelpers
    {
        /// <summary>
        /// Returns a semi-indented JSON version of this object
        /// </summary>
        /// <returns></returns>
        public static string ToJson<T>(this ImmutableHashSet<T> hashSet, string name) where T : IAltHash
        {
            var jObj = new StringBuilder();

            jObj.AppendLine($"\"{name}\":{{");
            jObj.AppendLine(string.Join(",", hashSet.Select(r => $"\"{r.GetAltHashCode}\": {r}")));
            jObj.AppendLine("}");

            return jObj.ToString();
        }
    }
}
