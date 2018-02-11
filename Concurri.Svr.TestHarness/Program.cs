using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jint;
using Redux;
using RulEng.Helpers;
using RulEng.Reformers;
using RulEng.States;

namespace Concurri.Svr.TestHarness
{
    public static class Program
    {
        private static IStore<RulEngStore> RvStore { get; set; }

        public static void Main()
        {
            Console.WriteLine("Hello World!");

            var regexToken = new Regex(@".*?(?<Token>\$\{(?<Index>\d+)\}).*?");
            var jTempl = "(${2}+${1})/(${3}+${1})";
            var vals = new [] {12M, 15.5M, 16.8M, 13.45M};

            var jCode = jTempl;
            var isSubstOk = true;
            foreach (Match match in regexToken.Matches(jTempl))
            {
                var token = match.Groups["Token"].Value;
                var indexOk = int.TryParse(match.Groups["Index"].Value, out var index);

                if (!indexOk)
                {
                    isSubstOk = false;
                    break;
                }

                if (vals.Length < index)
                {
                    isSubstOk = false;
                    break;
                }

                jCode = jCode.Replace(token, vals[index].ToString(CultureInfo.InvariantCulture));
                    
                Console.WriteLine($"Token:{token}, Index:{index} Value:{vals[index]}");
            }

            if (isSubstOk)
            {
                Console.WriteLine($"{jTempl} => {jCode}");

                var e = new Engine();
                var result = e.Execute(jCode).GetCompletionValue().ToObject();
                Console.WriteLine(result);
            }

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

            RvStore = new Store<RulEngStore>(StoreReducers.ReduceStore);

            RulEngStore changes;
            RvStore.Subscribe(state => changes = state);

            var act = RvStore.Dispatch(rulePrescription);
            foreach (var prescription in operationPrescriptions)
            {
                act = RvStore.Dispatch(prescription);
            }

            File.WriteAllText("storeBefore.json", RvStore.GetState().ToString());
            //act = rvStore.Dispatch(procAllRules);
            // File.WriteAllText("storeMiddle.json", rvStore.GetState().ToString());
            //act = rvStore.Dispatch(procAllOperations);
            File.WriteAllText("storeAfter.json", RvStore.GetState().ToString());
        }
    }
}