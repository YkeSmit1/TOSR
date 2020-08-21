using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Tosr
{
    public class BidManager
    {
        public static Auction GetAuction(string handsString, IBidGenerator bidGenerator, 
            Dictionary<Fase, bool> fasesWithOffset)
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
                        NorthBid(biddingState);
                        auction.AddBid(biddingState.currentBid);
                        break;
                    case Player.South:
                        SouthBid(biddingState, handsString, bidGenerator);
                        auction.AddBid(biddingState.currentBid);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                currentPlayer = NextPlayer(currentPlayer);
            }
            while (!biddingState.EndOfBidding);
            return auction;
        }
        private static Player NextPlayer(Player currentPlayer)
        {
            if (currentPlayer == Player.South)
                currentPlayer = Player.West;
            else currentPlayer++;
            return currentPlayer;
        }

        public static void NorthBid(BiddingState biddingState)
        {
            biddingState.currentBid = NextBid(biddingState.currentBid);
        }

        public static void SouthBid(BiddingState biddingState, string handsString, IBidGenerator bidGenerator)
        {
            var (bidIdFromRule, nextfase, description) = bidGenerator.GetBid(biddingState, handsString);
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, description);
        }

        public static Bid GetBid(int bidId)
        {
            return bidId == 0 ? Bid.PassBid : new Bid((bidId - 1) / 5 + 1, (Suit)((bidId - 1) % 5));
        }

        public static int GetBidId(Bid bid)
        {
            return ((bid.rank - 1) * 5) + (int)bid.suit + 1;
        }

        public static Bid NextBid(Bid bid)
        {
            if (bid == Bid.PassBid)
                return new Bid(1, Suit.Clubs);
            if (bid.suit == Suit.NoTrump)
                return new Bid(bid.rank + 1, Suit.Clubs);
            return new Bid(bid.rank, bid.suit + 1);
        }
    }
}
