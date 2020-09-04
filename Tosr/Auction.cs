using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Common;

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
                        richTextBox1.AppendText(Common.Common.GetSuitDescription(bid.Value.suit), Color.Red);
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
}
