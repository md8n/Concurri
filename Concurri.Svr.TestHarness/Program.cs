using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Redux;
using RulEng.Helpers;
using RulEng.Reformers;
using RulEng.States;

namespace Concurri.Svr.TestHarness
{
    public class Program
    {
        public static IStore<RulEngStore> rvStore { get; private set; }

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var rules = new List<Rule>();
            var ruleResults = new List<RuleResult>();
            var operations = new List<Operation>();
            var values = new List<Value>();
            var rulePrescriptions = new List<IAction>();
            var operationPrescriptions = new List<IAction>();

            Rule rule;
            RuleResult ruleResult;
            Operation operation;
            Value value;
            IAction rulePrescription;
            IAction operationPrescription;

            //var requestJObj = JToken.Parse("{\"Q\": \"How can we help?\", \"AA\":[\"New Claim\", \"Existing Claim\"]}");
            var requestObj = new Value(12);

            (rule, ruleResult, rulePrescription) = requestObj.Exists();

            rules.Add(rule);
            ruleResults.Add(ruleResult);
            rulePrescriptions.Add(rulePrescription);

            (operation, operationPrescription) = ruleResult.Create<Value>(requestObj);

            operations.Add(operation);
            operationPrescriptions.Add(operationPrescription);
            values.Add(requestObj);

            requestObj = new Value(13);
            (ruleResult, rulePrescription) = requestObj.Exists(rule);
            ruleResults.Add(ruleResult);

            (operation, operationPrescription) = ruleResult.Create<Value>(requestObj);

            operations.Add(operation);
            operationPrescriptions.Add(operationPrescription);
            values.Add(requestObj);



            var valIds = new List<Guid> {values[0].ValueId, values[1].ValueId};
            (operation, value, operationPrescription) = ruleResult.Create<Value>(new [] { values[0].ValueId }.ToList());
            operations.Add(operation);
            values.Add(value);
            operationPrescriptions.Add(operationPrescription);
            (operation, value, operationPrescription) = ruleResult.Create<Value>(new[] { values[1].ValueId }.ToList());
            operations.Add(operation);
            values.Add(value);
            operationPrescriptions.Add(operationPrescription);

            //var procAllRules = new ProcessAllRulesPrescription();
            //var procAllOperations = new ProcessAllOperationsPrescription();

            rvStore = new Store<RulEngStore>(StoreReducers.ReduceStore, null);

            RulEngStore changes;
            rvStore.Subscribe(state => changes = state);

            var act = rvStore.Dispatch(rulePrescription);
            foreach (var prescription in operationPrescriptions)
            {
                act = rvStore.Dispatch(prescription);
            }

            File.WriteAllText("storeBefore.json", rvStore.GetState().ToString());
            //act = rvStore.Dispatch(procAllRules);
            // File.WriteAllText("storeMiddle.json", rvStore.GetState().ToString());
            //act = rvStore.Dispatch(procAllOperations);
            File.WriteAllText("storeAfter.json", rvStore.GetState().ToString());
        }
    }
}