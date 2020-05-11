using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Tosr
{
    public partial class Form1 : Form
    {
        readonly BiddingBox biddingBox;
        private readonly Auction auction;
        private Card[] unOrderedCards;
        private string handsString;

        private readonly Dictionary<string, List<string>> auctionPerHand = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<string>> handPerauction = new Dictionary<string, List<string>>();

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
            biddingBox.Left = 40;
            biddingBox.Top = 130;
            biddingBox.Show();

            auction = new Auction
            {
                Parent = this,
                Left = 350,
                Top = 150,
                Width = 200,
                Height = 100
            };
            auction.Show();
            Shuffle();
        }

        private void buttonShuffleClick(object sender, EventArgs e)
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
                card.Top = 20;
                card.Parent = this;
                card.Show();
                left -= 20;
            }
        }

        private void buttonClearAuctionClick(object sender, EventArgs e)
        {
            auction.Clear();
            biddingBox.Clear();
        }

        private void GetAuctionFromRules()
        {
            var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new Card.FaceComparer());
            handsString = GetDeckAsString(orderedCards);
            if (handsString.Length != 16)
                return;
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
                        bidId = Pinvoke.GetBidFromRule(faseId, handsString, lastBidId);
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

        private void buttonGetAuctionClick(object sender, EventArgs e)
        {
            GetAuctionFromRules();
        }

        private void buttonBatchBiddingClick(object sender, EventArgs e)
        {
            try
            {
                listBox1.Items.Clear();
                for (int i = (int) (numericUpDown1.Value - 1); i >= 0; i--)
                {
                    Shuffle();
                    GetAuctionFromRules();
                    var bids = auction.GetBids(Player.South);
                    AddHandAndAuction(handsString, bids.Substring(0, bids.Length - 4));
                }

                File.WriteAllText("HandPerAuction.txt", new JavaScriptSerializer().Serialize(handPerauction));
                File.WriteAllText("AuctionPerHand.txt", new JavaScriptSerializer().Serialize(auctionPerHand));

                MessageBox.Show("Done");
            }
            catch (Exception exception)
            {
                listBox1.Items.Add(exception.Message);
            }
        }

        private void AddHandAndAuction(string strHand, string strAuction)
        {
            var str = string.Join("", strHand.Split(',').Select(x => x.Length));

            if (!auctionPerHand.ContainsKey(str))
                auctionPerHand[str] = new List<string>();
            if (!auctionPerHand[str].Contains(strAuction))
                auctionPerHand[str].Add(strAuction);

            if (!handPerauction.ContainsKey(strAuction))
                handPerauction[strAuction] = new List<string>();
            if (!handPerauction[strAuction].Contains(str))
                handPerauction[strAuction].Add(str);
        }
    }
}
