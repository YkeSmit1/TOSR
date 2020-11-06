using Common;
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
    using ControlsPullDictionary = Dictionary<string, List<string>>;
    using ControlScanningPullDictionary = Dictionary<string, (List<string> controlsScanning, bool zoom)>;

    public class ReverseDictionaries
    {
        public ShapeDictionary ShapeAuctions { get; }
        public ControlsDictionary ControlsAuctions { get; }
        public ControlsPullDictionary ControlsPullAuctions3NT { get; }
        public ControlsPullDictionary ControlsPullAuctions4Di { get; }
        public ControlsOnlyDictionary ControlsOnlyAuctions { get; }
        public ControlScanningDictionary ControlScanningAuctions { get; }
        public ControlScanningPullDictionary ControlScanningPullAuctions3NT { get; }
        public ControlScanningPullDictionary ControlScanningPullAuctions4Di { get; }

        private readonly Dictionary<Fase, bool> fasesWithOffset;

        public ReverseDictionaries(ShapeDictionary shapeAuctions, ControlsDictionary controlsAuctions, ControlsOnlyDictionary controlsOnlyAuctions, 
            ControlScanningDictionary controlScanningAuctions, ControlsPullDictionary controlsPullAuctions3NT, ControlScanningPullDictionary controlScanningPullAuctions3NT,
            ControlsPullDictionary controlsPullAuctions4Di, ControlScanningPullDictionary controlScanningPullAuctions4Di)
        {
            ShapeAuctions = shapeAuctions;
            ControlsAuctions = controlsAuctions;
            ControlsPullAuctions3NT = controlsPullAuctions3NT;
            ControlsPullAuctions4Di = controlsPullAuctions4Di;
            ControlsOnlyAuctions = controlsOnlyAuctions;
            ControlScanningAuctions = controlScanningAuctions;
            ControlScanningPullAuctions3NT = controlScanningPullAuctions3NT;
            ControlScanningPullAuctions4Di = controlScanningPullAuctions4Di;
        }

        public ReverseDictionaries(Dictionary<Fase, bool> fasesWithOffset)
        {
            this.fasesWithOffset = fasesWithOffset;
            ShapeAuctions = Util.LoadAuctions("txt\\AuctionsByShape.txt", GenerateAuctionsForShape);
            ControlsOnlyAuctions = Util.LoadAuctions("txt\\AuctionsByControlsOnly.txt", GenerateAuctionsForControlsOnly);
            ControlScanningAuctions = Util.LoadAuctions("txt\\AuctionsByControlsScanning.txt", GenerateAuctionsForControlsScanning);
            ControlScanningPullAuctions3NT = Util.LoadAuctions("txt\\AuctionsByControlsScanningPull3NT.txt", GenerateAuctionsForControlsScanningPull3NT);
            ControlScanningPullAuctions4Di = Util.LoadAuctions("txt\\AuctionsByControlsScanningPull4Di.txt", GenerateAuctionsForControlsScanningPull4Di);
            ControlsAuctions = Util.LoadAuctions("txt\\AuctionsByControls.txt", GenerateAuctionsForControls);
            ControlsPullAuctions3NT = Util.LoadAuctions("txt\\AuctionsByControlsPull3NT.txt", GenerateAuctionsForControlsPull3NT);
            ControlsPullAuctions4Di = Util.LoadAuctions("txt\\AuctionsByControlsPull4Di.txt", GenerateAuctionsForControlsPull4Di);
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

                                if (!Util.IsFreakHand(str))
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
            return GenerateAuctionsForControlsScanningInternal(bidManager, 
                (hand) => Util.GetControlCount(hand) == 4 && Util.GetHcpCount(hand) < 12 ? (new int[] { 0, 12 }) : (new int[] { 0 }));
        }

        public ControlScanningPullDictionary GenerateAuctionsForControlsScanningPull3NT()
        {
            var dictionaries = Merge(GenerateDictionariesControlsScanning(0, Bid.threeNTBid), 
                GenerateDictionariesControlsScanning(1, Bid.threeNTBid), 
                GenerateDictionariesControlsScanning(2, Bid.threeNTBid));
            return dictionaries;
        }

        public ControlScanningPullDictionary GenerateAuctionsForControlsScanningPull4Di()
        {
            var dictionaries = Merge(GenerateDictionariesControlsScanning(0, Bid.fourDiamondBid), GenerateDictionariesControlsScanning(1, Bid.fourDiamondBid));
            return dictionaries;
        }

        private ControlScanningPullDictionary GenerateDictionariesControlsScanning(int controlBidCount, Bid relayBid)
        {
            var fasesControl = new Fase[] { Fase.Controls };
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset,
                (biddingState, auction, northHand) => GetRelayBidFunc(biddingState, auction, fasesControl, controlBidCount, relayBid));

            Dictionary<(Bid, int), Func<string, int[]>> HcpRequiredToPull = new Dictionary<(Bid, int), Func<string, int[]>>() {
            { (Bid.threeNTBid, 0), (hand) => (new[] {13, 15, 17, 19, 21, 23 }) },
            { (Bid.threeNTBid, 1), GetHcpAfterOneAsk(11, 15) },
            { (Bid.threeNTBid, 2), (hand) => (new[] { 9 }) },
            { (Bid.fourDiamondBid, 0), (hand) => (new [] {14 }) },
            { (Bid.fourDiamondBid, 1), GetHcpAfterOneAsk(11, 13) } };

            static Func<string, int[]> GetHcpAfterOneAsk(int hcpMin, int hcpMax)
            {
                return (hand) =>
                {
                    int controlCount = Util.GetControlCount(hand);
                    return controlCount switch
                    {
                        var c when c < 4 => new int[] { hcpMin },
                        4 => new int[] { hcpMin, hcpMax },
                        _ => new[] { hcpMax }
                    };
                };
            }

            return GenerateAuctionsForControlsScanningInternal(bidManager, HcpRequiredToPull[(relayBid, controlBidCount)]);
        }

        private ControlScanningDictionary GenerateAuctionsForControlsScanningInternal(BidManager bidManager, Func<string, int[]> hcpGenerator)
        {
            var auctions = new ControlScanningDictionary();
            var suitLength = new[] { 4, 3, 3, 3 };
            var controls = new[] { "", "A", "K", "AK"};

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                        {
                            var hand = ConstructHand(suitLength, spades, hearts, diamonds, clubs);

                            foreach (var hcp in hcpGenerator.Invoke(hand))
                            {
                                var suits = new string[] { spades, hearts, diamonds, clubs };
                                if (TryAddQuacksTillHCP(hcp, ref suits))
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
                    if (IsSignOffOrInvalidAuction(auction))
                    {
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
        }

        public ControlsDictionary GenerateAuctionsForControls()
        {
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            return GenerateAuctionsForControlsInternal(bidManager);
        }

        public ControlsPullDictionary GenerateAuctionsForControlsPull3NT()
        {
            var dictionaries = Merge(GenerateDictionariesControls(0, Bid.threeNTBid), GenerateDictionariesControls(1, Bid.threeNTBid), GenerateDictionariesControls(2, Bid.threeNTBid));
            return dictionaries;
        }

        public ControlsPullDictionary GenerateAuctionsForControlsPull4Di()
        {
            var dictionaries = Merge(GenerateDictionariesControls(0, Bid.fourDiamondBid), GenerateDictionariesControls(1, Bid.fourDiamondBid));
            return dictionaries;
        }

        private ControlsDictionary GenerateDictionariesControls(int controlBidCount, Bid relayBid)
        {
            var fasesControl = new Fase[] { Fase.Controls };
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset,
                (biddingState, auction, northHand) => GetRelayBidFunc(biddingState, auction, fasesControl, controlBidCount, relayBid));
            return GenerateAuctionsForControlsInternal(bidManager);
        }

        private static ControlsDictionary GenerateAuctionsForControlsInternal(BidManager bidManager)
        {
            var auctions = new ControlsDictionary();
            var suitLength = new[] { 4, 3, 3, 3 };
            var controls = new[] { "", "A", "K", "Q", "AK", "AQ", "KQ", "AKQ" };

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                        {
                            var hand = ConstructHand(suitLength, spades, hearts, diamonds, clubs);
                            var controlCount = Util.GetControlCount(hand);
                            var hcpCount = Util.GetHcpCount(hand);
                            if (controlCount > 1 && hcpCount < 25)
                            {
                                BidAndStoreHand(hand, hand);
                                // Also try to store the hand with extra jacks for exactly 4 controls, because auction will be different if there are more then 12 HCP in the hand
                                if (controlCount == 4 && hcpCount < 12)
                                {
                                    var handWithJacks = AddHonors(suitLength, "J", spades, hearts, diamonds, clubs);
                                    BidAndStoreHand(handWithJacks, hand);
                                }
                            }
                        }
            return auctions;

            void BidAndStoreHand(string hand, string handToStore)
            {
                Debug.Assert(hand.Length == 16);
                var auction = bidManager.GetAuction(string.Empty, hand);// No northhand. Just for generating reverse dictionaries
                var key = auction.GetBidsAsString(new List<Fase> { Fase.Controls, Fase.ScanningControls, Fase.ScanningOther }.Concat(BidManager.signOffFases).ToArray());
                if (IsSignOffOrInvalidAuction(auction))
                {
                    if (!auctions.ContainsKey(key))
                        auctions.Add(key, new List<string>());
                    if (!auctions[key].Contains(handToStore))
                        auctions[key].Add(handToStore);
                }
            }
        }

        private static Dictionary<TKey, TValue> Merge<TKey, TValue>(params Dictionary<TKey, TValue>[] dictionaries)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var dict in dictionaries)
                foreach (var x in dict)
                    result[x.Key] = x.Value;
            return result;
        }

        private static Bid GetRelayBidFunc(BiddingState biddingState, Auction auction, Fase[] fases, int fasesBidCount, Bid relayBid)
        {
            if (biddingState.Fase != Fase.Shape)
            {
                if (biddingState.Fase == Fase.BidGame)
                {
                    biddingState.Fase = Fase.End;
                    auction.hasSignedOff = true;
                    return Bid.PassBid;
                }

                if (!biddingState.HasSignedOff && auction.GetBids(Player.South, fases).Count() == fasesBidCount)
                    biddingState.UpdateBiddingStateSignOff(fasesBidCount, relayBid);
            }

            return Bid.NextBid(biddingState.CurrentBid);
        }

        private static string AddHonors(int[] suitLength, string honor, params string[] suits)
        {
            // Don't add queens in spades, It will zoom
            return ConstructHand(suitLength, suits[0],
                   GetControlsWithHonorsIfPossible(suits[1], suitLength[1], honor),
                   GetControlsWithHonorsIfPossible(suits[2], suitLength[2], honor),
                   GetControlsWithHonorsIfPossible(suits[3], suitLength[3], honor));
        }

        private static string ConstructHand(int[] suitLength, params string[] suits)
        {
            return string.Join(',', suitLength.Zip(suits, (x, y) => y.PadRight(x, 'x')));
        }

        private static string GetControlsWithHonorsIfPossible(string suit, int suitLength, string honor)
        {
            return suit + (suit.Length == suitLength ? "" : honor);
        }

        private static bool TryAddQuacksTillHCP(int hcp, ref string[] suits)
        {
            if (hcp <= Util.GetHcpCount(suits))
                return true;

            int suitToAdd = 3;
            while (hcp - Util.GetHcpCount(suits) > 1)
            {
                if (suitToAdd == 0)
                    return false;

                suits[suitToAdd] += 'Q';
                suitToAdd--;
            };

            suitToAdd = 3;
            while (hcp != Util.GetHcpCount(suits))
            {
                if (suitToAdd == 0)
                    return false;

                if (suits[suitToAdd].Length < 3)
                    suits[suitToAdd] += 'J';

                suitToAdd--;
            };

            return true;
        }

        private static bool IsSignOffOrInvalidAuction(Auction auction)
        {
            return !auction.isInvalid && !auction.hasSignedOff;
        }
    }
}