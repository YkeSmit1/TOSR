using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tosr
{
    public partial class Form1 : Form
    {
        private BiddingBox biddingBox;
        private AuctionControl auctionControl;
        private IEnumerable<CardDto> unOrderedCards;

        private Tuple<string, string>[] hands;
        private readonly ShuffleRestrictions shuffleRestrictions = new ShuffleRestrictions();
        private string handsString;
        private readonly BiddingState biddingState = new BiddingState();
        private IBidGenerator bidGenerator = new BidGenerator();

        public Form1()
        {
            InitializeComponent();
            ShowBiddingBox();
            ShowAuction();

            // Need to set in code because of a .net core bug
            numericUpDown1.Maximum = 100_000;
            numericUpDown1.Value = 1000;
            Shuffle();
            BidTillSouth(auctionControl.auction, biddingState);
        }

        private void ShowBiddingBox()
        {
            void handler(object x, EventArgs y)
            {
                var biddingBoxButton = (BiddingBoxButton)x;
                BidManager.SouthBid(biddingState, handsString, true, bidGenerator);
                if (biddingBoxButton.bid != biddingState.currentBid)
                {
                    MessageBox.Show($"The correct bid is {biddingState.currentBid}. Description: {biddingState.currentBid.description}.", "Incorrect bid");
                }

                auctionControl.AddBid(biddingState.currentBid);
                BidTillSouth(auctionControl.auction, biddingState);
            }
            biddingBox = new BiddingBox(handler)
            {
                Parent = this,
                Left = 50,
                Top = 200
            };
            biddingBox.Show();
        }

        private void ShowAuction()
        {
            auctionControl = new AuctionControl
            {
                Parent = this,
                Left = 300,
                Top = 200,
                Width = 205,
                Height = 200
            };
            auctionControl.Show();
        }

        private void BidTillSouth(Auction auction, BiddingState biddingState)
        {
            // West
            auction.AddBid(Bid.PassBid);
            // North
            BidManager.NorthBid(biddingState);
            auction.AddBid(biddingState.currentBid);
            // East
            auction.AddBid(Bid.PassBid);

            auctionControl.ReDraw();
            biddingBox.UpdateButtons(biddingState.currentBid, auctionControl.auction.currentPlayer);
        }

        private void ButtonShuffleClick(object sender, EventArgs e)
        {
            Shuffle();
            biddingState.Init();
            Clear();
            BidTillSouth(auctionControl.auction, biddingState);
            biddingBox.Enabled = true;
        }

        private void Shuffle()
        {
            foreach (var card in Controls.OfType<Card>())
            {
                card.Hide();
            }

            do
                handsString = ShuffleRandomHand().Item1;
            while
                (!shuffleRestrictions.Match(handsString));

            var left = 20 * 13;
            var cardDtos = unOrderedCards.OrderBy(x => x.Suit, new GuiSuitComparer()).ThenBy(c => c.Face, new FaceComparer()).ToArray();
            foreach (var cardDto in cardDtos)
            {
                var card = new Card(cardDto.Face, cardDto.Suit, Back.Crosshatch, false, false)
                {
                    Left = left,
                    Top = 80,
                    Parent = this
                };
                card.Show();
                left -= 20;
            }
        }

        private void Clear()
        {
            auctionControl.auction.Clear();
            auctionControl.ReDraw();
            biddingBox.Clear();
        }

        private void ButtonGetAuctionClick(object sender, EventArgs e)
        {
            Clear();
            var orderedCards = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
            var handsString = Common.GetDeckAsString(orderedCards);
            BidManager.GetAuction(handsString, auctionControl.auction, true, bidGenerator);
            auctionControl.ReDraw();
            biddingBox.Enabled = false;
        }

        private void ButtonBatchBiddingClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                BatchBidding batchBidding = new BatchBidding(bidGenerator);
                batchBidding.Execute(hands);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private Tuple<string, string>[] GenerateHandStrings(int batchSize)
        {
            hands = new Tuple<string, string>[batchSize];
            var localshuffleRestrictions = new ShuffleRestrictions();

            for (int i = 0; i < batchSize; ++i)
            {
                do
                    hands[i] = ShuffleRandomHand();
                while
                    (!localshuffleRestrictions.Match(hands[i].Item1));
            }

            return hands;
        }

        private Tuple<string, string> ShuffleRandomHand()
        {
            var cards = Shuffling.FisherYates(26);

            var orderedCardsNorth = cards.Take(13).OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
            var handNorth = Common.GetDeckAsString(orderedCardsNorth);

            var orderedCardsSouth = cards.Skip(13).Take(13).OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
            var handSouth = Common.GetDeckAsString(orderedCardsNorth);

            unOrderedCards = cards.Take(13);

            return new Tuple<string, string>(handNorth, handSouth);
        }

        private void ButtonGenerateHandsClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                hands = GenerateHandStrings((int)numericUpDown1.Value);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void ToolStripButton4Click(object sender, EventArgs e)
        {
            using ShuffleRestrictionsForm shuffleRestrictionsForm = new ShuffleRestrictionsForm(shuffleRestrictions);
            shuffleRestrictionsForm.ShowDialog();
        }
    }
}