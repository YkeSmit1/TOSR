using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Tosr
{
    public partial class AuctionControl : UserControl
    {
        public Auction auction = new Auction();
        private readonly ToolTip toolTip = new ToolTip();

        public AuctionControl()
        {
            InitializeComponent();
            void handler(object x, MouseEventArgs y)
            {
                var index = richTextBox1.GetCharIndexFromPosition(y.Location);
                var biddinground = richTextBox1.GetLineFromCharIndex(index);
                if (auction.bids.ContainsKey(biddinground))
                {
                    if (auction.bids[biddinground].ContainsKey(Player.South))
                    {
                        toolTip.Show(auction.bids[biddinground][Player.South].description, richTextBox1);
                    }
                }
            }
            richTextBox1.MouseMove += handler;
            auction.Clear();
            ReDraw();
        }

        public void AddBid(Bid bid)
        {
            auction.AddBid(bid);
            ReDraw();
        }

        public void ReDraw()
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("West\tNorth\tEast\tSouth");

            foreach (var biddingRound in auction.bids)
            {
                richTextBox1.AppendText(Environment.NewLine);
                string strBiddingRound = string.Empty;
                foreach (var bid in biddingRound.Value)
                {
                    if (bid.Value.suit == Suit.Hearts || bid.Value.suit == Suit.Diamonds)
                    {
                        richTextBox1.AppendText(bid.Value.rank.ToString());
                        richTextBox1.AppendText(Common.GetSuitDescription(bid.Value.suit), Color.Red);
                    }
                    else
                    {
                        richTextBox1.AppendText(bid.Value.ToString());
                    }
                    richTextBox1.AppendText("\t");
                }
            }
        }
    }

    public class Auction
    {
        public Player currentPlayer;
        private int currentBiddingRound;
        public readonly Dictionary<int, Dictionary<Player, Bid>> bids = new Dictionary<int, Dictionary<Player, Bid>>();
        public Bid currentContract = Bid.PassBid;

        internal Player GetDeclarer(Suit suit)
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
