using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Tosr
{
    public partial class Auction : UserControl
    {
        public Player currentPlayer;
        private int currentBiddingRound;
        private readonly Dictionary<int, Dictionary<Player, Bid>> bids = new Dictionary<int, Dictionary<Player, Bid>>();

        public string GetBids(Player player)
        {
            return bids.Aggregate(string.Empty, (current, biddingRound) => current + biddingRound.Value[player]);
        }


        public Auction()
        {
            InitializeComponent();
            Clear();
        }


        public void Clear()
        {
            bids.Clear();
            currentPlayer = Player.West;
            currentBiddingRound = 1;
            ReDraw();
        }

        public void AddBid(Bid bid)
        {
            if (!bids.ContainsKey(currentBiddingRound))
            {
                bids[currentBiddingRound] = new Dictionary<Player, Bid>();
            }
            bids[currentBiddingRound][currentPlayer] = bid;
            ReDraw();

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

        private void ReDraw()
        {
            textBox1.Clear();
            textBox1.AppendText("West\tNorth\tEast\tSouth");

            foreach (var biddingRound in bids)
            {
                textBox1.AppendText(Environment.NewLine);
                string strBiddingRound = string.Empty;
                foreach (var bid in biddingRound.Value)
                {
                    strBiddingRound += bid.Value;
                    strBiddingRound += '\t';
                }
                textBox1.AppendText(strBiddingRound);
            }
        }


    }
}
