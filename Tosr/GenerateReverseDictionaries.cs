using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tosr
{
    class GenerateReverseDictionaries
    {
        private Dictionary<Fase, bool> fasesWithOffset;

        public GenerateReverseDictionaries(Dictionary<Fase, bool> fasesWithOffset)
        {
            this.fasesWithOffset = fasesWithOffset;
        }

        public Dictionary<string, string> GenerateAuctionsForShape()
        {
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
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

                                if (!Common.Common.IsFreakHand(str))
                                {
                                    var auction = bidManager.GetAuction(string.Empty, hand); // No southhand. Just for generating reverse dictionaries
                                    auctions.Add(auction.GetBidsAsString(Fase.Shape), str);
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
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
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

                var auction = bidManager.GetAuction(string.Empty, hand);// No southhand. Just for generating reverse dictionaries
                string key = auction.GetBidsAsString(new[] { Fase.Controls, Fase.Scanning });
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
