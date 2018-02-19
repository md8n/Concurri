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
            //    Console.WriteLine($"{jTempl} => {jCode}");

            //    var e = new Engine();
            //    var result = e.Execute(jCode).GetCompletionValue().ToObject();
            //    Console.WriteLine(result);
            //}

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

            // Travelling Salesman - Setup

            // 100 LonLat points and a GeoJSON FeatureCollection
            var rnd = new Random();
            for (var ix = 0; ix < 100; ix++)
            {
                var lat = -21.0 + -7.0 * rnd.NextDouble();
                var lon = 142.0 + 7.0 * rnd.NextDouble();
                var pointGeo =
                    $"{{\"type\":\"Feature\",\"properties\":{{\"cityNo\":{ix}}},\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{lon},{lat}]}}}}";
                var lonLat = JObject.Parse(pointGeo);
                var coord = new Value(lonLat);
                values.Add(coord);
            }

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

                var roadsAdded = 0;

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
                    values.Add(roadAtoBValue);
                    roadsAdded++;
                }

                Console.WriteLine($"City #:{ix}, added {roadsAdded} roads");
            }

            // We'll start by adding all the shortest ones as the first set of 'actual' roads
            // A minimum of two * (cityCount - 1) roads will be required
            var roadSet = values
                .Where(r => r.Detail["properties"]["cityAId"] != null && (r.Detail["properties"]["usage"] == null || (string)r.Detail["properties"]["usage"] == "Not Set"))
                .OrderBy(r => (double)r.Detail["properties"]["distance"]);
            var cityIds = new List<( Guid, int)>();
            foreach (var road in roadSet)
            {
                var roadGeoJson = (JObject) road.Detail;
                var cityAId = (Guid)roadGeoJson["properties"]["cityAId"];
                var cityBId = (Guid)roadGeoJson["properties"]["cityBId"];

                // Test whether either city at the end of this road already have two roads
                var cityHasRoads = cityIds.Where(ci => ci.Item1 == cityAId || ci.Item1 == cityBId).ToList();

                switch (cityHasRoads.Where(chr => chr.Item2 >= 2).Count())
                {
                    case 1:
                        // Road connecting one fully connected city
                        if (cityHasRoads.Count == 1)
                        {
                            // And only one city - so create an empty connection record for the other
                            if (cityHasRoads.All(chr => chr.Item1 == cityAId))
                            {
                                // Create an empty record for B
                                cityHasRoads.Add((cityBId, 0));
                            }
                            else
                            {
                                // Create an empty record for A
                                cityHasRoads.Add((cityAId, 0));
                            }
                        }

                        // But the other exists already with zero or one roads to it
                        continue;
                    case 0:
                        // Road connects two cities and neither has a full set of connecting roads
                        // Do connection
                        roadGeoJson["properties"]["usage"] = "Accepted";
                        try
                        {
                            var a = cityHasRoads.First(chr => chr.Item1 == cityAId);
                            a.Item2++;
                        }
                        catch
                        {
                            cityHasRoads.Add((cityAId, 1));
                        }
                        try
                        {
                            var b = cityHasRoads.First(chr => chr.Item1 == cityBId);
                            b.Item2++;
                        }
                        catch
                        {
                            cityHasRoads.Add((cityBId, 1));
                        }
                        break;
                    case 2:
                    default:
                        // Road connecting two already full connected cities
                        continue;
                }
            }

            var acceptedRoads = values.Where(v => 
                v.Detail["properties"]["cityAId"] != null &&
                v.Detail["properties"]["usage"] != null && (string) v.Detail["properties"]["usage"] == "Accepted")
                .ToList();
            salesmansJourney = new StringBuilder();
            salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
            salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
            salesmansJourney.Append("]}");
            jny = JObject.Parse(salesmansJourney.ToString());
            File.WriteAllText("firstRoutes.json", jny.ToString());

            var pass = 1;
            var citiesWithRoadsCount = 0;
            var citiesWithNoRoadsCount = 0;

            // Now we'll ensure that every city has at least two roads connecting it
            // First step is to group all of the cities and get a count for the number of roads to each one
            var citiesWithRoads = acceptedRoads
                .SelectMany(ar => new[]
                    {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
                .GroupBy(cg => cg)
                .Select(cg => new { cityId = cg.Key, Count = cg.Count() })
                .ToList();
            citiesWithRoadsCount = citiesWithRoads.Count;

            // Then there's a need to check for any cities with no roads at all connected to them (a possibility)
            // and add these to the same list with a count of zero for each one.
            if (citiesWithRoadsCount < cityCount)
            {
                var citiesWithNoRoads = values.Where(c =>
                        c.Detail["properties"]["cityNo"] != null &&
                        !citiesWithRoads.Select(cr => cr.cityId).Contains(c.EntityId))
                    .Select(cn => new {cityId = cn.EntityId, Count = 0})
                    .ToList();

                citiesWithNoRoadsCount = citiesWithNoRoads.Count;

                citiesWithRoads.AddRange(citiesWithNoRoads);
            }

            do
            {
                if (pass > 100)
                {
                    break;
                }

                // Take this list and add the two closest roads for each city with less than two roads
                // and output the result.
                foreach (var cwr in citiesWithRoads.Where(cwr => cwr.Count < 2))
                {
                    roadSet = values.Where(v =>
                            v.Detail["properties"]["cityAId"] != null &&
                            v.Detail["properties"]["usage"] != null &&
                            (string) v.Detail["properties"]["usage"] == "Not Set" &&
                            ((Guid) v.Detail["properties"]["cityAId"] == cwr.cityId ||
                             (Guid) v.Detail["properties"]["cityBId"] == cwr.cityId))
                        .OrderBy(r => (double) r.Detail["properties"]["distance"])
                        .Take(2 - cwr.Count);

                    foreach (var road in roadSet)
                    {
                        var roadGeoJson = (JObject) road.Detail;

                        roadGeoJson["properties"]["usage"] = "Accepted";
                    }
                }

                acceptedRoads = values.Where(v =>
                        v.Detail["properties"]["cityAId"] != null &&
                        v.Detail["properties"]["usage"] != null &&
                        (string) v.Detail["properties"]["usage"] == "Accepted")
                    .ToList();
                salesmansJourney = new StringBuilder();
                salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
                salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
                salesmansJourney.Append("]}");
                jny = JObject.Parse(salesmansJourney.ToString());
                File.WriteAllText($"routes{pass++}.json", jny.ToString());

                // Identify cities with too many roads
                var citiesWithTooManyRoads = acceptedRoads
                    .SelectMany(ar => new[]
                        {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
                    .GroupBy(cg => cg)
                    .Select(cg => new {cityId = cg.Key, Count = cg.Count()})
                    .Where(cwr => cwr.Count > 2)
                    .ToList();

                foreach (var cwr in citiesWithTooManyRoads)
                {
                    var road = values.Where(v =>
                            v.Detail["properties"]["cityAId"] != null &&
                            v.Detail["properties"]["usage"] != null &&
                            (string) v.Detail["properties"]["usage"] == "Accepted" &&
                            ((Guid) v.Detail["properties"]["cityAId"] == cwr.cityId ||
                             (Guid) v.Detail["properties"]["cityBId"] == cwr.cityId))
                        .OrderByDescending(r => (double) r.Detail["properties"]["distance"])
                        .First();
                        //.Take(cwr.Count - 2);

                    //foreach (var road in roadSet)
                    //{
                        Guid otherCityId;
                        otherCityId = (Guid) road.Detail["properties"]["cityAId"] == cwr.cityId
                            ? (Guid) road.Detail["properties"]["cityBId"]
                            : (Guid) road.Detail["properties"]["cityAId"];
                        var otherCityHasTooManyRoads = values.Count(v =>
                                                           v.Detail["properties"]["cityAId"] != null &&
                                                           v.Detail["properties"]["usage"] != null &&
                                                           (string) v.Detail["properties"]["usage"] == "Accepted" &&
                                                           ((Guid) v.Detail["properties"]["cityAId"] == otherCityId ||
                                                            (Guid) v.Detail["properties"]["cityBId"] == otherCityId)) >
                                                       2;

                        if (!otherCityHasTooManyRoads)
                        {
                            continue;
                        }

                        var roadGeoJson = (JObject) road.Detail;

                        roadGeoJson["properties"]["usage"] = "Rejected";
                    //}
                }

                acceptedRoads = values.Where(v =>
                        v.Detail["properties"]["cityAId"] != null &&
                        v.Detail["properties"]["usage"] != null &&
                        (string) v.Detail["properties"]["usage"] == "Accepted")
                    .ToList();
                salesmansJourney = new StringBuilder();
                salesmansJourney.Append("{\"type\":\"FeatureCollection\",\"features\":[");
                salesmansJourney.Append(string.Join(',', acceptedRoads.Select(v => v.Detail.ToString())));
                salesmansJourney.Append("]}");
                jny = JObject.Parse(salesmansJourney.ToString());
                File.WriteAllText($"routes{pass++}.json", jny.ToString());

                citiesWithRoads = acceptedRoads
                    .SelectMany(ar => new[]
                        {(Guid) ar.Detail["properties"]["cityAId"], (Guid) ar.Detail["properties"]["cityBId"]})
                    .GroupBy(cg => cg)
                    .Select(cg => new {cityId = cg.Key, Count = cg.Count()})
                    .ToList();
                citiesWithRoadsCount = citiesWithRoads.Count(cwr => cwr.Count >= 2);

                // Then there's a need to check for any cities with no roads at all connected to them (a possibility)
                // and add these to the same list with a count of zero for each one.
                if (citiesWithRoadsCount < cityCount)
                {
                    var citiesWithNoRoads = values.Where(c =>
                            c.Detail["properties"]["cityNo"] != null &&
                            !citiesWithRoads.Select(cr => cr.cityId).Contains(c.EntityId))
                        .Select(cn => new {cityId = cn.EntityId, Count = 0})
                        .ToList();

                    citiesWithNoRoadsCount = citiesWithNoRoads.Count;
                    citiesWithRoads.AddRange(citiesWithNoRoads);
                }
            } while (citiesWithRoadsCount != cityCount || citiesWithNoRoadsCount != 0);
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