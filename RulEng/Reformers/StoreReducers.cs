using System.Collections.Immutable;
using Redux;
using RulEng.Prescriptions;
using RulEng.States;

namespace RulEng.Reformers
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

            if (action is ICrud crud)
            {
                return new RulEngStore
                {
                    Rules = CrudReducers.CrudReducer(state.Rules, crud),
                    Operations = CrudReducers.CrudReducer(state.Operations, crud),
                    Requests = CrudReducers.CrudReducer(state.Requests, crud),
                    Values = CrudReducers.CrudReducer(state.Values, crud)
                };
            }

            if (!(action is IProcessing))
            {
                return state;
            }

            switch (action)
            {
                case IRuleProcessing rulesAction:
                    return ProcessingReducers.ProcessAllRulesReducer(state, rulesAction);
                case IOpReqProcessing opReqAction:
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
