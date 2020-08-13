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

        public void Execute(HandsNorthSouth[] hands, Dictionary<string, string> auctions)
        {
            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            for (int i = 0; i < hands.Length; ++i)
            {
                try
                {
                    var auction = BidManager.GetAuction(hands[i].SouthHand, bidGenerator);
                    AddHandAndAuction(hands[i], auction, auctions);
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

        private void AddHandAndAuction(HandsNorthSouth strHand, Auction auction, Dictionary<string, string> auctions)
        {
            var strAuction = auction.GetBids(Player.South, x => x.Value[Player.South].fase == Fase.Shape);

            var suitLengthSouth = strHand.SouthHand.Split(',').Select(x => x.Length);
            var str = string.Join("", suitLengthSouth);

            if (IsFreakHand(str))
                return;

            if (!handPerAuction.ContainsKey(strAuction))
                handPerAuction[strAuction] = new List<string>();
            if (!handPerAuction[strAuction].Contains(str))
                handPerAuction[strAuction].Add(str);

            var shape = auctions[strAuction];

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
                                var hand = new string('x', spades) + "," + new string('x', hearts) + ","  + new string('x', diamonds) + "," + new string('x', clubs);
                                var suitLengthSouth = hand.Split(',').Select(x => x.Length);
                                var str = string.Join("", suitLengthSouth);

                                if (!IsFreakHand(str))
                                {
                                    var auction = BidManager.GetAuction(hand, new BidGenerator());
                                    auctions.Add(auction.GetBids(Player.South, x => x.Value[Player.South].fase == Fase.Shape), str);
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
                            var nrOfKings = c.Count(x => x == 'K');
                            var nrOfAces = c.Count(x => x == 'A');
                            if (nrOfAces * 2 + nrOfKings > 1)
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

                var auction = BidManager.GetAuction(hand, new BidGenerator());
                string key = auction.GetBids(Player.South, x => x.Value[Player.South].fase != Fase.Shape);
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