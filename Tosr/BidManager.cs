
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;
using NLog.Filters;
using Common;
using Solver;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;

    using RelayBidKindFunc = Func<Auction, string, string, IEnumerable<Bid>, Suit, BidManager.RelayBidKind>;

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
        }

        public enum RelayBidKind
        {
            Relay,
            fourDiamondEndSignal,
            gameBid,
        }

        public enum CorrectnessContract
        {
            WrongTrumpSuit,
            MissedSmallSlam,
            MissedGrandSlam,
            GrandSlamTooHigh,
            SmallSlamTooHigh,
            SmallSlamCorrect,
            GrandSlamCorrect,
            GameCorrect,
            Unknonwn,
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
            {((-0.01, 30.01) , RelayBidKind.gameBid )},
            {((30.0, 70.01) , RelayBidKind.fourDiamondEndSignal)},
            {((70.0, 100.01) , RelayBidKind.Relay )},
        };

        static readonly int requiredMaxHcpToBid4Diamond = 17;
        public static readonly List<Fase> signOffFases = new List<Fase> {Fase.Pull3NTNoAsk, Fase.Pull3NTOneAsk, Fase.Pull3NTTwoAsks, Fase.Pull4DiamondsNoAsk, Fase.Pull4DiamondsOneAsk};
        public static readonly List<Fase> signOffFasesWithout3NTNoAsk = new List<Fase> { Fase.Pull3NTOneAsk, Fase.Pull3NTTwoAsks, Fase.Pull4DiamondsNoAsk, Fase.Pull4DiamondsOneAsk };

        private readonly RelayBidKindFunc getRelayBidKindFunc = null;

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
            if (useSingleDummySolver)
                this.getRelayBidKindFunc = GetRelayBidKindSolver;
        }

        // Constructor used for generate reverse dictionaries
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;
            this.getRelayBidKindFunc = GetRelayBidKind;
        }

        public void Init(Auction auction)
        {
            shape = new Lazy<(List<string> shapes, int zoomOffset)>(() => GetShapeStrFromAuction(auction, reverseDictionaries.ShapeAuctions));
            controlsScanning = new Lazy<(List<string> controls, int zoomOffset)>(() => GetControlsScanningStrFromAuction(auction, reverseDictionaries, 
                shape.Value.zoomOffset, Util.NrOfShortages(shape.Value.shapes.First())));
        }

        public Auction GetAuction(string northHand, string southHand)
        {
            logger.Debug($"Starting GetAuction for hand : {southHand}");
            Auction auction = new Auction();

            BiddingState biddingState = new BiddingState(fasesWithOffset);
            Player currentPlayer = Player.West;
            constructedSouthhandOutcome = ConstructedSouthhandOutcome.NotSet;
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
                if (!string.IsNullOrWhiteSpace(northHand))
                {
                    var southHand = string.Join(',', southHandShape.Select(x => new string('x', int.Parse(x.ToString()))));
                    var trumpSuit = Util.GetTrumpSuit(northHand, southHand);
                    if (biddingState.Fase == Fase.BidGame)
                    {
                        biddingState.Fase = Fase.End;
                        var game = Bid.GetGameContract(trumpSuit, biddingState.CurrentBid);
                        return biddingState.CurrentBid == game ? Bid.PassBid : game;
                    }

                    if (!biddingState.HasSignedOff)
                    {
                        var controls = GetAuctionForFaseWithOffset(auction, Bid.threeDiamondBid, shape.Value.zoomOffset, new Fase[] { Fase.Controls });
                        var controlBidCount = controls.Count();
                        var hcp = Util.GetHcpCount(northHand);
                        if (trumpSuit == Suit.NoTrump)
                        {
                            if (requirementsToPull3NT.TryGetValue(controlBidCount, out var hcps) && hcps.Contains(hcp))
                            {
                                var noTrumpBid = biddingState.CurrentBid < Bid.threeNTBid ? Bid.threeNTBid : Bid.fourNTBid;
                                biddingState.UpdateBiddingStateSignOff(controlBidCount, noTrumpBid);
                                return noTrumpBid;
                            }
                        }
                        else
                        {
                            if (controlBidCount == 0 && hcp <= requiredMaxHcpToBid4Diamond)
                            {
                                biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid);
                                return Bid.fourDiamondBid;
                            }
                            if (controlBidCount == 1)
                            {
                                var relayBidkind = getRelayBidKindFunc(auction, northHand, southHandShape, controls, trumpSuit);
                                switch (relayBidkind)
                                {
                                    case RelayBidKind.Relay:
                                        break;
                                    case RelayBidKind.fourDiamondEndSignal:
                                        if (biddingState.CurrentBid < Bid.fourClubBid)
                                        {
                                            biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid);
                                            return Bid.fourDiamondBid;
                                        }
                                        break;
                                    case RelayBidKind.gameBid:
                                        {
                                            biddingState.Fase = Fase.End;
                                            auction.hasSignedOff = true;
                                            return Bid.GetGameContract(trumpSuit, biddingState.CurrentBid);
                                        }
                                    default:
                                        throw new ArgumentException(nameof(relayBidkind));
                                }
                            }
                        }
                    }

                    if (biddingState.Fase == Fase.ScanningOther && !auction.GetBids(Player.South, Fase.ScanningOther).Any() && useSingleDummySolver)
                    {
                        var declarer = auction.GetDeclarer(trumpSuit);
                        var matches = GetMatchesWithNorthHand(shape.Value.shapes, controlsScanning.Value.controls, northHand);
                        if (matches.Count() == 1)
                        {
                            var confidenceTricks = GetConfidenceTricks(northHand, matches.First(), trumpSuit, declarer != Player.UnKnown ? declarer : Player.North);
                            if (GetConfidenceToBidSlam(confidenceTricks) < 60.0)
                            {
                                biddingState.Fase = Fase.End;
                                auction.hasSignedOff = true;
                                return Bid.GetGameContract(trumpSuit, biddingState.CurrentBid);
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

        public RelayBidKind GetRelayBidKindSolver(Auction auction, string northHand, string southHandShape, IEnumerable<Bid> controls, Suit trumpSuit)
        {
            Player declarer = auction.GetDeclarer(trumpSuit);
            var strControls = string.Join("", controls);
            List<int> possibleControls = reverseDictionaries.ControlsOnlyAuctions[strControls];
            var confidence = GetConfidenceTricks(northHand, southHandShape, possibleControls.First(), possibleControls.Last(), trumpSuit, declarer != Player.UnKnown ? declarer : Player.North);
            int confidenceToBidSlam = GetConfidenceToBidSlam(confidence);
            var relayBidkind = requirementsForRelayBid.Where(x =>confidenceToBidSlam > x.range.min && confidenceToBidSlam < x.range.max).First().relayBidKind;
            return relayBidkind;
        }

        public RelayBidKind GetRelayBidKind(Auction auction, string northHand, string southHandShape, IEnumerable<Bid> controls, Suit trumpSuit)
        {
            return (Util.GetHcpCount(northHand)) switch
            {
                var x when x < 19 => RelayBidKind.gameBid,
                var x when x >= 19 && x < 22 => RelayBidKind.fourDiamondEndSignal,
                _ => RelayBidKind.Relay,
            };
        }

        private static int GetConfidenceToBidSlam(Dictionary<int, int> confidenceTricks)
        {
            return (confidenceTricks.TryGetValue(12, out var smallSlamTricks) ? smallSlamTricks : 0) + (confidenceTricks.TryGetValue(13, out var grandSlamTricks) ? grandSlamTricks : 0);
        }

        private Dictionary<int, int> GetConfidenceTricks(string northHand, string southHandShape, int minControls, int maxControls, Suit trumpSuit, Player declarer)
        {
            var tricks = SingleDummySolver.SolveSingleDummy(3 - (int)trumpSuit, 3 - (int)declarer, northHand, southHandShape, minControls, maxControls);
            return tricks.GroupBy(x => x).ToDictionary(g => g.Key, g => (int)(100 * (double)g.ToList().Count() / ((double)tricks.Count)));
        }

        private Dictionary<int, int> GetConfidenceTricks(string northHand, string southHandControlScanning, Suit trumpSuit, Player declarer)
        {
            var tricks = SingleDummySolver.SolveSingleDummy(3 - (int)trumpSuit, 3 - (int)declarer, northHand, southHandControlScanning);
            return tricks.GroupBy(x => x).ToDictionary(g => g.Key, g => (int)(100 * (double)g.ToList().Count() / ((double)tricks.Count)));
        }

        private Bid CalculateEndContract(Auction auction, string northHand)
        {
            var constructedSouthHand = ConstructSouthHand(northHand, auction);
            var suit = Util.GetLongestSuit(northHand, constructedSouthHand);
            if (useSingleDummySolver)
            {
                var scores = SingleDummySolver.SolveSingleDummy(3 - (int)suit.Item1, 3 - (int)auction.GetDeclarer(suit.Item1), northHand, constructedSouthHand);
                var mostFrequent = scores.GroupBy(x => x).OrderByDescending(y => y.Count()).Take(1).Select(z => z.Key).First();
                var bid = new Bid(mostFrequent - 6, suit.Item1);
                return bid;
            }
            return Bid.PassBid;
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
            // Check if controls and their positions are correctly evaluated.
            if (nextfase == Fase.ScanningOther && reverseDictionaries != null)
            {
                var controlsInSuit = Util.GetHandWithOnlyControlsAs4333(handsString, "AK");
                if (!controlsScanning.Value.controls.Contains(controlsInSuit))
                    throw new InvalidOperationException($"Cannot find {controlsInSuit} in {string.Join('|', controlsScanning.Value.controls)}");

            }
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, bidId, lzoomOffset);
            if (nextfase == Fase.BidGame)
                auction.hasSignedOff = true;

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

            var fases = new [] { Fase.Controls, Fase.ScanningControls, Fase.ScanningOther };
            var controls = GetAuctionForFaseWithOffset(auction, Bid.threeDiamondBid, zoomOffset, fases);
            var strControls = string.Join("", controls);

            var controlsAuctions = Util.NrOfShortages(shape.Value.shapes.First()) switch
            {
                0 => reverseDictionaries.ControlsAuctions0,
                1 => reverseDictionaries.ControlsAuctions1,
                2 => reverseDictionaries.ControlsAuctions2,
                _ => throw new ArgumentException("nrOfshortages should be 0, 1 or 2"),
            };
            if (!controlsAuctions.TryGetValue(strControls, out var possibleControls))
            {
                throw SetOutcome($"Auction not found in controls. controls: {strControls}. NorthHand: {northHand}.", ConstructedSouthhandOutcome.AuctionNotFoundInControls);
            }
            var matches = GetMatchesWithNorthHand(shape.Value.shapes, possibleControls, northHand);
            if (matches.Count() == 1)
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
        /// <param name="hand"></param>
        /// <param name="auction"></param>
        /// <returns></returns>
        public string ConstructSouthHandSafe(HandsNorthSouth hand, Auction auction)
        {
            try
            {
                var southHand = ConstructSouthHand(hand.NorthHand, auction);
                var southHandStr = HandWithx(hand.SouthHand);

                if (southHand == southHandStr)
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.SouthhandMatches;
                    return $"Match is found: {southHand}. NorthHand: {hand.NorthHand}. SouthHand: {hand.SouthHand}";
                }
                else
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.IncorrectSouthhand;
                    return $"SouthHand is not equal to expected. Expected: {southHand}. Actual {southHandStr}. NorthHand: {hand.NorthHand}. SouthHand: {hand.SouthHand}";
                }
            }
            catch (Exception e)
            {
                return $"{e.Message} SouthHand: {hand.SouthHand}. Projected AKQ controls as 4333:{Util.GetHandWithOnlyControlsAs4333(hand.SouthHand, "AKQ")}. " +
                    $"Sign-off fases:{auction.GetBids(Player.South, signOffFases.ToArray()).FirstOrDefault()?.fase}";
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
        public static (List<string> controls, int zoomOffset) GetControlsScanningStrFromAuction(Auction auction, ReverseDictionaries reverseDictionaries, int zoomOffsetShape, int nrOfShortagess)
        {
            var fases = new [] { Fase.Controls, Fase.ScanningControls };
            var bidsForFase = GetAuctionForFaseWithOffset(auction, Bid.threeDiamondBid, zoomOffsetShape, fases).ToList();
            var bidsForFaseStr = string.Join("", bidsForFase);

            var controlScanningAuctions = nrOfShortagess switch
            {
                0 => reverseDictionaries.ControlScanningAuctions0,
                1 => reverseDictionaries.ControlScanningAuctions1,
                2 => reverseDictionaries.ControlScanningAuctions2,
                _ => throw new ArgumentException("nrOfshortages should be 0, 1 or 2"),
            };

            if (controlScanningAuctions.TryGetValue(bidsForFaseStr, out var controls))
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

            throw new InvalidOperationException($"{ string.Join("", bidsForFase) } not found in controls scanning dictionary. Auction:{auction.GetPrettyAuction("|")}. " +
                $"zoom-offset shape:{zoomOffsetShape}");
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
            var bidsForFases = auction.GetBids(Player.South, fases.Concat(signOffFasesWithout3NTNoAsk).ToArray());
            var offSet = lastBidShape - offsetBid;
            if (zoomOffset != 0)
                bidsForFases = new List<Bid> { lastBidShape }.Concat(bidsForFases);

            var used4ClAsRelay = Used4ClAsRelay(auction);
            var fourCLubsRelayOffSet = 0;
            var signOffOffset = 0;
            var previousBid = lastBidShape;
            var bidPull3NTNoAsk = auction.GetBids(Player.South).Where(bid => bid.fase == Fase.Pull3NTNoAsk);

            var bidsForFasesResult = bidsForFases.Select(b =>
            {
                if (signOffFases.Contains(b.fase) || (bidPull3NTNoAsk.Count() == 1 && signOffOffset == 0 && b > bidPull3NTNoAsk.Single()))
                    signOffOffset = GetSignOffBid(b) - previousBid;

                if (used4ClAsRelay && b > Bid.fourClubBid)
                    fourCLubsRelayOffSet = 1;
                previousBid = b;

                return b += zoomOffset - fourCLubsRelayOffSet - offSet - signOffOffset;
            });

            return bidsForFasesResult;

            Bid GetSignOffBid(Bid b)
            {
                var signOffBiddingRounds = auction.bids.Where(bids => bids.Value.TryGetValue(Player.South, out var bid) && signOffFases.Contains(bid.fase));
                if (signOffBiddingRounds.Count() == 0)
                    return Bid.PassBid;
                var signOffBiddingRound = signOffBiddingRounds.Single();
                // Return the next bid. The sign-off bid only shows points
                if (signOffBiddingRound.Value[Player.South].fase == Fase.Pull3NTNoAsk)
                    return auction.bids[signOffBiddingRound.Key + 1][Player.North] - 1;

                var signoffBidNorth = signOffBiddingRound.Value[Player.North];
                var bid = signoffBidNorth.suit == Suit.NoTrump ? signoffBidNorth - 1 : signoffBidNorth;
                return bid;
            }
        }

        private static bool Used4ClAsRelay(Auction auction)
        {
            var previousBiddingRound = auction.bids.First();
            foreach (var biddingRound in auction.bids.Skip(1))
            {
                if (biddingRound.Value.TryGetValue(Player.North, out var bid) && bid == Bid.fourClubBid)
                    return previousBiddingRound.Value[Player.South] == Bid.threeSpadeBid;

                previousBiddingRound = biddingRound;
            }
            return false;
        }

        private static IEnumerable<string> GetMatchesWithNorthHand(List<string> shapeLengthStrs, List<string> possibleControls, string northHandStr)
        {
            var northHand = northHandStr.Split(',');
            foreach (var controlStr in possibleControls)
            {
                var controls = controlStr.Split(',').Select(x => x.TrimEnd('x')).ToArray();
                var controlByShapes = shapeLengthStrs.Select(shapeLengthStr => MergeControlAndShape(controls, shapeLengthStr)).Where(x => x.Sum(x => x.Length) == 13);
                var southHands = controlByShapes.Where(controlByShape => Match(controlByShape.ToArray(), northHand));
                foreach (var southHand in southHands)
                {
                    yield return string.Join(',', southHand);
                }
            }
        }

        /// <summary>
        /// Merges shapes and controls
        /// </summary>
        /// <param name="controls">{"A","","Q","K"}</param>
        /// <param name="shapeLengthStr">"3451"</param>
        /// <returns>{"Qxx","xxxx","Axxxx","K"}</returns>
        public static IEnumerable<string> MergeControlAndShape(string[] controls, string shapeLengthStr)
        {
            var shapes = shapeLengthStr.ToArray().Select((x, index) => (float.Parse(x.ToString()), index)); // {3,4,5,1}
            // Sort by length, then by position 
            var shapesOrdered = shapes.OrderByDescending(x => x.Item1).ThenBy(x => x.index).ToList(); // {5,4,3,1}
            return shapes.Select(shape => controls[shapesOrdered.IndexOf(shape)].PadRight((int)shape.Item1, 'x'));
        }

        private static bool Match(string[] hand1, string[] hand2)
        {
            foreach (var suit in hand1.Zip(hand2, (x, y) => (x, y)))
                if (relevantCards.Any(c => suit.x.Contains(c) && suit.y.Contains(c)))
                    return false; 
            return true;
        }

        public CorrectnessContract CheckContract(Bid contract, HandsNorthSouth strHand, Player declarer)
        {
            if (!useSingleDummySolver)
                return CorrectnessContract.Unknonwn;
            var tricks = SingleDummySolver.SolveSingleDummy(3 - (int)contract.suit, 3 - (int)declarer, strHand.NorthHand, strHand.SouthHand);
            var mostFrequentRank = tricks.GroupBy(x => x).OrderByDescending(g => g.Count()).Take(1).Select(y => y.Key).Single() - 6;
            if (mostFrequentRank == 7 && contract.rank < 7)
                return CorrectnessContract.MissedGrandSlam;
            if (mostFrequentRank == 6 && contract.rank < 6)
                return CorrectnessContract.MissedSmallSlam;
            if (mostFrequentRank < 7 && contract.rank == 7)
                return CorrectnessContract.GrandSlamTooHigh;
            if (mostFrequentRank < 6 && contract.rank == 6)
                return CorrectnessContract.SmallSlamTooHigh;
            if (mostFrequentRank == 7 && contract.rank == 7)
                return CorrectnessContract.GrandSlamCorrect;
            if (mostFrequentRank == 6 && contract.rank == 6)
                return CorrectnessContract.SmallSlamCorrect;
            return CorrectnessContract.GameCorrect;
        }
    }
}
