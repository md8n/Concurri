using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Jint;
using Newtonsoft.Json.Linq;
using Redux;
using RulEng.Helpers;
using RulEng.Reformers;
using RulEng.States;
using RulEng.Prescriptions;
using Newtonsoft.Json;
using System.Dynamic;

namespace Concurri.Svr.TestHarness
{
    public static class Program
    {
        private static IStore<RulEngStore> RvStore { get; set; }

        public static void Main()
        {
            Console.WriteLine("Hello World!");

            //var regexToken = new Regex(@".*?(?<Token>\$\{(?<Index>\d+)\}).*?");
            //var jTempl = "(${2}+${1})/(${3}+${1})";
            //var vals = new [] {12M, 15.5M, 16.8M, 13.45M};

            //var jCode = jTempl;
            //var isSubstOk = true;
            //foreach (Match match in regexToken.Matches(jTempl))
            //{
            //    var token = match.Groups["Token"].Value;
            //    var indexOk = int.TryParse(match.Groups["Index"].Value, out var index);

            //    if (!indexOk)
            //    {
            //        isSubstOk = false;
            //        break;
            //    }

            //    if (vals.Length < index)
            //    {
            //        isSubstOk = false;
            //        break;
            //    }

            //    jCode = jCode.Replace(token, vals[index].ToString(CultureInfo.InvariantCulture));

            //    Console.WriteLine($"Token:{token}, Index:{index} Value:{vals[index]}");
            //}

            //if (isSubstOk)
            //{
            //var jTempl = "{\"ValueId\":\"20d25e4b-7d8c-4836-849f-5535b4e1a6f6\",\"EntType\":5,\"Detail\":{\"type\":\"FeatureCollection\",\"features\":[${0},${1},${2},${3},${4},${5},${6},${7},${8},${9}]},\"LastChanged\":\"1980-01-01 00:00:00Z\"}";
            //var jCode = "{\"ValueId\":\"20d25e4b-7d8c-4836-849f-5535b4e1a6f6\",\"EntType\":5,\"Detail\":{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"properties\":{\"cityNo\":0},\"geometry\":{\"type\":\"Point\",\"coordinates\":[143.867563808275,-25.3952077005036]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":1},\"geometry\":{\"type\":\"Point\",\"coordinates\":[148.650800422603,-21.3406091967321]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":2},\"geometry\":{\"type\":\"Point\",\"coordinates\":[147.70423036474,-25.4519063590336]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":3},\"geometry\":{\"type\":\"Point\",\"coordinates\":[147.90635649483,-21.9078147508706]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":4},\"geometry\":{\"type\":\"Point\",\"coordinates\":[142.456384286031,-23.3582771836586]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":5},\"geometry\":{\"type\":\"Point\",\"coordinates\":[145.22966226201,-22.885232692531]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":6},\"geometry\":{\"type\":\"Point\",\"coordinates\":[142.077799188941,-26.761976752785]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":7},\"geometry\":{\"type\":\"Point\",\"coordinates\":[146.068306779055,-22.6915270349437]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":8},\"geometry\":{\"type\":\"Point\",\"coordinates\":[148.761142209061,-21.7728364536412]}},{\"type\":\"Feature\",\"properties\":{\"cityNo\":9},\"geometry\":{\"type\":\"Point\",\"coordinates\":[148.213422938815,-25.9715534434521]}}]},\"LastChanged\":\"1980-01-01 00:00:00Z\"}";
            //Console.WriteLine($"{jTempl} => {jCode}");

            //    var e = new Engine();
            //var result = e
            //    .SetValue("v", jCode)
            //    .Execute("JSON.parse(v)")
            //    .GetCompletionValue()
            //    .ToObject();
            ////var result = e
            ////    .Execute(jCode)
            ////    .GetCompletionValue()
            ////    .ToObject();
            //Console.WriteLine(result);
            //var jResult = JsonConvert.SerializeObject(result);
            //Console.WriteLine(jResult);
            //}

            var rules = new List<Rule>();
            var ruleResults = new List<RuleResult>();
            var operations = new List<Operation>();
            var values = new List<Value>();
            var rulePrescriptions = new List<IRuleProcessing>();
            var operationPrescriptions = new List<IOpReqProcessing>();

            Rule rule;
            RuleResult ruleResult;
            Operation operation;
            Value value;
            IRuleProcessing rulePrescription;

            // Travelling Salesman - Setup

            // 10 LonLat points and a GeoJSON FeatureCollection
            var rnd = new Random();
            for (var ix = 0; ix < 10; ix++)
            {
                var lat = -21.0 + -7.0 * rnd.NextDouble();
                var lon = 142.0 + 7.0 * rnd.NextDouble();
                var pointGeo =
                    $"{{\"type\":\"Feature\",\"properties\":{{\"cityNo\":{ix}}},\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{lon},{lat}]}}}}";
                var lonLat = JObject.Parse(pointGeo);
                var coordValue = new Value(lonLat);
                values.Add(coordValue);

                (rule, ruleResult, rulePrescription) = coordValue.Exists(false);

                if (rule.ReferenceValues.RuleResultId != ruleResult.RuleResultId)
                {
                    throw new Exception("RuleResultId does not line up");
                }
                rules.Add(rule);
                ruleResults.Add(ruleResult);
                rulePrescriptions.Add(rulePrescription);
            }

            // Add Collection Rule for all of the above rules
            (var collectRule, var collectRuleResult, var collectRulePrescription) = ruleResults.And(false);
            rules.Add(collectRule);
            ruleResults.Add(collectRuleResult);
            rulePrescriptions.Add(collectRulePrescription);

            // Build the Javascript template for creating the entire Value
            var valueBody = "{\"type\":\"FeatureCollection\",\"features\":[";
            for (var ix = 0; ix < 10; ix++)
            {
                if (ix > 0)
                {
                    valueBody += ",";
                }
                valueBody += $"${{{ix}}}";
            }
            valueBody += "]}";
            // var valueTemplate = Guid.NewGuid().OperationValueTemplate(valueBody);
            var valueTemplate = $"JSON.parse('{valueBody}')";

            // Add an Operation to reference the collect Rule and merge all of the results into one GeoJSON
            var opKey = values.Where(c => c.Detail["properties"]["cityNo"] != null).OperandKey(EntityType.Value);
            var buildGeoJsonOperation = new Operation
            {
                OperationId = Guid.NewGuid(),
                OperationType = OperationType.CreateUpdate,
                RuleResultId = collectRuleResult.EntityId,
                Operands = ImmutableArray.Create(opKey),
                OperationTemplate = valueTemplate
            };
            var buildGeoJsonPrescription = buildGeoJsonOperation.AddUpdate();
            operations.Add(buildGeoJsonOperation);
            operationPrescriptions.Add(buildGeoJsonPrescription);

            var startingStore = new RulEngStore
            {
                Rules = rules.ToImmutableHashSet(),
                RuleResults = ruleResults.ToImmutableHashSet(),
                Operations = operations.ToImmutableHashSet(),
                Values = values.ToImmutableHashSet()
            };

            RvStore = new Store<RulEngStore>(StoreReducers.ReduceStore, startingStore);
            File.WriteAllText("storeStart.json", RvStore.GetState().ToString());

            //RvStore = new Store<RulEngStore>(StoreReducers.ReduceStore);

            RulEngStore changes;
            RvStore.Subscribe(state => changes = state);

            foreach(var prescription in rulePrescriptions)
            {
                var act = RvStore.Dispatch(prescription);
            }
            File.WriteAllText("storePass00A.json", RvStore.GetState().ToString());

            foreach (var prescription in operationPrescriptions)
            {
                var act = RvStore.Dispatch(prescription);
            }
            File.WriteAllText("storePass00B.json", RvStore.GetState().ToString());

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





            // How many Cities? (again)
            var cityCount = values.Count(c => c.Detail["properties"]["cityNo"] != null);

            var salesmansJourney = new StringBuilder();
            salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            salesmansJourney.Append(string.Join(',', values.Where(v => v.Detail["properties"]["cityNo"] != null).Select(v => v.Detail.ToString())));
            salesmansJourney.Append("]}");
            var jny = JObject.Parse(salesmansJourney.ToString());
            File.WriteAllText("salesmansCities.json", jny.ToString());

            // Build all possible roads
            for (var ix = 0; ix < cityCount; ix++)
            {
                var cityAValue = values.First(c => c.Detail["properties"]["cityNo"] != null && (int)c.Detail["properties"]["cityNo"] == ix);

                var aValLon = double.Parse(cityAValue.Detail["geometry"]["coordinates"][0].ToString());
                var aValLat = double.Parse(cityAValue.Detail["geometry"]["coordinates"][1].ToString());

                var cityRoadValues = new List<Value>();

                for (var jx = 0; jx < cityCount; jx++)
                {
                    if (jx == ix)
                    {
                        continue;
                    }

                    var cityBValue = values.First(c => c.Detail["properties"]?["cityNo"] != null && (int)c.Detail["properties"]["cityNo"] == jx);

                    var allRoads = values.Where(r => ((JObject)r.Detail["properties"])["cityAId"] != null);

                    var roadAtoBValue = allRoads.FirstOrDefault(r =>
                            ((Guid)r.Detail["properties"]["cityAId"] == cityAValue.EntityId &&
                            (Guid)r.Detail["properties"]["cityBId"] == cityBValue.EntityId) ||
                            ((Guid)r.Detail["properties"]["cityBId"] == cityAValue.EntityId &&
                            (Guid)r.Detail["properties"]["cityAId"] == cityBValue.EntityId));

                    if (roadAtoBValue != null)
                    {
                        continue;
                    }

                    var bValLon = double.Parse(cityBValue.Detail["geometry"]["coordinates"][0].ToString());
                    var bValLat = double.Parse(cityBValue.Detail["geometry"]["coordinates"][1].ToString());

                    var distAtoB = Math.Pow(Math.Pow(aValLon - bValLon, 2) + Math.Pow(aValLat - bValLat, 2), 0.5);

                    var lineGeo =
                        $"{{\"type\":\"Feature\",\"properties\":{{\"cityAId\":\"{cityAValue.EntityId}\",\"cityBId\":\"{cityBValue.EntityId}\",\"distance\":{distAtoB},\"usage\":\"Not Set\"}},\"geometry\":{{\"type\":\"LineString\",\"coordinates\":[[{aValLon},{aValLat}],[{bValLon},{bValLat}]]}}}}";

                    var lonLat = JObject.Parse(lineGeo);
                    roadAtoBValue = new Value(lonLat);
                    cityRoadValues.Add(roadAtoBValue);
                }

                var top10Roads = cityRoadValues.OrderBy(r => (double)r.Detail["properties"]["distance"]).Take(10);
                values.AddRange(top10Roads);

                Console.WriteLine($"City #:{ix}, added up to 10 roads");
            }

            // We'll start by adding all the shortest ones as the first set of 'actual' roads
            // A minimum of two * (cityCount - 1) roads will be required
            var roadSet = values
                .Where(r => r.Detail["properties"]["cityAId"] != null && (r.Detail["properties"]["usage"] == null || (string)r.Detail["properties"]["usage"] == "Not Set"))
                .OrderBy(r => (double)r.Detail["properties"]["distance"]);
            var cityIds = new List<(Guid cityId, int Count)>();
            foreach (var road in roadSet)
            {
                var roadGeoJson = (JObject)road.Detail;
                var cityAId = (Guid)roadGeoJson["properties"]["cityAId"];
                var cityBId = (Guid)roadGeoJson["properties"]["cityBId"];

                // Test whether either city at the end of this road already have two roads
                var cityHasRoads = cityIds.Where(ci => ci.cityId == cityAId || ci.cityId == cityBId).ToList();

                var citiesFullyConnected = cityHasRoads.Count(chr => chr.Count >= 2);

                Console.WriteLine($"{cityIds.Count} - {citiesFullyConnected}");

                switch (citiesFullyConnected)
                {
                    case 0:
                        // Road connects two cities and neither has a full set of connecting roads
                        // Do connection
                        roadGeoJson["properties"]["usage"] = "Accepted";
                        try
                        {
                            var a = cityIds.First(chr => chr.cityId == cityAId);
                            cityIds.Remove(a);
                            a.Count++;
                            cityIds.Add(a);
                        }
                        catch
                        {
                            cityIds.Add((cityAId, 1));
                        }
                        try
                        {
                            var b = cityIds.First(chr => chr.cityId == cityBId);
                            cityIds.Remove(b);
                            b.Count++;
                            cityIds.Add(b);
                        }
                        catch
                        {
                            cityIds.Add((cityBId, 1));
                        }
                        break;
                    case 1:
                        // Road connecting one fully connected city
                        if (cityHasRoads.Count == 1)
                        {
                            // And only one city - so create an empty connection record for the other
                            var ci = cityHasRoads.All(chr => chr.cityId == cityAId) ? (cityBId, 0) : (cityAId, 0);
                            cityIds.Add(ci);
                        }
                        break;

                    case 2:
                    default:
                        // Road connecting two already full connected cities
                        break;
                }

                if (cityIds.Count >= 10)
                {
                    break;
                }
            }

            var acceptedRoads = values.Where(v =>
                v.Detail["properties"]["cityAId"] != null &&
                v.Detail["properties"]["usage"] != null && (string)v.Detail["properties"]["usage"] == "Accepted")
                .ToList();
            salesmansJourney = new StringBuilder();
            salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
            salesmansJourney.Append("]}");
            jny = JObject.Parse(salesmansJourney.ToString());
            File.WriteAllText("Routes00.json", jny.ToString());

            var pass = 1;
            var citiesWithRoadsCount = 0;
            var citiesWithNoRoadsCount = cityIds.Count(ci => ci.Count == 0);

            // For each city with no connections
            // Determine its two closest connected neighbours and reject that road
            // and accept the two roads to and from this city to those two closest neighbours
            foreach (var ci in cityIds.Where(ci => ci.Count == 0))
            {
                Console.WriteLine($"{ci.cityId}");

                var closestNeighboursWithRoads = values.Where(v =>
                    v.Detail["properties"]["cityAId"] != null &&
                    ((Guid)v.Detail["properties"]["cityAId"] == ci.cityId || (Guid)v.Detail["properties"]["cityBId"] == ci.cityId) &&
                    v.Detail["properties"]["usage"] != null && (string)v.Detail["properties"]["usage"] == "Accepted")
                    .OrderBy(r => (double)r.Detail["properties"]["distance"])
                    .ToList();

                var closestNeighbourGuids = closestNeighboursWithRoads.SelectMany(ar => new[]
                    {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
                    .GroupBy(cg => cg)
                    .Select(cg => cg.Key)
                    .ToList();

                foreach (var cng in closestNeighbourGuids)
                {
                    var notCng = closestNeighbourGuids.Where(cg => cng != cg).ToList();

                    var cnwr = closestNeighboursWithRoads.Where(v =>
                    v.Detail["properties"]["cityAId"] != null &&
                    ((Guid)v.Detail["properties"]["cityAId"] == ci.cityId || (Guid)v.Detail["properties"]["cityBId"] == ci.cityId) &&
                    (notCng.Contains((Guid)v.Detail["properties"]["cityAId"]) || notCng.Contains((Guid)v.Detail["properties"]["cityBId"])))
                    .ToList();

                    if (!cnwr.Any())
                    {
                        continue;
                    }
                    // Road to Reject
                    var r2R = cnwr.First();
                    values.First(r => r.EntityId == r2R.EntityId).Detail["properties"]["usage"] = "Rejected";

                    // Rejected road cities
                    var cnwor = new[] { (Guid)r2R.Detail["properties"]["cityAId"], (Guid)r2R.Detail["properties"]["cityBId"] };

                    // Roads to Accept
                    var r2A = closestNeighboursWithRoads.Where(v =>
                        v.Detail["properties"]["cityAId"] != null &&
                        ((Guid)v.Detail["properties"]["cityAId"] == ci.cityId || (Guid)v.Detail["properties"]["cityBId"] == ci.cityId) &&
                        (cnwor.Contains((Guid)v.Detail["properties"]["cityAId"]) || cnwor.Contains((Guid)v.Detail["properties"]["cityBId"])));
                    foreach (var rd in r2A)
                    {
                        values.First(r => r.EntityId == rd.EntityId).Detail["properties"]["usage"] = "Accepted";
                    }
                    break;
                }
            }

            // Now we'll ensure that every city has at least two roads connecting it
            // First step is to group all of the cities and get a count for the number of roads to each one
            //var citiesWithRoads = acceptedRoads
            //    .SelectMany(ar => new[]
            //        {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
            //    .GroupBy(cg => cg)
            //    .Select(cg => new { cityId = cg.Key, Count = cg.Count() })
            //    .ToList();
            //citiesWithRoadsCount = citiesWithRoads.Count;

            acceptedRoads = values.Where(v =>
    v.Detail["properties"]["cityAId"] != null &&
    v.Detail["properties"]["usage"] != null && (string)v.Detail["properties"]["usage"] == "Accepted")
    .ToList();
            salesmansJourney = new StringBuilder();
            salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
            salesmansJourney.Append("]}");
            jny = JObject.Parse(salesmansJourney.ToString());
            File.WriteAllText("Routes01.json", jny.ToString());

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
        }
    }
}