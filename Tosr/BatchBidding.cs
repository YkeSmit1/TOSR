using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Common;

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

        private readonly Statistics statistics = new Statistics();
        private readonly Dictionary<string, List<string>> handPerAuction = new Dictionary<string, List<string>>();
        private readonly StringBuilder expectedSouthHands = new StringBuilder();
        readonly Dictionary<string, Tuple<string, bool>> shapeAuctions;
        readonly Dictionary<string, List<string>> controlsAuctions;
        readonly BidManager bidManager;

        public BatchBidding(Dictionary<string, Tuple<string, bool>> shapeAuctions, Dictionary<string, List<string>> controlsAuctions, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.shapeAuctions = shapeAuctions;
            this.controlsAuctions = controlsAuctions;
            bidManager = new BidManager(new BidGenerator(), fasesWithOffset, shapeAuctions, controlsAuctions);
        }

        public void Execute(HandsNorthSouth[] hands)
        {

            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            if (hands == null)
            {
                MessageBox.Show("Cannot do batchbidding. Shuffle first.");
                return;
            }

            foreach (var hand in hands)
            {
                try
                {
                    var auction = bidManager.GetAuction(hand.NorthHand, hand.SouthHand);
                    AddHandAndAuction(hand, auction);
                }
                catch (Exception exception)
                {
                    stringbuilder.AppendLine(exception.Message);
                }
            }
            stringbuilder.AppendLine(@$"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}
Duplicate auctions are written to ""HandPerAuction.txt""
Statistics are written to ""Statistics.txt""
Error info for hand-matching is written to ""ExpectedSouthHands.txt""");
            SaveAuctions();

            MessageBox.Show(stringbuilder.ToString(), "Batch bidding done");
        }

        private void AddHandAndAuction(HandsNorthSouth strHand, Auction auction)
        {
            var suitLengthSouth = strHand.SouthHand.Split(',').Select(x => x.Length);
            var str = string.Join("", suitLengthSouth);

            if (Util.IsFreakHand(str))
                return;

            var strAuction = auction.GetBidsAsString(Fase.Shape);

            AddHandPerAuction(str, strAuction);

            var longestSuit = Util.GetLongestSuit(strHand.NorthHand, strHand.SouthHand);

            statistics.AddOrUpdateDeclarer(auction.GetDeclarer((Suit)(3 - longestSuit)));
            statistics.AddOrUpdateContract(auction);

            // Start calculating hand
            expectedSouthHands.AppendLine(bidManager.ConstructSouthHandSafe(strHand, auction));
        }

        private void AddHandPerAuction(string str, string strAuction)
        {
            if (!handPerAuction.ContainsKey(strAuction))
                handPerAuction[strAuction] = new List<string>();
            if (!handPerAuction[strAuction].Contains(str))
                handPerAuction[strAuction].Add(str);
        }

        private void SaveAuctions()
        {
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
            File.WriteAllText("ExpectedSouthHands.txt", expectedSouthHands.ToString());
        }
    }
}