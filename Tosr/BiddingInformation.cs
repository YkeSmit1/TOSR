using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;
using Newtonsoft.Json;
using NLog;
using Solver;

namespace Tosr
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using FaseDictionary = Dictionary<Fase, Dictionary<string, List<int>>>;

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

    public class BiddingInformation
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger loggerBidding = LogManager.GetLogger("bidding");

        public Lazy<(List<string> shapes, int zoomOffset)> shape;
        public Lazy<(List<string> controls, int zoomOffset)> controlsScanning;

        static readonly char[] relevantCards = new[] { 'A', 'K' };
        public ConstructedSouthhandOutcome constructedSouthhandOutcome = ConstructedSouthhandOutcome.NotSet;
        private readonly ReverseDictionaries reverseDictionaries;
        private readonly Auction auction;

        public BiddingInformation(ReverseDictionaries reverseDictionaries, Auction auction)
        {
            shape = new Lazy<(List<string> shapes, int zoomOffset)>(() => GetShapeStrFromAuction(auction, reverseDictionaries.ShapeAuctions));
            controlsScanning = new Lazy<(List<string> controls, int zoomOffset)>(() => GetControlsScanningStrFromAuction(auction, reverseDictionaries, shape.Value.zoomOffset, shape.Value.shapes.First()));
            this.reverseDictionaries = reverseDictionaries;
            this.auction = auction;
            constructedSouthhandOutcome = ConstructedSouthhandOutcome.NotSet;
        }

        public SouthInformation GetInformationFromAuction(Auction auction, string northHand)
        {
            var southInformation = new SouthInformation
            {
                Shapes = shape.Value.shapes,
                Hcp = GetHcpFromAuction(auction, reverseDictionaries.SignOffFasesAuctions)
            };

            var controls = GetAuctionForFaseWithOffset(auction, shape.Value.zoomOffset, new Fase[] { Fase.Controls }).ToList();
            southInformation.ControlBidCount = controls.Count();
            if (controls.Count() > 0)
            {
                var possibleControls = reverseDictionaries.ControlsOnlyAuctions[string.Join("", controls)];
                southInformation.Controls = new MinMax(possibleControls.First(), possibleControls.Last());
            }

            if (controlsScanning.IsValueCreated)
            {
                var matches = GetMatchesWithNorthHand(shape.Value.shapes, controlsScanning.Value.controls, northHand);
                if (matches.Count() == 0)
                    throw new InvalidOperationException($"No matches found. NorthHand:{northHand}");

                southInformation.SpecificControls = matches.Select(match => match.Split(',').Select(x => Regex.Match(x, "[AK]").ToString()).ToArray());
                southInformation.Queens = GetQueensFromAuction(auction, reverseDictionaries);
            }

            loggerBidding.Info($"SouthInformation. {JsonConvert.SerializeObject(southInformation)}");
            return southInformation;
        }

        /// <summary>
        /// Construct southhand to compare with the actual southhand
        /// </summary>
        public string ConstructSouthHandSafe(string[] hand)
        {
            var northHand = hand[(int)Player.North];
            var southHand = hand[(int)Player.South];

            try
            {
                var constructedSouthHand = ConstructSouthHand(northHand);
                if (constructedSouthHand.Count() > 1)
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.MultipleMatchesFound;
                    return $"Multiple matches found. Matches: {string.Join('|', constructedSouthHand)}. NorthHand: {northHand}. SouthHand: {southHand}";
                }

                if (constructedSouthHand.First() == Util.HandWithx(southHand))
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.SouthhandMatches;
                    var queens = GetQueensFromAuction(auction, reverseDictionaries);
                    if (!CheckQueens(queens, southHand))
                        return $"Match is found but queens are wrong : Expected queens: {queens}. SouthHand: {southHand}";

                    return $"Match is found: {constructedSouthHand.First()}. NorthHand: {northHand}. SouthHand: {southHand}";
                }
                else
                {
                    constructedSouthhandOutcome = ConstructedSouthhandOutcome.IncorrectSouthhand;
                    return $"SouthHand is not equal to expected. Expected: {constructedSouthHand.First()}. NorthHand: {northHand}. SouthHand: {southHand}";
                }
            }
            catch (Exception e)
            {
                return $"{e.Message} SouthHand: {southHand}. Projected AKQ controls as 4333:{Util.GetHandWithOnlyControlsAs4333(southHand, "AKQ")}. " +
                    $"Sign-off fases:{auction.GetBids(Player.South, Util.signOffFases.ToArray()).FirstOrDefault()?.fase}";
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
        private static (List<string> controls, int zoomOffset) GetControlsScanningStrFromAuction(Auction auction, ReverseDictionaries reverseDictionaries, int zoomOffsetShape, string shapeStr)
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
            var bidsForFases = auction.GetBids(Player.South, fases);
            var offSet = lastBidShape - Bid.threeDiamondBid;
            if (zoomOffset != 0)
                bidsForFases = new[] { lastBidShape }.Concat(bidsForFases);

            var used4ClAsRelay = Used4ClAsRelay(auction);
            var fourCLubsRelayOffSet = 0;
            var signOffOffset = 0;
            var previousBid = lastBidShape;
            var bidPull3NTNoAsk = auction.GetBids(Player.South).Where(bid => bid.pullFase == Fase.Pull3NTNoAsk);
            var signOffBid = GetSignOffBid();

            var bidsForFasesResult = bidsForFases.Select(b =>
            {
                if (Util.signOffFases.Contains(b.pullFase) || (bidPull3NTNoAsk.Count() == 1 && signOffOffset == 0 && b > bidPull3NTNoAsk.Single()))
                    signOffOffset = signOffBid - previousBid;

                if (used4ClAsRelay && b > Bid.fourClubBid)
                    fourCLubsRelayOffSet = 1;
                previousBid = b;

                return b += zoomOffset - fourCLubsRelayOffSet - offSet - signOffOffset;
            });

            return bidsForFasesResult;

            Bid GetSignOffBid()
            {
                var signOffBiddingRounds = auction.bids.Where(bids => bids.Value.TryGetValue(Player.South, out var bid) && Util.signOffFases.Contains(bid.pullFase));
                if (signOffBiddingRounds.Count() == 0)
                    return Bid.PassBid;
                var signOffBiddingRound = signOffBiddingRounds.Single();
                // Return the next bid. The sign-off bid only shows points
                if (signOffBiddingRound.Value[Player.South].pullFase == Fase.Pull3NTNoAsk)
                    return auction.bids[signOffBiddingRound.Key + 1].TryGetValue(Player.North, out var bidNorth) ? bidNorth - 1 : Bid.PassBid;

                var signoffBidNorth = signOffBiddingRound.Value[Player.North];
                var bid = signoffBidNorth.suit == Suit.NoTrump ? signoffBidNorth - 1 : signoffBidNorth;
                return bid;
            }
        }

        private static MinMax GetHcpFromAuction(Auction auction, FaseDictionary faseAuctions)
        {
            var signOffBids = auction.GetBids(Player.South, Util.signOffFases.ToArray());
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

        /// <summary>
        /// Returns a string of 4 characters when each character is "Y" "N" "X". "X" means not yet known.
        /// </summary>
        public string GetQueensFromAuction(Auction auction, ReverseDictionaries reverseDictionaries)
        {
            string shapeStr = shape.Value.shapes.First();
            int zoomOffset = controlsScanning.Value.zoomOffset;
            var lastBidPreviousFase = auction.GetBids(Player.South, (new[] { Fase.Controls, Fase.ScanningControls })).Last();
            var queensBids = auction.GetBids(Player.South, Fase.ScanningOther);
            var offset = lastBidPreviousFase - ReverseDictionaries.GetOffsetBidForQueens(shapeStr);
            if (zoomOffset != 0)
            {
                queensBids = new[] { lastBidPreviousFase }.Concat(queensBids);
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
