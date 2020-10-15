using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NLog;
using Common;
using Solver;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsOnlyDictionary = Dictionary<string, List<int>>;
    using ControlsDictionary = Dictionary<string, List<string>>;

    public class BidManager
    {
        public enum ConstructedSouthhandOutcome
        {
            NotSet,
            SouthhandMatches,
            AuctionNotFoundInShape,
            AuctionNotFoundInControls,
            NoMatchFound,
            MultipleMatchesFound,
            IncorrectSouthhand,
            HasSignedOff,
        }

        private enum RelayBidKind
        {
            Relay,
            fourDiamondEndSignal,
            gameBid,
        }

        private readonly IBidGenerator bidGenerator;
        readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ControlsDictionary controlsAuctions = null;
        private readonly ShapeDictionary shapeAuctions;
        private readonly ControlsOnlyDictionary controlsOnlyAuctions;
        readonly bool useSingleDummySolver = false;
        Lazy<(List<string> shapes, int zoomOffset)> shape;

        static readonly Bid twoNTBid = new Bid(2, Suit.NoTrump);
        static readonly Bid threeDiamondBid = new Bid(3, Suit.Diamonds);
        static readonly Bid threeSpadeBid = new Bid(3, Suit.Spades);
        static readonly Bid threeNTBid = new Bid(3, Suit.NoTrump);
        static readonly Bid fourClubBid = new Bid(4, Suit.Clubs);
        static readonly Bid fourDiamondBid = new Bid(4, Suit.Diamonds);
        static readonly Bid fourNTBid = new Bid(4, Suit.NoTrump);

        static readonly char[] relevantCards = new[] { 'A', 'K', 'Q' };
        public ConstructedSouthhandOutcome constructedSouthhandOutcome = ConstructedSouthhandOutcome.NotSet;

        // Special TOSR logic. Should be in a JSON file as parameters
        static readonly Dictionary<int, List<int>> requirementsToPull3NT = new Dictionary<int, List<int>> {
            {0, Enumerable.Range(16, 5).ToList()},
            {1, Enumerable.Range(21, 2).ToList()},
            {2, Enumerable.Range(23, 2).ToList()}};

        static readonly List<((double, double), RelayBidKind)> requirementsForRelayBid = new List<((double, double), RelayBidKind)> { 
            {((0.0, 11.0) , RelayBidKind.gameBid )},
            {((11.0, 12.0) , RelayBidKind.fourDiamondEndSignal)},
            {((12.0, 13.0) , RelayBidKind.Relay )},
        };

        static readonly int requiredMaxHcpToBid4Diamond = 17;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;

            useSingleDummySolver = false;
        }

        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ShapeDictionary shapeAuctions, 
            ControlsDictionary controlsAuctions, ControlsOnlyDictionary controlsOnlyAuctions, bool useSingleDummySolver) :
            this(bidGenerator, fasesWithOffset)
        {
            this.shapeAuctions = shapeAuctions;
            this.controlsOnlyAuctions = controlsOnlyAuctions;
            this.controlsAuctions = controlsAuctions;

            this.useSingleDummySolver = useSingleDummySolver;
        }

        public void Init(Auction auction)
        {
            shape = new Lazy<(List<string> shapes, int zoomOffset)>(() => GetShapeStrFromAuction(auction, shapeAuctions));
        }

        public Auction GetAuction(string northHand, string southHand)
        {
            logger.Debug($"Starting GetAuction for hand : {southHand}");
            Auction auction = new Auction();

            BiddingState biddingState = new BiddingState(fasesWithOffset);
            Player currentPlayer = Player.West;
            constructedSouthhandOutcome = BidManager.ConstructedSouthhandOutcome.NotSet;
            Init(auction);

            do
            {
                switch (currentPlayer)
                {
                    case Player.West:
                    case Player.East:
                        auction.AddBid(Bid.PassBid);
                        break;
                    case Player.North:
                        NorthBid(biddingState, auction, northHand);
                        auction.AddBid(biddingState.CurrentBid);
                        break;
                    case Player.South:
                        SouthBid(biddingState, auction, southHand);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                currentPlayer = currentPlayer == Player.South ? Player.West : currentPlayer + 1;
            }
            while (!biddingState.EndOfBidding);

            logger.Debug($"Ending GetAuction for hand : {southHand}");
            if (biddingState.HasSignedOff)
                constructedSouthhandOutcome = ConstructedSouthhandOutcome.HasSignedOff;
            return auction;
        }

        public void NorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.Fase != Fase.End)
                biddingState.CurrentBid = GetRelayBid(biddingState, auction, northHand);
            else
            {
                biddingState.EndOfBidding = true;
                // Try to guess contract by using single dummy solver
                if (useSingleDummySolver)
                {
                    try
                    {
                        var constructedSouthHand = ConstructSouthHand(northHand, auction);
                        var suit = Util.GetLongestSuit(northHand, constructedSouthHand);
                        var scores = SingleDummySolver.SolveSingleDummy(3 - (int)suit.Item1, 3 - (int)auction.GetDeclarer(suit.Item1), northHand, constructedSouthHand);
                        var mostFrequent = scores.GroupBy(x => x).OrderByDescending(y => y.Count()).Take(1).Select(z => z.Key).First();
                        Bid bid = new Bid(mostFrequent - 6, (Suit)(3 - suit.Item1));
                        if (bid > biddingState.CurrentBid)
                        {
                            biddingState.CurrentBid = bid;
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                biddingState.CurrentBid = Bid.PassBid;
            }
        }

        private Bid GetRelayBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.Fase != Fase.Shape && shapeAuctions != null)
            {
                // TODO make lazy
                var southHandShape = GetShapeStrFromAuction(auction, shapeAuctions).shapes.First();
                if (biddingState.Fase == Fase.BidGame)
                {
                    biddingState.Fase = Fase.End;
                    var southHand = string.Join(',', southHandShape.Select(x => new string('x', int.Parse(x.ToString()))));
                    var trumpSuit = Util.GetTrumpSuit(northHand, southHand);
                    var game = Bid.GetGameContract(trumpSuit);
                    return biddingState.CurrentBid == game ? Bid.PassBid : game;
                }

                if (!biddingState.HasSignedOff && !string.IsNullOrWhiteSpace(northHand))
                {
                    var controls = GetAuctionForFaseWithOffset(auction, threeDiamondBid, shape.Value.zoomOffset, new Fase[] { Fase.Controls });
                    var controlBidCount = controls.Count();
                    var southHand = string.Join(',', southHandShape.Select(x => new string('x', int.Parse(x.ToString()))));
                    var trumpSuit = Util.GetTrumpSuit(northHand, southHand);
                    var hcp = Util.GetHcpCount(northHand);
                    if (trumpSuit == Suit.NoTrump)
                    {
                        if (requirementsToPull3NT.TryGetValue(controlBidCount, out var hcps) && hcps.Contains(hcp))
                        {
                            var noTrumpBid = biddingState.CurrentBid < threeNTBid ? threeNTBid : fourNTBid;
                            biddingState.UpdateBiddingStateSignOff(controlBidCount, noTrumpBid, true);
                            return noTrumpBid;
                        }
                    }
                    else
                    {
                        if (controlBidCount == 0 && hcp <= requiredMaxHcpToBid4Diamond)
                        {
                            biddingState.UpdateBiddingStateSignOff(controlBidCount, fourDiamondBid, false);
                            return fourDiamondBid;
                        }
                        if (controlBidCount == 1 && useSingleDummySolver)
                        {
                            var averageTricks = GetAverageTricks(northHand, southHandShape, controls, trumpSuit, auction.GetDeclarer(trumpSuit));
                            foreach (var (requirement, relayBidType) in requirementsForRelayBid)
                            {
                                if (averageTricks > requirement.Item1 && averageTricks < requirement.Item2)
                                {
                                    switch (relayBidType)
                                    {
                                        case RelayBidKind.Relay: 
                                            biddingState.HasSignedOff = true;
                                            break;
                                        case RelayBidKind.fourDiamondEndSignal:
                                            if (biddingState.CurrentBid < fourClubBid)
                                            {
                                                biddingState.UpdateBiddingStateSignOff(controlBidCount, fourDiamondBid, false);
                                                return fourDiamondBid;
                                            }
                                            break;
                                        case RelayBidKind.gameBid:
                                            {
                                                biddingState.Fase = Fase.End;
                                                return Bid.GetGameContract(trumpSuit);
                                            }
                                        default:
                                            throw new ArgumentException(nameof(relayBidType));
                                    }
                                }
                            }
                        }
                    }
                }

                if (biddingState.CurrentBid == threeSpadeBid)
                {
                    var shape = southHandShape.ToCharArray().OrderByDescending(x => x);
                    var shapeStringSorted = new string(shape.ToArray());

                    if (shapeStringSorted != "7330")
                    {
                        biddingState.RelayBidIdLastFase++;
                        return fourClubBid;
                    }
                }
            }

            return Bid.NextBid(biddingState.CurrentBid);
        }

        private double GetAverageTricks(string northHand, string southHandShape, IEnumerable<Bid> controls, Suit trumpSuit, Player declarer)
        {
            var strControls = string.Join("", controls);
            var tricks = SingleDummySolver.SolveSingleDummy(3 - (int)trumpSuit, 3 - (int)declarer,
                northHand, southHandShape, controlsOnlyAuctions[strControls].First(), controlsOnlyAuctions[strControls].Last());
            var averageTricks = tricks.Average();
            return averageTricks;
        }

        public void SouthBid(BiddingState biddingState, Auction auction, string handsString)
        {
            if (biddingState.Fase == Fase.End)
            {
                auction.AddBid(Bid.PassBid);
                biddingState.EndOfBidding = true;
                return;
            }
            var (bidIdFromRule, nextfase, description, zoom) = bidGenerator.GetBid(biddingState, handsString);
            var bidId = biddingState.CalculateBid(bidIdFromRule, description, zoom);
            auction.AddBid(biddingState.CurrentBid);
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, bidId, 
                shapeAuctions == null || nextfase != Fase.Controls ? 0 : shape.Value.zoomOffset);
        }

        /// <summary>
        /// Construct southhand to use for single dummy analyses
        /// Does throw
        /// </summary>
        /// <param name="northHand"></param>
        /// <param name="auction"></param>
        /// <returns></returns>
        public string ConstructSouthHand(string northHand, Auction auction)
        {
            logger.Debug($"Starting ConstructSouthHand for northhand : {northHand}");

            int zoomOffset;
            try
            {
                zoomOffset = shape.Value.zoomOffset;
            }
            catch (Exception exception)
            {
                throw SetOutcome(exception.Message, ConstructedSouthhandOutcome.AuctionNotFoundInControls);
            }
            var controls = GetAuctionForFaseWithOffset(auction, threeDiamondBid, zoomOffset, new Fase[] { Fase.Controls, Fase.Scanning });
            var strControls = string.Join("", controls);

            if (!controlsAuctions.TryGetValue(strControls, out var possibleControls))
            {
                throw SetOutcome($"Auction not found in controls. controls: {strControls}. NorthHand: {northHand}.", ConstructedSouthhandOutcome.AuctionNotFoundInControls);
            }
            var matches = GetMatchesWithNorthHand(shape.Value.shapes, possibleControls, northHand);
            logger.Debug($"Ending ConstructSouthHand. southhand : {matches.First()}");

            return (matches.Count()) switch
            {
                0 => throw SetOutcome($"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {northHand}.", ConstructedSouthhandOutcome.NoMatchFound),
                1 => matches.First(),
                _ => throw SetOutcome($"Multiple matches found. Matches: {string.Join('|', matches)}. NorthHand: {northHand}.", ConstructedSouthhandOutcome.MultipleMatchesFound),
            };
        }

        private Exception SetOutcome(string message, ConstructedSouthhandOutcome outcome)
        {
            logger.Warn($"Outcome not satisfied. {outcome}. Message : {message}");
            constructedSouthhandOutcome = outcome;
            return new InvalidOperationException(message);
        }

        /// <summary>
        /// Construct southhand to compare with the actual southhand
        /// </summary>
        /// <param name="strHand"></param>
        /// <param name="auction"></param>
        /// <returns></returns>
        public string ConstructSouthHandSafe(HandsNorthSouth strHand, Auction auction)
        {
            try
            {
                var southHand = ConstructSouthHand(strHand.NorthHand, auction);
                var southHandStr = HandWithx(strHand.SouthHand);

                if (southHand == southHandStr)
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.SouthhandMatches;
                    return $"Match is found: {southHand}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
                }
                else
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.IncorrectSouthhand;
                    return $"SouthHand is not equal to expected. Expected: {southHand}. Actual {southHandStr}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
                }
            }
            catch (Exception e)
            {
                return $"{e.Message} SouthHand: {strHand.SouthHand}";
            }
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

        /// <summary>
        /// Lookup in the shape dictionary. If not found, it tries to find an auction when the last bid was done with zoom
        /// </summary>
        /// <param name="auction"></param>
        /// <returns></returns>
        public static (List<string> shapes, int zoomOffset) GetShapeStrFromAuction(Auction auction, ShapeDictionary shapeAuctions)
        {
            var strAuction = auction.GetBidsAsString(Fase.Shape);

            if (shapeAuctions.ContainsKey(strAuction))
                return (shapeAuctions[strAuction].pattern, shapeAuctions[strAuction].zoom ? 2 : 0);

            var allBids = auction.GetBids(Player.South, Fase.Shape);
            var lastBid = allBids.Last();
            var allButLastBid = allBids.Take(allBids.Count() - 1);
            for (var bid = lastBid - 1; bid >= twoNTBid; bid--)
            {
                var allBidsNew = allButLastBid.Concat(new[] { bid });
                var bidsStr = allBidsNew.Aggregate(string.Empty, (current, bid) => current + bid);
                // Add two because auction is two bids lower if zoom applies
                if (shapeAuctions.ContainsKey(bidsStr) && shapeAuctions[bidsStr].zoom)
                    return (shapeAuctions[bidsStr].pattern, (lastBid - bid) + 2);
            }

            throw new InvalidOperationException($"{ strAuction } not found in shape dictionary");
        }

        /// <summary>
        /// Extracts the control and scanning part of the auction and applies an offset of offsetBid
        /// </summary>
        /// <param name="auction">Generated auction </param>
        /// <param name="offsetBid">Offset used to generate AuctionsByControl.txt</param>
        /// <param name="fase">Which fases to get the offset from</param>
        /// <returns></returns>
        public static IEnumerable<Bid> GetAuctionForFaseWithOffset(Auction auction, Bid offsetBid, int zoomOffset, Fase[] fases)
        {
            var lastBidShape = auction.GetBids(Player.South, Fase.Shape).Last();
            var bidsForFases = auction.GetBids(Player.South, fases);
            var offSet = lastBidShape - offsetBid;
            if (zoomOffset != 0)
                bidsForFases = new List<Bid> { lastBidShape }.Concat(bidsForFases);

            var used4ClAsRelay = Used4ClAsRelay(auction);
            bidsForFases = bidsForFases.Select(b => b = (b - (used4ClAsRelay && b > fourClubBid ? offSet + 1 : offSet)) + zoomOffset);
            return bidsForFases;
        }

        private static bool Used4ClAsRelay(Auction auction)
        {
            var previousBiddingRound = auction.bids.First();
            foreach (var biddingRound in auction.bids.Skip(1))
            {
                if (biddingRound.Value.ContainsKey(Player.North) && biddingRound.Value[Player.North] == fourClubBid)
                    return previousBiddingRound.Value[Player.South] == threeSpadeBid;

                previousBiddingRound = biddingRound;
            }
            return false;
        }

        public static IEnumerable<string> GetMatchesWithNorthHand(List<string> shapeLengthStrs, List<string> possibleControls, string northHandStr)
        {
            var northHand = northHandStr.Split(',');
            foreach (var controlStr in possibleControls)
            {
                var controls = controlStr.Split(',').Select(x => x.TrimEnd('x')).ToArray();
                var controlByShapes = shapeLengthStrs.Select(shapeLengthStr => MergeControlAndShape(controls, shapeLengthStr)).Where(x => x.Count() == 4);
                var southHands = controlByShapes.Where(controlByShape => Match(controlByShape.ToArray(), northHand));
                foreach (var southHand in southHands)
                {
                    yield return string.Join(',', southHand);
                }
            }
        }

        /// <summary>
        /// Merges shapes and controls. If controls does not fit, it returns an IEnumerable with length < 4
        /// TODO This function needs improvement
        /// </summary>
        /// <param name="controlStr">{"A","","Q","K"}</param>
        /// <param name="shapeLengthStr">"3451"</param>
        /// <returns>"Qxx,xxxx,Axxxx,K"</returns>
        public static IEnumerable<string> MergeControlAndShape(string[] controls, string shapeLengthStr)
        {
            var shapes = shapeLengthStr.ToArray().Select(x => float.Parse(x.ToString())).ToList(); // {3,4,5,1}

            // This is because there can be two suits with the same length. So we added a small offset to make it unique
            foreach (var suit in Enumerable.Range(0, 4))
                shapes[suit] += (float)(4 - suit) / 10;
            // Shapes : // {3.4,4.3,5.2,1.3}

            var shapesOrdered = shapes.OrderByDescending(x => x).ToList(); // {5.2,4.3,3.4,1.3}

            var shapesLookup = shapes.ToLookup(key => shapes.IndexOf(key), value => shapesOrdered.IndexOf(value));

            foreach (var suit in Enumerable.Range(0, 4))
            {
                var shape = (int)shapes[suit];
                string controlStrSuit = controls[shapesLookup[suit].First()];
                if (shape < controlStrSuit.Length)
                    yield break;
                yield return controlStrSuit.PadRight(shape, 'x');
            }
        }

        private static bool Match(string[] hand1, string[] hand2)
        {
            foreach (var suit in Enumerable.Range(0, 4))
                foreach (var c in relevantCards)
                    if (hand1[suit].Contains(c) && hand2[suit].Contains(c))
                        return false;
            return true;
        }
    }
}
