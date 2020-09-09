using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Common;
using Solver;

namespace Tosr
{
    public class BidManager
    {
        readonly IBidGenerator bidGenerator;
        readonly Dictionary<Fase, bool> fasesWithOffset;
        readonly Dictionary<string, List<string>> controlsAuctions = null;
        readonly Dictionary<string, string> shapeAuctions;
        readonly bool useSingleDummySolver = false;

        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset)
        {
            this.bidGenerator = bidGenerator;
            this.fasesWithOffset = fasesWithOffset;

            this.useSingleDummySolver = false;
        }

        public BidManager(IBidGenerator bidGenerator, Dictionary<Fase, bool> fasesWithOffset, 
            Dictionary<string, string> shapeAuctions, Dictionary<string, List<string>> controlsAuctions) : this(bidGenerator, fasesWithOffset)
        {
            this.shapeAuctions = shapeAuctions;
            this.controlsAuctions = controlsAuctions;

            this.useSingleDummySolver = false;
        }

        public Auction GetAuction(string northHand, string southHand)
        {
            Auction auction = new Auction();

            BiddingState biddingState = new BiddingState(fasesWithOffset);
            Player currentPlayer = Player.West;

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
                        auction.AddBid(biddingState.currentBid);
                        break;
                    case Player.South:
                        SouthBid(biddingState, southHand);
                        auction.AddBid(biddingState.currentBid);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                currentPlayer = currentPlayer == Player.South ? Player.West : currentPlayer + 1;
            }
            while (!biddingState.EndOfBidding);
            return auction;
        }

        public void NorthBid(BiddingState biddingState, Auction auction, string northHand)
        {
            if (biddingState.fase != Fase.End)
                biddingState.currentBid = GetRelayBid(biddingState, auction);
            else
            {
                biddingState.EndOfBidding = true;
                // Try to guess contract by using single dummy solver
                if (useSingleDummySolver)
                {
                    try
                    {
                        var southHand = ConstructSouthHand(northHand, auction);
                        var suit = Common.Common.GetLongestSuit(northHand, southHand);
                        var scores = SingleDummySolver.SolveSingleDummy(suit, 0, northHand, southHand);
                        var mostFrequent = scores.GroupBy(x => x).OrderByDescending(y => y.Count()).Take(1).Select(z => z.Key).First();
                        Bid bid = new Bid(mostFrequent - 6, (Suit)(3 - suit));
                        if (bid > biddingState.currentBid)
                        {
                            biddingState.currentBid = bid;
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                biddingState.currentBid = Bid.PassBid;
            }
        }

        private Bid GetRelayBid(BiddingState biddingState, Auction auction)
        {
            if (biddingState.currentBid == new Bid(3, Suit.Spades))
            {
                var strAuction = auction.GetBidsAsString(Fase.Shape);
                var shape = shapeAuctions[strAuction].ToCharArray().OrderByDescending(x => x);
                var shapeStringSorted = new string(shape.ToArray());

                if (shapeStringSorted != "7330")
                    return new Bid(4, Suit.Clubs);
            }

            return Bid.NextBid(biddingState.currentBid);
        }

        public void SouthBid(BiddingState biddingState, string handsString)
        {
            if (biddingState.EndOfBidding)
                return;
            var (bidIdFromRule, nextfase, description) = bidGenerator.GetBid(biddingState, handsString);
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, description);
        }

        public string ConstructSouthHand(string northHand, Auction auction)
        {
            var strControls = GetAuctionForControlsWithOffset(auction, new Bid(3, Suit.Diamonds));

            if (!controlsAuctions.TryGetValue(strControls, out var possibleControls))
            {
                throw new InvalidOperationException($"Auction not found in controls. controls: {strControls}. NorthHand: {northHand}.");
            }
            var strAuction = auction.GetBidsAsString(Fase.Shape);
            var matches = GetMatchesWithNorthHand(shapeAuctions[strAuction], possibleControls, northHand);
            return (matches.Count()) switch
            {
                0 => throw new InvalidOperationException($"No matches found. Possible controls: {string.Join('|', possibleControls)}. NorthHand: {northHand}."),
                1 => matches.First(),
                _ => throw new InvalidOperationException($"Multiple matches found. Matches: {string.Join('|', matches)}. NorthHand: {northHand}."),
            };
        }

        /// <summary>
        /// Extracts the control and scanning part of the auction and applies an offset of offsetBid
        /// </summary>
        /// <param name="auction">Generated auction </param>
        /// <param name="offsetBid">Offset used to generate AuctionsByControl.txt</param>
        /// <returns></returns>
        public static string GetAuctionForControlsWithOffset(Auction auction, Bid offsetBid)
        {
            var shapeBidsSouth = auction.GetBids(Player.South, Fase.Shape);
            var lastBidShape = shapeBidsSouth.Last();
            var bidsControls = auction.GetBids(Player.South, new Fase[] { Fase.Controls, Fase.Scanning });
            var offSet = lastBidShape - offsetBid;
            bidsControls = bidsControls.Select(b => b -= offSet);
            var strControls = string.Join("", bidsControls);
            return strControls;
        }

        public IEnumerable<string> GetMatchesWithNorthHand(string shapeLengthStr, List<string> possibleControls, string northHandStr)
        {
            var northHand = northHandStr.Split(',');
            foreach (var controlStr in possibleControls)
            {
                var controlByShape = MergeControlAndShape(controlStr, shapeLengthStr);
                if (controlByShape.Count() == 4)
                {
                    if (Match(controlByShape.ToArray(), northHand))
                        yield return string.Join(',', controlByShape);
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
        public static IEnumerable<string> MergeControlAndShape(string controlStr, string shapeLengthStr)
        {
            var controls = controlStr.Split(',').Select(x => x.TrimEnd('x')).ToArray();
            var shapes = shapeLengthStr.ToArray().Select(x => float.Parse(x.ToString())).ToList(); // 3424
            // This is because there can be two suits with the same length. So we added a small offset to make it unique
            foreach (var suit in Enumerable.Range(0, 4))
            {
                shapes[suit] += (float)(4 - suit) / 10;
            }

            var shapesOrdered = shapes.OrderByDescending(x => x).ToList(); // 4432

            var shapesDic = shapes.ToDictionary(key => shapes.IndexOf(key), value => shapesOrdered.IndexOf(value));
            foreach (var suit in Enumerable.Range(0, 4))
            {
                var shape = shapes[suit];
                string controlStrSuit = controls[shapesDic[suit]];
                if (shape < controlStrSuit.Length)
                {
                    yield break;
                }
                yield return controlStrSuit + new string('x', (int)shape - controlStrSuit.Length);
            }
        }

        private bool Match(string[] hand1, string[] hand2)
        {
            var relevantCards = new[] { 'A', 'K', 'Q' };
            foreach (var suit in Enumerable.Range(0, 4))
            {
                foreach (var c in relevantCards)
                {
                    if (hand1[suit].Contains(c) && hand2[suit].Contains(c))
                        return false;
                }
            }
            return true;
        }
    }
}
