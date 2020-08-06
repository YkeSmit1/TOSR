using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tosr
{
    public class BatchBidding
    {
        class Statistics
        {
            public Dictionary<Player, int> dealers = new Dictionary<Player, int>();
            public SortedDictionary<Bid, int> contracts = new SortedDictionary<Bid, int>();

            public void AddOrUpdateDeclarer(Player player)
            {
                if (!dealers.ContainsKey(player))
                {
                    dealers.Add(player, 1);
                }
                else
                {
                    dealers[player]++;
                }
            }

            public void AddOrUpdateContract(Auction auction)
            {
                if (!contracts.ContainsKey(auction.currentContract))
                {
                    contracts.Add(auction.currentContract, 1);
                }
                else
                {
                    contracts[auction.currentContract]++;
                }
            }
        }

        readonly IBidGenerator bidGenerator;
        private readonly Statistics statistics = new Statistics();
        private readonly Dictionary<string, List<string>> handPerAuction = new Dictionary<string, List<string>>();

        public BatchBidding()
        {
            this.bidGenerator = new BidGenerator();
        }

        public void Execute(HandsNorthSouth[] hands)
        {
            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            for (int i = 0; i < hands.Length; ++i)
            {
                try
                {
                    var auction = BidManager.GetAuction(hands[i].SouthHand, bidGenerator);
                    AddHandAndAuction(hands[i], auction);
                }
                catch (Exception exception)
                {
                    stringbuilder.AppendLine(exception.Message);
                }
            }
            stringbuilder.AppendLine(@$"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}
Duplicate auctions are written to ""HandPerAuction.txt""
Statistics are written to ""Statistics.txt""");
            SaveAuctions();

            MessageBox.Show(stringbuilder.ToString(), "Batch bidding done");
        }

        private void AddHandAndAuction(HandsNorthSouth strHand, Auction auction)
        {
            var bids = auction.GetBids(Player.South);
            var strAuction = bids[0..^4];

            var suitLengthSouth = strHand.SouthHand.Split(',').Select(x => x.Length);
            var str = string.Join("", suitLengthSouth);

            if (IsFreakHand(str))
                return;

            if (!handPerAuction.ContainsKey(strAuction))
                handPerAuction[strAuction] = new List<string>();
            if (!handPerAuction[strAuction].Contains(str))
                handPerAuction[strAuction].Add(str);

            var suitLengthNorth = strHand.NorthHand.Split(',').Select(x => x.Length);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);

            var longestSuit = (Suit)(3 - suitLengthNS.ToList().IndexOf(suitLengthNS.Max()));

            statistics.AddOrUpdateDeclarer(auction.GetDeclarer(longestSuit));
            statistics.AddOrUpdateContract(auction);
        }

        private static bool IsFreakHand(string handLength)
        {
            var handPattern = string.Concat(handLength.OrderByDescending(y => y));
            return handPattern == "7321" || Char.GetNumericValue(handPattern[0]) >= 8 ||
                Char.GetNumericValue(handPattern[0]) + Char.GetNumericValue(handPattern[1]) >= 12;
        }

        private void SaveAuctions()
        {
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
        }
    }
}
