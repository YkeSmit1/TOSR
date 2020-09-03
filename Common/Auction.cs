using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tosr
{
    public class Auction
    {
        public Player currentPlayer;
        private int currentBiddingRound;
        public readonly Dictionary<int, Dictionary<Player, Bid>> bids = new Dictionary<int, Dictionary<Player, Bid>>();
        public Bid currentContract = Bid.PassBid;

        public Player GetDeclarer(Suit suit)
        {
            foreach (var biddingRoud in bids.Values)
            {
                foreach (var bid in biddingRoud)
                {
                    if (bid.Value.bidType == BidType.bid && bid.Value.suit == suit)
                        return bid.Key;
                }
            }
            return Player.UnKnown;
        }

        public void AddBid(Bid bid)
        {
            if (!bids.ContainsKey(currentBiddingRound))
            {
                bids[currentBiddingRound] = new Dictionary<Player, Bid>();
            }
            bids[currentBiddingRound][currentPlayer] = bid;

            if (currentPlayer == Player.South)
            {
                currentPlayer = Player.West;
                ++currentBiddingRound;
            }
            else
            {
                ++currentPlayer;
            }
            if (bid.bidType == BidType.bid)
            {
                currentContract = bid;
            }
        }

        public void Clear()
        {
            bids.Clear();
            currentPlayer = Player.West;
            currentBiddingRound = 1;
        }

        public string GetBidsAsString(Player player)
        {
            return bids.Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[player]);
        }

        public string GetBidsAsString(Player player, Func<KeyValuePair<int, Dictionary<Player, Bid>>, bool> predicate)
        {
            return bids.Where(predicate).Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[player]);
        }

        public IEnumerable<Bid> GetBids(Player player, Func<KeyValuePair<int, Dictionary<Player, Bid>>, bool> predicate)
        {
            return bids.Where(predicate).Select(x => x.Value[player]);
        }
    }
}
