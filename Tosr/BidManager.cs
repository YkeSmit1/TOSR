using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Common;

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
            biddingState.currentBid = Bid.NextBid(biddingState.currentBid);
        }

        public static void SouthBid(BiddingState biddingState, string handsString, IBidGenerator bidGenerator)
        {
            var (bidIdFromRule, nextfase, description) = bidGenerator.GetBid(biddingState, handsString);
            biddingState.UpdateBiddingState(bidIdFromRule, nextfase, description);
        }
    }
}
