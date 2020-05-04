using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Tosr
{
    public partial class Form1 : Form
    {
        readonly BiddingBox biddingBox;
        private readonly Auction auction;
        private Card[] unOrderedCards;
        public Form1()
        {
            InitializeComponent();
            // ReSharper disable once UseObjectOrCollectionInitializer
            biddingBox = new BiddingBox((x, y) =>
            {
                var biddingBoxButton = (BiddingBoxButton) x;
                auction.AddBid(biddingBoxButton.bid);
                biddingBox.UpdateButtons(biddingBoxButton.bid, auction.currentPlayer);
            });
            biddingBox.Parent = this;
            biddingBox.Top = 100;
            biddingBox.Show();

            auction = new Auction {Parent = this};
            auction.Left = 400;
            auction.Show();
            Shuffle();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Shuffle();
        }

        private void Shuffle()
        {
            foreach (var card in Controls.OfType<Card>())
            {
                card.Hide();
            }

            unOrderedCards = Shuffling.RandomizeDeck(13, Back.Crosshatch, false);
            var left = 20 * 13;
            foreach (var card in unOrderedCards.OrderBy(x => x.Suit, new Card.GuiSuitComparer())
                .ThenBy(c => c.Face, new Card.FaceComparer()).ToArray())
            {
                card.Left = left;
                left -= 20;
                card.Show();
                card.Parent = this;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            auction.Clear();
            biddingBox.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int faseId = 1;
            int lastBidId = 1;
            var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
            var bidId = Pinvoke.GetBidFromRule(faseId, GetDeckAsString(orderedCards), lastBidId);
            int suit;
            int rank;
            Pinvoke.GetBid(bidId, out rank, out suit);
            MessageBox.Show(rank + Common.GetSuitDescription((Suit)suit));
        }

        private void GetAuctionFromRules()
        {
            var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
            var deckAsString = GetDeckAsString(orderedCards);
            auction.Clear();

            int faseId = 1;
            int lastBidId = 1;
            int bidId = int.MaxValue;
            var currentPlayer = Player.West;
            Bid currentBid = Bid.PassBid;

            do
            {
                switch (currentPlayer)
                {
                    case Player.West:
                    case Player.East:
                        auction.AddBid(Bid.PassBid);
                        break;
                    case Player.North:
                        currentBid = Common.NextBid(currentBid);
                        auction.AddBid(currentBid);
                        lastBidId = Common.GetBidId(currentBid);
                        break;
                    case Player.South:
                        bidId = Pinvoke.GetBidFromRule(faseId, deckAsString, lastBidId);
                        currentBid = Common.GetBid(bidId);
                        auction.AddBid(currentBid);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (currentPlayer == Player.South)
                    currentPlayer = Player.West;
                else currentPlayer++;
            }
            while (bidId != 0);
        }

        private string GetDeckAsString(IEnumerable<Card> cards)
        {
            string res = string.Empty;
            var currentSuit = Suit.Spades;

            foreach (var card in cards)
            {
                if (card.Suit != currentSuit)
                {
                    res += ",";
                    currentSuit = card.Suit;
                }

                res += Common.GetFaceDescription(card.Face);
            }
            return res;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            GetAuctionFromRules();
        }
    }
}
