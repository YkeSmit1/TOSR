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
        readonly Dictionary<string, string> shapeAuctions;
        readonly Dictionary<string, List<string>> controlsAuctions;
        readonly BidManager bidManager;

        public BatchBidding(Dictionary<string, string> shapeAuctions, Dictionary<string, List<string>> controlsAuctions, Dictionary<Fase, bool> fasesWithOffset)
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

            if (Common.Common.IsFreakHand(str))
                return;

            var strAuction = auction.GetBidsAsString(Fase.Shape);

            AddHandPerAuction(str, strAuction);

            var longestSuit = Common.Common.GetLongestSuit(strHand.NorthHand, strHand.SouthHand);

            statistics.AddOrUpdateDeclarer(auction.GetDeclarer((Suit)(3 - longestSuit)));
            statistics.AddOrUpdateContract(auction);

            // Start calculating hand
            expectedSouthHands.AppendLine(ConstructSouthHand(strHand, auction, shapeAuctions[strAuction]));
        }

        private string ConstructSouthHand(HandsNorthSouth strHand, Auction auction, string shapeLengthStr)
        {
            string strControls = BidManager.GetAuctionForControlsWithOffset(auction, new Bid(3, Suit.Diamonds));
            if (!controlsAuctions.TryGetValue(strControls, out var possibleControls))
            {
                return $"Auction not found in controls. controls: {strControls}. NorthHand: {strHand.NorthHand}.";
            }
            var matches = bidManager.GetMatchesWithNorthHand(shapeLengthStr, possibleControls, strHand.NorthHand);
            switch (matches.Count())
            {
                case 0:
                    return $"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {strHand.NorthHand}.";
                case 1:
                    {
                        var southHandStr = HandWithx(strHand.SouthHand);
                        var shapeStr = matches.First();

                        if (shapeStr == southHandStr)
                            return $"Match is found: {shapeStr}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
                        else
                            return $"SouthHand is not equal to expected. Expected: {shapeStr}. Actual {southHandStr}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
                    }
                default:
                    return $"Multiple matches found. Matches: {string.Join('|', matches)}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
            }
        }

        /// <summary>
        /// Replaces cards smaller then queen into x's;
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public static string HandWithx(string hand)
        {
            var southHand = new string(hand).ToList();
            var relevantCards = new[] { 'A', 'K', 'Q', ',' };
            southHand = southHand.Select(x => x = !relevantCards.Contains(x) ? 'x' : x).ToList();
            var southHandStr = new string(southHand.ToArray());
            return southHandStr;
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