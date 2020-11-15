using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsOnlyDictionary = Dictionary<string, List<int>>;
    using ControlsDictionary = Dictionary<string, List<string>>;
    using ControlScanningDictionary = Dictionary<string, (List<string> controlsScanning, bool zoom)>;

    public class ReverseDictionaries
    {
        public ShapeDictionary ShapeAuctions { get; }
        public ControlsDictionary ControlsAuctions { get; }
        public ControlsOnlyDictionary ControlsOnlyAuctions { get; }
        public ControlScanningDictionary ControlScanningAuctions { get; }

        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ReverseDictionaries(ShapeDictionary shapeAuctions, ControlsDictionary controlsAuctions, ControlsOnlyDictionary controlsOnlyAuctions, 
            ControlScanningDictionary controlScanningAuctions)
        {
            ShapeAuctions = shapeAuctions;
            ControlsAuctions = controlsAuctions;
            ControlsOnlyAuctions = controlsOnlyAuctions;
            ControlScanningAuctions = controlScanningAuctions;
        }

        public ReverseDictionaries(Dictionary<Fase, bool> fasesWithOffset, IProgress<string> progress)
        {
            this.fasesWithOffset = fasesWithOffset;
            progress.Report(nameof(ShapeAuctions));
            ShapeAuctions = Util.LoadAuctions("txt\\AuctionsByShape.txt", GenerateAuctionsForShape);
            progress.Report(nameof(ControlsOnlyAuctions));
            ControlsOnlyAuctions = Util.LoadAuctions("txt\\AuctionsByControlsOnly.txt", GenerateAuctionsForControlsOnly);
            progress.Report(nameof(ControlScanningAuctions));
            ControlScanningAuctions = Util.LoadAuctions("txt\\AuctionsByControlsScanning.txt", GenerateAuctionsForControlsScanning);
            progress.Report(nameof(ControlsAuctions));
            ControlsAuctions = Util.LoadAuctions("txt\\AuctionsByControls.txt", GenerateAuctionsForControls);
        }

        public ShapeDictionary GenerateAuctionsForShape()
        {
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ShapeDictionary();
            var regex = new Regex("x");

            for (int spades = 0; spades < 8; spades++)
                for (int hearts = 0; hearts < 8; hearts++)
                    for (int diamonds = 0; diamonds < 8; diamonds++)
                        for (int clubs = 0; clubs < 8; clubs++)
                            if (spades + hearts + diamonds + clubs == 13)
                            {
                                var hand = new string('x', spades) + "," + new string('x', hearts) + "," + new string('x', diamonds) + "," + new string('x', clubs);
                                // We need a hand with two controls. Otherwise engine cannot find a bid
                                hand = regex.Replace(hand, "A", 1);
                                var suitLengthSouth = hand.Split(',').Select(x => x.Length);
                                var str = string.Join("", suitLengthSouth);

                                if (!Util.IsFreakHand(suitLengthSouth))
                                {
                                    var auction = bidManager.GetAuction(string.Empty, hand); // No northhand. Just for generating reverse dictionaries
                                    var isZoom = auction.GetBids(Player.South, Fase.Shape).Any(x => x.zoom);
                                    var key = auction.GetBidsAsString(Fase.Shape);
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
                    var key = auction.Substring(0, 2);
                    if (!oneAskAuction.ContainsKey(key))
                        oneAskAuction.Add(key, new List<int>());
                    oneAskAuction[key].Add(auctions[auction].First());
                }
            }
            return auctions.Concat(oneAskAuction).ToDictionary(pair => pair.Key, pair => pair.Value);

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

        public ControlScanningDictionary GenerateAuctionsForControlsScanning()
        {
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ControlScanningDictionary();
            var suitLength = new[] { 4, 3, 3, 3 };
            var controls = new[] { "", "A", "K", "AK"};

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                        {
                            var hand = ConstructHand(suitLength, spades, hearts, diamonds, clubs);

                            foreach (var hcp in GetHcpGeneratorGeneral().Invoke(hand))
                            {
                                var suits = new string[] { spades, hearts, diamonds, clubs };
                                if (Util.TryAddQuacksTillHCP(hcp, ref suits))
                                    BidAndStoreHand(ConstructHand(suitLength, suits), hand);
                            }
                        }
            return auctions;

            void BidAndStoreHand(string hand, string handToStore)
            {
                Debug.Assert(hand.Length == 16);
                if (Util.GetControlCount(hand) > 1 && Util.GetHcpCount(hand) < 25)
                {
                    var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                    var fases = new List<Fase> { Fase.Controls, Fase.ScanningControls }.Concat(BidManager.signOffFases).ToArray();
                    var key = auction.GetBidsAsString(fases);
                    var isZoom = auction.GetBids(Player.South, Fase.ScanningControls).Any(x => x.zoom);
                    if (!auctions.ContainsKey(key))
                        auctions.Add(key, (new List<string>() { handToStore }, isZoom));
                    else if (!auctions[key].controlsScanning.Contains(handToStore))
                        auctions[key].controlsScanning.Add(handToStore);
                }
            }
        }

        public ControlsDictionary GenerateAuctionsForControls()
        {
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ControlsDictionary();
            var suitLength = new[] { 4, 3, 3, 3 };
            var controls = new[] { "", "A", "K", "Q", "AK", "AQ", "KQ", "AKQ" };

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                        {
                            var hand = ConstructHand(suitLength, spades, hearts, diamonds, clubs);

                            foreach (var hcp in GetHcpGeneratorGeneral().Invoke(hand))
                            {
                                var suits = new string[] { spades, hearts, diamonds, clubs };
                                if (Util.TryAddJacksTillHCP(hcp, ref suits))
                                    BidAndStoreHand(ConstructHand(suitLength, suits), hand);
                            }
                        }
            return auctions;

            void BidAndStoreHand(string hand, string handToStore)
            {
                Debug.Assert(hand.Length == 16);
                if (Util.GetControlCount(hand) > 1 && Util.GetHcpCount(hand) < 25)
                {
                    var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                    var key = auction.GetBidsAsString(new List<Fase> { Fase.Controls, Fase.ScanningControls, Fase.ScanningOther }.Concat(BidManager.signOffFases).ToArray());
                    if (!auctions.ContainsKey(key))
                        auctions.Add(key, new List<string>());
                    if (!auctions[key].Contains(handToStore))
                        auctions[key].Add(handToStore);
                }
            }
        }

        private static Func<string, int[]> GetHcpGeneratorGeneral()
        {
            return (hand) => Util.GetControlCount(hand) == 4 && Util.GetHcpCount(hand) < 12 ? (new int[] { 0, 12 }) : (new int[] { 0 });
        }

        private static string ConstructHand(int[] suitLength, params string[] suits)
        {
            return string.Join(',', suitLength.Zip(suits, (x, y) => y.PadRight(x, 'x')));
        }
    }
}