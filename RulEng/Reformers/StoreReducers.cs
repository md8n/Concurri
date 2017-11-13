using System.Collections.Immutable;
using Redux;
using RulEng.Prescriptions;
using RulEng.States;

namespace RulEng.Reducers
{
    public static class StoreReducers
    {
        public static RulEngStore ReduceStore(RulEngStore state, IAction action)
        {
            if (state == null)
            {
                state = new RulEngStore();
            }

            // This is where CQRS like behaviour hits
            // Actions that only target one of the 'tables' (e.g. CRUD) are passed the specific item impacted
            // Actions that target multiple 'tables' (e.g. Processing) are passed the whole structure

            var crud = action as ICrud;
            if (crud != null)
            {
                return new RulEngStore
                {
                    Rules = CrudReducers.CrudReducer<Rule>(state.Rules, crud),
                    Operations = CrudReducers.CrudReducer<Operation>(state.Operations, crud),
                    Requests = CrudReducers.CrudReducer<Request>(state.Requests, crud),
                    Values = CrudReducers.CrudReducer<Value>(state.Values, crud)
                };
            }

            if (!(action is IProcessing))
            {
                return state;
            }

            var rulesAction = action as IRuleProcessing;
            if (rulesAction != null)
            {
                return ProcessingReducers.ProcessAllRulesReducer(state, rulesAction);
            }

            var opReqAction = action as IOpReqProcessing;
            if (opReqAction != null)
            {
                return ProcessingReducers.ProcessAllOperationsReducer(state, opReqAction);
            }

            return state;
        }

        public static ImmutableHashSet<Value> ReduceValues(ImmutableHashSet<Value> state, IAction action)
        {
            //if (action is ReceiveRepositoriesAction)
            //{
            //    return false;
            //}

            //if (action is SearchRepositoriesAction)
            //{
            //    return true;
            //}

            return state;
        }
    }
}
