using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Jint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Redux;

using RulEng.Helpers;
using RulEng.Reformers;
using RulEng.States;
using RulEng.Prescriptions;

namespace Concurri.Svr.TestHarness
{
    public static class Program
    {
        private static IStore<RulEngStore> RvStore { get; set; }

        public static void Main()
        {
            Console.WriteLine("Hello Salesman!");

            // Travelling Salesman - Setup
            const int cityCount = 50;
            Console.WriteLine($"Start Setup for {cityCount} cities : {DateTime.UtcNow.ToString("yyyy-MMM-dd HH:mm:ss.ff")}");

            (var rules, var ruleResults, var values, var rulePrescriptions) = BuildTheCities(cityCount);

            // Build a Collection Rule, Result and Prescription for all of the above rules
            (var collectRule, var collectRuleResult, var collectRulePrescription) = ruleResults.And();

            // Add the Collection Rule, Result and Prescription
            rules.Add(collectRule);
            ruleResults.Add(collectRuleResult);
            rulePrescriptions.Add(collectRulePrescription);

            var operations = new List<Operation>();
            var operationPrescriptions = new List<OperationMxProcessing>();

            var cityValues = values.Where(c => c.Detail["properties"]["cityNo"] != null).ToList();

            (var pointOperations, var pointOperationPrescriptions) = BuildTheGeoJsonOutput(collectRuleResult, cityValues);
            operations.Add(pointOperations);
            operationPrescriptions.Add(pointOperationPrescriptions);

            (var distOperations, var distOperationPrescriptions) = BuildTheCityDistances(collectRuleResult, cityValues);
            operations.AddRange(distOperations);
            operationPrescriptions.AddRange(distOperationPrescriptions);

            // Build the Rule Engine Store ready for processing
            var startingStore = new RulEngStore
            {
                Rules = rules.ToImmutableHashSet(),
                RuleResults = ruleResults.ToImmutableHashSet(),
                Operations = operations.ToImmutableHashSet(),
                Values = values.ToImmutableHashSet()
            };

            RvStore = new Store<RulEngStore>(StoreReducers.ReduceStore, startingStore);
            File.WriteAllText("storeStart.json", RvStore.GetState().ToString());

            // Commence Processing
            RulEngStore changes;
            RvStore.Subscribe(state => changes = state);

            var pass = 0;
            TikTok(pass++, rulePrescriptions, operationPrescriptions);

            Console.WriteLine($"Add more to Store for {cityCount} cities : {DateTime.UtcNow.ToString("yyyy-MMM-dd HH:mm:ss.ff")}");
            // Build the exists rules for the values resulting from the distance operations
            (var distRules, var distRuleResults, var distRulePrescriptions) =
                distOperations.SelectMany(dp => dp.Operands).Exists(false);

            // Build the operations to convert the distance results to roads
            var storeValues = RvStore.GetState().Values.ToList();
            (var roadOperations, var roadOperationPrescriptions) = BuildTheCityRoads(distRules, storeValues);

            // Add the new Entities to the Store ready for processing
            RvStore.AddUpdate(distRules, distRuleResults, roadOperations, null);
            TikTok(pass++, distRulePrescriptions, roadOperationPrescriptions);

            Console.WriteLine($"Add more to Store for {cityCount} cities : {DateTime.UtcNow.ToString("yyyy-MMM-dd HH:mm:ss.ff")}");
            // Build the exists rules for the values resulting from the 'road' operations
            (var roadExistsRules, var roadExistsRuleResults, var roadExistsRulePrescriptions) =
                roadOperations.SelectMany(rp => rp.Operands).Exists(false);
            // TODO: Build operations to mark the roads between CityA and CityB (and vice versa) as 'accepted'
            // Build a Collection Rule, Result and Prescription for all of the 'road' rules
            (var collectRoadRule, var collectRoadRuleResult, var collectRoadRulePrescription) = roadExistsRuleResults.And();
            roadExistsRules.Add(collectRoadRule);
            roadExistsRuleResults.Add(collectRoadRuleResult);
            roadExistsRulePrescriptions.Add(collectRoadRulePrescription);

            // Join the new Roads together in one map
            var roadValues = RvStore.GetState().Values
                .Where(c => c.Detail != null && c.Detail.Type == JTokenType.Object && c.Detail["properties"]?["roadId"] != null)
                .ToList();
            (var lineOperation, var lineOperationPrescription) = BuildTheGeoJsonOutput(collectRoadRuleResult, roadValues);

            // Add the new Entities to the Store ready for processing
            RvStore
                .AddUpdate(roadExistsRules, roadExistsRuleResults, new[] { lineOperation }.ToList(), null);

            TikTok(pass++, roadExistsRulePrescriptions, new[] { lineOperationPrescription });

            // Searching or finding entities by arbitrary requirements is not currently supported
            // It could potentially be implemented with:
            // a new Rule (e.g. AnyMatch) that triggers
            // a special Operation (Match -> Generates Exists Rules as simple triggers)
            // regular Operation(s) to process the matching entities

            // So search for duplicate roads (A->B and B->A) by code external to the RuleEngine store
            //(var dupRoadExistsRules, var dupRoadExistsRuleResults, var dupRoadDeleteOperations,
            //        var dupRoadRulePrescriptions, var dupRoadOpPrescriptions) =
            //    DeleteTheDuplicateRoads(roadValues, roadExistsRules);

            //RvStore.AddUpdate(dupRoadExistsRules, dupRoadExistsRuleResults, dupRoadDeleteOperations, null);

            //TikTok(pass++, dupRoadRulePrescriptions, dupRoadOpPrescriptions);

            // Refresh the list of roads
            roadValues = RvStore.GetState().Values
                .Where(c => c.Detail != null && c.Detail.Type == JTokenType.Object && c.Detail["properties"]?["roadId"] != null)
                .ToList();
            roadExistsRules = RvStore.GetState().Rules
                .Where(c => c.RuleType == RuleType.Exists &&
                            roadValues.Select(v => v.ValueId).Contains(c.ReferenceValues.EntityIds[0].EntityId))
                .ToList();
            roadExistsRuleResults = RvStore.GetState().RuleResults
                .Where(s => roadExistsRules.Select(c => c.RuleId).Contains(s.RuleId))
                .ToList();

            // recreate the prescriptions for road rules
            roadExistsRulePrescriptions = new List<IRuleProcessing>();
            foreach (var rv in roadExistsRules)
            {
                roadExistsRulePrescriptions.Add(rv.Exists());
            }
            // Rebuild the Collection Rule, Result and Prescription for all of the 'road' rules
            (collectRoadRule, collectRoadRuleResult, collectRoadRulePrescription) = roadExistsRuleResults.And(collectRoadRule, collectRoadRuleResult);
            roadExistsRules.Add(collectRoadRule);
            roadExistsRuleResults.Add(collectRoadRuleResult);
            roadExistsRulePrescriptions.Add(collectRoadRulePrescription);

            // Join the new Roads together in one map
            (lineOperation, lineOperationPrescription) = BuildTheGeoJsonOutput(collectRoadRuleResult, roadValues, lineOperation);

            // Add the new Entities to the Store ready for processing
            RvStore
                .AddUpdate(roadExistsRules, roadExistsRuleResults, new[] { lineOperation }.ToList(), null);

            TikTok(pass++, roadExistsRulePrescriptions, new[] { lineOperationPrescription });

            var opKeyDups = new OperandKey
            {
                EntityId = Guid.NewGuid(),
                EntTags = new List<string> { "Duplicates" },
                EntType = EntityType.Rule,
                SourceEntType = EntityType.Value
            };

            // The source data is always presented as a serialised JSON string
            var searchTemplate = "JSON.parse(source)"
                    // Filter for values with a roadId property
                    + ".filter(function(s){return s.Detail&&s.Detail.properties&&s.Detail.properties.roadId})"
                    // Map the results to just the values Id and the roadId (a hash)
                    + ".map(function(s){return {vId:s.ValueId,rId:s.Detail.properties.roadId};})"
                    // Reduce to an array grouping by the roadId, the first valueId with that roadId will also be in the structure
                    + ".reduce(function(a,c){var ix=0;"
                    + "for(;ix<a.length;ix++){if(a[ix].el.rId===c.rId)break;}"
                    + "if(ix<a.length){a[ix].t++;}else{a.push({el:c,t:1});}"
                    + "return a;},[])"
                    // Filter for roadIds that occur more than once
                    + ".filter(function(s){return s.t>1})"
                    // Get the valueId
                    + ".map(function(s){return s.el.vId})"
                    // Sort (makes it easier to follow what's going on)
                    + ".sort(function(a,b){if(a<b)return -1;return(a>b)?1:0;})";

            var opRoadSearch = collectRuleResult.SearchOperation(new[] { opKeyDups }, Guid.NewGuid(), searchTemplate);
            var opRoadSearchPrescription = opRoadSearch.Search();

            RvStore.AddUpdate(null, null, opRoadSearch, null);

            var opKeyDels = new OperandKey
            {
                EntityId = Guid.NewGuid(),
                EntTags = new List<string> { "Duplicates" },
                EntType = EntityType.Operation,
                SourceEntType = EntityType.RuleResult
            };

            // The source data is always presented as a serialised JSON string
            var searchDupsTemplate = "JSON.parse(source)"
                                 // Filter for values with a roadId property
                                 + ".filter(function(s){return s.EntTags&&s.EntTags[0]==='Duplicates'})"
                                 // Map the results to just the ruleresult Id
                                 + ".map(function(s){return s.RuleResultId;})"
                                 // Sort (makes it easier to follow what's going on)
                                 + ".sort(function(a,b){if(a<b)return -1;return(a>b)?1:0;})";

            var opDelRoadSearch = collectRuleResult.SearchOperation(new[] { opKeyDels }, Guid.NewGuid(), searchDupsTemplate);
            var opDelRoadSearchPrescription = opDelRoadSearch.Search();

            RvStore.AddUpdate(null, null, opDelRoadSearch, null);


            TikTok(pass++, null, new[] { opRoadSearchPrescription });

            // We'll start by adding all the shortest ones as the first set of 'actual' roads
            // A minimum of two * (cityCount - 1) roads will be required
            ////var roadSet = values
            ////    .Where(r => r.Detail["properties"]["cityAId"] != null && (r.Detail["properties"]["usage"] == null || (string)r.Detail["properties"]["usage"] == "Not Set"))
            ////    .OrderBy(r => (double)r.Detail["properties"]["distance"]);
            ////var cityIds = new List<(Guid cityId, int Count)>();
            ////foreach (var road in roadSet)
            ////{
            ////    var roadGeoJson = (JObject)road.Detail;
            ////    var cityAId = (Guid)roadGeoJson["properties"]["cityAId"];
            ////    var cityBId = (Guid)roadGeoJson["properties"]["cityBId"];

            ////    // Test whether either city at the end of this road already have two roads
            ////    var cityHasRoads = cityIds.Where(ci => ci.cityId == cityAId || ci.cityId == cityBId).ToList();

            ////    var citiesFullyConnected = cityHasRoads.Count(chr => chr.Count >= 2);

            ////    Console.WriteLine($"{cityIds.Count} - {citiesFullyConnected}");

            ////    switch (citiesFullyConnected)
            ////    {
            ////        case 0:
            ////            // Road connects two cities and neither has a full set of connecting roads
            ////            // Do connection
            ////            roadGeoJson["properties"]["usage"] = "Accepted";
            ////            try
            ////            {
            ////                var a = cityIds.First(chr => chr.cityId == cityAId);
            ////                cityIds.Remove(a);
            ////                a.Count++;
            ////                cityIds.Add(a);
            ////            }
            ////            catch
            ////            {
            ////                cityIds.Add((cityAId, 1));
            ////            }
            ////            try
            ////            {
            ////                var b = cityIds.First(chr => chr.cityId == cityBId);
            ////                cityIds.Remove(b);
            ////                b.Count++;
            ////                cityIds.Add(b);
            ////            }
            ////            catch
            ////            {
            ////                cityIds.Add((cityBId, 1));
            ////            }
            ////            break;
            ////        case 1:
            ////            // Road connecting one fully connected city
            ////            if (cityHasRoads.Count == 1)
            ////            {
            ////                // And only one city - so create an empty connection record for the other
            ////                var ci = cityHasRoads.All(chr => chr.cityId == cityAId) ? (cityBId, 0) : (cityAId, 0);
            ////                cityIds.Add(ci);
            ////            }
            ////            break;

            ////        case 2:
            ////        default:
            ////            // Road connecting two already full connected cities
            ////            break;
            ////    }

            ////    if (cityIds.Count >= 10)
            ////    {
            ////        break;
            ////    }
            ////}

            ////var acceptedRoads = values.Where(v =>
            ////    v.Detail["properties"]["cityAId"] != null &&
            ////    v.Detail["properties"]["usage"] != null && (string)v.Detail["properties"]["usage"] == "Accepted")
            ////    .ToList();
            ////var salesmansJourney = new StringBuilder();
            ////salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            ////salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
            ////salesmansJourney.Append("]}");
            ////var jny = JObject.Parse(salesmansJourney.ToString());
            ////File.WriteAllText("Routes00.json", jny.ToString());

            //var citiesWithNoRoadsCount = cityIds.Count(ci => ci.Count == 0);

            // For each city with no connections
            // Determine its two closest connected neighbours and reject that road
            // and accept the two roads to and from this city to those two closest neighbours
            ////foreach (var ci in cityIds.Where(ci => ci.Count == 0))
            ////{
            ////    Console.WriteLine($"{ci.cityId}");

            ////    var closestNeighboursWithRoads = values.Where(v =>
            ////        v.Detail["properties"]["cityAId"] != null &&
            ////        ((Guid)v.Detail["properties"]["cityAId"] == ci.cityId || (Guid)v.Detail["properties"]["cityBId"] == ci.cityId) &&
            ////        v.Detail["properties"]["usage"] != null && (string)v.Detail["properties"]["usage"] == "Accepted")
            ////        .OrderBy(r => (double)r.Detail["properties"]["distance"])
            ////        .ToList();

            ////    var closestNeighbourGuids = closestNeighboursWithRoads.SelectMany(ar => new[]
            ////        {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
            ////        .GroupBy(cg => cg)
            ////        .Select(cg => cg.Key)
            ////        .ToList();

            ////    foreach (var cng in closestNeighbourGuids)
            ////    {
            ////        var notCng = closestNeighbourGuids.Where(cg => cng != cg).ToList();

            ////        var cnwr = closestNeighboursWithRoads.Where(v =>
            ////        v.Detail["properties"]["cityAId"] != null &&
            ////        ((Guid)v.Detail["properties"]["cityAId"] == ci.cityId || (Guid)v.Detail["properties"]["cityBId"] == ci.cityId) &&
            ////        (notCng.Contains((Guid)v.Detail["properties"]["cityAId"]) || notCng.Contains((Guid)v.Detail["properties"]["cityBId"])))
            ////        .ToList();

            ////        if (!cnwr.Any())
            ////        {
            ////            continue;
            ////        }
            ////        // Road to Reject
            ////        var r2R = cnwr.First();
            ////        values.First(r => r.EntityId == r2R.EntityId).Detail["properties"]["usage"] = "Rejected";

            ////        // Rejected road cities
            ////        var cnwor = new[] { (Guid)r2R.Detail["properties"]["cityAId"], (Guid)r2R.Detail["properties"]["cityBId"] };

            ////        // Roads to Accept
            ////        var r2A = closestNeighboursWithRoads.Where(v =>
            ////            v.Detail["properties"]["cityAId"] != null &&
            ////            ((Guid)v.Detail["properties"]["cityAId"] == ci.cityId || (Guid)v.Detail["properties"]["cityBId"] == ci.cityId) &&
            ////            (cnwor.Contains((Guid)v.Detail["properties"]["cityAId"]) || cnwor.Contains((Guid)v.Detail["properties"]["cityBId"])));
            ////        foreach (var rd in r2A)
            ////        {
            ////            values.First(r => r.EntityId == rd.EntityId).Detail["properties"]["usage"] = "Accepted";
            ////        }
            ////        break;
            ////    }
            ////}

            // Now we'll ensure that every city has at least two roads connecting it
            // First step is to group all of the cities and get a count for the number of roads to each one
            //var citiesWithRoads = acceptedRoads
            //    .SelectMany(ar => new[]
            //        {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
            //    .GroupBy(cg => cg)
            //    .Select(cg => new { cityId = cg.Key, Count = cg.Count() })
            //    .ToList();
            //citiesWithRoadsCount = citiesWithRoads.Count;

            ////        acceptedRoads = values.Where(v =>
            ////v.Detail["properties"]["cityAId"] != null &&
            ////v.Detail["properties"]["usage"] != null && (string)v.Detail["properties"]["usage"] == "Accepted")
            ////.ToList();
            ////        salesmansJourney = new StringBuilder();
            ////        salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            ////        salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
            ////        salesmansJourney.Append("]}");
            ////        jny = JObject.Parse(salesmansJourney.ToString());
            ////        File.WriteAllText("Routes01.json", jny.ToString());

            // Then there's a need to check for any cities with no roads at all connected to them (a possibility)
            // and add these to the same list with a count of zero for each one.
            //if (citiesWithRoadsCount < cityCount)
            //{
            //    var citiesWithNoRoads = values.Where(c =>
            //            c.Detail["properties"]["cityNo"] != null &&
            //            !citiesWithRoads.Select(cr => cr.cityId).Contains(c.EntityId))
            //        .Select(cn => new { cityId = cn.EntityId, Count = 0 })
            //        .ToList();

            //    citiesWithNoRoadsCount = citiesWithNoRoads.Count;

            //    citiesWithRoads.AddRange(citiesWithNoRoads);
            //}

            //do
            //{
            //    if (pass > 10)
            //    {
            //        break;
            //    }

            //    // Take this list and add the two closest roads for each city with less than two roads
            //    // and output the result.
            //    foreach (var cwr in citiesWithRoads.Where(cwr => cwr.Count < 2))
            //    {
            //        roadSet = values.Where(v =>
            //                v.Detail["properties"]["cityAId"] != null &&
            //                v.Detail["properties"]["usage"] != null &&
            //                (string) v.Detail["properties"]["usage"] == "Not Set" &&
            //                ((Guid) v.Detail["properties"]["cityAId"] == cwr.cityId ||
            //                 (Guid) v.Detail["properties"]["cityBId"] == cwr.cityId))
            //            .OrderBy(r => (double) r.Detail["properties"]["distance"])
            //            .Take(2 - cwr.Count);

            //        foreach (var road in roadSet)
            //        {
            //            var roadGeoJson = (JObject) road.Detail;

            //            roadGeoJson["properties"]["usage"] = "Accepted";
            //        }
            //    }

            //    acceptedRoads = values.Where(v =>
            //            v.Detail["properties"]["cityAId"] != null &&
            //            v.Detail["properties"]["usage"] != null &&
            //            (string) v.Detail["properties"]["usage"] == "Accepted")
            //        .ToList();
            //    salesmansJourney = new StringBuilder();
            //    salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            //    salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
            //    salesmansJourney.Append("]}");
            //    jny = JObject.Parse(salesmansJourney.ToString());
            //    File.WriteAllText($"routes{pass++}.json", jny.ToString());

            //    // Identify cities with too many roads
            //    var citiesWithTooManyRoads = acceptedRoads
            //        .SelectMany(ar => new[]
            //            {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
            //        .GroupBy(cg => cg)
            //        .Select(cg => new {cityId = cg.Key, Count = cg.Count()})
            //        .Where(cwr => cwr.Count > 2)
            //        .ToList();

            //    foreach (var cwr in citiesWithTooManyRoads)
            //    {
            //        var road = values.Where(v =>
            //                v.Detail["properties"]["cityAId"] != null &&
            //                v.Detail["properties"]["usage"] != null &&
            //                (string) v.Detail["properties"]["usage"] == "Accepted" &&
            //                ((Guid) v.Detail["properties"]["cityAId"] == cwr.cityId ||
            //                 (Guid) v.Detail["properties"]["cityBId"] == cwr.cityId))
            //            .OrderByDescending(r => (double) r.Detail["properties"]["distance"])
            //            .First();
            //            //.Take(cwr.Count - 2);

            //        //foreach (var road in roadSet)
            //        //{
            //            Guid otherCityId;
            //            otherCityId = (Guid) road.Detail["properties"]["cityAId"] == cwr.cityId
            //                ? (Guid) road.Detail["properties"]["cityBId"]
            //                : (Guid) road.Detail["properties"]["cityAId"];
            //            var otherCityHasTooManyRoads = values.Count(v =>
            //                                               v.Detail["properties"]["cityAId"] != null &&
            //                                               v.Detail["properties"]["usage"] != null &&
            //                                               (string) v.Detail["properties"]["usage"] == "Accepted" &&
            //                                               ((Guid) v.Detail["properties"]["cityAId"] == otherCityId ||
            //                                                (Guid) v.Detail["properties"]["cityBId"] == otherCityId)) >
            //                                           2;

            //            if (!otherCityHasTooManyRoads)
            //            {
            //                continue;
            //            }

            //            var roadGeoJson = (JObject) road.Detail;

            //            roadGeoJson["properties"]["usage"] = "Rejected";
            //        //}
            //    }

            //    acceptedRoads = values.Where(v =>
            //            v.Detail["properties"]["cityAId"] != null &&
            //            v.Detail["properties"]["usage"] != null &&
            //            (string) v.Detail["properties"]["usage"] == "Accepted")
            //        .ToList();
            //    salesmansJourney = new StringBuilder();
            //    salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            //    salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
            //    salesmansJourney.Append("]}");
            //    jny = JObject.Parse(salesmansJourney.ToString());
            //    File.WriteAllText($"routes{pass++}.json", jny.ToString());

            //    citiesWithRoads = acceptedRoads
            //        .SelectMany(ar => new[]
            //            {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
            //        .GroupBy(cg => cg)
            //        .Select(cg => new {cityId = cg.Key, Count = cg.Count()})
            //        .ToList();
            //    citiesWithRoadsCount = citiesWithRoads.Count(cwr => cwr.Count >= 2);

            //    // Then there's a need to check for any cities with no roads at all connected to them (a possibility)
            //    // and add these to the same list with a count of zero for each one.
            //    if (citiesWithRoadsCount < cityCount)
            //    {
            //        var citiesWithNoRoads = values.Where(c =>
            //                c.Detail["properties"]["cityNo"] != null &&
            //                !citiesWithRoads.Select(cr => cr.cityId).Contains(c.EntityId))
            //            .Select(cn => new {cityId = cn.EntityId, Count = 0})
            //            .ToList();

            //        citiesWithNoRoadsCount = citiesWithNoRoads.Count;
            //        citiesWithRoads.AddRange(citiesWithNoRoads);
            //    }
            //} while (citiesWithRoadsCount != cityCount || citiesWithNoRoadsCount != 0);
            //values.Add(new Value(jny));

            //var startingStore = new RulEngStore
            //{
            //    Values = values.ToImmutableHashSet()
            //};

            //RvStore = new Store<RulEngStore>(null, startingStore);



            //var requestJObj = JToken.Parse("{\"Q\": \"How can we help?\", \"AA\":[\"New Claim\", \"Existing Claim\"]}");
            //var requestObj = new Value(12);

            //(rule, ruleResult, rulePrescription) = requestObj.Exists();

            //rules.Add(rule);
            //ruleResults.Add(ruleResult);
            //rulePrescriptions.Add(rulePrescription);

            //(operation, operationPrescription) = ruleResult.Create<Value>(requestObj);

            //operations.Add(operation);
            //operationPrescriptions.Add(operationPrescription);
            //values.Add(requestObj);

            //requestObj = new Value(13);
            //(ruleResult, rulePrescription) = requestObj.Exists(rule);
            //ruleResults.Add(ruleResult);

            //(operation, operationPrescription) = ruleResult.Create<Value>(requestObj);

            //operations.Add(operation);
            //operationPrescriptions.Add(operationPrescription);
            //values.Add(requestObj);



            //var valIds = new List<Guid> {values[0].ValueId, values[1].ValueId};
            //(operation, value, operationPrescription) = ruleResult.Create<Value>(new [] { values[0].ValueId }.ToList());
            //operations.Add(operation);
            //values.Add(value);
            //operationPrescriptions.Add(operationPrescription);
            //(operation, value, operationPrescription) = ruleResult.Create<Value>(new[] { values[1].ValueId }.ToList());
            //operations.Add(operation);
            //values.Add(value);
            //operationPrescriptions.Add(operationPrescription);

            ////var procAllRules = new ProcessAllRulesPrescription();
            ////var procAllOperations = new ProcessAllOperationsPrescription();

            //RvStore = new Store<RulEngStore>(StoreReducers.ReduceStore);

            //RulEngStore changes;
            //RvStore.Subscribe(state => changes = state);

            //var act = RvStore.Dispatch(rulePrescription);
            //foreach (var prescription in operationPrescriptions)
            //{
            //    act = RvStore.Dispatch(prescription);
            //}

            //File.WriteAllText("storeBefore.json", RvStore.GetState().ToString());
            //act = rvStore.Dispatch(procAllRules);
            // File.WriteAllText("storeMiddle.json", rvStore.GetState().ToString());
            //act = rvStore.Dispatch(procAllOperations);
            //File.WriteAllText("storeAfter.json", RvStore.GetState().ToString());
            pass++;
        }

        /// <summary>
        /// Place to do experiments, for figuring out how to construct the JS templates
        /// </summary>
        /// <param name="values"></param>
        public static void DoJintTest(List<Value> values)
        {
            var regexToken = new Regex(@".*?(?<Token>\$\{(?<Index>\d+)\}).*?");
            var jTemplate = new StringBuilder();
            jTemplate.AppendLine("[");

            const string cityATempl = "{{'cityAId':'{0}','destinations':[";
            const string lonTempl = "JSON.parse('${{{0}}}')['geometry']['coordinates'][0]";
            const string latTempl = "JSON.parse('${{{0}}}')['geometry']['coordinates'][1]";
            const string getDistTempl = "{{'cityBId':'{{{0}}}','distance':Math.pow(Math.pow({1} - {2}, 2) + Math.pow({3} - {4}, 2), 0.5)}}";

            for (var ix = 0; ix < 1; ix++) // values.Count; ix++)
            {
                jTemplate.AppendFormat(cityATempl, values[ix].EntityId);
                jTemplate.AppendLine();
                var cityALonTempl = string.Format(lonTempl, ix);
                var cityALatTempl = string.Format(latTempl, ix);

                var needsComma = false;
                for (var jx = 0; jx < values.Count; jx++)
                {
                    if (ix == jx)
                    {
                        continue;
                    }

                    var cityBLonTempl = string.Format(lonTempl, jx);
                    var cityBLatTempl = string.Format(latTempl, jx);
                    var cityAtoBDistTempl = string.Format(getDistTempl, values[jx].EntityId, cityALonTempl, cityBLonTempl, cityALatTempl, cityBLatTempl);

                    if (needsComma)
                    {
                        jTemplate.AppendLine(",");
                    }
                    jTemplate.Append(cityAtoBDistTempl);
                    if (!needsComma)
                    {
                        needsComma = true;
                    }
                }
                jTemplate.AppendLine();
                jTemplate.AppendLine("].sort(function(a, b) {return a.distance - b.distance;})");
                jTemplate.AppendLine("}");
                jTemplate.AppendLine("]");
            }

            var jTempl = jTemplate.ToString();

            var e = new Engine();
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

                if (values.Count < index)
                {
                    isSubstOk = false;
                    break;
                }

                jCode = jCode.Replace(token, values[index].Detail.ToString(Formatting.None));

                //Console.WriteLine($"Token:{token}, Index:{index} Value:{values[index].Detail.ToString(Formatting.None)}");
            }

            if (isSubstOk)
            {
                Console.WriteLine($"{jTempl} =>");
                Console.WriteLine($"{jCode}");

                object result;
                try
                {
                    result = e
                        .Execute(jCode)
                        .GetCompletionValue()
                        .ToObject();

                    Console.WriteLine(result);
                    var jResult = JsonConvert.SerializeObject(result);
                    Console.WriteLine(jResult);
                }
                catch (Exception ex)
                {
                    File.WriteAllText("jCode.js", jCode);
                }
            }

        }

        private static (List<Rule> rules, List<RuleResult> ruleResults, List<Value> values, List<IRuleProcessing> ruleProcessing) BuildTheCities(int cityCount)
        {
            var rules = new List<Rule>();
            var ruleResults = new List<RuleResult>();
            var values = new List<Value>();
            var rulePrescriptions = new List<IRuleProcessing>();

            var rnd = new Random();
            for (var ix = 0; ix < cityCount; ix++)
            {
                var lat = -21.0 + -7.0 * rnd.NextDouble();
                var lon = 142.0 + 7.0 * rnd.NextDouble();
                var pointGeo =
                    $"{{\"type\":\"Feature\",\"properties\":{{\"cityNo\":{ix}}},\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{lon},{lat}]}}}}";
                var lonLat = JObject.Parse(pointGeo);
                var coordValue = new Value(lonLat);
                values.Add(coordValue);

                Rule rule;
                RuleResult ruleResult;
                IRuleProcessing rulePrescription;
                (rule, ruleResult, rulePrescription) = coordValue.Exists(null, null, false);

                if (rule.ReferenceValues.RuleResultId != ruleResult.RuleResultId)
                {
                    throw new Exception("RuleResultId does not line up");
                }
                rules.Add(rule);
                ruleResults.Add(ruleResult);
                rulePrescriptions.Add(rulePrescription);
            }

            return (rules, ruleResults, values, rulePrescriptions);
        }

        private static (List<Operation> operations, List<OperationMxProcessing> operationPrescriptions) BuildTheCityDistances(RuleResult cityRuleResults, List<Value> values)
        {
            var operations = new List<Operation>();
            var operationPrescriptions = new List<OperationMxProcessing>();

            // Build the Javascript template for calculating the length of each connecting GeoJSON line
            // Concept - Id of this city, then formula to calculate each distance and output the result as a sorted list.
            const string cityATempl = "{{'cityAId':'{0}','destinations':[";
            const string lonTempl = "JSON.parse('${{{0}}}')['geometry']['coordinates'][0]";
            const string latTempl = "JSON.parse('${{{0}}}')['geometry']['coordinates'][1]";
            const string getDistTempl = "{{'cityBId':'{0}','distance':Math.pow(Math.pow({1} - {2}, 2) + Math.pow({3} - {4}, 2), 0.5),'usage':'not set'}}";
            for (var ix = 0; ix < values.Count; ix++)
            {
                var jTemplate = new StringBuilder();
                jTemplate.AppendLine("[");

                jTemplate.AppendFormat(cityATempl, values[ix].EntityId);
                jTemplate.AppendLine();
                var cityALonTempl = string.Format(lonTempl, ix);
                var cityALatTempl = string.Format(latTempl, ix);

                var needsComma = false;
                for (var jx = 0; jx < values.Count; jx++)
                {
                    if (ix == jx)
                    {
                        continue;
                    }

                    var cityBLonTempl = string.Format(lonTempl, jx);
                    var cityBLatTempl = string.Format(latTempl, jx);
                    var cityAtoBDistTempl = string.Format(getDistTempl, values[jx].EntityId, cityALonTempl, cityBLonTempl, cityALatTempl, cityBLatTempl);

                    if (needsComma)
                    {
                        jTemplate.AppendLine(",");
                    }
                    jTemplate.Append(cityAtoBDistTempl);
                    if (!needsComma)
                    {
                        needsComma = true;
                    }
                }
                jTemplate.AppendLine();
                jTemplate.AppendLine("].sort(function(a, b) {return a.distance - b.distance;})");
                // Adding a slice at the end (for the 10 shortest paths) reduce the overall file size but did not seem to reduce processing time
                // jTemplate.AppendLine("].sort(function(a, b) {return a.distance - b.distance;}).slice(0, 10)");
                jTemplate.AppendLine("}");
                jTemplate.AppendLine("]");

                var jTempl = jTemplate.ToString();

                // Although the source values are always the same we need a new OperandKey each time
                // for each new Value to be generated
                var opKeys = new[] { values.OperandKey(EntityType.Value) };

                // Add an Operation to reference the collect Rule and merge all of the results into one GeoJSON
                var buildCityDistancesOperation = cityRuleResults.CreateUpdateOperation(opKeys, Guid.NewGuid(), jTempl);
                var buildCityDistancesPrescription = buildCityDistancesOperation.AddUpdate();

                operations.Add(buildCityDistancesOperation);
                operationPrescriptions.Add(buildCityDistancesPrescription);
            }

            return (operations, operationPrescriptions);
        }

        private static (List<Operation> operations, List<OperationMxProcessing> operationPrescriptions) BuildTheCityRoads(
            List<Rule> cityDistRules, List<Value> values)
        {
            var operations = new List<Operation>();
            var operationPrescriptions = new List<OperationMxProcessing>();

            foreach (var cityDistRule in cityDistRules)
            {
                var cityDistRuleResultId = cityDistRule.ReferenceValues.RuleResultId;
                var cityDistSourceEntType = cityDistRule.ReferenceValues.EntityIds[0];
                var cityDistValue = values.FirstOrDefault(c => c.ValueId == cityDistSourceEntType.EntityId);

                if (cityDistValue == null)
                {
                    continue;
                }

                var cityAId = Guid.Parse((string)cityDistValue.Detail[0]["cityAId"]);
                var nextCityB = (JObject)((JArray)cityDistValue.Detail[0]["destinations"])
                    .FirstOrDefault(d => (string)d["usage"] == "not set");
                // TODO: Should not be setting usage here - this needs to be its own operation
                nextCityB["usage"] = "accepted";
                var nextCityBId = Guid.Parse((string)nextCityB["cityBId"]);

                var roadId = cityAId.Merge(nextCityBId);

                var lineGeo =
                    $"{{JSON.parse(JSON.stringify({{\"type\":\"Feature\",\"properties\":{{\"roadId\":\"{roadId}\",\"cityAId\":\"{cityAId}\",\"cityBId\":\"{nextCityBId}\"}},\"geometry\":{{\"type\":\"LineString\",\"coordinates\":[JSON.parse('${{0}}')[\"geometry\"][\"coordinates\"],JSON.parse('${{1}}')[\"geometry\"][\"coordinates\"]]}}}}))}}";
                var opKeys = new[] { cityAId, nextCityBId }.OperandKey(EntityType.Value);

                // Add an Operation to reference the collect Rule and merge all of the results into one GeoJSON
                var buildCityRoadOperation = cityDistRuleResultId.CreateUpdateOperation(new[] { opKeys }, Guid.NewGuid(), lineGeo);
                var buildCityRoadPrescription = buildCityRoadOperation.AddUpdate();

                operations.Add(buildCityRoadOperation);
                operationPrescriptions.Add(buildCityRoadPrescription);
            }

            return (operations, operationPrescriptions);
        }

        private static (Operation operation, OperationMxProcessing operationPrescription) BuildTheGeoJsonOutput(RuleResult collectRuleResult, List<Value> values, Operation buildGeoJsonOperation = null)
        {
            var cityCount = values.Count;

            // Build the Javascript template for creating the entire GeoJSON Value
            var valueBody = "{\"type\":\"FeatureCollection\",\"features\":[";
            for (var ix = 0; ix < cityCount; ix++)
            {
                if (ix > 0)
                {
                    valueBody += ",";
                }
                valueBody += $"${{{ix}}}";
            }
            valueBody += "]}";
            var valueTemplate = $"{{JSON.parse('{valueBody}')}}";

            // Add an Operation to reference the collect Rule and merge all of the results into one GeoJSON
            if (buildGeoJsonOperation == null)
            {
                var opKey = values.OperandKey(EntityType.Value);

                buildGeoJsonOperation = collectRuleResult.CreateUpdateOperation(new[] { opKey }, Guid.NewGuid(), valueTemplate);
            }
            else
            {
                var opKey = values.OperandKey(EntityType.Value, buildGeoJsonOperation.Operands[0].EntityId);

                buildGeoJsonOperation =
                    buildGeoJsonOperation.RecreateUpdateOperation(collectRuleResult, new[] { opKey }, valueTemplate);
            }

            var buildGeoJsonPrescription = buildGeoJsonOperation.AddUpdate();

            return (buildGeoJsonOperation, buildGeoJsonPrescription);
        }

        private static (
            List<Rule> dupRoadExistsRules, List<RuleResult> dupRoadExistsRuleResults, List<Operation> dupRoadDeleteOperations,
            List<IRuleProcessing> dupRoadRulePrescriptions, List<OperationDxProcessing> dupRoadOpPrescriptions)
            DeleteTheDuplicateRoads(List<Value> roadValues, List<Rule> roadExistsRules)
        {
            var dupRoadIds = roadValues
                .Select(rv => (string)rv.Detail["properties"]?["roadId"])
                .GroupBy(ri => ri)
                .Select(ri => new { roadId = ri.Key, count = ri.Count() })
                .Where(ri => ri.count > 1)
                .ToList();

            // For each duplicate road, find the first one, and delete it.
            var dupRoadExistsRules = new List<Rule>();
            var dupRoadExistsRuleResults = new List<RuleResult>();
            var dupRoadDeleteOperations = new List<Operation>();
            var dupRoadRulePrescriptions = new List<IRuleProcessing>();
            var dupRoadOpPrescriptions = new List<OperationDxProcessing>();
            foreach (var dupRoadId in dupRoadIds)
            {
                var firstDupRoad =
                    roadValues.First(rv => (string)rv.Detail["properties"]?["roadId"] == dupRoadId.roadId);
                var firstDupRoadRule = roadExistsRules.First(rer =>
                    rer.ReferenceValues.EntityIds[0].EntType == EntityType.Value &&
                    rer.ReferenceValues.EntityIds[0].EntityId == firstDupRoad.ValueId);

                (var roadExistsRule, var roadExistsRuleResult, var roadExistsRulePrescription) = firstDupRoad.Exists();

                (var deleteRoadOperation, var deleteRoadPrescription) =
                    roadExistsRuleResult.Delete(new[] { firstDupRoad }, Guid.Empty);
                (var deleteRoadRuleOperation, var deleteRoadRulePrescription) =
                    roadExistsRuleResult.Delete(new[] { firstDupRoadRule }, Guid.Empty);

                dupRoadExistsRules.Add(roadExistsRule);
                dupRoadExistsRuleResults.Add(roadExistsRuleResult);
                dupRoadDeleteOperations.Add(deleteRoadOperation);
                dupRoadDeleteOperations.Add(deleteRoadRuleOperation);

                dupRoadRulePrescriptions.Add(roadExistsRulePrescription);
                dupRoadOpPrescriptions.Add(deleteRoadPrescription);
                dupRoadOpPrescriptions.Add(deleteRoadRulePrescription); // This will also delete the RuleResult and any dependent Operations / Requests
            }

            return (dupRoadExistsRules, dupRoadExistsRuleResults, dupRoadDeleteOperations, dupRoadRulePrescriptions, dupRoadOpPrescriptions);
        }

        private static void TikTok(int pass, IEnumerable<IRuleProcessing> rulePrescriptions, IEnumerable<IOpReqProcessing> operationPrescriptions)
        {
            if (rulePrescriptions != null)
            {
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"Pass {pass:0000}A Commence : {startTime:yyyy-MMM-dd HH:mm:ss.ff}");
                foreach (var prescription in rulePrescriptions)
                {
                    var act = RvStore.Dispatch(prescription);
                }

                var endTime = DateTime.UtcNow;
                Console.WriteLine($"Pass {pass:0000}A Complete : {endTime:yyyy-MMM-dd HH:mm:ss.ff}");

                var duration = endTime - startTime;
                Console.WriteLine($"Pass {pass:0000}A Duration : {duration.Days} {duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}.{duration.Milliseconds:00}");

                File.WriteAllText($"storePass{pass:0000}A.json", RvStore.GetState().ToString());
            }
            else
            {
                Console.WriteLine($"Pass {pass:0000}A Not Performed, no Rule Prescriptions provided : {DateTime.UtcNow:yyyy-MMM-dd HH:mm:ss.ff}");
            }

            if (operationPrescriptions != null)
            {
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"Pass {pass:0000}B Commence : {startTime:yyyy-MMM-dd HH:mm:ss.ff}");
                foreach (var prescription in operationPrescriptions)
                {
                    var act = RvStore.Dispatch(prescription);
                }

                var endTime = DateTime.UtcNow;
                Console.WriteLine($"Pass {pass:0000}B Complete : {endTime:yyyy-MMM-dd HH:mm:ss.ff}");

                var duration = endTime - startTime;
                Console.WriteLine($"Pass {pass:0000}A Duration :  {duration.Days} {duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}.{duration.Milliseconds:00}");

                File.WriteAllText($"storePass{pass:0000}B.json", RvStore.GetState().ToString());
            }
            else
            {
                Console.WriteLine($"Pass {pass:0000}B Not Performed, no Operation Prescriptions provided : {DateTime.UtcNow:yyyy-MMM-dd HH:mm:ss.ff}");
            }
        }
    }
}