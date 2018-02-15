using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Flurl;
using Flurl.Http;
using Jil;
using LossMaker.Models;

namespace LossMaker
{
    class Program
    {
        private static Station TargetStation;

        static void Main(string[] args)
        {

            DoTheWork().GetAwaiter().GetResult();
            Console.WriteLine("Finished. Press any key to exit....");
            Console.ReadLine();
        }

        private static async Task DoTheWork()
        {
            var targetSystem = "G 141-21";
            var targetStation = "Scithers Hub";
            var minLoss = 600;
            var lyRadius = 400;

            await DownloadFilesIfRequired();

            var inRange = await GetSystemsInSphereAround(targetSystem, lyRadius);

            var commodityData = GetCommodityData();
            var populatedSystems = GetSystemData(inRange, targetSystem);
            var stations = GetStationData(populatedSystems);

            TargetStation = stations.Values.Where(x => x.name == targetStation).Single();

            var targetMarketPrices = GetPriceData(stations, minLoss);

            FindLargestLossTrades(stations, targetMarketPrices, commodityData, minLoss);


        }

        private static async Task DownloadFilesIfRequired()
        {
            var downloadRequired = false;
            const string DataFileName = "listings.csv";

            if (!File.Exists(DataFileName))
            {
                Console.WriteLine("No market data found, downloading...");
                downloadRequired = true;
            }
            else
            {
                var createDate = File.GetCreationTime(DataFileName);
                if (createDate < DateTime.Now.Subtract(TimeSpan.FromDays(1)))
                {
                    Console.WriteLine("Market data is stale,  re-downloading...");
                    downloadRequired = true;
                    File.Delete(DataFileName);
                }
            }

            if (downloadRequired)
            {

                const string url = "https://eddb.io/archive/v5/listings.csv";

                using (var progress = new ConsoleProgressBar())
                {
                    await FileDownloader.DownloadFileAsync(url, DataFileName, progress, CancellationToken.None);
                }
                Console.WriteLine("  Download complete.");
            }
        }

        private static void FindLargestLossTrades(Dictionary<int, Station> stations, Dictionary<int, Price> targetMarketPrices, List<Commodity> commodityData, int minLoss)
        {

            var lossTrades = new List<(string Commodity, int Loss, Price Price, Station PurchaseStation, int SellPrice)>(); //all money losing trades

            foreach (var commodityId in targetMarketPrices.Keys)
            {
                var commodity = commodityData.Where(x => x.Id == commodityId).First();
                var sellPrice = targetMarketPrices[commodityId];

                Console.WriteLine($"Looking for losses on {commodity.Name}...");

                foreach (var station in stations)
                {
                    Price price;

                    if (station.Value.Prices.TryGetValue(commodityId, out price))
                    {
                        var lossPerTonne = price.BuyPrice - sellPrice.SellPrice;
                        if (lossPerTonne > minLoss) //it's a loss
                        {
                            lossTrades.Add((commodity.Name, lossPerTonne.GetValueOrDefault(), price, station.Value, sellPrice.SellPrice.GetValueOrDefault()));
                        }
                    }
                } // next possible purchase station


            } // next commodity for sale in dest system


            var top5LossesPerCommodity = lossTrades.GroupBy(x => x.Commodity)
                                                    .SelectMany(g => g.OrderByDescending(x => x.Loss).Take(5));

            foreach (var loss in top5LossesPerCommodity)
            {
                Console.WriteLine($"    ({loss.Loss}cr/t loss) Purchase {loss.Commodity} at {loss.PurchaseStation.SystemName} ({loss.PurchaseStation.DistanceToSystem}ly) : {loss.PurchaseStation.name} ({loss.PurchaseStation.distance_to_star}ls, {loss.PurchaseStation.max_landing_pad_size} pad) for {loss.Price.BuyPrice}cr/t, sell for {loss.SellPrice}");
            }


        }

        private static Dictionary<int, Price> GetTargetPrices(string targetStation, Dictionary<int, Station> stations)
        {
            var stationObj = stations.First(x => x.Value.name == targetStation);

            return stationObj.Value.Prices;
        }

        private static async Task<Dictionary<int, EdsmSystem>> GetSystemsInSphereAround(string target, int radius)
        {
            Console.WriteLine("Getting systems in sphere around target...");
            var url = $"https://www.edsm.net/api-v1/sphere-systems?showId=1&radius={radius}&systemName={Url.Encode(target, true)}";

            using (var reader = new StreamReader(await url.GetStreamAsync()))
            {
                var values = JSON.Deserialize<List<EdsmSystem>>(reader).Select(x => KeyValuePair.Create(x.Id, x));

                Console.WriteLine($"   Found {values.Count()} populated systems in range...");
                return new Dictionary<int, EdsmSystem>(values);
            }
        }

        private static Dictionary<int, Station> GetStationData(Dictionary<int, EddbSystem> toInclude)
        {
            Console.WriteLine("Loading Station data...");
            using (var reader = new StreamReader(File.OpenRead("stations.json")))
            {
                var data = JSON.Deserialize<List<Station>>(reader)
                    .Where(x => x.system_id.HasValue
                                && toInclude.ContainsKey(x.system_id.Value)
                                && x.max_landing_pad_size == "L")
                    .Select(x =>
                    {
                        x.SystemName = toInclude[x.system_id.Value].name;
                        x.DistanceToSystem = toInclude[x.system_id.Value].distance;
                        return KeyValuePair.Create(x.id.Value, x);
                    });

                Console.WriteLine($"   Found {data.Count()} stations with large pads in those systems...");
                return new Dictionary<int, Station>(data);
            }
        }

        private static Dictionary<int, EddbSystem> GetSystemData(Dictionary<int, EdsmSystem> toInclude, string targetSystem)
        {
            Console.WriteLine("Loading System data...");

            using (var reader = new StreamReader(File.OpenRead("systems_populated.json")))
            {

                var data = JSON.Deserialize<List<EddbSystem>>(reader)
                    .Where(x => x.edsm_id.HasValue && x.id.HasValue &&
                    (toInclude.ContainsKey(x.edsm_id.Value) || //in our set of in-range systems
                     x.name == targetSystem)) // Our destination
                    .Select(x =>
                    {
                        x.distance = toInclude[x.edsm_id.Value].Distance;
                        return KeyValuePair.Create(x.id.Value, x);
                    });

                return new Dictionary<int, EddbSystem>(data);
            }
        }

        private static Dictionary<int, Price> GetPriceData(Dictionary<int, Station> toInclude, int minLoss)
        {
            //This method is highly performance sensitive!

            Console.WriteLine("Loading pricing data.  This may take a while...");

            var returnValue = new Dictionary<int, Price>(50);

            using (var reader = new StreamReader(File.OpenRead("listings.csv")))
            {
                var parser = new CsvReader(reader);
                parser.Configuration.RegisterClassMap<PriceMap>();

                var releventPrices = 0;

                foreach (var price in parser.GetRecords<Price>()) //iterate through this for flatter mem usage, materialising the full set costs > 350Mb
                {

                    //Handle getting purchase prices into searchable systems
                    if (price.BuyPrice.GetValueOrDefault(int.MaxValue) > minLoss && //can't make a loss if buying for less than the loss amount... this greatly reduces our memory consumption
                        toInclude.ContainsKey(price.StationId))
                    {
                        toInclude[price.StationId].Prices.Add(price.CommodityId, price);
                        releventPrices++;
                    }


                    //if for the target system
                    if (price.StationId == TargetStation.system_id)
                    {
                        returnValue.Add(price.CommodityId, price);
                    }

                }

                return returnValue;
            }
        }


        private static List<Commodity> GetCommodityData()
        {
            Console.WriteLine("Loading commodity data...");
            using (var reader = new StreamReader(File.OpenRead("commodities.json")))
            {
                return JSON.Deserialize<List<Commodity>>(reader);
            }

        }
    }
}
