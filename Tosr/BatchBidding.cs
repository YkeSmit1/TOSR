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
            public Dictionary<BidManager.ConstructedSouthhandOutcome, int> outcomes = new Dictionary<BidManager.ConstructedSouthhandOutcome, int>();
            public Dictionary<Player, int> dealers = new Dictionary<Player, int>();
            public SortedDictionary<int, int> bidsNonShape = new SortedDictionary<int, int>();
        }

        private readonly Statistics statistics = new Statistics();
        private readonly Dictionary<string, List<string>> handPerAuction = new Dictionary<string, List<string>>();
        private readonly StringBuilder expectedSouthHands = new StringBuilder();
        private readonly BidManager bidManager;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public BatchBidding(ReverseDictionaries reverseDictionaries, Dictionary<Fase, bool> fasesWithOffset)
        {
            bidManager = new BidManager(new BidGenerator(), fasesWithOffset, reverseDictionaries, false);
        }

        public void Execute(HandsNorthSouth[] hands)
        {
            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            if (hands == null)
            {
                MessageBox.Show("Cannot do batchbidding. Shuffle first.", "Error");
                return;
            }

            logger.Info($"Start batchbidding. Number of hands : {hands.Length}");

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
                    logger.Warn(exception, $"Error:{exception.Message} North hand:{hand.NorthHand}. South hand:{hand.SouthHand}. Controls:{Util.GetControlCount(hand.SouthHand)}. " +
                        $"HCP: {Util.GetHcpCount(hand.SouthHand)}. Controls in suit: {Util.GetHandWithOnlyControlsAs4333(hand.SouthHand, "AK")}");
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
            if (!auction.hasSignedOff)
                expectedSouthHands.AppendLine(bidManager.ConstructSouthHandSafe(strHand, auction));

            var longestSuit = Util.GetLongestSuit(strHand.NorthHand, strHand.SouthHand);
            statistics.dealers.AddOrUpdateDictionary(auction.GetDeclarer((Suit)(3 - longestSuit.Item1)));
            statistics.contracts.AddOrUpdateDictionary(auction.currentContract > new Bid(7, Suit.NoTrump) ? new Bid(7, Suit.NoTrump) : auction.currentContract);
            if (!auction.hasSignedOff)
                statistics.bidsNonShape.AddOrUpdateDictionary(auction.GetBids(Player.South).Where(bid => bid.bidType == BidType.bid).Last() - auction.GetBids(Player.South, Fase.Shape).Last());
            statistics.outcomes.AddOrUpdateDictionary(bidManager.constructedSouthhandOutcome);
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