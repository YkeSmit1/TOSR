using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;
using Newtonsoft.Json;
using NLog;
using Solver;

namespace BiddingLogic
{
    using FaseDictionary = Dictionary<Fase, Dictionary<string, List<int>>>;

    public class BiddingInformation
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger loggerBidding = LogManager.GetLogger("bidding");
        public Lazy<(List<string> shapes, int zoomOffset)> Shape { get; set; }
        public Lazy<(List<string> controls, int zoomOffset)> ControlsScanning { get; set; }
        private readonly ReverseDictionaries reverseDictionaries;

        public BiddingInformation(ReverseDictionaries reverseDictionaries, Auction auction, BiddingState biddingState)
        {
            Shape = new Lazy<(List<string> shapes, int zoomOffset)>(() =>
            {
                var bidsForFase = biddingState.GetBids(Fase.Shape);
                return GetInformationFromBids(reverseDictionaries.ShapeAuctions, bidsForFase);
            });
            ControlsScanning = new Lazy<(List<string> controls, int zoomOffset)>(() =>
            {
                var dictionary = reverseDictionaries.GetControlScanningDictionary(Shape.Value.shapes.Last());
                var bidsForFase = GetAuctionForFaseWithOffset(auction, Shape.Value.zoomOffset, biddingState, Fase.Controls, Fase.ScanningControls).ToList();
                return GetInformationFromBids(dictionary, bidsForFase);
            });
            this.reverseDictionaries = reverseDictionaries;
        }

        public SouthInformation GetInformationFromAuction(Auction auction, string northHand, BiddingState biddingState)
        {
            var southInformation = new SouthInformation
            {
                Shapes = Shape.Value.shapes,
                Hcp = GetHcpFromAuction(reverseDictionaries.SignOffFasesAuctions, biddingState)
            };

            southInformation.ControlBidCount = biddingState.GetBids(Fase.Controls).Count();
            southInformation.ControlsScanningBidCount = biddingState.GetBids(Fase.ScanningControls).Count();
            var controls = GetAuctionForFaseWithOffset(auction, Shape.Value.zoomOffset, biddingState, Fase.Controls).ToList();
            if (controls.Count > 0)
            {
                var possibleControls = reverseDictionaries.ControlsOnlyAuctions[string.Join("", controls)];
                southInformation.Controls = new MinMax(possibleControls.First(), possibleControls.Last());
                // Special case if relayer is able figure out the position of controls
                if (southInformation.ControlsScanningBidCount == 0 && possibleControls.Distinct().Count() == 1)
                {
                    var matches = GetMatchesWithNorthHand(Shape.Value.shapes, possibleControls.First(), northHand);
                    if (matches.Count() == 1)
                        southInformation.SpecificControls = matches.Select(match => match.Split(',').Select(x => Regex.Replace(x, "[^AK]", "")).ToArray());
                }
            }


            if (ControlsScanning.IsValueCreated)
            {
                var matches = GetMatchesWithNorthHand(Shape.Value.shapes, ControlsScanning.Value.controls, northHand);
                if (!matches.Any())
                    throw new InvalidOperationException($"No matches found. NorthHand:{northHand}");

                southInformation.SpecificControls = matches.Select(match => match.Split(',').Select(x => Regex.Replace(x, "[^AK]", "")).ToArray());
                southInformation.Queens = GetQueensFromAuction(auction, reverseDictionaries, biddingState);
            }

            loggerBidding.Info($"SouthInformation. {JsonConvert.SerializeObject(southInformation)}");
            return southInformation;
        }

        /// <summary>
        /// Construct southhand to use for single dummy analyses
        /// Can throw
        /// </summary>
        public IEnumerable<string> ConstructSouthHand(string northHand)
        {
            logger.Debug($"Starting ConstructSouthHand for northhand : {northHand}");
            var possibleControls = ControlsScanning.Value.controls;
            var matches = GetMatchesWithNorthHand(Shape.Value.shapes, possibleControls, northHand);
            if (!matches.Any())
                throw new InvalidOperationException($"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {northHand}.");

            logger.Debug($"Ending ConstructSouthHand. southhand : {string.Join("|", matches)}");
            return matches;
        }

        /// <summary>
        /// Lookup in the dictionary. If not found, it tries to find an auction when the last bid was done with zoom
        /// </summary>
        public static (List<string> information, int zoomOffset) GetInformationFromBids(Dictionary<string, (List<string> pattern, bool zoom)> dictionary, IEnumerable<Bid> bidsForFase)
        {
            if (dictionary.TryGetValue(string.Join("", bidsForFase), out var information))
                return (information.pattern, information.zoom ? 2 : 0);

            var lastBid = bidsForFase.Last();
            var firstBid = bidsForFase.First();
            var allButLastBid = bidsForFase.Take(bidsForFase.Count() - 1);
            for (var bid = lastBid - 1; bid >= firstBid; bid--)
            {
                var allBidsNew = allButLastBid.Append(bid);
                var bidsStr = string.Join("", allBidsNew);
                // Add two because auction is two bids lower if zoom applies
                if (dictionary.TryGetValue(bidsStr, out var informationZoom) && informationZoom.zoom)
                    return (informationZoom.pattern, lastBid - bid + 2);
            }

            throw new InvalidOperationException($"{ string.Join("", bidsForFase) } not found in dictionary");
        }

        /// <summary>
        /// Extracts the control and scanning part of the auction and applies an offset of offsetBid
        /// </summary>
        /// <param name="auction">Generated auction </param>
        /// <param name="fase">Which fases to get the offset from</param>
        /// <returns></returns>
        public static IEnumerable<Bid> GetAuctionForFaseWithOffset(Auction auction, int zoomOffset, BiddingState biddingState, params Fase[] fases)
        {
            var lastBidShape = biddingState.GetBids(Fase.Shape).Last();
            var bidsForFases = biddingState.GetBids(fases);

            return GetBidsForFaseWithOffset(bidsForFases, Bid.threeDiamondBid, lastBidShape, zoomOffset, GetOffsetRelayBid);

            int GetOffsetRelayBid(Bid currentBid)
            {
                var previousBidSouth = auction.GetRelativeBid(currentBid, -1, Player.South);
                if (previousBidSouth == default)
                    return 0;
                var previousBidNorth = auction.GetRelativeBid(currentBid, 0, Player.North);

                // In case of 3NT pull without ask, use the bid before. The sign-off bid only shows points
                return previousBidSouth == biddingState.GetBids(Fase.Pull3NTNoAsk).SingleOrDefault()
                    ? previousBidNorth - auction.GetRelativeBid(currentBid, -2, Player.South) - 1
                    : previousBidNorth - previousBidSouth - (currentBid == biddingState.GetPullBid() && BiddingState.SignOffFasesFor4Di.Contains(biddingState.GetPullFase()) ? 0 : 1);
            }
        }

        /// <summary>
        /// Returns a string of 4 characters when each character is "Y" "N" "X". "X" means not yet known.
        /// </summary>
        public string GetQueensFromAuction(Auction auction, ReverseDictionaries reverseDictionaries, BiddingState biddingState)
        {
            // Because the last shape is the one with the highest numeric value generated in ReverseDictionaries
            var shapeStr = Shape.Value.shapes.Last();
            var zoomOffset = ControlsScanning.Value.zoomOffset;
            var lastBidScanningControl = biddingState.GetBids(Fase.ScanningControls).Last();
            var queensBids = biddingState.GetBids(Fase.ScanningOther);
            var offsetBid = ReverseDictionaries.GetOffsetBidForQueens(shapeStr);

            var queensBidsResult = GetBidsForFaseWithOffset(queensBids, offsetBid, lastBidScanningControl, zoomOffset, GetOffsetRelayBid).ToList();
            if (!queensBidsResult.Any())
                return null;
            var queensAuctions = reverseDictionaries.GetQueensDictionary(shapeStr);
            var bidsForFaseQueens = string.Join("", queensBidsResult);
              
            if (queensAuctions.TryGetValue(bidsForFaseQueens, out var queens))
            {
                logger.Debug($"Found queens for auction. Queens:{queens}. QueensBids:{bidsForFaseQueens}. Auction:{auction.GetPrettyAuction("|")}");
                return GetQueensOrdered(shapeStr, queens);
            }

            throw new InvalidOperationException($"{ bidsForFaseQueens } not found in queens dictionary. Auction:{auction.GetPrettyAuction("|")}. " +
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

        private static IEnumerable<Bid> GetBidsForFaseWithOffset(IEnumerable<Bid> bidsForFases, Bid offSetBid, Bid lastBidPreviousFase, int zoomOffset, Func<Bid, int> GetOffsetRelayBid)
        {
            var offset = lastBidPreviousFase - offSetBid;
            if (zoomOffset != 0)
                bidsForFases = new[] { lastBidPreviousFase }.Concat(bidsForFases);

            var offsetRelayBid = 0;
            var bidsForFasesResult = bidsForFases.Select(b =>
            {
                offsetRelayBid -= GetOffsetRelayBid(b);
                return b + zoomOffset + offsetRelayBid - offset;
            });
            return bidsForFasesResult;
        }

        private static MinMax GetHcpFromAuction(FaseDictionary faseAuctions, BiddingState biddingState)
        {
            var pullFase = biddingState.GetPullFase();
            var pullBid = biddingState.GetPullBid();
            if (pullFase != default)
            {
                var sigOffBid = BiddingState.GetSignOffBid(pullFase, pullBid);
                // TODO handle case where sign-off bid is 4NT
                if (faseAuctions[pullFase].TryGetValue(sigOffBid.ToString(), out var hcps))
                    return new MinMax(hcps.Min(), hcps.Max());
            }

            return new MinMax(8, 37);
        }

        private static string GetQueensOrdered(string shapeStr, string queens)
        {
            var shapes = shapeStr.ToArray().Select((x, index) => (int.Parse(x.ToString()), index)); // {3,4,5,1}
            var shapesOrdered = shapes.OrderByDescending(x => x.Item1).ThenBy(x => x.index).ToList(); // {5,4,3,1}
            var queensOrdered = new string(shapes.Select(x => queens[shapesOrdered.IndexOf(x)]).ToArray());
            return queensOrdered;
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
        /// Try to find control matches without asking for it
        /// </summary>
        /// <param name="shapeLengthStrs"></param>
        /// <param name="northHandStr"></param>
        /// <returns>List of possible matches with regard to the northhand</returns>
        private static IEnumerable<string> GetMatchesWithNorthHand(List<string> shapeLengthStrs, int controlCount, string northHandStr)
        {
            var northHand = northHandStr.Split(',');
            foreach (var controlStr in GenerateControlLocations(controlCount))
            {
                var controls = controlStr.Split(',');
                var controlByShapes = shapeLengthStrs.Select(shapeLengthStr => MergeControlAndShape(controls, shapeLengthStr)).Where(x => x.Sum(x => x.Length) == 13);
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
        public static IEnumerable<string> MergeControlAndShape(string[] controls, string shapeLengthStr)
        {
            var shapes = shapeLengthStr.ToArray().Select((x, index) => (int.Parse(x.ToString()), index)); // {3,4,5,1}
            // Sort by length, then by position 
            var shapesOrdered = shapes.OrderByDescending(x => x.Item1).ThenBy(x => x.index).ToList(); // {5,4,3,1}
            return shapes.Select(shape => controls[shapesOrdered.IndexOf(shape)].PadRight(shape.Item1, 'x'));
        }

        private static bool Match(string[] hand1, string[] hand2)
        {
            var relevantCards = new[] { 'A', 'K' };
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
