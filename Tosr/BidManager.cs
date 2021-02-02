
using System;
using System.Linq;
using System.Collections.Generic;
using NLog;
using Common;
using Solver;
using Newtonsoft.Json;
using System.IO;
using Tosr.Properties;
using Newtonsoft.Json.Linq;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using FaseDictionary = Dictionary<Fase, Dictionary<string, List<int>>>;

    using RelayBidKindFunc = Func<Auction, string, IEnumerable<string>, IEnumerable<Bid>, Suit, BidManager.RelayBidKind>;

    public class BidManager
    {
        public enum ConstructedSouthhandOutcome
        {
            NotSet,
            AuctionNotFoundInControls,
            NoMatchFound,
            SouthhandMatches,
            MultipleMatchesFound,
            IncorrectSouthhand,
            NoMatchFoundNoQueens,
        }

        public enum RelayBidKind
        {
            Relay,
            fourDiamondEndSignal,
            gameBid,
        }

        public struct SystemParameters
        {
            [JsonProperty(Required = Required.Always)]
            public Dictionary<int, List<int>> hcpRelayerToSignOffInNT;

            [JsonProperty(Required = Required.Always)]
            [JsonConverter(typeof(requirementsForRelayBidConverter))]
            public List<((double min, double max) range, RelayBidKind relayBidKind)> requirementsForRelayBid;

            [JsonProperty(Required = Required.Always)]
            public int requiredMaxHcpToBid4Diamond;
        }

        public struct OptimizationParameters
        {
            [JsonProperty(Required = Required.Always)]
            public double requiredConfidenceToContinueRelaying;

            [JsonProperty(Required = Required.Always)]
            public int numberOfHandsForSolver;
        }

        public class requirementsForRelayBidConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JArray jArray = JArray.Load(reader);
                var requirementsForRelayBid = new List<((double min, double max) range, RelayBidKind relayBidKind)>();
                foreach (var entry in jArray)
                {
                    var range = ValueTuple.Create((double)entry[0][0], (double)entry[0][1]);
                    var item = (((double, double), RelayBidKind))ValueTuple.Create(range, Enum.Parse(typeof(RelayBidKind), entry[1].ToString()));
                    requirementsForRelayBid.Add(item);
                }
                return requirementsForRelayBid;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public static SystemParameters systemParameters;
        public static OptimizationParameters optimizationParameters;
        public static void SetSystemParameters(string json)
        {
            systemParameters = JsonConvert.DeserializeObject<SystemParameters>(json);
        }
        public static void SetOptimizationParameters(string json)
        {
            optimizationParameters = JsonConvert.DeserializeObject<OptimizationParameters>(json);
        }

        private readonly IBidGenerator bidGenerator;
        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries = null;
        readonly bool useSingleDummySolver = false;
        public Lazy<(List<string> shapes, int zoomOffset)> shape;
        public Lazy<(List<string> controls, int zoomOffset)> controlsScanning;

        static readonly char[] relevantCards = new[] { 'A', 'K' };
        public ConstructedSouthhandOutcome constructedSouthhandOutcome = ConstructedSouthhandOutcome.NotSet;

        public static readonly List<Fase> signOffFasesFor3NT = new List<Fase> { Fase.Pull3NTNoAsk, Fase.Pull3NTOneAskMin, Fase.Pull3NTOneAskMax, Fase.Pull3NTTwoAsks };
        public static readonly List<Fase> signOffFasesFor4Di = new List<Fase> { Fase.Pull4DiamondsNoAsk, Fase.Pull4DiamondsOneAskMin, Fase.Pull4DiamondsOneAskMax };
        public static readonly List<Fase> signOffFases = signOffFasesFor3NT.Concat(signOffFasesFor4Di).ToList();
        public static readonly List<Fase> signOffFasesWithout3NTNoAsk = signOffFasesFor4Di.Concat( new [] { Fase.Pull3NTOneAskMin, Fase.Pull3NTOneAskMax, Fase.Pull3NTTwoAsks }).ToList();

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
                getRelayBidKindFunc = GetRelayBidKindSolver;
        }

        // Constructor used for generate reverse dictionaries
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;
            getRelayBidKindFunc = GetRelayBidKind;
        }

        public void Init(Auction auction)
        {
            shape = new Lazy<(List<string> shapes, int zoomOffset)>(() => GetShapeStrFromAuction(auction, reverseDictionaries.ShapeAuctions));
            controlsScanning = new Lazy<(List<string> controls, int zoomOffset)>(() => GetControlsScanningStrFromAuction(auction, reverseDictionaries, shape.Value.zoomOffset, shape.Value.shapes.First()));
        }

        public Auction GetAuction(string northHand, string southHand)
        {
            logger.Debug($"Starting GetAuction for hand : {southHand}");
            var auction = new Auction();

            var biddingState = new BiddingState(fasesWithOffset);
            var currentPlayer = Player.West;
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
            // Add final pass
            auction.AddBid(Bid.PassBid);

            logger.Debug($"Ending GetAuction for hand : {southHand}");
            return auction;
        }

        public void NorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.EndOfBidding)
                return;

            if (biddingState.Fase != Fase.End && (biddingState.CurrentBid == Bid.PassBid || biddingState.CurrentBid < Bid.sixSpadeBid || !useSingleDummySolver))
                biddingState.CurrentBid = GetRelayBid(biddingState, auction, northHand);
            else
            {
                biddingState.EndOfBidding = true;
                // Try to guess contract by using single dummy solver
                if (useSingleDummySolver)
                {
                    try
                    {
                        biddingState.CurrentBid = CalculateEndContract(auction, northHand, biddingState.CurrentBid);
                        return;
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
                        if (game == Bid.PassBid)
                            biddingState.EndOfBidding = true;
                        return game;
                    }

                    if (!biddingState.RelayerHasSignedOff)
                    {
                        var controls = GetAuctionForFaseWithOffset(auction, shape.Value.zoomOffset, new Fase[] { Fase.Controls });
                        var controlBidCount = controls.Count();
                        var hcp = Util.GetHcpCount(northHand);
                        if (trumpSuit == Suit.NoTrump)
                        {
                            if (systemParameters.hcpRelayerToSignOffInNT.TryGetValue(controlBidCount, out var hcps) && hcps.Contains(hcp))
                            {
                                var noTrumpBid = biddingState.CurrentBid < Bid.threeNTBid ? Bid.threeNTBid : Bid.fourNTBid;
                                biddingState.UpdateBiddingStateSignOff(controlBidCount, noTrumpBid);
                                return noTrumpBid;
                            }
                        }
                        else
                        {
                            if (controlBidCount == 0 && hcp <= systemParameters.requiredMaxHcpToBid4Diamond && biddingState.CurrentBid >= Bid.threeSpadeBid)
                            {
                                biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid);
                                return Bid.fourDiamondBid;
                            }
                            if (controlBidCount == 1)
                            {
                                var relayBidkind = getRelayBidKindFunc(auction, northHand, shape.Value.shapes, controls, trumpSuit);
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
                                            auction.responderHasSignedOff = true;
                                            var game = Bid.GetGameContract(trumpSuit, biddingState.CurrentBid);
                                            if (game == Bid.PassBid)
                                                biddingState.EndOfBidding = true;
                                            return game;
                                        }
                                    default:
                                        throw new ArgumentException(nameof(relayBidkind));
                                }
                            }
                        }
                    }

                    if (biddingState.Fase == Fase.ScanningOther && useSingleDummySolver)
                    {
                        var matches = GetMatchesWithNorthHand(shape.Value.shapes, controlsScanning.Value.controls, northHand);
                        if (matches.Count() >= 1)
                        {
                            var bidsScanningOther = auction.GetBids(Player.South, Fase.ScanningOther);
                            // TODO also call this function when some part of the queens is known. I.e. when control scanning has used zoom
                            var queens = GetQueensFromAuction(auction, reverseDictionaries);
                            var hcp = GetHcpFromAuction(auction, reverseDictionaries.SignOffFasesAuctions);
                            var declarer = auction.GetDeclarerOrNorth(trumpSuit);
                            var confidenceTricks = GetConfidenceTricks(northHand, matches, hcp, queens, trumpSuit, declarer);
                            if (GetConfidenceToBidSlam(confidenceTricks) < optimizationParameters.requiredConfidenceToContinueRelaying)
                            {
                                biddingState.Fase = Fase.End;
                                constructedSouthhandOutcome = ConstructedSouthhandOutcome.SouthhandMatches;
                                auction.responderHasSignedOff = true;
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

        private static MinMax GetHcpFromAuction(Auction auction, FaseDictionary faseAuctions)
        {
            var signOffBids = auction.GetBids(Player.South, signOffFases.ToArray());
            if (signOffBids.Count() == 1)
            {
                var signOffBid = signOffBids.Single();
                var sigOffBidStr = (signOffBid.fase switch
                {
                    Fase.Pull3NTNoAsk => signOffBid,
                    Fase.Pull3NTOneAskMin => Bid.threeNTBid + 1,
                    Fase.Pull3NTOneAskMax => Bid.threeNTBid + 1,
                    Fase.Pull3NTTwoAsks => Bid.threeNTBid + 1,
                    Fase.Pull4DiamondsNoAsk => Bid.fourDiamondBid + 2,
                    Fase.Pull4DiamondsOneAskMin => Bid.fourDiamondBid + 2,
                    Fase.Pull4DiamondsOneAskMax => Bid.fourDiamondBid + 2,
                    _ => throw new ArgumentException(nameof(signOffBid.fase)),
                }).ToString();
                // TODO handle case where sign-off bid is 4NT
                if (faseAuctions[signOffBid.fase].TryGetValue(sigOffBidStr, out var hcps))
                    return new MinMax(hcps.Min(), hcps.Max());
            }

            return null;
        }

        public RelayBidKind GetRelayBidKindSolver(Auction auction, string northHand, IEnumerable<string> southHandShapes, IEnumerable<Bid> controls, Suit trumpSuit)
        {
            var declarer = auction.GetDeclarer(trumpSuit);
            var strControls = string.Join("", controls);
            var possibleControls = reverseDictionaries.ControlsOnlyAuctions[strControls];
            var confidence = GetConfidenceTricks(northHand, southHandShapes, possibleControls.First(), possibleControls.Last(), trumpSuit, declarer != Player.UnKnown ? declarer : Player.North);
            var confidenceToBidSlam = GetConfidenceToBidSlam(confidence);
            var relayBidkind = systemParameters.requirementsForRelayBid.Where(x =>confidenceToBidSlam >= x.range.min && confidenceToBidSlam <= x.range.max).First().relayBidKind;
            return relayBidkind;
        }

        public RelayBidKind GetRelayBidKind(Auction auction, string northHand, IEnumerable<string> southHandShapes, IEnumerable<Bid> controls, Suit trumpSuit)
        {
            return (Util.GetHcpCount(northHand)) switch
            {
                var x when x < 19 => RelayBidKind.gameBid,
                var x when x >= 19 && x < 22 => RelayBidKind.fourDiamondEndSignal,
                _ => RelayBidKind.Relay,
            };
        }

        private static Dictionary<int, double> GetConfidenceTricks(string northHand, IEnumerable<string> matches, int minControls, int maxControls, Suit trumpSuit, Player declarer)
        {
            return GroupTricks(matches.SelectMany(match => SingleDummySolver.SolveSingleDummy(trumpSuit, declarer, northHand, match, minControls, maxControls, optimizationParameters.numberOfHandsForSolver)));
        }

        private static Dictionary<int, double> GetConfidenceTricks(string northHand, IEnumerable<string> matches, MinMax hcp, string queens, Suit trumpSuit, Player declarer)
        {
            return GroupTricks(matches.SelectMany(match => SingleDummySolver.SolveSingleDummy(trumpSuit, declarer, northHand, match, hcp, queens, optimizationParameters.numberOfHandsForSolver)));
        }

        private static Dictionary<int, double> GroupTricks(IEnumerable<int> tricks)
        {
            return tricks.GroupBy(x => x).ToDictionary(g => g.Key, g => 100 * (double)g.ToList().Count() / tricks.Count());
        }

        private static double GetConfidenceToBidSlam(Dictionary<int, double> confidenceTricks)
        {
            return (confidenceTricks.TryGetValue(12, out var smallSlamTricks) ? smallSlamTricks : 0.0) + (confidenceTricks.TryGetValue(13, out var grandSlamTricks) ? grandSlamTricks : 0.0);
        }

        private Bid CalculateEndContract(Auction auction, string northHand, Bid currentBid)
        {
            if (!useSingleDummySolver)
                return Bid.PassBid;

            var constructedSouthHands = ConstructSouthHand(northHand);
            var suitAndLength = Util.GetLongestSuit(northHand, constructedSouthHands.First());
            var suit = suitAndLength.Item2 >= 8 ? suitAndLength.Item1 : Suit.NoTrump;
            var queens = GetQueensFromAuction(auction, reverseDictionaries);
            var hcp = GetHcpFromAuction(auction, reverseDictionaries.SignOffFasesAuctions);
            var scores = constructedSouthHands.SelectMany(match => SingleDummySolver.SolveSingleDummy(suit, auction.GetDeclarerOrNorth(suit), northHand, match, hcp, queens, optimizationParameters.numberOfHandsForSolver));
            var bid = Bid.GetBestContract(Util.GetExpectedContract(scores), suit, currentBid);
            if (bid > currentBid)
                return bid;
            if (bid == currentBid)
                return Bid.PassBid;

            // Try NT
            var scoresNT = constructedSouthHands.SelectMany(match => SingleDummySolver.SolveSingleDummy(Suit.NoTrump, auction.GetDeclarerOrNorth(Suit.NoTrump), northHand, match, hcp, queens, optimizationParameters.numberOfHandsForSolver));
            var bidNT = Bid.GetBestContract(Util.GetExpectedContract(scoresNT), Suit.NoTrump, currentBid);
            return bidNT > currentBid ? bidNT : Bid.PassBid;
        }

        public void SouthBid(BiddingState biddingState, Auction auction, string handsString)
        {
            if (biddingState.Fase == Fase.End)
            {
                auction.AddBid(Bid.PassBid);
                biddingState.CurrentBid = Bid.PassBid;
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
                auction.responderHasSignedOff = true;

        }

        /// <summary>
        /// Construct southhand to compare with the actual southhand
        /// </summary>
        public string ConstructSouthHandSafe(string[] hand, Auction auction)
        {
            try
            {
                var southHand = ConstructSouthHand(hand[(int)Player.North]);

                if (southHand.Count() > 1)
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.MultipleMatchesFound;
                    return $"Multiple matches found. Matches: {string.Join('|', southHand)}. NorthHand: {hand[(int)Player.North]}. SouthHand: {hand[(int)Player.South]}";
                }

                var southHandStr = Util.HandWithx(hand[(int)Player.South]);
                if (southHand.First() == southHandStr)
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.SouthhandMatches;
                    if (!CheckQueens(GetQueensFromAuction(auction, reverseDictionaries), hand[(int)Player.South]))
                        return $"Match is found but queens are wrong : Expected queens: {GetQueensFromAuction(auction, reverseDictionaries)}. SouthHand: {hand[(int)Player.South]}";

                    return $"Match is found: {southHand.First()}. NorthHand: {hand[(int)Player.North]}. SouthHand: {hand[(int)Player.South]}";
                }
                else
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.IncorrectSouthhand;
                    return $"SouthHand is not equal to expected. Expected: {southHand}. Actual {southHandStr}. NorthHand: {hand[(int)Player.North]}. SouthHand: {hand[(int)Player.South]}";
                }
            }
            catch (Exception e)
            {
                return $"{e.Message} SouthHand: {hand[(int)Player.South]}. Projected AKQ controls as 4333:{Util.GetHandWithOnlyControlsAs4333(hand[(int)Player.South], "AKQ")}. " +
                    $"Sign-off fases:{auction.GetBids(Player.South, signOffFases.ToArray()).FirstOrDefault()?.fase}";
            }
        }

        /// <summary>
        /// Construct southhand to use for single dummy analyses
        /// Can throw
        /// </summary>
        public IEnumerable<string> ConstructSouthHand(string northHand)
        {
            logger.Debug($"Starting ConstructSouthHand for northhand : {northHand}");

            List<string> possibleControls;
            try
            {
                possibleControls = controlsScanning.Value.controls;
            }
            catch (Exception exception)
            {
                throw SetOutcome(exception.Message, ConstructedSouthhandOutcome.AuctionNotFoundInControls);
            }
            var matches = GetMatchesWithNorthHand(shape.Value.shapes, possibleControls, northHand);
            if (matches.Count() == 0)
                throw SetOutcome($"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {northHand}.", ConstructedSouthhandOutcome.NoMatchFound);

            logger.Debug($"Ending ConstructSouthHand. southhand : {string.Join("|", matches)}");
            return matches;
        }

        private Exception SetOutcome(string message, ConstructedSouthhandOutcome outcome)
        {
            logger.Warn($"Outcome not satisfied. {outcome}. Message : {message}");
            constructedSouthhandOutcome = outcome;
            return new InvalidOperationException(message);
        }

        /// <summary>
        /// Lookup in the shape dictionary. If not found, it tries to find an auction when the last bid was done with zoom
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
        public static (List<string> controls, int zoomOffset) GetControlsScanningStrFromAuction(Auction auction, ReverseDictionaries reverseDictionaries, int zoomOffsetShape, string shapeStr)
        {
            var fases = new[] { Fase.Controls, Fase.ScanningControls };
            var bidsForFase = GetAuctionForFaseWithOffset(auction, zoomOffsetShape, fases).ToList();
            var controlScanningAuctions = reverseDictionaries.GetControlScanningDictionary(shapeStr);

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

            throw new InvalidOperationException($"{ string.Join("", bidsForFase) } not found in controls scanning dictionary. Auction:{auction.GetPrettyAuction("|")}. " +
                $"zoom-offset shape:{zoomOffsetShape}");
        }

        /// <summary>
        /// Extracts the control and scanning part of the auction and applies an offset of offsetBid
        /// </summary>
        /// <param name="auction">Generated auction </param>
        /// <param name="fase">Which fases to get the offset from</param>
        /// <returns></returns>
        public static IEnumerable<Bid> GetAuctionForFaseWithOffset(Auction auction, int zoomOffset, Fase[] fases)
        {
            var lastBidShape = auction.GetBids(Player.South, Fase.Shape).Last();
            var bidsForFases = auction.GetBids(Player.South, fases.Concat(signOffFasesWithout3NTNoAsk).ToArray());
            var offSet = lastBidShape - Bid.threeDiamondBid;
            if (zoomOffset != 0)
                bidsForFases = new [] { lastBidShape }.Concat(bidsForFases);

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

        /// <summary>
        /// Returns a string of 4 characters when each character is "Y" "N" "X". "X" means not yet known.
        /// </summary>
        public string GetQueensFromAuction(Auction auction, ReverseDictionaries reverseDictionaries)
        {
            string shapeStr = shape.Value.shapes.First();
            int zoomOffset = controlsScanning.Value.zoomOffset;
            var lastBidPreviousFase = auction.GetBids(Player.South, (new[] { Fase.Controls, Fase.ScanningControls }).Concat(signOffFasesWithout3NTNoAsk).ToArray()).Last();
            var queensBids = auction.GetBids(Player.South, Fase.ScanningOther);
            var offset = lastBidPreviousFase - ReverseDictionaries.GetOffsetBidForQueens(shapeStr);
            if (zoomOffset != 0)
            {
                queensBids = new [] { lastBidPreviousFase}.Concat(queensBids);
                offset -= (zoomOffset + 1);
            }

            if (queensBids.Count() == 0)
                return null;

            queensBids = queensBids.Select(bid => bid - offset);
            var queensAuctions = reverseDictionaries.GetQueensDictionary(shapeStr);
            var bidsForFaseQueens = string.Join("", queensBids);

            if (queensAuctions.TryGetValue(bidsForFaseQueens, out var queens))
            {
                logger.Debug($"Found queens for auction. Queens:{queens}. QueensBids:{bidsForFaseQueens}. Auction:{auction.GetPrettyAuction("|")}");
                return GetQueensOrdered(shapeStr, queens);
            }

            throw new InvalidOperationException($"{ bidsForFaseQueens } not found in queens dictionary. Auction:{auction.GetPrettyAuction("|")}. " +
                $"zoom-offset control scanning:{zoomOffset}");
        }

        private static string GetQueensOrdered(string shapeStr, string queens)
        {
            var shapes = shapeStr.ToArray().Select((x, index) => (int.Parse(x.ToString()), index)); // {3,4,5,1}
            var shapesOrdered = shapes.OrderByDescending(x => x.Item1).ThenBy(x => x.index).ToList(); // {5,4,3,1}
            var queensOrdered = new string(shapes.Select(x => queens[shapesOrdered.IndexOf(x)]).ToArray());
            return queensOrdered;
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
                    yield return string.Join(',', southHand);
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
            var shapes = shapeLengthStr.ToArray().Select((x, index) => (int.Parse(x.ToString()), index)); // {3,4,5,1}
            // Sort by length, then by position 
            var shapesOrdered = shapes.OrderByDescending(x => x.Item1).ThenBy(x => x.index).ToList(); // {5,4,3,1}
            return shapes.Select(shape => controls[shapesOrdered.IndexOf(shape)].PadRight(shape.Item1, 'x'));
        }

        private static bool Match(string[] hand1, string[] hand2)
        {
            foreach (var suit in hand1.Zip(hand2, (x, y) => (x, y)))
                if (relevantCards.Any(c => suit.x.Contains(c) && suit.y.Contains(c)))
                    return false; 
            return true;
        }

        public static bool CheckQueens(string queens, string hand)
        {
            if (queens == null)
                return true;
            var zip = hand.Split(",").Zip(queens.ToCharArray(), (suit, queen) => (suit, queen));
            foreach (var (suit, queen) in zip)
            {
                if (queen == 'Y' && !suit.Contains('Q'))
                    return false;
                if (queen == 'N' && suit.Contains('Q'))
                    return false;
            }
            return true;
        }
    }
}
