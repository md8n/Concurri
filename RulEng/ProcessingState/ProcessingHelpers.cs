using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RulEng.States;

namespace RulEng.ProcessingState
{
    public static class ProcessingHelpers
    {
        //public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        //{
        //    return source == null 
        //        ? new HashSet<T>() 
        //        : new HashSet<T>(source);
        //}

        public static ImmutableHashSet<T> ToImmutableHashSet<T>(this IEnumerable<T> source)
        {
            return source == null 
                ? ImmutableHashSet.Create<T>() 
                : ImmutableHashSet.CreateRange(source);
        }

        public static ProcessingRulEngStore DeepClone(this RulEngStore rvStore)
        {
            var newStore = new ProcessingRulEngStore
            {
                Rules = rvStore?.Rules.ToHashSet(),
                RuleResults = rvStore?.RuleResults.ToHashSet(),
                Operations = rvStore?.Operations.ToHashSet(),
                Requests = rvStore?.Requests.ToHashSet(),
                Values = rvStore?.Values.ToHashSet()
            };

            return newStore;
        }

        public static RulEngStore DeepClone(this ProcessingRulEngStore rvStore)
        {
            var newStore = new RulEngStore
            {
                Rules = rvStore.Rules.ToImmutableHashSet(),
                RuleResults = rvStore.RuleResults.ToImmutableHashSet(),
                Operations = rvStore.Operations.ToImmutableHashSet(),
                Requests = rvStore.Requests.ToImmutableHashSet(),
                Values = rvStore.Values.ToImmutableHashSet()
            };

            return newStore;
        }
    }
}
