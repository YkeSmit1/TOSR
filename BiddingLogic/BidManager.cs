using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Common;
using Solver;
using MoreLinq;
using System.ComponentModel;

namespace BiddingLogic
{
    using RelayBidKindFunc = Func<Auction, string, SouthInformation, BidManager.RelayBidKind>;

    public enum BidPosibilities
    {
        CannotBid,
        CanInvestigate,
        CannotInvestigate
    }

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
        private readonly bool useSingleDummySolver = false;
        private readonly bool useSingleDummySolverDuringRelaying = false;

        private readonly RelayBidKindFunc GetRelayBidKindFunc = null;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger loggerBidding = LogManager.GetLogger("bidding");

        public BiddingInformation biddingInformation;
        private Dictionary<Bid, int> occurrencesForBids;

        // Constructor used for test
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, RelayBidKindFunc getRelayBidKindFunc) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            GetRelayBidKindFunc = getRelayBidKindFunc;
        }

        // Standard constructor
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, bool useSingleDummySolver) :
            this(bidGenerator, fasesWithOffset, reverseDictionaries, useSingleDummySolver, useSingleDummySolver)
        {
        }

        // Standard constructor
        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, bool useSingleDummySolver, bool useSingleDummySolverDuringRelaying) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            this.useSingleDummySolver = useSingleDummySolver;
            this.useSingleDummySolverDuringRelaying = useSingleDummySolverDuringRelaying;
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
            occurrencesForBids = null;

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
                        throw new InvalidEnumArgumentException(nameof(currentPlayer));
                }

                currentPlayer = currentPlayer == Player.South ? Player.West : currentPlayer + 1;
                if (auction.bids.Count > 30)
                    throw new InvalidOperationException("Bidding is stuck in a loop");
            }
            while (!auction.IsEndOfBidding());

            logger.Debug($"Ending GetAuction for hand : {southHand}");
            loggerBidding.Info("Auction:");
            loggerBidding.Info($"{auction.GetPrettyAuction("\n")}");
            loggerBidding.Info("*** End bidding ***");
            return auction;
        }

        public void NorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (auction.IsEndOfBidding())
                return;

            if (biddingState.Fase != Fase.End && (biddingState.CurrentBid == Bid.PassBid || biddingState.CurrentBid < Bid.sixSpadeBid || !useSingleDummySolver))
                biddingState.CurrentBid = GetNorthBid(biddingState, auction, northHand);
            else
            {
                biddingState.Fase = Fase.End;
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

                if (CanSignOff(trumpSuit))
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
                else
                {
                    if (useSingleDummySolverDuringRelaying)
                        if (TryGetEndContract(southInformation, out var bid))
                            return bid;
                }
            }

            return GetRelayBid();

            Bid GetGameBid(Suit trumpSuit)
            {
                biddingState.Fase = Fase.End;
                return Bid.GetGameContractSafe(trumpSuit, biddingState.CurrentBid, true);
            }

            bool CanSignOff(Suit trumpSuit)
            {
                if (trumpSuit != Suit.NoTrump)
                    return biddingState.CurrentBid < Bid.fourClubBid;

                // NoTrump
                if (biddingState.CurrentBid >= Bid.fourNTBid)
                    return false;
                var shapeOrdered = new string(biddingInformation.shape.Value.shapes.First().ToCharArray().OrderByDescending(x => x).ToArray());
                if (auction.GetBids(Player.North).Any(bid => bid == Bid.threeNTBid) && shapeOrdered != "7330")
                    return false;
                return true;
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
                            biddingState.UpdateBiddingStateSignOff(controlBidCount, Bid.fourDiamondBid);
                            suitBid = Bid.fourDiamondBid;
                            return true;
                        case RelayBidKind.gameBid:
                            biddingState.Fase = Fase.End;
                            auction.responderHasSignedOff = true;
                            suitBid = Bid.GetGameContractSafe(trumpSuit, biddingState.CurrentBid, false);
                            return true;
                        default:
                            throw new InvalidEnumArgumentException(nameof(relayBidkind));
                    }
                }
                return false;
            }

            bool TryGetEndContract(SouthInformation southInformation, out Bid bid)
            {
                loggerBidding.Info($"TryGetEndContract. Current contract:{biddingState.CurrentBid}");

                var possibleContracts = GetPossibleContractsFromAuction(auction, northHand, southInformation, biddingState);
                bid = GetEndContract(possibleContracts, biddingState.CurrentBid);
                if (bid != null)
                {
                    if (biddingState.Fase == Fase.ScanningOther)
                        biddingInformation.constructedSouthhandOutcome = southInformation.SpecificControls.Count() == 1 ?
                            ConstructedSouthhandOutcome.SouthhandMatches : ConstructedSouthhandOutcome.MultipleMatchesFound;
                    biddingState.Fase = Fase.End;
                    auction.responderHasSignedOff = true;
                }

                bid = biddingState.CurrentBid == bid ? Bid.PassBid : bid;

                return bid != null;
            }

            Bid GetRelayBid()
            {
                if (biddingState.CurrentBid == Bid.threeSpadeBid && biddingState.Fase != Fase.Shape && reverseDictionaries != null)
                {
                    var shapeOrdered = new string(biddingInformation.shape.Value.shapes.First().ToCharArray().OrderByDescending(x => x).ToArray());
                    if (shapeOrdered != "7330")
                    {
                        biddingState.RelayBidIdLastFase++;
                        return Bid.fourClubBid;
                    }
                }
                return Bid.NextBid(biddingState.CurrentBid);
            }
        }

        public static Bid GetEndContract(Dictionary<Bid, int> possibleContracts, Bid currentBid)
        {
            Dictionary<Bid, (int occurrences, BidPosibilities posibility)> enrichedContracts = possibleContracts.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value, GetBidPosibility(kvp.Key, currentBid)));
            GroupGameContracts();
            var reachableContracts = enrichedContracts.Where(y => y.Value.posibility != BidPosibilities.CannotBid).ToDictionary(x => x.Key, y => y.Value);

            var bid = reachableContracts.Count switch
            {
                0 => Bid.PassBid,
                1 => reachableContracts.Single().Key,
                _ => reachableContracts.Count(y => y.Value.posibility == BidPosibilities.CanInvestigate) <= 1
                        ? reachableContracts.MaxBy(y => y.Value.occurrences).First().Key
                        : null,
            };

            var bidString = bid == null ? "Relay a bit more" : $"Bid: {bid}";
            loggerBidding.Info($"{reachableContracts.Count} contracts are possible. " +
                $"Reachable contracts: {string.Join(';', reachableContracts.Select(y => y.Key))}. " +
                $"Investigatable contracts: {string.Join(';', reachableContracts.Where(y => y.Value.posibility == BidPosibilities.CanInvestigate).Select(y => y.Key))} {bidString}");

            return bid;

            void GroupGameContracts()
            {
                var bestGames = enrichedContracts.Where(x => x.Key.rank < 6 && x.Value.posibility != BidPosibilities.CannotBid).MinBy(x => x.Key);
                if (bestGames.Any())
                {
                    var bestGame = bestGames.Single().Key;
                    enrichedContracts = enrichedContracts.GroupBy(x => x.Key.rank < 6 ? bestGame : x.Key)
                        .ToDictionary(g => g.Key, g => (g.Sum(v => v.Value.occurrences),
                            g.Key == bestGame ? g.Any(v => v.Value.posibility == BidPosibilities.CanInvestigate) ? BidPosibilities.CanInvestigate :
                                BidPosibilities.CannotInvestigate : g.Single().Value.posibility));
                }
            }
        }

        public RelayBidKind GetRelayBidKindSolver(Auction auction, string northHand, SouthInformation southInformation)
        {
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, suit => auction.GetDeclarerOrNorth(suit));
            var confidenceTricks = GetConfidenceTricks(northHand, southInformation, declarers);
            var confidenceToBidSlam = (confidenceTricks.TryGetValue(12, out var smallSlamTricks) ? smallSlamTricks : 0.0) + (confidenceTricks.TryGetValue(13, out var grandSlamTricks) ? grandSlamTricks : 0.0);
            var relayBidkind = systemParameters.requirementsForRelayBid.Where(x => confidenceToBidSlam >= x.range.min && confidenceToBidSlam <= x.range.max).First().relayBidKind;
            loggerBidding.Info($"RelayBidkind:{relayBidkind} confidence in GetRelayBid:{JsonConvert.SerializeObject(confidenceTricks)}");
            return relayBidkind;
        }

        public static RelayBidKind GetRelayBidKind(Auction auction, string northHand, SouthInformation southInformation)
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

        private Dictionary<Bid, int> GetPossibleContractsFromAuction(Auction auction, string northHand,
            SouthInformation southInformation, BiddingState biddingState)
        {
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, suit => auction.GetDeclarerOrNorth(suit));
            bool canReuseSolverOutput = biddingState.Fase == Fase.ScanningControls && southInformation.ControlsScanningBidCount > 0;
            if (!canReuseSolverOutput || occurrencesForBids == null)
                occurrencesForBids = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            loggerBidding.Info($"Occurrences by bid in GetPossibleContractsFromAuction: {JsonConvert.SerializeObject(occurrencesForBids)}");

            return occurrencesForBids;
        }

        private static BidPosibilities GetBidPosibility(Bid contract, Bid currentBid)
        {
            if (contract < currentBid || contract == currentBid + 1)
                return BidPosibilities.CannotBid;
            if (contract == currentBid || contract == currentBid + 3)
                return BidPosibilities.CannotInvestigate;
            return BidPosibilities.CanInvestigate;
        }

        private Dictionary<int, double> GetConfidenceTricks(string northHand, SouthInformation southInformation, Dictionary<Suit, Player> declarers)
        {
            occurrencesForBids = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            var nrOfHands = occurrencesForBids.Sum(x => x.Value);
            var groupedTricked = occurrencesForBids.GroupBy(x => x.Key.rank + 6);
            var confidenceTricks = groupedTricked.ToDictionary(bid => bid.Key, bid => (double)100 * bid.Select(x => x.Value).Sum() / nrOfHands);
            return confidenceTricks;
        }

        private Bid CalculateEndContract(Auction auction, string northHand, Bid currentBid)
        {
            var southInformation = biddingInformation.GetInformationFromAuction(auction, northHand);
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, suit => auction.GetDeclarerOrNorth(suit));
            var tricksForBid = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            var possibleTricksForBid = tricksForBid.Where(bid => bid.Key >= currentBid);
            if (!possibleTricksForBid.Any())
                return Bid.PassBid;
            var maxBid = possibleTricksForBid.MaxBy(x => x.Value).First().Key;
            return maxBid == currentBid ? Bid.PassBid : maxBid;
        }

        public void SouthBid(BiddingState biddingState, Auction auction, string handsString)
        {
            if (biddingState.Fase == Fase.End)
            {
                auction.AddBid(Bid.PassBid);
                biddingState.CurrentBid = Bid.PassBid;
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
