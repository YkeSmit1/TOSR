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
        private readonly StringBuilder expectedSouthHands = new StringBuilder();
        Dictionary<string, string> shapeAuctions;
        Dictionary<string, List<string>> controlsAuctions;
        Dictionary<Fase, bool> fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));

        public BatchBidding()
        {
            bidGenerator = new BidGenerator();
        }

        public void Execute(HandsNorthSouth[] hands, Dictionary<string, string> shapeAuctions, Dictionary<string, List<string>> controlsAuctions)
        {
            this.shapeAuctions = shapeAuctions;
            this.controlsAuctions = controlsAuctions;

            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            foreach (var hand in hands)
            {
                try
                {
                    var auction = BidManager.GetAuction(hand.SouthHand, bidGenerator, fasesWithOffset);
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

            if (IsFreakHand(str))
                return;

            var strAuction = auction.GetBidsAsString(Player.South, x => x.Value[Player.South].fase == Fase.Shape);

            AddHandPerAuction(str, strAuction);

            var suitLengthNorth = strHand.NorthHand.Split(',').Select(x => x.Length);
            var suitLengthNS = suitLengthNorth.Zip(suitLengthSouth, (x, y) => x + y);

            var longestSuit = (Suit)(3 - suitLengthNS.ToList().IndexOf(suitLengthNS.Max()));

            statistics.AddOrUpdateDeclarer(auction.GetDeclarer(longestSuit));
            statistics.AddOrUpdateContract(auction);

            // Start calculating hand
            expectedSouthHands.AppendLine(ConstructSouthHand(strHand, auction, shapeAuctions[strAuction]));
        }

        private string ConstructSouthHand(HandsNorthSouth strHand, Auction auction, string shapeLengthStr)
        {
            string strControls = GetAuctionForControlsWithOffset(auction, new Bid(3, Suit.Diamonds));
            if (!controlsAuctions.TryGetValue(strControls, out var possibleControls))
            {
                return $"Auction not found in controls. controls: {strControls}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
            }
            var matches = GetMatchesWithNorthHand(shapeLengthStr, possibleControls, strHand.NorthHand);
            switch (matches.Count())
            {
                case 0:
                    return $"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
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
        /// Extracts the control and scanning part of the auction and applies an offset of offsetBid
        /// </summary>
        /// <param name="auction">Generated auction </param>
        /// <param name="offsetBid">Offset used to generate AuctionsByControl.txt</param>
        /// <returns></returns>
        private static string GetAuctionForControlsWithOffset(Auction auction, Bid offsetBid)
        {
            var lastBidShape = auction.GetBids(Player.South, x => x.Value[Player.South].fase == Fase.Shape).Last();
            var bidsControls = auction.GetBids(Player.South, x => x.Value[Player.South].fase != Fase.Shape);
            var offSet = lastBidShape - offsetBid;
            bidsControls = bidsControls.Select(b => b -= offSet);
            var strControls = string.Join("", bidsControls);
            return strControls;
        }

        private IEnumerable<string> GetMatchesWithNorthHand(string shapeLengthStr, List<string> possibleControls, string northHandStr)
        {
            var northHand = northHandStr.Split(',');
            foreach (var controlStr in possibleControls)
            {
                var controlByShape = MergeControlAndShape(controlStr, shapeLengthStr);
                if (controlByShape.Count() == 4)
                {
                    if (Match(controlByShape.ToArray(), northHand))
                        yield return string.Join(',', controlByShape);
                }
            }
        }

        /// <summary>
        /// Merges shapes and controls. If controls does not fit, it returns an IEnumerable with length < 4
        /// TODO This function needs improvement
        /// </summary>
        /// <param name="controlStr">"Axxx,Kxx,Qxx,xxx"</param>
        /// <param name="shapeLengthStr">"3451"</param>
        /// <returns>"Qxx,Kxxx,Axxxx,x"</returns>
        public static IEnumerable<string> MergeControlAndShape(string controlStr, string shapeLengthStr)
        {
            var controls = controlStr.Split(',').Select(x => x.TrimEnd('x')).ToArray();
            var shapes = shapeLengthStr.ToArray().Select(x => float.Parse(x.ToString())).ToList(); // 3424
            // This is because there can be two suits with the same length. So we added a small offset to make it unique
            foreach (var suit in Enumerable.Range(0, 4))
            {
                shapes[suit] += (float)(4 - suit) / 10;
            }

            var shapesOrdered = shapes.OrderByDescending(x => x).ToList(); // 4432

            var shapesDic = shapes.ToDictionary(key => shapes.IndexOf(key), value => shapesOrdered.IndexOf(value));
            foreach (var suit in Enumerable.Range(0, 4))
            {
                var shape = shapes[suit];
                string controlStrSuit = controls[shapesDic[suit]];
                if (shape < controlStrSuit.Length)
                {
                    yield break;
            }
                yield return controlStrSuit + new string('x', (int)shape - controlStrSuit.Length);
        }
        }

        private bool Match(string[] hand1, string[] hand2)
        {
            var relevantCards = new[] { 'A', 'K', 'Q' };
            foreach (var suit in Enumerable.Range(0, 4))
            {
                foreach (var c in relevantCards)
                {
                    if (hand1[suit].Contains(c) && hand2[suit].Contains(c))
                        return false;
                }
            }
            return true;
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

        private static bool IsFreakHand(string handLength)
        {
            var handPattern = string.Concat(handLength.OrderByDescending(y => y));
            return handPattern == "7321" || int.Parse(handPattern[0].ToString()) >= 8 ||
                int.Parse(handPattern[0].ToString()) + int.Parse(handPattern[1].ToString()) >= 12;
        }

        private void SaveAuctions()
        {
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
            File.WriteAllText("ExpectedSouthHands.txt", expectedSouthHands.ToString());
        }

        public Dictionary<string, string> GenerateAuctionsForShape()
        {
            var auctions = new Dictionary<string, string>();
            for (int spades = 0; spades < 8; spades++)
            {
                for (int hearts = 0; hearts < 8; hearts++)
                {
                    for (int diamonds = 0; diamonds < 8; diamonds++)
                    {
                        for (int clubs = 0; clubs < 8; clubs++)
                        {
                            if (spades + hearts + diamonds + clubs == 13)
                            {
                                var hand = new string('x', spades) + "," + new string('x', hearts) + "," + new string('x', diamonds) + "," + new string('x', clubs);
                                var suitLengthSouth = hand.Split(',').Select(x => x.Length);
                                var str = string.Join("", suitLengthSouth);

                                if (!IsFreakHand(str))
                                {
                                    var auction = BidManager.GetAuction(hand, new BidGenerator(), fasesWithOffset);
                                    auctions.Add(auction.GetBidsAsString(Player.South, x => x.Value[Player.South].fase == Fase.Shape), str);
                                }
                            }
                        }
                    }
                }
            }
            return auctions;
        }

        public Dictionary<string, List<string>> GenerateAuctionsForControls()
        {
            string[] controls = new[] { "", "A", "K", "Q", "AK", "AQ", "KQ", "AKQ" };

            var poss = new List<string[]>();

            foreach (var spade in controls)
            {
                foreach (var hearts in controls)
                {
                    foreach (var diamonds in controls)
                    {
                        foreach (var clubs in controls)
                        {
                            var c = spade + hearts + diamonds + clubs;

                            if (c.Count(x => x == 'A') * 2 + c.Count(x => x == 'K') > 1)
                            {
                                poss.Add(new string[] { spade, hearts, diamonds, clubs });
                            }
                        }
                    }
                }
            };

            var auctions = new Dictionary<string, List<string>>();
            foreach (var pos in poss)
            {
                var hand =
                    pos[0] + new string('x', 4 - pos[0].Length) + ',' +
                    pos[1] + new string('x', 3 - pos[1].Length) + ',' +
                    pos[2] + new string('x', 3 - pos[2].Length) + ',' +
                    pos[3] + new string('x', 3 - pos[3].Length);
                Debug.Assert(hand.Length == 16);

                var auction = BidManager.GetAuction(hand, new BidGenerator(), fasesWithOffset);
                string key = auction.GetBidsAsString(Player.South, x => x.Value[Player.South].fase != Fase.Shape);
                if (!auctions.ContainsKey(key))
                {
                    auctions.Add(key, new List<string>());
                }
                auctions[key].Add(hand);
            }

            return auctions;
        }
    }
}