using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Common;
using Solver;
using System.ComponentModel;
using Common.Tosr;

namespace BiddingLogic
{
    using RelayBidKindFunc = Func<Auction, string, SouthInformation, BidManager.RelayBidKind>;

    public enum BidPossibilities
    {
        CannotBid,
        CanInvestigate,
        CannotInvestigate
    }

    public enum ConstructedSouthHandOutcome
    {
        NotSet,
        AuctionNotFoundInControls,
        NoMatchFound,
        SouthHandMatches,
        MultipleMatchesFound,
        IncorrectSouthHand,
        NoMatchFoundNoQueens,
    }

    public class BidManager
    {
        public enum RelayBidKind
        {
            // Don't change this. It will break the unit test
            Relay,
            // ReSharper disable once InconsistentNaming
            fourDiamondEndSignal,
            // ReSharper disable once InconsistentNaming
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

        private class RequirementsForRelayBidConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return JArray.Load(reader).Select(entry => (((double)entry[0]?[0], (double)entry[0]?[1]),
                    Enum.Parse<RelayBidKind>(entry[1].ToString()))).ToList();
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
        private readonly Dictionary<Phase, bool> fasesWithOffset;
        private readonly ReverseDictionaries reverseDictionaries;
        private readonly bool useSingleDummySolver;
        private readonly bool useSingleDummySolverDuringRelaying;

        private readonly RelayBidKindFunc getRelayBidKindFunc;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger LoggerBidding = LogManager.GetLogger("bidding");

        public BiddingInformation biddingInformation;
        private Dictionary<Bid, int> occurrencesForBids;
        public ConstructedSouthHandOutcome constructedSouthHandOutcome = ConstructedSouthHandOutcome.NotSet;

        public BiddingState BiddingState { get; private set; }

        // Constructor used for test
        public BidManager(IBidGenerator bidGenerator, Dictionary<Phase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, RelayBidKindFunc getRelayBidKindFunc) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            this.getRelayBidKindFunc = getRelayBidKindFunc;
        }

        // Standard constructor
        public BidManager(IBidGenerator bidGenerator, Dictionary<Phase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, bool useSingleDummySolver) :
            this(bidGenerator, fasesWithOffset, reverseDictionaries, useSingleDummySolver, useSingleDummySolver)
        {
        }

        // Standard constructor
        public BidManager(IBidGenerator bidGenerator, Dictionary<Phase, bool> fasesWithOffset, ReverseDictionaries reverseDictionaries, bool useSingleDummySolver, bool useSingleDummySolverDuringRelaying) :
            this(bidGenerator, fasesWithOffset)
        {
            this.reverseDictionaries = reverseDictionaries;
            this.useSingleDummySolver = useSingleDummySolver;
            this.useSingleDummySolverDuringRelaying = useSingleDummySolverDuringRelaying;
            if (useSingleDummySolver)
                getRelayBidKindFunc = GetRelayBidKindSolver;
        }

        // Constructor used for generate reverse dictionaries
        public BidManager(IBidGenerator bidGenerator, Dictionary<Phase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;
            getRelayBidKindFunc = GetRelayBidKind;
            BiddingState = new BiddingState(fasesWithOffset);
        }

        public void Init(Auction auction)
        {
            BiddingState = new BiddingState(fasesWithOffset);
            biddingInformation = new BiddingInformation(reverseDictionaries, auction, BiddingState);
        }

        public Auction GetAuction(string northHand, string southHand)
        {
            Logger.Debug($"Starting GetAuction for hand : {southHand}");
            LoggerBidding.Info("");
            LoggerBidding.Info("*** Start bidding ***");
            LoggerBidding.Info($"North:{northHand} South:{southHand}");

            var auction = new Auction();
            var currentPlayer = Player.West;
            occurrencesForBids = null;
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
                        NorthBid(auction, northHand);
                        auction.AddBid(BiddingState.CurrentBid);
                        break;
                    case Player.South:
                        SouthBid(auction, southHand);
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(currentPlayer));
                }

                currentPlayer = currentPlayer == Player.South ? Player.West : currentPlayer + 1;
                if (auction.Bids.Count > 30)
                    throw new InvalidOperationException("Bidding is stuck in a loop");
            }
            while (!auction.IsEndOfBidding());

            Logger.Debug($"Ending GetAuction for hand : {southHand}");
            LoggerBidding.Info("Auction:");
            LoggerBidding.Info($"{auction.GetPrettyAuction("\n")}");
            LoggerBidding.Info("*** End bidding ***");
            return auction;
        }

        public void NorthBid(Auction auction, string northHand)
        {
            if (auction.IsEndOfBidding())
                return;

            if (BiddingState.Phase != Phase.End && (BiddingState.CurrentBid == Bid.PassBid || BiddingState.CurrentBid < Bids.SixSpadeBid || !useSingleDummySolver))
                BiddingState.CurrentBid = GetNorthBid(BiddingState, auction, northHand);
            else
            {
                BiddingState.Phase = Phase.End;
                // Try to guess contract by using single dummy solver
                BiddingState.CurrentBid = useSingleDummySolver ? CalculateEndContract(auction, northHand, BiddingState.CurrentBid) : Bid.PassBid;
            }
        }

        private Bid GetNorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.Phase != Phase.Shape && reverseDictionaries != null && !string.IsNullOrWhiteSpace(northHand))
            {
                var southInformation = biddingInformation.GetInformationFromAuction(auction, northHand, biddingState);
                var trumpSuit = Util.GetTrumpSuitShape(northHand, southInformation.Shapes.First());

                if (biddingState.Phase == Phase.BidGame)
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
                biddingState.Phase = Phase.End;
                return Bid.GetGameContractSafe(trumpSuit, biddingState.CurrentBid, true);
            }

            bool CanSignOff(Suit trumpSuit)
            {
                if (trumpSuit != Suit.NoTrump)
                    return biddingState.CurrentBid < Bids.FourClubBid;

                // NoTrump
                if (biddingState.CurrentBid >= Bids.FourNTBid)
                    return false;
                var shapeOrdered = new string(biddingInformation.Shape.Value.shapes.First().ToCharArray().OrderByDescending(x => x).ToArray());
                if (auction.GetBids(Player.North).Any(bid => bid == Bids.ThreeNTBid) && shapeOrdered != "7330")
                    return false;
                return true;
            }

            bool TryGetNoTrumpBid(SouthInformation southInformation, out Bid noTrumpBid)
            {
                var hcpNorth = Util.GetHcpCount(northHand);
                var controlBidCount = southInformation.ControlBidCount;
                if (systemParameters.hcpRelayerToSignOffInNT.TryGetValue(controlBidCount, out var hcps) && hcps.Contains(hcpNorth))
                {
                    noTrumpBid = biddingState.CurrentBid < Bids.ThreeNTBid ? Bids.ThreeNTBid : Bids.FourNTBid;
                    biddingState.UpdateBiddingStateSignOff(controlBidCount, noTrumpBid);
                    LoggerBidding.Info($"Signed off with {noTrumpBid} because HCP {hcpNorth} was found in {string.Join(",", hcps)} for ControlBidCount:{controlBidCount}");
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
                if (controlBidCount == 0 && hcpNorth <= systemParameters.requiredMaxHcpToBid4Diamond && biddingState.CurrentBid >= Bids.ThreeSpadeBid)
                {
                    LoggerBidding.Info($"Signed off with {Bids.FourDiamondBid} because hcp {hcpNorth} was smaller or equal then {systemParameters.requiredMaxHcpToBid4Diamond} for ControlBidCount:{controlBidCount}");
                    biddingState.UpdateBiddingStateSignOff(controlBidCount, Bids.FourDiamondBid);
                    suitBid = Bids.FourDiamondBid;
                    return true;
                }
                if (controlBidCount == 1)
                {
                    var relayBidKind = getRelayBidKindFunc(auction, northHand, southInformation);
                    switch (relayBidKind)
                    {
                        case RelayBidKind.Relay:
                            return false;
                        case RelayBidKind.fourDiamondEndSignal:
                            biddingState.UpdateBiddingStateSignOff(controlBidCount, Bids.FourDiamondBid);
                            suitBid = Bids.FourDiamondBid;
                            return true;
                        case RelayBidKind.gameBid:
                            biddingState.Phase = Phase.End;
                            auction.responderHasSignedOff = true;
                            suitBid = Bid.GetGameContractSafe(trumpSuit, biddingState.CurrentBid, false);
                            return true;
                        default:
                            throw new InvalidEnumArgumentException(nameof(relayBidKind));
                    }
                }
                return false;
            }

            bool TryGetEndContract(SouthInformation southInformation, out Bid bid)
            {
                LoggerBidding.Info($"TryGetEndContract. Current contract:{biddingState.CurrentBid}");

                var possibleContracts = GetPossibleContractsFromAuction(auction, northHand, southInformation, biddingState);
                bid = GetEndContract(possibleContracts, biddingState.CurrentBid);
                if (bid != null)
                {
                    if (biddingState.Phase == Phase.ScanningOther)
                        constructedSouthHandOutcome = southInformation.SpecificControls.Count() == 1 ?
                            ConstructedSouthHandOutcome.SouthHandMatches : ConstructedSouthHandOutcome.MultipleMatchesFound;
                    biddingState.Phase = Phase.End;
                    auction.responderHasSignedOff = true;
                }

                bid = biddingState.CurrentBid == bid ? Bid.PassBid : bid;

                return bid != null;
            }

            Bid GetRelayBid()
            {
                if (biddingState.CurrentBid == Bids.ThreeSpadeBid && biddingState.Phase != Phase.Shape && reverseDictionaries != null)
                {
                    var shapeOrdered = new string(biddingInformation.Shape.Value.shapes.First().ToCharArray().OrderByDescending(x => x).ToArray());
                    if (shapeOrdered != "7330")
                    {
                        biddingState.RelayBidIdLastPhase++;
                        return Bids.FourClubBid;
                    }
                }
                return Bid.NextBid(biddingState.CurrentBid);
            }
        }

        public static Bid GetEndContract(Dictionary<Bid, int> possibleContracts, Bid currentBid)
        {
            Dictionary<Bid, (int occurrences, BidPossibilities posibility)> enrichedContracts = possibleContracts.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value, GetBidPossibility(kvp.Key, currentBid)));
            GroupGameContracts();
            var reachableContracts = enrichedContracts.Where(y => y.Value.posibility != BidPossibilities.CannotBid).ToDictionary(x => x.Key, y => y.Value);

            var bid = reachableContracts.Count switch
            {
                0 => Bid.PassBid,
                1 => reachableContracts.Single().Key,
                _ => reachableContracts.Count(y => y.Value.posibility == BidPossibilities.CanInvestigate) <= 1
                        ? reachableContracts.MaxBy(y => y.Value.occurrences).Key
                        : null,
            };

            var bidString = bid == null ? "Relay a bit more" : $"Bid: {bid}";
            LoggerBidding.Info($"{reachableContracts.Count} contracts are possible. " +
                $"Reachable contracts: {string.Join(';', reachableContracts.Select(y => y.Key))}. " +
                $"Investigatable contracts: {string.Join(';', reachableContracts.Where(y => y.Value.posibility == BidPossibilities.CanInvestigate).Select(y => y.Key))} {bidString}");
            LoggerBidding.Info("*************************");

            return bid;

            void GroupGameContracts()
            {
                var pairs = enrichedContracts.Where(x => x.Key.Rank < 6 && x.Value.posibility != BidPossibilities.CannotBid).ToList();
                if (!pairs.Any())
                    return;
                enrichedContracts = enrichedContracts.GroupBy(x => x.Key.Rank < 6 ? pairs.MinBy(x1 => x1.Key).Key : x.Key)
                    .ToDictionary(g => g.Key, g => (g.Sum(v => v.Value.occurrences),
                        g.Key == pairs.MinBy(x => x.Key).Key
                            ? g.Any(v => v.Value.posibility == BidPossibilities.CanInvestigate) ? BidPossibilities.CanInvestigate : BidPossibilities.CannotInvestigate
                            : g.Single().Value.posibility));
            }
        }

        private RelayBidKind GetRelayBidKindSolver(Auction auction, string northHand, SouthInformation southInformation)
        {
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, auction.GetDeclarerOrNorth);
            var confidenceTricks = GetConfidenceTricks(northHand, southInformation, declarers);
            var confidenceToBidSlam = (confidenceTricks.TryGetValue(12, out var smallSlamTricks) ? smallSlamTricks : 0.0) + (confidenceTricks.TryGetValue(13, out var grandSlamTricks) ? grandSlamTricks : 0.0);
            var relayBidKind = systemParameters.requirementsForRelayBid.First(x => confidenceToBidSlam >= x.range.min && confidenceToBidSlam <= x.range.max).relayBidKind;
            LoggerBidding.Info($"RelayBidkind:{relayBidKind} confidence in GetRelayBid:{JsonConvert.SerializeObject(confidenceTricks)}");
            return relayBidKind;
        }

        private static RelayBidKind GetRelayBidKind(Auction auction, string northHand, SouthInformation southInformation)
        {
            var hcp = Util.GetHcpCount(northHand);
            LoggerBidding.Info($"GetRelaybid no solver HCP:{hcp}");
            return hcp switch
            {
                < 19 => RelayBidKind.gameBid,
                < 22 and >= 19 => RelayBidKind.fourDiamondEndSignal,
                _ => RelayBidKind.Relay,
            };
        }

        private Dictionary<Bid, int> GetPossibleContractsFromAuction(Auction auction, string northHand,
            SouthInformation southInformation, BiddingState biddingState)
        {
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, auction.GetDeclarerOrNorth);
            bool canReuseSolverOutput = biddingState.Phase == Phase.ScanningControls && southInformation.ControlsScanningBidCount > 0;
            if (!canReuseSolverOutput || occurrencesForBids == null)
                occurrencesForBids = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            LoggerBidding.Info($"Occurrences by bid in GetPossibleContractsFromAuction: {JsonConvert.SerializeObject(occurrencesForBids)}");

            return occurrencesForBids;
        }

        private static BidPossibilities GetBidPossibility(Bid contract, Bid currentBid)
        {
            if (contract < currentBid || contract == currentBid + 1)
                return BidPossibilities.CannotBid;
            if (contract == currentBid || contract == currentBid + 3)
                return BidPossibilities.CannotInvestigate;
            return BidPossibilities.CanInvestigate;
        }

        private Dictionary<int, double> GetConfidenceTricks(string northHand, SouthInformation southInformation, Dictionary<Suit, Player> declarers)
        {
            occurrencesForBids = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            var nrOfHands = occurrencesForBids.Sum(x => x.Value);
            var groupedTricked = occurrencesForBids.GroupBy(x => x.Key.Rank + 6);
            var confidenceTricks = groupedTricked.ToDictionary(bid => bid.Key, bid => (double)100 * bid.Select(x => x.Value).Sum() / nrOfHands);
            return confidenceTricks;
        }

        private Bid CalculateEndContract(Auction auction, string northHand, Bid currentBid)
        {
            var southInformation = biddingInformation.GetInformationFromAuction(auction, northHand, BiddingState);
            var declarers = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToDictionary(suit => suit, auction.GetDeclarerOrNorth);
            var tricksForBid = SingleDummySolver.SolveSingleDummy(northHand, southInformation, optimizationParameters.numberOfHandsForSolver, declarers);
            var possibleTricksForBid = tricksForBid.Where(bid => bid.Key >= currentBid).ToList();
            if (!possibleTricksForBid.Any())
                return Bid.PassBid;
            var maxBid = possibleTricksForBid.MaxBy(x => x.Value).Key;
            return maxBid == currentBid ? Bid.PassBid : maxBid;
        }

        public void SouthBid(Auction auction, string handsString)
        {
            if (BiddingState.Phase == Phase.End)
            {
                auction.AddBid(Bid.PassBid);
                BiddingState.CurrentBid = Bid.PassBid;
                return;
            }
            var (bidIdFromRule, nextPhase, description, zoomOffset) = bidGenerator.GetBid(BiddingState, handsString);
            var bidId = BiddingState.CalculateBid(bidIdFromRule, description, zoomOffset != 0);
            auction.AddBid(BiddingState.CurrentBid);

            var lZoomOffset = nextPhase switch
            {
                Phase.Controls => reverseDictionaries == null ? zoomOffset : biddingInformation.Shape.Value.zoomOffset,
                Phase.ScanningOther => reverseDictionaries == null ? zoomOffset : biddingInformation.ControlsScanning.Value.zoomOffset,
                _ => 0,
            };
            // Check if controls and their positions are correctly evaluated.
            if (nextPhase == Phase.ScanningOther && reverseDictionaries != null)
            {
                var controlsInSuit = UtilTosr.GetHandWithOnlyControlsAs4333(handsString, "AK");
                if (!biddingInformation.ControlsScanning.Value.controls.Contains(controlsInSuit))
                    throw new InvalidOperationException($"Cannot find {controlsInSuit} in {string.Join('|', biddingInformation.ControlsScanning.Value.controls)}");

            }
            BiddingState.UpdateBiddingState(bidIdFromRule, nextPhase, bidId, lZoomOffset);
            if (nextPhase == Phase.BidGame)
                auction.responderHasSignedOff = true;
        }

        /// <summary>
        /// Construct south hand to compare with the actual south hand
        /// </summary>
        public string ConstructSouthHandSafe(Dictionary<Player, string> hand, Auction auction)
        {
            var northHand = hand[Player.North];
            var southHand = hand[Player.South];

            try
            {
                var constructedSouthHand = biddingInformation.ConstructSouthHand(northHand).ToList();
                if (constructedSouthHand.Count > 1)
                {
                    constructedSouthHandOutcome = ConstructedSouthHandOutcome.MultipleMatchesFound;
                    return $"Multiple matches found. Matches: {string.Join('|', constructedSouthHand)}. NorthHand: {northHand}. SouthHand: {southHand}";
                }

                if (constructedSouthHand.First() == UtilTosr.HandWithX(southHand))
                {
                    constructedSouthHandOutcome = ConstructedSouthHandOutcome.SouthHandMatches;
                    var queens = biddingInformation.GetQueensFromAuction(auction, BiddingState);
                    if (!BiddingInformation.CheckQueens(queens, southHand))
                        return $"Match is found but queens are wrong : Expected queens: {queens}. SouthHand: {southHand}";

                    return $"Match is found: {constructedSouthHand.First()}. NorthHand: {northHand}. SouthHand: {southHand}";
                }
                else
                {
                    constructedSouthHandOutcome = ConstructedSouthHandOutcome.IncorrectSouthHand;
                    return $"SouthHand is not equal to expected. Expected: {constructedSouthHand.First()}. NorthHand: {northHand}. SouthHand: {southHand}";
                }
            }
            catch (Exception e)
            {
                constructedSouthHandOutcome = !biddingInformation.ControlsScanning.IsValueCreated ? ConstructedSouthHandOutcome.AuctionNotFoundInControls : ConstructedSouthHandOutcome.NoMatchFound;
                return $"{e.Message} SouthHand: {southHand}. Projected AKQ controls as 4333:{UtilTosr.GetHandWithOnlyControlsAs4333(southHand, "AKQ")}. ";
            }
        }

    }
}
