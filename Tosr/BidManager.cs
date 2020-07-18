using System;
using System.Text;
using System.Linq;

namespace Tosr
{
    public class BidManager
    {
        public static void GetAuction(string handsString, Auction auction, bool requestDescription, IBidGenerator bidGenerator)
        {
            auction.Clear();

            BiddingState biddingState = new BiddingState();
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
                        SouthBid(biddingState, handsString, requestDescription, bidGenerator);
                        auction.AddBid(biddingState.currentBid);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                currentPlayer = NextPlayer(currentPlayer);
            }
            while (biddingState.bidId != 0);
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
            biddingState.lastBidId = GetBidId(biddingState.currentBid);
        }

        public static void SouthBid(BiddingState biddingState, string handsString, bool requestDescription, IBidGenerator bidGenerator)
        {
            var (bidFromRule, nextfase, description) = bidGenerator.GetBid(biddingState, handsString, requestDescription);
            biddingState.bidId = bidFromRule + biddingState.relayBidIdLastFase;
            if (bidFromRule == 0)
            {
                biddingState.currentBid = Bid.PassBid;
                biddingState.bidId = 0;
                return;
            }
            if (nextfase != biddingState.fase)
            {
                biddingState.relayBidIdLastFase = biddingState.bidId + 1;
                biddingState.fase = nextfase;
            }

            var currentBid = GetBid(biddingState.bidId);
            currentBid.description = requestDescription ? description.ToString() : string.Empty;
            biddingState.currentBid = currentBid;
        }

        public static (int, Fase, string) GetBid(BiddingState biddingState, string handsString, bool requestDescription)
        {
            var description = new StringBuilder(128);
            var bidFromRule = requestDescription ?
                    Pinvoke.GetBidFromRuleEx(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out var nextfase, description) :
                    Pinvoke.GetBidFromRule(biddingState.fase, handsString, biddingState.lastBidId - biddingState.relayBidIdLastFase, out nextfase);
            return (bidFromRule, nextfase, description.ToString());
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
