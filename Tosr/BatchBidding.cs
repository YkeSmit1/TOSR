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
using ShapeDictionary = System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<string> pattern, bool zoom)>;
using ControlsDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;
using NLog;

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
        private readonly BidManager bidManager;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public BatchBidding(ShapeDictionary shapeAuctions, ControlsDictionary controlsAuctions, Dictionary<Fase, bool> fasesWithOffset)
        {
            bidManager = new BidManager(new BidGenerator(), fasesWithOffset, shapeAuctions, controlsAuctions);
        }

        public void Execute(HandsNorthSouth[] hands)
        {
            logger.Info($"Start batchbidding. Number of hands : {hands.Length}");
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
                    if (Util.IsFreakHand(string.Join("", hand.SouthHand.Split(',').Select(x => x.Length))))
                    {
                        logger.Debug($"Hand {hand.SouthHand} is a freak hand. Will not be bid");
                        continue;
                    }

                    var auction = bidManager.GetAuction(hand.NorthHand, hand.SouthHand);
                    AddHandAndAuction(hand, auction);
                }
                catch (Exception exception)
                {
                    logger.Warn(exception, $"Hand:{hand.SouthHand}");
                    stringbuilder.AppendLine(exception.Message);
                }
            }
            stringbuilder.AppendLine(@$"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}
Duplicate auctions are written to ""HandPerAuction.txt""
Statistics are written to ""Statistics.txt""
Error info for hand-matching is written to ""ExpectedSouthHands.txt""");
            SaveAuctions();

            logger.Info($"End batchbidding");

            MessageBox.Show(stringbuilder.ToString(), "Batch bidding done");
        }

        private void AddHandAndAuction(HandsNorthSouth strHand, Auction auction)
        {
            var suitLengthSouth = strHand.SouthHand.Split(',').Select(x => x.Length);
            var str = string.Join("", suitLengthSouth);

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
            logger.Debug("Save auctions");
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
            File.WriteAllText("ExpectedSouthHands.txt", expectedSouthHands.ToString());
        }
    }
}