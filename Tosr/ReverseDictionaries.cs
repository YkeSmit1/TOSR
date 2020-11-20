using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ObjectCloner;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsOnlyDictionary = Dictionary<string, List<int>>;
    using ControlsDictionary = Dictionary<string, List<string>>;
    using ControlScanningDictionary = Dictionary<string, (List<string> controlsScanning, bool zoom)>;

    public class ReverseDictionaries
    {
        public ShapeDictionary ShapeAuctions { get; }
        public ControlsDictionary ControlsAuctions0 { get; }
        public ControlsDictionary ControlsAuctions1 { get; }
        public ControlsDictionary ControlsAuctions2 { get; }
        public ControlsOnlyDictionary ControlsOnlyAuctions { get; }
        public ControlScanningDictionary ControlScanningAuctions0 { get; }
        public ControlScanningDictionary ControlScanningAuctions1 { get; }
        public ControlScanningDictionary ControlScanningAuctions2 { get; }

        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly int[] suitLengthNoSingleton = new[] { 4, 3, 3, 3 };
        private static readonly int[] suitLengthSingleton = new[] { 5, 4, 3, 1 };
        private static readonly int[] suitLength2Singletons = new[] { 6, 5, 1, 1 };

        public ReverseDictionaries(ShapeDictionary shapeAuctions, ControlsDictionary controlsAuctions, ControlsOnlyDictionary controlsOnlyAuctions, 
            ControlScanningDictionary controlScanningAuctions)
        {
            ShapeAuctions = shapeAuctions;
            ControlsAuctions0 = controlsAuctions;
            ControlsOnlyAuctions = controlsOnlyAuctions;
            ControlScanningAuctions0 = controlScanningAuctions;
        }

        public ReverseDictionaries(Dictionary<Fase, bool> fasesWithOffset, IProgress<string> progress)
        {
            this.fasesWithOffset = fasesWithOffset;

            progress.Report(nameof(ShapeAuctions));
            ShapeAuctions = Util.LoadAuctions("txt\\AuctionsByShape.txt", GenerateAuctionsForShape, 0);
            progress.Report(nameof(ControlsOnlyAuctions));
            ControlsOnlyAuctions = Util.LoadAuctions("txt\\AuctionsByControlsOnly.txt", GenerateAuctionsForControlsOnly, 0);

            progress.Report(nameof(ControlScanningAuctions0));
            ControlScanningAuctions0 = Util.LoadAuctions("txt\\AuctionsByControlsScanning0.txt", GenerateAuctionsForControlsScanning, 0);
            progress.Report(nameof(ControlScanningAuctions1));
            ControlScanningAuctions1 = Util.LoadAuctions("txt\\AuctionsByControlsScanning1.txt", GenerateAuctionsForControlsScanning, 1);
            progress.Report(nameof(ControlScanningAuctions2));
            ControlScanningAuctions2 = Util.LoadAuctions("txt\\AuctionsByControlsScanning2.txt", GenerateAuctionsForControlsScanning, 2);

            progress.Report(nameof(ControlsAuctions0));
            ControlsAuctions0 = Util.LoadAuctions("txt\\AuctionsByControls0.txt", GenerateAuctionsForControls, 0);
            progress.Report(nameof(ControlsAuctions1));
            ControlsAuctions1 = Util.LoadAuctions("txt\\AuctionsByControls1.txt", GenerateAuctionsForControls, 1);
            progress.Report(nameof(ControlsAuctions2));
            ControlsAuctions2 = Util.LoadAuctions("txt\\AuctionsByControls2.txt", GenerateAuctionsForControls, 2);
        }

        public ShapeDictionary GenerateAuctionsForShape(int nrOfShortages)
        {
            logger.Info("Generating dictionaries for shape");
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

        public ControlsOnlyDictionary GenerateAuctionsForControlsOnly(int nrOfShortages)
        {
            logger.Info("Generating dictionaries for controls only");
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

        public ControlScanningDictionary GenerateAuctionsForControlsScanning(int nrOfShortages)
        {
            logger.Info($"Generating dictionaries for controlsScanning. Shortages:{nrOfShortages}");
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ControlScanningDictionary();
            var controls = new[] { "", "A", "K", "AK"};

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                            BidAndStoreHandsByHcp(GetSuitLength(nrOfShortages), spades, hearts, diamonds, clubs);
            return auctions;

            void BidAndStoreHandsByHcp(int[] suitLengthBid, params string[] suits)
            {
                var lsuits =  ObjectCloner.ObjectCloner.DeepClone(suits);
                var hand = ConstructHand(suitLengthNoSingleton, lsuits);
                if (Util.GetControlCount(hand) > 1)
                {
                    foreach (var hcp in GetHcpGeneratorGeneral().Invoke(hand))
                    {
                        if (Util.TryAddQuacksTillHCP(hcp, ref lsuits))
                            BidAndStoreHand(ConstructHand(suitLengthBid, lsuits), hand);
                    }
                }
            }

            void BidAndStoreHand(string hand, string handToStore)
            {
                if (hand.Length != 16)
                    return;
                var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                var key = string.Join("", auction.GetBids(Player.South, new[] { Fase.Controls, Fase.ScanningControls }).
                    Select(bid => bid - (auction.GetBids(Player.South, Fase.Shape).Last() - Bid.threeDiamondBid)));
                var isZoom = auction.GetBids(Player.South, Fase.ScanningControls).Any(x => x.zoom);
                if (!auctions.ContainsKey(key))
                    auctions.Add(key, (new List<string>() { handToStore }, isZoom));
                else if (!auctions[key].controlsScanning.Contains(handToStore))
                    auctions[key].controlsScanning.Add(handToStore);
            }
        }

        public ControlsDictionary GenerateAuctionsForControls(int nrOfShortages)
        {
            logger.Info($"Generating dictionaries for controls. Shortages:{nrOfShortages}");
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ControlsDictionary();
            var controls = new[] { "", "A", "K", "Q", "AK", "AQ", "KQ", "AKQ" };

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                            BidAndStoreHandsByHcp(GetSuitLength(nrOfShortages), spades, hearts, diamonds, clubs);
            return auctions;

            void BidAndStoreHandsByHcp(int[] suitLength, params string[] suits)
            {
                var lsuits = ObjectCloner.ObjectCloner.DeepClone(suits);
                var hand = ConstructHand(suitLengthNoSingleton, lsuits);
                if (Util.GetControlCount(hand) > 1)
                {
                    foreach (var hcp in GetHcpGeneratorGeneral().Invoke(hand))
                    {
                        if (Util.TryAddJacksTillHCP(hcp, ref lsuits))
                            BidAndStoreHand(ConstructHand(suitLength, lsuits), hand);
                    }
                }
            }

            void BidAndStoreHand(string hand, string handToStore)
            {
                if (hand.Length != 16)
                    return;
                var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                var key = string.Join("", auction.GetBids(Player.South, new[] { Fase.Controls, Fase.ScanningControls, Fase.ScanningOther }).
                    Select(bid => bid - (auction.GetBids(Player.South, Fase.Shape).Last() - Bid.threeDiamondBid)));
                if (!auctions.ContainsKey(key))
                    auctions.Add(key, new List<string>());
                if (!auctions[key].Contains(handToStore))
                    auctions[key].Add(handToStore);
            }
        }

        private static int[] GetSuitLength(int nrOfShortages)
        {
            return nrOfShortages switch
            {
                0 => suitLengthNoSingleton,
                1 => suitLengthSingleton,
                2 => suitLength2Singletons,
                _ => throw new ArgumentException("nrOfshortages should be 0, 1 or 2")
            };
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