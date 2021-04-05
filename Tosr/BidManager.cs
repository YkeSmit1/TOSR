using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Common;
using Solver;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using FaseDictionary = Dictionary<Fase, Dictionary<string, List<int>>>;

    using RelayBidKindFunc = Func<Auction, string, SouthInformation, BidManager.RelayBidKind>;

    public class BidManager
    {
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
            [JsonConverter(typeof(RequirementsForRelayBidConverter))]
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

        public class RequirementsForRelayBidConverter : JsonConverter
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

        private static SystemParameters systemParameters;
        private static OptimizationParameters optimizationParameters;
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

        private readonly RelayBidKindFunc GetRelayBidKindFunc = null;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger loggerBidding = LogManager.GetLogger("bidding");

        public BiddingInformation biddingInformation;

        // Constructor used for test
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, RelayBidKindFunc getRelayBidKindFunc) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            this.GetRelayBidKindFunc = getRelayBidKindFunc;
        }

        // Standard constructor
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, bool useSingleDummySolver) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            this.useSingleDummySolver = useSingleDummySolver;
            if (useSingleDummySolver)
                GetRelayBidKindFunc = GetRelayBidKindSolver;
        }

        // Constructor used for generate reverse dictionaries
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;
            GetRelayBidKindFunc = GetRelayBidKind;
        }

        public void Init(Auction auction)
        {
            biddingInformation = new BiddingInformation(reverseDictionaries, auction);
        }

        public Auction GetAuction(string northHand, string southHand)
        {
            logger.Debug($"Starting GetAuction for hand : {southHand}");
            loggerBidding.Info("");
            loggerBidding.Info("*** Start bidding ***");
            loggerBidding.Info($"North:{northHand} South:{southHand}");

            var auction = new Auction();
            biddingInformation = new BiddingInformation(reverseDictionaries, auction);
            var biddingState = new BiddingState(fasesWithOffset);
            var currentPlayer = Player.West;

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
            loggerBidding.Info("Auction:");
            loggerBidding.Info($"{auction.GetPrettyAuction("\n")}");
            loggerBidding.Info("*** End bidding ***");
            return auction;
        }

        public void NorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.EndOfBidding)
                return;

            if (biddingState.Fase != Fase.End && (biddingState.CurrentBid == Bid.PassBid || biddingState.CurrentBid < Bid.sixSpadeBid || !useSingleDummySolver))
                biddingState.CurrentBid = GetNorthBid(biddingState, auction, northHand);
            else
            {
                biddingState.EndOfBidding = true;
                // Try to guess contract by using single dummy solver
                biddingState.CurrentBid = useSingleDummySolver ? CalculateEndContract(auction, northHand, biddingState.CurrentBid) : Bid.PassBid;
            }
        }

        private Bid GetNorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.Fase != Fase.Shape && reverseDictionaries != null && !string.IsNullOrWhiteSpace(northHand))
            {
                var southInformation = biddingInformation.GetInformationFromAuction(auction, northHand);
                var trumpSuit = Util.GetTrumpSuitShape(northHand, southInformation.Shapes.First());

                if (biddingState.Fase == Fase.BidGame)
                    return GetGameBid(trumpSuit);

                if (!biddingState.RelayerHasSignedOff)
                {
                    if (trumpSuit == Suit.NoTrump)
                    {
                        if (TryGetNoTrumpBid(southInformation, out var noTrumpBid))
                            return noTrumpBid;
                    }
                    else
                    {
                        if (TryGetSuitBid(southInformation, trumpSuit, out var suitBid))
                            return suitBid;
                    }
                }

                if (biddingState.Fase == Fase.ScanningOther && useSingleDummySolver)
                    if (TryGetBidInScanningOther(southInformation, trumpSuit, out var bid))
                        return bid;
            }

            return GetRelayBid();

            Bid GetGameBid(Suit trumpSuit)
            {
                biddingState.Fase = Fase.End;
                var game = Bid.GetGameContractSafe(trumpSuit, biddingState.CurrentBid, true);
                if (game == Bid.PassBid)
                    biddingState.EndOfBidding = true;
                return game;
            }

            bool TryGetNoTrumpBid(SouthInformation southInformation, out Bid noTrumpBid)
            {
                var hcpNorth = Util.GetHcpCount(northHand);
                var controlBidCount = southInformation.ControlBidCount;
                if (systemParameters.hcpRelayerToSignOffInNT.TryGetValue(controlBidCount, out var hcps) && hcps.Contains(hcpNorth))
                {
                    noTrumpBid = biddingState.CurrentBid < Bid.threeNTBid ? Bid.threeNTBid : Bid.fourNTBid;
                    biddingState.UpdateBiddingStateSignOff(controlBidCount, noTrumpBid);
                    loggerBidding.Info($"Signed off with {noTrumpBid} because HCP {hcpNorth} was found in {string.Join(",", hcps)} for ControlBidCount:{controlBidCount}");
                    return true;
                }
                noTrumpBid = null;
                return false;
            }

            bool TryGetSuitBid(SouthInformation southInformation, Suit trumpSuit, out Bid suitBid)
            {
                suitBid = null;
                var controlBidCount = southInformation.ControlBidCount;
                var hcpNorth = Util.GetHcpCount(northHand);
                if (controlBidCount == 0 && hcpNorth <= systemParameters.requiredMaxHcpToBid4Diamond && biddingState.CurrentBid >= Bid.threeSpadeBid)
                {
                    loggerBidding.Info($"Signed off with {Bid.fourDiamondBid} because hcp {hcpNorth} was smaller or equal then {systemParameters.requiredMaxHcpToBid4Diamond} for ControlBidCount:{controlBidCount}");
                    biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid);
                    suitBid = Bid.fourDiamondBid;
                    return true;
                }
                if (controlBidCount == 1)
                {
                    var relayBidkind = GetRelayBidKindFunc(auction, northHand, southInformation);
                    switch (relayBidkind)
                    {
                        case RelayBidKind.Relay:
                            return false;
                        case RelayBidKind.fourDiamondEndSignal:
                            if (biddingState.CurrentBid < Bid.fourClubBid)
                            {
                                biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid);
                                suitBid = Bid.fourDiamondBid;
                                return true;
                            }
                            else return false;
                        case RelayBidKind.gameBid:
                            {
                                biddingState.Fase = Fase.End;
                                auction.responderHasSignedOff = true;
                                suitBid = Bid.GetGameContractSafe(trumpSuit, biddingState.CurrentBid, false);
                                if (suitBid == Bid.PassBid)
                                    biddingState.EndOfBidding = true;
                                return true;
                            }
                        default:
                            throw new ArgumentException(nameof(relayBidkind));
                    }
                }
                return false;
            }

            bool TryGetBidInScanningOther(SouthInformation southInformation, Suit trumpSuit, out Bid bid)
            {
                // TODO also call this function when some part of the queens is known. I.e. when control scanning has used zoom
                var confidenceTricks = GetConfidenceFromAuction(auction, northHand, southInformation);

                if (GetConfidenceToBidSlam(confidenceTricks) < optimizationParameters.requiredConfidenceToContinueRelaying)
                {
                    biddingState.Fase = Fase.End;
                    biddingInformation.constructedSouthhandOutcome = southInformation.SpecificControls.Count() == 1 ?
                        ConstructedSouthhandOutcome.SouthhandMatches : ConstructedSouthhandOutcome.MultipleMatchesFound;
                    auction.responderHasSignedOff = true;
                    bid = Bid.GetGameContractSafe(trumpSuit, biddingState.CurrentBid, false);
                    return true;
                }
                bid = null;
                return false;
            }

            Bid GetRelayBid()
            {
                if (biddingState.CurrentBid == Bid.threeSpadeBid && biddingState.Fase != Fase.Shape && reverseDictionaries != null)
                {
                    string shapeOrdered = new string(biddingInformation.shape.Value.shapes.First().ToCharArray().OrderByDescending(x => x).ToArray());
                    if (shapeOrdered != "7330")
                    {
                        biddingState.RelayBidIdLastFase++;
                        return Bid.fourClubBid;
                    }
                }
                return Bid.NextBid(biddingState.CurrentBid);
            }
        }

        public RelayBidKind GetRelayBidKindSolver(Auction auction, string northHand, SouthInformation southInformation)
        {
            var confidenceTricks = GetConfidenceFromAuction(auction, northHand, southInformation);
            var confidenceToBidSlam = GetConfidenceToBidSlam(confidenceTricks);
            var relayBidkind = systemParameters.requirementsForRelayBid.Where(x =>confidenceToBidSlam >= x.range.min && confidenceToBidSlam <= x.range.max).First().relayBidKind;
            loggerBidding.Info($"RelayBidkind:{relayBidkind} confidence in GetRelayBid:{JsonConvert.SerializeObject(confidenceTricks)}");
            return relayBidkind;
        }

        public RelayBidKind GetRelayBidKind(Auction auction, string northHand, SouthInformation southInformation)
        {
            var hcp = Util.GetHcpCount(northHand);
            loggerBidding.Info($"GetRelaybid no solver HCP:{hcp}");
            return hcp switch
            {
                var x when x < 19 => RelayBidKind.gameBid,
                var x when x >= 19 && x < 22 => RelayBidKind.fourDiamondEndSignal,
                _ => RelayBidKind.Relay,
            };
        }

        private Dictionary<int, double> GetConfidenceFromAuction(Auction auction, string northHand, SouthInformation southInformation)
        {
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, suit => auction.GetDeclarerOrNorth(suit));
            var confidenceTricks = GetConfidenceTricks(northHand, southInformation, declarers);
            loggerBidding.Info($"ConfidenceTricks: {JsonConvert.SerializeObject(confidenceTricks)}");

            return confidenceTricks;
        }

        private static Dictionary<int, double> GetConfidenceTricks(string northHand, SouthInformation southInformation, Dictionary<Suit, Player> declarers)
        {
            var tricksForBid = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            var nrOfHands = tricksForBid.Sum(x => x.Value);
            var groupedTricked = tricksForBid.GroupBy(x => x.Key.rank + 6);
            var confidenceTricks = groupedTricked.ToDictionary(bid => bid.Key, bid => (double)100 * bid.Select(x => x.Value).Sum() / nrOfHands);
            return confidenceTricks;
        }

        private static double GetConfidenceToBidSlam(Dictionary<int, double> confidenceTricks)
        {
            return (confidenceTricks.TryGetValue(12, out var smallSlamTricks) ? smallSlamTricks : 0.0) + (confidenceTricks.TryGetValue(13, out var grandSlamTricks) ? grandSlamTricks : 0.0);
        }

        private Bid CalculateEndContract(Auction auction, string northHand, Bid currentBid)
        {
            var southInformation = biddingInformation.GetInformationFromAuction(auction, northHand);
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, suit => auction.GetDeclarerOrNorth(suit));
            var tricksForBid = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            var possibleTricksForBid = tricksForBid.Where(bid => bid.Key >= currentBid);
            if (!possibleTricksForBid.Any())
                return Bid.PassBid;
            var maxBid = possibleTricksForBid.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            return maxBid == currentBid ? Bid.PassBid : maxBid;
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
                    Fase.Controls => reverseDictionaries == null ? zoomOffset : biddingInformation.shape.Value.zoomOffset,
                    Fase.ScanningOther => reverseDictionaries == null ? zoomOffset : biddingInformation.controlsScanning.Value.zoomOffset,
                    _ => 0,
                };
            // Check if controls and their positions are correctly evaluated.
            if (nextfase == Fase.ScanningOther && reverseDictionaries != null)
            {
                var controlsInSuit = Util.GetHandWithOnlyControlsAs4333(handsString, "AK");
                if (!biddingInformation.controlsScanning.Value.controls.Contains(controlsInSuit))
                    throw new InvalidOperationException($"Cannot find {controlsInSuit} in {string.Join('|', biddingInformation.controlsScanning.Value.controls)}");

            }
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, bidId, lzoomOffset);
            if (nextfase == Fase.BidGame)
                auction.responderHasSignedOff = true;
        }
    }
}
