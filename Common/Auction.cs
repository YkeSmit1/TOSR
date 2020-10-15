using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Common
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Auction
    {
        public Player currentPlayer;
        private int currentBiddingRound;
        public readonly Dictionary<int, Dictionary<Player, Bid>> bids = new Dictionary<int, Dictionary<Player, Bid>>();
        public Bid currentContract = Bid.PassBid;

        private string DebuggerDisplay
        {
            get { return string.Join(Environment.NewLine, GetBids(Player.North).Zip(GetBids(Player.South), (x, y) => $"{x}{y}")); }
        }

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
            return bids.Where(x => x.Value.ContainsKey(player)).Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[player]);
        }

        public string GetBidsAsString(Fase fase)
        {
            return GetBidsAsString(new Fase[] { fase });
        }

        public string GetBidsAsString(Fase[] fases)
        {
            const Player south = Player.South;
            return bids.Where(x => x.Value.ContainsKey(south) && fases.Contains(x.Value[south].fase)).
                Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[south]);
        }

        public IEnumerable<Bid> GetBids(Player player)
        {
            return bids.Where(x => x.Value.ContainsKey(player)).Select(x => x.Value[player]);
        }

        public IEnumerable<Bid> GetBids(Player player, Fase fase)
        {
            return GetBids(player, new Fase[] { fase});
        }

        public IEnumerable<Bid> GetBids(Player player, Fase[] fases)
        {
            return bids.Where(x => x.Value.ContainsKey(player) && fases.Contains(x.Value[player].fase)).Select(x => x.Value[player]);
        }

        public void SetBids(Player player, IEnumerable<Bid> newBids)
        {
            bids.Clear();
            var biddingRound = 1;
            foreach (var bid in newBids)
            {
                bids[biddingRound] = new Dictionary<Player, Bid>(new List<KeyValuePair<Player, Bid>> { new KeyValuePair<Player, Bid>(player, bid) });
                biddingRound++;
            }
        }
    }
}
