using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsDictionary = Dictionary<string, List<string>>;

    public class GenerateReverseDictionaries
    {
        private readonly Dictionary<Fase, bool> fasesWithOffset;

        public GenerateReverseDictionaries(Dictionary<Fase, bool> fasesWithOffset)
        {
            this.fasesWithOffset = fasesWithOffset;
        }

        public ShapeDictionary GenerateAuctionsForShape()
        {
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ShapeDictionary();

            for (int spades = 0; spades < 8; spades++)
                for (int hearts = 0; hearts < 8; hearts++)
                    for (int diamonds = 0; diamonds < 8; diamonds++)
                        for (int clubs = 0; clubs < 8; clubs++)
                            if (spades + hearts + diamonds + clubs == 13)
                            {
                                var hand = new string('x', spades) + "," + new string('x', hearts) + "," + new string('x', diamonds) + "," + new string('x', clubs);
                                var suitLengthSouth = hand.Split(',').Select(x => x.Length);
                                var str = string.Join("", suitLengthSouth);

                                if (!Util.IsFreakHand(str))
                                {
                                    var auction = bidManager.GetAuction(string.Empty, hand); // No northhand. Just for generating reverse dictionaries
                                    var isZoom = auction.GetBids(Player.South, Fase.Shape).Any(x => x.zoom);
                                    string key = auction.GetBidsAsString(Fase.Shape);
                                    if (auctions.ContainsKey(key))
                                        auctions[key].pattern.Add(str);
                                    else 
                                        auctions.Add(key, (new List<string> { str }, isZoom));
                                }
                            }
            return auctions;
        }

        public ControlsDictionary GenerateAuctionsForControls()
        {
            var auctions = new ControlsDictionary();
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            string[] controls = new[] { "", "A", "K", "Q", "AK", "AQ", "KQ", "AKQ" };

            foreach (var spade in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                        {
                            var hand = spade.PadRight(4, 'x') + ',' +
                                      hearts.PadRight(3, 'x') + ',' +
                                    diamonds.PadRight(3, 'x') + ',' +
                                       clubs.PadRight(3, 'x');
                            Debug.Assert(hand.Length == 16);

                            if (hand.Count(x => x == 'A') * 2 + hand.Count(x => x == 'K') > 1)
                            {
                                var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                                string key = auction.GetBidsAsString(new[] { Fase.Controls, Fase.Scanning });
                                if (!auctions.ContainsKey(key))
                                    auctions.Add(key, new List<string>());
                                auctions[key].Add(hand);
                            }
                        }
            return auctions;
        }
    }
}
