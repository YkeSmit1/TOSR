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
            int[] suitLength = new[] { 4, 3, 3, 3 };
            string[] controls = new[] { "", "A", "K", "Q", "AK", "AQ", "KQ", "AKQ" };

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                        {
                            var hand = spades.PadRight(suitLength[0], 'x') + ',' +
                                       hearts.PadRight(suitLength[1], 'x') + ',' +
                                     diamonds.PadRight(suitLength[2], 'x') + ',' +
                                        clubs.PadRight(suitLength[3], 'x');
                            Debug.Assert(hand.Length == 16);

                            int controlCount = Util.GetControlCount(hand);
                            if (controlCount > 1)
                            {
                                BidAndStoreHand(auctions, bidManager, hand, hand);
                                // Also try to store the hand with extra jacks for exactly 4 controls, because auction will be different if there are more then 12 HCP in the hand
                                if (controlCount == 4 && Util.GetHcpCount(hand) < 12)
                                {
                                    var handWithJacks = GetControlsWithJackIfPossible(spades, suitLength[0]).PadRight(suitLength[0], 'x') + ',' +
                                                        GetControlsWithJackIfPossible(hearts, suitLength[1]).PadRight(suitLength[1], 'x') + ',' +
                                                      GetControlsWithJackIfPossible(diamonds, suitLength[2]).PadRight(suitLength[2], 'x') + ',' +
                                                         GetControlsWithJackIfPossible(clubs, suitLength[3]).PadRight(suitLength[3], 'x');
                                    Debug.Assert(hand.Length == 16);
                                    BidAndStoreHand(auctions, bidManager, handWithJacks, hand);
                                }
                            }
                        }
            return auctions;

            static void BidAndStoreHand(ControlsDictionary auctions, BidManager bidManager, string hand, string handToStore)
            {
                var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                string key = auction.GetBidsAsString(new[] { Fase.Controls, Fase.Scanning });
                if (!auctions.ContainsKey(key))
                    auctions.Add(key, new List<string>());
                auctions[key].Add(handToStore);
            }

            static string GetControlsWithJackIfPossible(string suit, int suitLength)
            {
                return suit + (suit.Length == suitLength ? "" : "J");
            }
        }
    }
}
