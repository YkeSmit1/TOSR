using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Common;
using Common.Tosr;
using Newtonsoft.Json;
using NLog;
using Solver;

namespace BiddingLogic
{
    using PhaseDictionary = Dictionary<Phase, Dictionary<string, List<int>>>;

    public class BiddingInformation
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger LoggerBidding = LogManager.GetLogger("bidding");
        public Lazy<(List<string> shapes, int zoomOffset)> Shape { get; }
        public Lazy<(List<string> controls, int zoomOffset)> ControlsScanning { get; }
        private readonly ReverseDictionaries reverseDictionaries;

        public BiddingInformation(ReverseDictionaries reverseDictionaries, Auction auction, BiddingState biddingState)
        {
            Shape = new Lazy<(List<string> shapes, int zoomOffset)>(() =>
            {
                var bidsForPhase = biddingState.GetBids(Phase.Shape);
                return GetInformationFromBids(reverseDictionaries.ShapeAuctions, bidsForPhase);
            });
            ControlsScanning = new Lazy<(List<string> controls, int zoomOffset)>(() =>
            {
                var dictionary = reverseDictionaries.GetControlScanningDictionary(Shape.Value.shapes.Last());
                var bidsForPhase = GetAuctionForPhaseWithOffset(auction, Shape.Value.zoomOffset, biddingState, Phase.Controls, Phase.ScanningControls).ToList();
                return GetInformationFromBids(dictionary, bidsForPhase);
            });
            this.reverseDictionaries = reverseDictionaries;
        }

        public SouthInformation GetInformationFromAuction(Auction auction, string northHand, BiddingState biddingState)
        {
            var southInformation = new SouthInformation
            {
                Shapes = Shape.Value.shapes,
                Hcp = GetHcpFromAuction(reverseDictionaries.SignOffPhasesAuctions, biddingState),
                ControlBidCount = biddingState.GetBids(Phase.Controls).Count(),
                ControlsScanningBidCount = biddingState.GetBids(Phase.ScanningControls).Count()
            };

            var controls = GetAuctionForPhaseWithOffset(auction, Shape.Value.zoomOffset, biddingState, Phase.Controls).ToList();
            if (controls.Count > 0)
            {
                var possibleControls = reverseDictionaries.ControlsOnlyAuctions[string.Join("", controls)];
                southInformation.Controls = new MinMax(possibleControls.First(), possibleControls.Last());
                // Special case if relay-er is able to figure out the position of controls
                if (southInformation.ControlsScanningBidCount == 0)
                {
                    var matches = GetMatchesWithNorthHand(Shape.Value.shapes, possibleControls.First(), northHand).ToList();
                    if (matches.Count == 1)
                        southInformation.SpecificControls = matches.Select(match => match.Split(',').Select(x => Regex.Replace(x, "[^AK]", "")).ToArray());
                }
            }


            if (ControlsScanning.IsValueCreated)
            {
                var matches = GetMatchesWithNorthHand(Shape.Value.shapes, ControlsScanning.Value.controls, northHand).ToList();
                if (!matches.Any())
                    throw new InvalidOperationException($"No matches found. NorthHand:{northHand}");

                southInformation.SpecificControls = matches.Select(match => match.Split(',').Select(x => Regex.Replace(x, "[^AK]", "")).ToArray());
                southInformation.Queens = GetQueensFromAuction(auction, biddingState);
            }

            LoggerBidding.Info($"SouthInformation. {JsonConvert.SerializeObject(southInformation)}");
            return southInformation;
        }

        /// <summary>
        /// Construct south hand to use for single dummy analysis
        /// Can throw
        /// </summary>
        public IEnumerable<string> ConstructSouthHand(string northHand)
        {
            Logger.Debug($"Starting ConstructSouthHand for northhand : {northHand}");
            var possibleControls = ControlsScanning.Value.controls;
            var matches = GetMatchesWithNorthHand(Shape.Value.shapes, possibleControls, northHand);
            if (!matches.Any())
                throw new InvalidOperationException($"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {northHand}.");

            Logger.Debug($"Ending ConstructSouthHand. southhand : {string.Join("|", matches)}");
            return matches;
        }

        /// <summary>
        /// Lookup in the dictionary. If not found, it tries to find an auction when the last bid was done with zoom
        /// </summary>
        public static (List<string> information, int zoomOffset) GetInformationFromBids(Dictionary<string, (List<string> pattern, bool zoom)> dictionary, IEnumerable<Bid> bidsForPhase)
        {
            var bids = bidsForPhase.ToList();
            if (dictionary.TryGetValue(string.Join("", bids), out var information))
                return (information.pattern, information.zoom ? 2 : 0);

            var lastBid = bids.Last();
            var firstBid = bids.First();
            var allButLastBid = bids.Take(bids.Count - 1).ToList();
            for (var bid = lastBid - 1; bid >= firstBid; bid--)
            {
                var allBidsNew = allButLastBid.Append(bid);
                var bidsStr = string.Join("", allBidsNew);
                // Add two because auction is two bids lower if zoom applies
                if (dictionary.TryGetValue(bidsStr, out var informationZoom) && informationZoom.zoom)
                    return (informationZoom.pattern, lastBid - bid + 2);
            }

            throw new InvalidOperationException($"{ string.Join("", bids) } not found in dictionary");
        }

        /// <summary>
        /// Extracts the control and scanning part of the auction and applies an offset of offsetBid
        /// </summary>
        /// <param name="auction">Generated auction </param>
        /// <param name="zoomOffset"></param>
        /// <param name="biddingState"></param>
        /// <param name="fases">Which fases to get the offset from</param>
        /// <returns></returns>
        public static IEnumerable<Bid> GetAuctionForPhaseWithOffset(Auction auction, int zoomOffset, BiddingState biddingState, params Phase[] fases)
        {
            var lastBidShape = biddingState.GetBids(Phase.Shape).Last();
            var bidsForPhases = biddingState.GetBids(fases);

            return GetBidsForPhaseWithOffset(bidsForPhases, Bids.ThreeDiamondBid, lastBidShape, zoomOffset, GetOffsetRelayBid);

            int GetOffsetRelayBid(Bid currentBid)
            {
                var previousBidSouth = auction.GetRelativeBid(currentBid, -1, Player.South);
                if (previousBidSouth == default)
                    return 0;
                var previousBidNorth = auction.GetRelativeBid(currentBid, 0, Player.North);

                // In case of 3NT pull without ask, use the bid before. The sign-off bid only shows points
                return previousBidSouth == biddingState.GetBids(Phase.Pull3NTNoAsk).SingleOrDefault()
                    ? previousBidNorth - auction.GetRelativeBid(currentBid, -2, Player.South) - 1
                    : previousBidNorth - previousBidSouth - (currentBid == biddingState.GetPullBid() && BiddingState.SignOffPhasesFor4Di.Contains(biddingState.GetPullPhase()) ? 0 : 1);
            }
        }

        /// <summary>
        /// Returns a string of 4 characters when each character is "Y" "N" "X". "X" means not yet known.
        /// </summary>
        public string GetQueensFromAuction(Auction auction, BiddingState biddingState)
        {
            // Because the last shape is the one with the highest numeric value generated in ReverseDictionaries
            var shapeStr = Shape.Value.shapes.Last();
            var zoomOffset = ControlsScanning.Value.zoomOffset;
            var lastBidScanningControl = biddingState.GetBids(Phase.ScanningControls).Last();
            var queensBids = biddingState.GetBids(Phase.ScanningOther);
            var offsetBid = ReverseDictionaries.GetOffsetBidForQueens(shapeStr);

            var queensBidsResult = GetBidsForPhaseWithOffset(queensBids, offsetBid, lastBidScanningControl, zoomOffset, GetOffsetRelayBid).ToList();
            if (!queensBidsResult.Any())
                return null;
            var queensAuctions = reverseDictionaries.GetQueensDictionary(shapeStr);
            var bidsForPhaseQueens = string.Join("", queensBidsResult);
              
            if (queensAuctions.TryGetValue(bidsForPhaseQueens, out var queens))
            {
                Logger.Debug($"Found queens for auction. Queens:{queens}. QueensBids:{bidsForPhaseQueens}. Auction:{auction.GetPrettyAuction("|")}");
                return GetQueensOrdered(shapeStr, queens);
            }

            throw new InvalidOperationException($"{ bidsForPhaseQueens } not found in queens dictionary. Auction:{auction.GetPrettyAuction("|")}. " +
                $"zoom-offset control scanning:{zoomOffset}");

            int GetOffsetRelayBid(Bid currentBid)
            {
                if (currentBid == biddingState.GetPullBid())
                    return 0;
                var previousBidSouth = auction.GetRelativeBid(currentBid, -1, Player.South);
                if (previousBidSouth == default)
                    return 0;
                var previousBidNorth = auction.GetRelativeBid(currentBid, 0, Player.North);

                return previousBidNorth - previousBidSouth - 1;
            }
        }

        private static IEnumerable<Bid> GetBidsForPhaseWithOffset(IEnumerable<Bid> bidsForPhases, Bid offSetBid, Bid lastBidPreviousPhase, int zoomOffset, Func<Bid, int> getOffsetRelayBid)
        {
            var offset = lastBidPreviousPhase - offSetBid;
            if (zoomOffset != 0)
                bidsForPhases = new[] { lastBidPreviousPhase }.Concat(bidsForPhases);

            var offsetRelayBid = 0;
            var bidsForPhasesResult = bidsForPhases.Select(b =>
            {
                offsetRelayBid -= getOffsetRelayBid(b);
                return b + zoomOffset + offsetRelayBid - offset;
            });
            return bidsForPhasesResult;
        }

        private static MinMax GetHcpFromAuction(PhaseDictionary faseAuctions, BiddingState biddingState)
        {
            var pullPhase = biddingState.GetPullPhase();
            var pullBid = biddingState.GetPullBid();
            if (pullPhase != default)
            {
                var sigOffBid = BiddingState.GetSignOffBid(pullPhase, pullBid);
                // TODO handle case where sign-off bid is 4NT
                if (faseAuctions[pullPhase].TryGetValue(sigOffBid.ToString(), out var hcpList))
                    return new MinMax(hcpList.Min(), hcpList.Max());
            }

            return new MinMax(8, 37);
        }

        private static string GetQueensOrdered(string shapeStr, string queens)
        {
            var shapes = shapeStr.ToArray().Select((x, index) => (int.Parse(x.ToString()), index)).ToList(); // {3,4,5,1}
            var shapesOrdered = shapes.OrderByDescending(x => x.Item1).ThenBy(x => x.index).ToList(); // {5,4,3,1}
            var queensOrdered = new string(shapes.Select(x => queens[shapesOrdered.IndexOf(x)]).ToArray());
            return queensOrdered;
        }

        private static List<string> GetMatchesWithNorthHand(IEnumerable<string> shapeLengths, IEnumerable<string> possibleControls, string northHand)
        {
            return possibleControls
                .Select(control => control.Split(',').Select(x => x.TrimEnd('x')))
                .Select(controls => shapeLengths.Select(shapeLengthStr => MergeControlAndShape(controls, shapeLengthStr)).Where(x => x.Sum(y => y.Length) == 13))
                .Select(controlsByShapes => controlsByShapes.Where(controlByShape => Match(controlByShape, northHand.Split(','))))
                .SelectMany(southHands => southHands)
                .Select(southHand => string.Join(',', southHand)).ToList();
        }

        /// <summary>
        /// Try to find control matches without asking for it
        /// </summary>
        /// <returns>List of possible matches with regard to the north hand</returns>
        private static IEnumerable<string> GetMatchesWithNorthHand(IReadOnlyCollection<string> shapeLengths, int controlCount, string northHandStr)
        {
            var northHand = northHandStr.Split(',');
            foreach (var controlStr in GenerateControlLocations(controlCount))
            {
                var controls = controlStr.Split(',');
                var controlByShapes = shapeLengths.Select(shapeLengthStr => MergeControlAndShape(controls, shapeLengthStr)).Where(x => x.Sum(y => y.Length) == 13);
                var southHands = controlByShapes.Where(controlByShape => Match(controlByShape.ToArray(), northHand));
                foreach (var southHand in southHands)
                    yield return string.Join(',', southHand);
            }

            static IEnumerable<string> GenerateControlLocations(int controlCount)
            {
                var controls = new[] { "", "A", "K", "AK" };
                foreach (var spades in controls)
                    foreach (var hearts in controls)
                        foreach (var diamonds in controls)
                            foreach (var clubs in controls)
                            {
                                string hand = spades + "," + hearts + "," + diamonds + "," + clubs;
                                if (Util.GetControlCount(hand) == controlCount)
                                    yield return hand;
                            }
            }
        }

        /// <summary>
        /// Merges shapes and controls
        /// </summary>
        /// <param name="controls">{"A","","Q","K"}</param>
        /// <param name="shapeLengthStr">"3451"</param>
        /// <returns>{"Qxx","xxxx","Axxxx","K"}</returns>
        public static IEnumerable<string> MergeControlAndShape(IEnumerable<string> controls, string shapeLengthStr)
        {
            var shapes = shapeLengthStr.ToArray().Select((x, index) => (int.Parse(x.ToString()), index)).ToList(); // {3,4,5,1}
            // Sort by length, then by position 
            var shapesOrdered = shapes.OrderByDescending(x => x.Item1).ThenBy(x => x.index).ToList(); // {5,4,3,1}
            return shapes.Select(shape => controls.ToArray()[shapesOrdered.IndexOf(shape)].PadRight(shape.Item1, 'x'));
        }

        private static bool Match(IEnumerable<string> hand1, IEnumerable<string> hand2)
        {
            var relevantCards = new[] { 'A', 'K' };
            return hand1.Zip(hand2, (x, y) => (x, y)).All(suit => !relevantCards.Any(c => suit.x.Contains(c) && suit.y.Contains(c)));
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
