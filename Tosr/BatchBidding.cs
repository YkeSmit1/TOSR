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
    public static class DictionaryExtension 
    {
        public static void AddOrUpdateDictionary<T>(this IDictionary<T, int> dictionary, T item)
        {
            if (!dictionary.ContainsKey(item))
                dictionary.Add(item, 1);
            else
                dictionary[item]++;
        }
    }
    public class BatchBidding
    {
        class Statistics
        {
            public int handsBid;
            public int handsNotBidBecauseofFreakhand = 0;
            public int handsNotBidBecauseOfError = 0;
            public SortedDictionary<Bid, int> contracts = new SortedDictionary<Bid, int>();
            public Dictionary<BidManager.ConstuctedSouthhandOutcome, int> outcomes = new Dictionary<BidManager.ConstuctedSouthhandOutcome, int>();
            public Dictionary<Player, int> dealers = new Dictionary<Player, int>();
            public SortedDictionary<int, int> bidsNonShape = new SortedDictionary<int, int>();
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
                        statistics.handsNotBidBecauseofFreakhand++;
                        continue;
                    }

                    var auction = bidManager.GetAuction(hand.NorthHand, hand.SouthHand);
                    AddHandAndAuction(hand, auction);
                    statistics.handsBid++;
                }
                catch (Exception exception)
                {
                    statistics.handsNotBidBecauseOfError++;
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

            // Start calculating hand
            expectedSouthHands.AppendLine(bidManager.ConstructSouthHandSafe(strHand, auction));
            Fix4CtrlBug(strHand);

            var longestSuit = Util.GetLongestSuit(strHand.NorthHand, strHand.SouthHand);
            statistics.dealers.AddOrUpdateDictionary(auction.GetDeclarer((Suit)(3 - longestSuit)));
            statistics.contracts.AddOrUpdateDictionary(auction.currentContract);
            statistics.bidsNonShape.AddOrUpdateDictionary(auction.GetBids(Player.South).Last() - auction.GetBids(Player.South, Fase.Shape).Last());
            statistics.outcomes.AddOrUpdateDictionary(bidManager.constuctedSouthhandOutcome);
        }

        private void Fix4CtrlBug(HandsNorthSouth strHand)
        {
            if (bidManager.constuctedSouthhandOutcome == BidManager.ConstuctedSouthhandOutcome.AuctionNotFoundInControls)
            {
                var controls = strHand.SouthHand.Count(x => x == 'K') + strHand.SouthHand.Count(x => x == 'A') * 2;
                var hcp = strHand.SouthHand.Count(x => x == 'J') + strHand.SouthHand.Count(x => x == 'Q') * 2 + strHand.SouthHand.Count(x => x == 'K') * 3 + strHand.SouthHand.Count(x => x == 'A') * 4;
                if (controls == 4 && hcp >= 12 && hcp - strHand.SouthHand.Count(x => x == 'J') < 12)
                    bidManager.constuctedSouthhandOutcome = BidManager.ConstuctedSouthhandOutcome.AuctionNotFoundInControlsExpected;
            }
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
            logger.Info("Save auctions");
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("txt\\HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("txt\\Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
            File.WriteAllText("txt\\ExpectedSouthHands.txt", expectedSouthHands.ToString());
        }
    }
}