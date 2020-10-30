
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NLog;
using Common;
using Solver;
using System.Diagnostics;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsOnlyDictionary = Dictionary<string, List<int>>;
    using ControlsDictionary = Dictionary<string, List<string>>;
    using ControlScanningDictionary = Dictionary<string, (List<string> controlsScanning, bool zoom)>;

    using RelayBidKindFunc = Func<Auction, string, string, IEnumerable<Bid>, Suit, BidManager.RelayBidKind>;
    using RelayBidFunc = Func<BiddingState, Auction, string, Bid>;

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

        public enum RelayBidKind
        {
            Relay,
            fourDiamondEndSignal,
            gameBid,
        }

        private readonly IBidGenerator bidGenerator;
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries = null;
        readonly bool useSingleDummySolver = false;
        private Lazy<(List<string> shapes, int zoomOffset)> shape;
        private Lazy<(List<string> controls, int zoomOffset)> controlsScanning;

        static readonly char[] relevantCards = new[] { 'A', 'K', 'Q' };
        public ConstructedSouthhandOutcome constructedSouthhandOutcome = ConstructedSouthhandOutcome.NotSet;

        // Special TOSR logic. Should be in a JSON file as parameters
        static readonly Dictionary<int, List<int>> requirementsToPull3NT = new Dictionary<int, List<int>> {
            {0, Enumerable.Range(16, 5).ToList()},
            {1, Enumerable.Range(21, 2).ToList()},
            {2, Enumerable.Range(23, 2).ToList()}};

        static readonly List<((double min, double max) range, RelayBidKind relayBidKind)> requirementsForRelayBid = new List<((double, double), RelayBidKind)> {
            {((0.0, 11.01) , RelayBidKind.gameBid )},
            {((11.0, 12.01) , RelayBidKind.fourDiamondEndSignal)},
            {((12.0, 13.01) , RelayBidKind.Relay )},
        };

        static readonly int requiredMaxHcpToBid4Diamond = 17;
        public static readonly List<Fase> signOffFases = new List<Fase> {Fase.Pull3NTNoAsk, Fase.Pull3NTOneAsk, Fase.Pull3NTTwoAsks, Fase.Pull4DiamondsNoAsk, Fase.Pull4DiamondsOneAsk};

        private readonly RelayBidKindFunc getRelayBidKindFunc = (auction, northHand, southHandShape, controls, trumpSuit) => { return BidManager.RelayBidKind.Relay; };
        private readonly RelayBidFunc getRelayBidFunc = (BiddingState biddingState, Auction auction, string northHand) => { return Bid.NextBid(biddingState.CurrentBid); };

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Constructor used for test
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, RelayBidKindFunc getRelayBidKindFunc) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            this.getRelayBidKindFunc = getRelayBidKindFunc;
        }

        // Standard constructor
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, bool useSingleDummySolver) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            this.useSingleDummySolver = useSingleDummySolver;
        }

        // Constructor used for generate reverse dictionaries for sign-off auctions
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, RelayBidFunc getRelayBidFunc) :
            this(bidGenerator, fasesWithOffset)
        {
            this.getRelayBidFunc = getRelayBidFunc;
        }

        // Constructor used for generate reverse dictionaries
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;
            this.getRelayBidFunc = GetRelayBid;
            this.getRelayBidKindFunc = GetRelayBidKind;
        }

        public void Init(Auction auction)
        {
            shape = new Lazy<(List<string> shapes, int zoomOffset)>(() => GetShapeStrFromAuction(auction, reverseDictionaries.ShapeAuctions));
            controlsScanning = new Lazy<(List<string> controls, int zoomOffset)>(() => GetControlsScanningStrFromAuction(auction, reverseDictionaries, shape.Value.zoomOffset));
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
                if (auction.bids.Count() > 30)
                    throw new InvalidOperationException("Bidding is stuck in a loop");
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
                biddingState.CurrentBid = getRelayBidFunc(biddingState, auction, northHand);
            else
            {
                biddingState.EndOfBidding = true;
                // Try to guess contract by using single dummy solver
                if (useSingleDummySolver)
                {
                    try
                    {
                        var bid = CalculateEndContract(auction, northHand);
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
            if (biddingState.Fase != Fase.Shape && reverseDictionaries != null)
            {
                var southHandShape = shape.Value.shapes.First();
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
                    var controls = GetAuctionForFaseWithOffset(auction, Bid.threeDiamondBid, shape.Value.zoomOffset, new Fase[] { Fase.Controls });
                    var controlBidCount = controls.Count();
                    var southHand = string.Join(',', southHandShape.Select(x => new string('x', int.Parse(x.ToString()))));
                    var trumpSuit = Util.GetTrumpSuit(northHand, southHand);
                    var hcp = Util.GetHcpCount(northHand);
                    if (trumpSuit == Suit.NoTrump)
                    {
                        if (requirementsToPull3NT.TryGetValue(controlBidCount, out var hcps) && hcps.Contains(hcp))
                        {
                            var noTrumpBid = biddingState.CurrentBid < Bid.threeNTBid ? Bid.threeNTBid : Bid.fourNTBid;
                            biddingState.UpdateBiddingStateSignOff(controlBidCount, noTrumpBid, true);
                            return noTrumpBid;
                        }
                    }
                    else
                    {
                        if (controlBidCount == 0 && hcp <= requiredMaxHcpToBid4Diamond)
                        {
                            biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid, false);
                            return Bid.fourDiamondBid;
                        }
                        if (controlBidCount == 1)
                        {
                            var relayBidkind = getRelayBidKindFunc(auction, northHand, southHandShape, controls, trumpSuit);
                            switch (relayBidkind)
                            {
                                case RelayBidKind.Relay:
                                    biddingState.HasSignedOff = true;
                                    break;
                                case RelayBidKind.fourDiamondEndSignal:
                                    if (biddingState.CurrentBid < Bid.fourClubBid)
                                    {
                                        biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid, false);
                                        return Bid.fourDiamondBid;
                                    }
                                    break;
                                case RelayBidKind.gameBid:
                                    {
                                        biddingState.Fase = Fase.End;
                                        return Bid.GetGameContract(trumpSuit);
                                    }
                                default:
                                    throw new ArgumentException(nameof(relayBidkind));
                            }
                        }
                    }
                }

                if (biddingState.CurrentBid == Bid.threeSpadeBid)
                {
                    string shape = new string(southHandShape.ToCharArray().OrderByDescending(x => x).ToArray());
                    if (shape != "7330")
                    {
                        biddingState.RelayBidIdLastFase++;
                        return Bid.fourClubBid;
                    }
                }
            }

            return Bid.NextBid(biddingState.CurrentBid);
        }

        public RelayBidKind GetRelayBidKind(Auction auction, string northHand, string southHandShape, IEnumerable<Bid> controls, Suit trumpSuit)
        {
            Player declarer = auction.GetDeclarer(trumpSuit);
            var averageTricks = GetAverageTricks(northHand, southHandShape, controls, trumpSuit, declarer != Player.UnKnown ? declarer : Player.North);
            var relayBidkind = requirementsForRelayBid.Where(x => averageTricks > x.range.min && averageTricks < x.range.max).First().relayBidKind;
            return relayBidkind;
        }

        private double GetAverageTricks(string northHand, string southHandShape, IEnumerable<Bid> controls, Suit trumpSuit, Player declarer)
        {
            var strControls = string.Join("", controls);
            var possibleControls = reverseDictionaries.ControlsOnlyAuctions[strControls];
            var tricks = SingleDummySolver.SolveSingleDummy(3 - (int)trumpSuit, 3 - (int)declarer, northHand, 
                southHandShape, possibleControls.First(), possibleControls.Last());
            var averageTricks = tricks.Average();
            return averageTricks;
        }

        private Bid CalculateEndContract(Auction auction, string northHand)
        {
            // TODO solve sign-off bids!!!
            var constructedSouthHand = ConstructSouthHand(northHand, auction);
            var suit = Util.GetLongestSuit(northHand, constructedSouthHand);
            var scores = SingleDummySolver.SolveSingleDummy(3 - (int)suit.Item1, 3 - (int)auction.GetDeclarer(suit.Item1), northHand, constructedSouthHand);
            var mostFrequent = scores.GroupBy(x => x).OrderByDescending(y => y.Count()).Take(1).Select(z => z.Key).First();
            var bid = new Bid(mostFrequent - 6, (Suit)(3 - suit.Item1));
            return bid;
        }

        public void SouthBid(BiddingState biddingState, Auction auction, string handsString)
        {
            if (biddingState.Fase == Fase.End)
            {
                auction.AddBid(Bid.PassBid);
                biddingState.EndOfBidding = true;
                return;
            }
            var (bidIdFromRule, nextfase, description, zoomOffset) = bidGenerator.GetBid(biddingState, handsString);
            var bidId = biddingState.CalculateBid(bidIdFromRule, description, zoomOffset != 0);
            auction.AddBid(biddingState.CurrentBid);

            var lzoomOffset = nextfase switch
                {
                    Fase.Controls => reverseDictionaries == null ? zoomOffset : shape.Value.zoomOffset,
                    Fase.ScanningOther => reverseDictionaries == null ? zoomOffset : controlsScanning.Value.zoomOffset,
                    _ => 0,
                };
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, bidId, lzoomOffset);
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
            var controls = GetAuctionForFaseWithOffset(auction, Bid.threeDiamondBid, zoomOffset, new Fase[] { Fase.Controls, Fase.ScanningControls, Fase.ScanningOther });
            var strControls = string.Join("", controls);

            if (!reverseDictionaries.ControlsAuctions.TryGetValue(strControls, out var possibleControls))
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
        /// <param name="shapeAuctions"></param>
        /// <returns></returns>
        public static (List<string> shapes, int zoomOffset) GetShapeStrFromAuction(Auction auction, ShapeDictionary shapeAuctions)
        {
            var allBids = auction.GetBids(Player.South, Fase.Shape);
            if (shapeAuctions.TryGetValue(string.Join("", allBids), out var shape))
                return (shape.pattern, shape.zoom ? 2 : 0);

            var lastBid = allBids.Last();
            var allButLastBid = allBids.Take(allBids.Count() - 1);
            for (var bid = lastBid - 1; bid >= Bid.twoNTBid; bid--)
            {
                var allBidsNew = allButLastBid.Concat(new[] { bid });
                var bidsStr = allBidsNew.Aggregate(string.Empty, (current, bid) => current + bid);
                // Add two because auction is two bids lower if zoom applies
                if (shapeAuctions.TryGetValue(bidsStr, out var zoom) && zoom.zoom)
                    return (zoom.pattern, (lastBid - bid) + 2);
            }

            throw new InvalidOperationException($"{ string.Join("", allBids) } not found in shape dictionary");
        }

        /// <summary>
        /// Lookup in the controlScanning dictionary. If not found, it tries to find an auction when the last bid was done with zoom
        /// </summary>
        /// <param name="auction"></param>
        /// <param name="controlScanningAuctions"></param>
        /// <param name="zoomOffsetShape"></param>
        /// <returns></returns>
        public static (List<string> controls, int zoomOffset) GetControlsScanningStrFromAuction(Auction auction, ReverseDictionaries reverseDictionaries, int zoomOffsetShape)
        {
            var fases = new List<Fase> { Fase.Controls, Fase.ScanningControls }.Concat(BidManager.signOffFases).ToArray();
            var bidsForFase = GetAuctionForFaseWithOffset(auction, Bid.threeDiamondBid, zoomOffsetShape, fases);
            var signOffBids = auction.GetBids(Player.South, BidManager.signOffFases.ToArray());
            var sigOffFase = signOffBids.Any() ? signOffBids.First().fase : Fase.Unknown;
            var controlScanningAuctions = sigOffFase switch
            {
                Fase.Unknown => reverseDictionaries.ControlScanningAuctions,
                Fase.Pull3NTNoAsk => reverseDictionaries.ControlScanningPullAuctions3NT,
                Fase.Pull3NTOneAsk => reverseDictionaries.ControlScanningPullAuctions3NT,
                Fase.Pull3NTTwoAsks => reverseDictionaries.ControlScanningPullAuctions3NT,
                Fase.Pull4DiamondsNoAsk => reverseDictionaries.ControlScanningPullAuctions4Di,
                Fase.Pull4DiamondsOneAsk => reverseDictionaries.ControlScanningPullAuctions4Di,
                _ => null,
            };
            if (controlScanningAuctions.TryGetValue(string.Join("", bidsForFase), out var controls))
                return (controls.controlsScanning, controls.zoom ? 1 : 0);

            var lastBid = bidsForFase.Last();
            var firstBid = bidsForFase.First();
            var allButLastBid = bidsForFase.Take(bidsForFase.Count() - 1);
            for (var bid = lastBid - 1; bid >= firstBid; bid--)
            {
                var allBidsNew = allButLastBid.Concat(new[] { bid });
                // Add one because auction is one bids lower if zoom applies
                if (controlScanningAuctions.TryGetValue(string.Join("", allBidsNew), out var zoom) && zoom.zoom)
                    return (zoom.controlsScanning, (lastBid - bid) + 1);
            }

            throw new InvalidOperationException($"{ string.Join("", bidsForFase) } not found in controls scanning dictionary");
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
            var signOffBids = auction.GetBids(Player.South).Where(bid => signOffFases.Contains(bid.fase));
            Debug.Assert(signOffBids.Count() <= 1);
            var usedSignOffBid = signOffBids.Count() == 1;
            bidsForFases = bidsForFases.Select(b =>
            {
                var bidAfterSignOff = usedSignOffBid && b >= signOffBids.Single();
                var bidAfter4ClRelay = used4ClAsRelay && b > Bid.fourClubBid;
                return b = b + zoomOffset - (bidAfter4ClRelay && !bidAfterSignOff ? offSet + 1 : bidAfterSignOff ? 0 : offSet);
            });

            return bidsForFases;
        }

        private static bool Used4ClAsRelay(Auction auction)
        {
            var previousBiddingRound = auction.bids.First();
            foreach (var biddingRound in auction.bids.Skip(1))
            {
                if (biddingRound.Value.ContainsKey(Player.North) && biddingRound.Value[Player.North] == Bid.fourClubBid)
                    return previousBiddingRound.Value[Player.South] == Bid.threeSpadeBid;

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
