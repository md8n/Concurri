using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Redux;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class StoreHelpers
    {
        /// <summary>
        /// Add definitions of any Entity Type - including RuleResult but excluding Value into the Store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="rules"></param>
        /// <param name="ruleResults"></param>
        /// <param name="operations"></param>
        /// <param name="requests"></param>
        /// <returns></returns>
        public static IStore<RulEngStore> Add(this IStore<RulEngStore> store, List<Rule> rules, List<RuleResult> ruleResults, List<Operation> operations, List<Request> requests)
        {
            var storeState = store.GetState();

            var storeRules = storeState.Rules.ToHashSet();
            var storeRuleResults = storeState.RuleResults.ToHashSet();
            var storeOperations = storeState.Operations.ToHashSet();
            var storeRequests = storeState.Requests.ToHashSet();

            if (rules != null)
            {
                foreach (var r in rules)
                {
                    storeRules.Add(r);
                }
            }

            if (ruleResults != null)
            {
                foreach (var rr in ruleResults)
                {
                    storeRuleResults.Add(rr);
                }
            }

            if (operations != null)
            {
                foreach (var o in operations)
                {
                    storeOperations.Add(o);
                }
            }

            if (requests != null)
            {
                foreach (var q in requests)
                {
                    storeRequests.Add(q);
                }
            }

            store.GetState().Rules = storeRules.ToImmutableHashSet();
            store.GetState().RuleResults = storeRuleResults.ToImmutableHashSet();
            store.GetState().Operations = storeOperations.ToImmutableHashSet();
            store.GetState().Requests = storeRequests.ToImmutableHashSet();

            return store;
        }

        /// <summary>
        /// Add single definitions of any Entity Type - including RuleResult but excluding Value into the Store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="rule"></param>
        /// <param name="ruleResult"></param>
        /// <param name="operation"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IStore<RulEngStore> Add(this IStore<RulEngStore> store, Rule rule, RuleResult ruleResult, Operation operation, Request request)
        {
            var storeState = store.GetState();

            var storeRules = storeState.Rules.ToHashSet();
            var storeRuleResults = storeState.RuleResults.ToHashSet();
            var storeOperations = storeState.Operations.ToHashSet();
            var storeRequests = storeState.Requests.ToHashSet();

            if (rule != null)
            {
                storeRules.Add(rule);
            }

            if (ruleResult != null)
            {
                storeRuleResults.Add(ruleResult);
            }

            if (operation != null)
            {
                storeOperations.Add(operation);
            }

            if (request != null)
            {
                storeRequests.Add(request);
            }

            store.GetState().Rules = storeRules.ToImmutableHashSet();
            store.GetState().RuleResults = storeRuleResults.ToImmutableHashSet();
            store.GetState().Operations = storeOperations.ToImmutableHashSet();
            store.GetState().Requests = storeRequests.ToImmutableHashSet();

            return store;
        }

    }
}
