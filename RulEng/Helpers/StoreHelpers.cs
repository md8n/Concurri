using System.Collections.Generic;
using System.Linq;

using Redux;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class StoreHelpers
    {
        /// <summary>
        /// Add/Update definitions of any Entity Type - including RuleResult but excluding Value into the Store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="rules"></param>
        /// <param name="ruleResults"></param>
        /// <param name="operations"></param>
        /// <param name="requests"></param>
        /// <returns></returns>
        public static IStore<RulEngStore> AddUpdate(this IStore<RulEngStore> store, List<Rule> rules, List<RuleResult> ruleResults, List<Operation> operations, List<Request> requests)
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
                    var existingRule = storeRules.FirstOrDefault(rl => rl.RuleId == r.RuleId);

                    if (existingRule != null)
                    {
                        storeRules.Remove(existingRule);
                    }

                    storeRules.Add(r);
                }
            }

            if (ruleResults != null)
            {
                foreach (var rr in ruleResults)
                {
                    var existingRuleResult = storeRuleResults.FirstOrDefault(rl => rl.RuleResultId == rr.RuleResultId);

                    if (existingRuleResult != null)
                    {
                        storeRuleResults.Remove(existingRuleResult);
                    }

                    storeRuleResults.Add(rr);
                }
            }

            if (operations != null)
            {
                foreach (var o in operations)
                {
                    var existingOperation = storeOperations.FirstOrDefault(op => op.OperationId == o.OperationId);

                    if (existingOperation != null)
                    {
                        storeOperations.Remove(existingOperation);
                    }

                    storeOperations.Add(o);
                }
            }

            if (requests != null)
            {
                foreach (var q in requests)
                {
                    var existingRequest = storeRequests.FirstOrDefault(rq => rq.RequestId == q.RequestId);

                    if (existingRequest != null)
                    {
                        storeRequests.Remove(existingRequest);
                    }

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
        /// Add/Update single definitions of any Entity Type - including RuleResult but excluding Value into the Store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="rule"></param>
        /// <param name="ruleResult"></param>
        /// <param name="operation"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IStore<RulEngStore> AddUpdate(this IStore<RulEngStore> store, Rule rule, RuleResult ruleResult, Operation operation, Request request)
        {
            var storeState = store.GetState();

            var storeRules = storeState.Rules.ToHashSet();
            var storeRuleResults = storeState.RuleResults.ToHashSet();
            var storeOperations = storeState.Operations.ToHashSet();
            var storeRequests = storeState.Requests.ToHashSet();

            if (rule != null)
            {
                var existingRule = storeRules.FirstOrDefault(rl => rl.RuleId == rule.RuleId);

                if (existingRule != null)
                {
                    storeRules.Remove(existingRule);
                }

                storeRules.Add(rule);
            }

            if (ruleResult != null)
            {
                var existingRuleResult = storeRuleResults.FirstOrDefault(rl => rl.RuleResultId == ruleResult.RuleResultId);

                if (existingRuleResult != null)
                {
                    storeRuleResults.Remove(existingRuleResult);
                }

                storeRuleResults.Add(ruleResult);
            }

            if (operation != null)
            {
                var existingOperation = storeOperations.FirstOrDefault(op => op.OperationId == operation.OperationId);

                if (existingOperation != null)
                {
                    storeOperations.Remove(existingOperation);
                }

                storeOperations.Add(operation);
            }

            if (request != null)
            {
                var existingRequest = storeRequests.FirstOrDefault(rq => rq.RequestId == request.RequestId);

                if (existingRequest != null)
                {
                    storeRequests.Remove(existingRequest);
                }

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
