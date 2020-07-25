using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

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
                    Auction auction = new Auction();
                    BidManager.GetAuction(hands[i].SouthHand, auction, bidGenerator);
                    AddHandAndAuction(hands[i], auction);
                }
                catch (Exception exception)
                {
                    stringbuilder.AppendLine(exception.Message);
                }
            }
            stringbuilder.AppendLine($"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
            SaveAuctions();

            MessageBox.Show(stringbuilder.ToString());
        }

        private void AddHandAndAuction(HandsNorthSouth strHand, Auction auction)
        {
            var bids = auction.GetBids(Player.South);
            var strAuction = bids[0..^4];

            var suitLengthNorth = strHand.NorthHand.Split(',').Select(x => x.Length);
            var str = string.Join("", suitLengthNorth);

            if (IsFreakHand(str))
                return;

            if (!handPerAuction.ContainsKey(strAuction))
                handPerAuction[strAuction] = new List<string>();
            if (!handPerAuction[strAuction].Contains(str))
                handPerAuction[strAuction].Add(str);

            var suitLengthSouth = strHand.SouthHand.Split(',').Select(x => x.Length);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);

            var longestSuit = (Suit)(3 - suitLengthNS.ToList().IndexOf(suitLengthNS.Max()));

            statistics.AddOrUpdateDeclarer(auction.GetDeclarer(longestSuit));
            statistics.AddOrUpdateContract(auction);
        }

        private static bool IsFreakHand(string handLength)
        {
            var handPattern = new string(handLength.OrderByDescending(y => y).ToArray());
            return handPattern == "7321" || handPattern[0] == '8' || handPattern[0] == '9' ||
                   (handPattern[0] == '7' && handPattern[1] == '5') ||
                    (handPattern[0] == '6' && handPattern[1] == '6') ||
                    (handPattern[0] == '7' && handPattern[1] == '6');
        }

        private void SaveAuctions()
        {
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
        }
    }
}
