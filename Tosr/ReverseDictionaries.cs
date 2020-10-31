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
        private static readonly List<Bid> endBids = new List<Bid> { Bid.threeNTBid, Bid.fourNTBid };

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
            ControlsAuctions = Util.LoadAuctions("txt\\AuctionsByControls.txt", GenerateAuctionsForControls);
            ControlsPullAuctions3NT = Util.LoadAuctions("txt\\AuctionsByControlsPull3NT.txt", GenerateAuctionsForControlsPull3NT);
            ControlsPullAuctions4Di = Util.LoadAuctions("txt\\AuctionsByControlsPull4Di.txt", GenerateAuctionsForControlsPull4Di);
            ControlsOnlyAuctions = Util.LoadAuctions("txt\\AuctionsByControlsOnly.txt", GenerateAuctionsForControlsOnly);
            ControlScanningAuctions = Util.LoadAuctions("txt\\AuctionsByControlsScanning.txt", GenerateAuctionsForControlsScanning);
            ControlScanningPullAuctions3NT = Util.LoadAuctions("txt\\AuctionsByControlsScanningPull3NT.txt", GenerateAuctionsForControlsScanningPull3NT);
            ControlScanningPullAuctions4Di = Util.LoadAuctions("txt\\AuctionsByControlsScanningPull4Di.txt", GenerateAuctionsForControlsScanningPull4Di);
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
            return GenerateAuctionsForControlsScanningInternal(bidManager, false);
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
            return GenerateAuctionsForControlsScanningInternal(bidManager, true);
        }

        private static ControlScanningDictionary GenerateAuctionsForControlsScanningInternal(BidManager bidManager, bool useExtraQueens)
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
                            var controlCount = Util.GetControlCount(hand);
                            if (controlCount > 1)
                            {
                                BidAndStoreHand(hand, hand);
                                if (useExtraQueens)
                                {
                                    BidAndStoreHand(ConstructHand(suitLength, spades + "Q", hearts, diamonds, clubs), hand);
                                    BidAndStoreHand(ConstructHand(suitLength, spades + "Q", hearts + "Q", diamonds, clubs), hand);
                                    BidAndStoreHand(ConstructHand(suitLength, spades + "Q", hearts + "Q", diamonds + "Q", clubs), hand);
                                    BidAndStoreHand(ConstructHand(suitLength, spades + "Q", hearts + "Q", diamonds + "Q", clubs + "Q"), hand);
                                }
                                // Also try to store the hand with extra queens for exactly 4 controls, because auction will be different if there are more then 12 HCP in the hand
                                else if (controlCount == 4 && Util.GetHcpCount(hand) < 12)
                                {
                                    var handWithQueens = AddHonors(suitLength, spades, hearts, diamonds, clubs, "Q");
                                    BidAndStoreHand(handWithQueens, hand);
                                }
                            }
                        }
            return auctions;

            void BidAndStoreHand(string hand, string handToStore)
            {
                Debug.Assert(hand.Length == 16);
                if (Util.GetHcpCount(hand) < 25)
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
                            auctions[key].controlsScanning.Add((handToStore));
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
                                    var handWithJacks = AddHonors(suitLength, spades, hearts, diamonds, clubs, "J");
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

                if (!biddingState.HasSignedOff)
                    if (auction.GetBids(Player.South, fases).Count() == fasesBidCount)
                        return relayBid == Bid.threeNTBid ? DoRelayBid3NT(relayBid) : DoRelayBid4Diamonds(relayBid);
            }

            return Bid.NextBid(biddingState.CurrentBid);

            Bid DoRelayBid3NT(Bid relayBid)
            {
                var lRelayBid = relayBid;
                if (biddingState.CurrentBid > lRelayBid)
                    lRelayBid += 5; // Bid 4NT instead of 3NT
                biddingState.UpdateBiddingStateSignOff(fasesBidCount, lRelayBid, true);

                return lRelayBid;
            }

            Bid DoRelayBid4Diamonds(Bid relayBid)
            {
                if (biddingState.CurrentBid > Bid.fourClubBid)
                {
                    biddingState.Fase = Fase.End;
                    auction.isInvalid = true;
                    return Bid.PassBid;
                }

                biddingState.UpdateBiddingStateSignOff(fasesBidCount, relayBid, false);

                return relayBid;
            }
        }

        private static string AddHonors(int[] suitLength, string spades, string hearts, string diamonds, string clubs, string honor)
        {
            return ConstructHand(suitLength, GetControlsWithHonorsIfPossible(spades, suitLength[0], honor),
                   GetControlsWithHonorsIfPossible(hearts, suitLength[1], honor),
                   GetControlsWithHonorsIfPossible(diamonds, suitLength[2], honor),
                   GetControlsWithHonorsIfPossible(clubs, suitLength[3], honor));
        }

        private static string ConstructHand(int[] suitLength, params string[] suits)
        {
            return string.Join(',', suitLength.Zip(suits, (x, y) => y.PadRight(x, 'x')));
        }

        private static string GetControlsWithHonorsIfPossible(string suit, int suitLength, string honor)
        {
            return suit + (suit.Length == suitLength ? "" : honor);
        }

        private static bool IsSignOffOrInvalidAuction(Auction auction)
        {
            return !auction.isInvalid && !auction.hasSignedOff;
        }
    }
}