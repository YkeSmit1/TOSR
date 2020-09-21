using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NLog;
using Common;
using Solver;
using ShapeDictionary = System.Collections.Generic.Dictionary<string, (System.Collections.Generic.List<string> pattern, bool zoom)>;
using ControlsDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;

namespace Tosr
{
    public class BidManager
    {
        private readonly IBidGenerator bidGenerator;
        readonly Dictionary<Fase, bool> fasesWithOffset;
        private readonly ControlsDictionary controlsAuctions = null;
        private readonly ShapeDictionary shapeAuctions;
        readonly bool useSingleDummySolver = false;
        Lazy<Tuple<List<string>, int>> shape;

        static readonly Bid threeDiamondBid = new Bid(3, Suit.Diamonds);
        static readonly Bid threeSpadeBid = new Bid(3, Suit.Spades);
        static readonly Bid fourClBid = new Bid(4, Suit.Clubs);
        static readonly char[] relevantCards = new[] { 'A', 'K', 'Q' };

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;

            useSingleDummySolver = false;
        }

        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, ShapeDictionary shapeAuctions, ControlsDictionary controlsAuctions) :
            this(bidGenerator, fasesWithOffset)
        {
            this.shapeAuctions = shapeAuctions;
            this.controlsAuctions = controlsAuctions;

            useSingleDummySolver = false;
        }

        public Auction GetAuction(string northHand, string southHand)
        {
            logger.Info($"Starting GetAuction for hand : {southHand}");
            Auction auction = new Auction();

            BiddingState biddingState = new BiddingState(fasesWithOffset);
            Player currentPlayer = Player.West;
            shape = new Lazy<Tuple<List<string>, int>>(() => GetShapeStrFromAuction(auction, shapeAuctions));

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
                        SouthBid(biddingState, southHand);
                        auction.AddBid(biddingState.CurrentBid);
                        // Specific for zoom. TODO Code is ugly, needs improvement
                        if (shapeAuctions != null && biddingState.Fase == Fase.Controls && biddingState.NextBidIdForRule == 0 && shape.Value.Item2 != 0)
                        {
                            biddingState.NextBidIdForRule = shape.Value.Item2;
                            biddingState.RelayBidIdLastFase -= shape.Value.Item2;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                currentPlayer = currentPlayer == Player.South ? Player.West : currentPlayer + 1;
            }
            while (!biddingState.EndOfBidding);

            logger.Info($"Ending GetAuction for hand : {southHand}");
            return auction;
        }

        public void NorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.Fase != Fase.End)
                biddingState.CurrentBid = GetRelayBid(biddingState, auction);
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
                        var scores = SingleDummySolver.SolveSingleDummy(suit, 0, northHand, constructedSouthHand);
                        var mostFrequent = scores.GroupBy(x => x).OrderByDescending(y => y.Count()).Take(1).Select(z => z.Key).First();
                        Bid bid = new Bid(mostFrequent - 6, (Suit)(3 - suit));
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

        private Bid GetRelayBid(BiddingState biddingState, Auction auction)
        {
            if (biddingState.CurrentBid == threeSpadeBid && shapeAuctions != null)
            {
                var strAuction = auction.GetBidsAsString(Fase.Shape);
                var shapeStr = GetShapeStrFromAuction(auction, shapeAuctions).Item1;
                var shape = shapeStr.First().ToCharArray().OrderByDescending(x => x);
                var shapeStringSorted = new string(shape.ToArray());

                if (shapeStringSorted != "7330")
                {
                    biddingState.RelayBidIdLastFase++;
                    return fourClBid;
                }
            }

            return Bid.NextBid(biddingState.CurrentBid);
        }

        public void SouthBid(BiddingState biddingState, string handsString)
        {
            if (biddingState.EndOfBidding)
                return;
            var (bidIdFromRule, nextfase, description, zoom) = bidGenerator.GetBid(biddingState, handsString);
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, description, zoom);
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
            logger.Info($"Starting ConstructSouthHand for northhand : {northHand}");

            var strControls = GetAuctionForControlsWithOffset(auction, threeDiamondBid, shape.Value.Item2);

            if (!controlsAuctions.TryGetValue(strControls, out var possibleControls))
            {
                throw new InvalidOperationException($"Auction not found in controls. controls: {strControls}. NorthHand: {northHand}.");
            }
            var matches = GetMatchesWithNorthHand(shape.Value.Item1, possibleControls, northHand);
            logger.Info($"Ending ConstructSouthHand. southhand : {matches.First()}");
            return (matches.Count()) switch
            {
                0 => throw new InvalidOperationException($"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {northHand}."),
                1 => matches.First(),
                _ => throw new InvalidOperationException($"Multiple matches found. Matches: {string.Join('|', matches)}. NorthHand: {northHand}."),
            };
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
                    return $"Match is found: {southHand}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
                else
                    return $"SouthHand is not equal to expected. Expected: {southHand}. Actual {southHandStr}. NorthHand: {strHand.NorthHand}. SouthHand: {strHand.SouthHand}";
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
        public static Tuple<List<string>, int> GetShapeStrFromAuction(Auction auction, ShapeDictionary shapeAuctions)
        {
            var strAuction = auction.GetBidsAsString(Fase.Shape);

            if (shapeAuctions.ContainsKey(strAuction))
                return new Tuple<List<string>, int>(shapeAuctions[strAuction].pattern, shapeAuctions[strAuction].zoom ? 2 : 0);

            var allBids = auction.GetBids(Player.South, Fase.Shape);
            var lastBid = allBids.Last();
            var allButLastBid = allBids.Take(allBids.Count() - 1);
            for (var bid = lastBid - 1; bid >= threeDiamondBid; bid--)
            {
                var allBidsNew = allButLastBid.Concat(new[] { bid });
                var bidsStr = allBidsNew.Aggregate(string.Empty, (current, bid) => current + bid);
                // Add two because auction is two bids lower if zoom applies
                if (shapeAuctions.ContainsKey(bidsStr) && shapeAuctions[bidsStr].zoom)
                    return new Tuple<List<string>, int>(shapeAuctions[bidsStr].pattern, (lastBid - bid) + 2);
            }

            throw new InvalidOperationException($"{ strAuction } not found in shape dictionary");
        }

        /// <summary>
        /// Extracts the control and scanning part of the auction and applies an offset of offsetBid
        /// </summary>
        /// <param name="auction">Generated auction </param>
        /// <param name="offsetBid">Offset used to generate AuctionsByControl.txt</param>
        /// <returns></returns>
        public static string GetAuctionForControlsWithOffset(Auction auction, Bid offsetBid, int zoomOffset)
        {
            var lastBidShape = auction.GetBids(Player.South, Fase.Shape).Last();
            var bidsControls = auction.GetBids(Player.South, new Fase[] { Fase.Controls, Fase.Scanning });
            var offSet = lastBidShape - offsetBid;
            if (zoomOffset != 0)
                bidsControls = new List<Bid> { lastBidShape }.Concat(bidsControls);

            var used4ClAsRelay = Used4ClAsRelay(auction);
            bidsControls = bidsControls.Select(b => b = (b - (used4ClAsRelay && b > fourClBid ? offSet + 1 : offSet)) + zoomOffset);
            var strControls = string.Join("", bidsControls);
            return strControls;
        }

        private static bool Used4ClAsRelay(Auction auction)
        {
            var previousBiddingRound = auction.bids.First();
            foreach (var biddingRound in auction.bids.Skip(1))
            {
                if (biddingRound.Value.ContainsKey(Player.North) && biddingRound.Value[Player.North] == fourClBid)
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
        /// <param name="controlStr">"Axxx,Kxx,Qxx,xxx"</param>
        /// <param name="shapeLengthStr">"3451"</param>
        /// <returns>"Qxx,Kxxx,Axxxx,x"</returns>
        public static IEnumerable<string> MergeControlAndShape(string[] controls, string shapeLengthStr)
        {
            var shapes = shapeLengthStr.ToArray().Select(x => float.Parse(x.ToString())).ToList(); // 3424

            // This is because there can be two suits with the same length. So we added a small offset to make it unique
            foreach (var suit in Enumerable.Range(0, 4))
                shapes[suit] += (float)(4 - suit) / 10;

            var shapesOrdered = shapes.OrderByDescending(x => x).ToList(); // 4432

            var shapesDic = shapes.ToDictionary(key => shapes.IndexOf(key), value => shapesOrdered.IndexOf(value));
            foreach (var suit in Enumerable.Range(0, 4))
            {
                var shape = shapes[suit];
                string controlStrSuit = controls[shapesDic[suit]];
                if (shape < controlStrSuit.Length)
                    yield break;
                yield return controlStrSuit + new string('x', (int)shape - controlStrSuit.Length);
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
