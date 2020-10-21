using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsOnlyDictionary = Dictionary<string, List<int>>;
    using ControlsDictionary = Dictionary<string, List<string>>;

    public class ReverseDictionaries
    {
        public ShapeDictionary ShapeAuctions { get; }
        public ControlsDictionary ControlsAuctions { get; }
        public ControlsOnlyDictionary ControlsOnlyAuctions { get; }

        public ReverseDictionaries(ShapeDictionary shapeAuctions, ControlsDictionary controlsAuctions, ControlsOnlyDictionary controlsOnlyAuctions)
        {
            ShapeAuctions = shapeAuctions;
            ControlsAuctions = controlsAuctions;
            ControlsOnlyAuctions = controlsOnlyAuctions;
        }

        public ReverseDictionaries(string shapeFilename, string controlFilename, string controlsOnlyFilename, string contorlsScanning, Dictionary<Fase, bool> fasesWithOffset)
        {
            ShapeAuctions = Util.LoadAuctions(shapeFilename, () => new ReverseDictionaries(fasesWithOffset).GenerateAuctionsForShape());
            ControlsAuctions = Util.LoadAuctions(controlFilename, () => new ReverseDictionaries(fasesWithOffset).GenerateAuctionsForControls());
            ControlsOnlyAuctions = Util.LoadAuctions(controlsOnlyFilename, () => new ReverseDictionaries(fasesWithOffset).GenerateAuctionsForControlsOnly());
        }

        private readonly Dictionary<Fase, bool> fasesWithOffset;

        public ReverseDictionaries(Dictionary<Fase, bool> fasesWithOffset)
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

        public ControlsOnlyDictionary GenerateAuctionsForControlsOnly()
        {
            var auctions = new ControlsOnlyDictionary();
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);

            var shuffleRestrictions = new ShuffleRestrictions
            {
                shape = "4333",
                restrictShape = true,
            };

            foreach (var control in Enumerable.Range(2, 9))
            {
                if (control == 4)
                {
                    shuffleRestrictions.SetHcp(0, 11);
                    BidAndStoreHand(control);
                    shuffleRestrictions.SetHcp(12, 37);
                    BidAndStoreHand(control);
                    shuffleRestrictions.restrictHcp = false;
                }
                else
                    BidAndStoreHand(control);
            }
            // Generate entries for one ask control(s)
            var oneAskAuction = new ControlsOnlyDictionary();
            foreach (var auction in auctions.Keys)
            {
                if (auction.Length == 4)
                {
                    string key = auction.Substring(0, 2);
                    if (!oneAskAuction.ContainsKey(key))
                    {
                        oneAskAuction.Add(key, new List<int>());
                    }

                    oneAskAuction[key].Add(auctions[auction].First());

                }
            }
            return auctions.Union(oneAskAuction).ToDictionary(pair => pair.Key, pair => pair.Value);

            void BidAndStoreHand(int control)
            {
                shuffleRestrictions.SetControls(control, control);
                string hand;
                do
                {
                    var cards = Shuffling.FisherYates(13).ToList();

                    var orderedCards = cards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
                    hand = Util.GetDeckAsString(orderedCards);
                }
                while
                    (!shuffleRestrictions.Match(hand));

                var auction = bidManager.GetAuction(string.Empty, hand); // No northhand. Just for generating reverse dictionaries
                auctions.Add(auction.GetBidsAsString(Fase.Controls), new List<int> { control });
            }

        }

        public ControlsDictionary GenerateAuctionsForControlsScanning()
        {
            var auctions = new ControlsDictionary();
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            int[] suitLength = new[] { 4, 3, 3, 3 };
            string[] controls = new[] { "", "A", "K", "AK"};

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
                                BidAndStoreHand(hand, hand);
                                // Also try to store the hand with extra queens for exactly 4 controls, because auction will be different if there are more then 12 HCP in the hand
                                if (controlCount == 4 && Util.GetHcpCount(hand) < 12)
                                {
                                    var handWithJacks = GetControlsWithHonorsIfPossible(spades, suitLength[0], "Q").PadRight(suitLength[0], 'x') + ',' +
                                                        GetControlsWithHonorsIfPossible(hearts, suitLength[1], "Q").PadRight(suitLength[1], 'x') + ',' +
                                                      GetControlsWithHonorsIfPossible(diamonds, suitLength[2], "Q").PadRight(suitLength[2], 'x') + ',' +
                                                         GetControlsWithHonorsIfPossible(clubs, suitLength[3], "Q").PadRight(suitLength[3], 'x');
                                    Debug.Assert(hand.Length == 16);
                                    BidAndStoreHand(handWithJacks, hand);
                                }
                            }
                        }
            return auctions;

            void BidAndStoreHand(string hand, string handToStore)
            {
                var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                // TODO fix correct fase
                var key = auction.GetBidsAsString(new[] { Fase.Controls, Fase.Scanning });
                if (auctions.ContainsKey(key))
                    auctions[key].Add((handToStore));
                else
                    auctions.Add(key, (new List<string>() { handToStore }));
            }
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
                                BidAndStoreHand(hand, hand);
                                // Also try to store the hand with extra jacks for exactly 4 controls, because auction will be different if there are more then 12 HCP in the hand
                                if (controlCount == 4 && Util.GetHcpCount(hand) < 12)
                                {
                                    var handWithJacks = GetControlsWithHonorsIfPossible(spades, suitLength[0], "J").PadRight(suitLength[0], 'x') + ',' +
                                                        GetControlsWithHonorsIfPossible(hearts, suitLength[1], "J").PadRight(suitLength[1], 'x') + ',' +
                                                      GetControlsWithHonorsIfPossible(diamonds, suitLength[2], "J").PadRight(suitLength[2], 'x') + ',' +
                                                         GetControlsWithHonorsIfPossible(clubs, suitLength[3], "J").PadRight(suitLength[3], 'x');
                                    Debug.Assert(hand.Length == 16);
                                    BidAndStoreHand(handWithJacks, hand);
                                }
                            }
                        }
            return auctions;

            void BidAndStoreHand(string hand, string handToStore)
            {
                var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                string key = auction.GetBidsAsString(new[] { Fase.Controls, Fase.Scanning });
                if (!auctions.ContainsKey(key))
                    auctions.Add(key, new List<string>());
                auctions[key].Add(handToStore);
            }
        }

        static string GetControlsWithHonorsIfPossible(string suit, int suitLength, string honor)
        {
            return suit + (suit.Length == suitLength ? "" : honor);
        }
    }
}
