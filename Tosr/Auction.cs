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

        public AuctionControl()
        {
            InitializeComponent();
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
        }

        public void Clear()
        {
            bids.Clear();
            currentPlayer = Player.West;
            currentBiddingRound = 1;
        }

        public string GetBids(Player player)
        {
            return bids.Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[player]);
        }
    }
}
