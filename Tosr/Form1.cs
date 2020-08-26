using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Tosr
{
    public struct HandsNorthSouth
    {
        public string NorthHand;
        public string SouthHand;
    }
    public partial class Form1 : Form
    {
        private BiddingBox biddingBox;
        private AuctionControl auctionControl;
        private List<CardDto> unOrderedCards;

        private HandsNorthSouth[] hands;
        private readonly ShuffleRestrictions shuffleRestrictions = new ShuffleRestrictions();
        private string handsString;
        private readonly IBidGenerator bidGenerator = new BidGeneratorDescription();
        private readonly IPinvoke pinvoke = new Pinvoke();
        private readonly Dictionary<string, string> auctionsShape;
        private readonly Dictionary<string, List<string>> auctionsControls;
        private readonly static Dictionary<Fase, bool> fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
        private readonly BiddingState biddingState = new BiddingState(fasesWithOffset);

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
            pinvoke.Setup("Tosr.db3");
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;

            auctionsShape = LoadAuctions<string>("AuctionsByShape.txt", () => new BatchBidding().GenerateAuctionsForShape());
            auctionsControls = LoadAuctions<List<string>>("AuctionsByControls.txt", () => new BatchBidding().GenerateAuctionsForControls());
        }

        public Dictionary<string, T> LoadAuctions<T>(string fileName, Func<Dictionary<string, T>> generateAuctions)
        {
            Dictionary < string, T> auctions;
            if (File.Exists(fileName))
            {
                auctions = JsonConvert.DeserializeObject< Dictionary<string, T>>(File.ReadAllText(fileName));
            }
            else
            {
                auctions = generateAuctions();
                var sortedAuctions = auctions.ToImmutableSortedDictionary();
                File.WriteAllText(fileName, JsonConvert.SerializeObject(sortedAuctions, Formatting.Indented));
            }
            return auctions;
        }

        private void ShowBiddingBox()
        {
            void handler(object x, EventArgs y)
            {
                var biddingBoxButton = (BiddingBoxButton)x;
                BidManager.SouthBid(biddingState, handsString, bidGenerator, pinvoke);
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
                handsString = ShuffleRandomHand().SouthHand;
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
            auctionControl.auction = BidManager.GetAuction(handsString, bidGenerator, pinvoke, fasesWithOffset);
            auctionControl.ReDraw();
            biddingBox.Enabled = false;
        }

        private void ButtonBatchBiddingClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                BatchBidding batchBidding = new BatchBidding();
                batchBidding.Execute(hands, auctionsShape, auctionsControls);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private HandsNorthSouth[] GenerateHandStrings(int batchSize)
        {
            hands = new HandsNorthSouth[batchSize];
            var localshuffleRestrictions = new ShuffleRestrictions();

            for (int i = 0; i < batchSize; ++i)
            {
                int hcp;
                do
                {
                    hands[i] = ShuffleRandomHand();
                    var northHand = hands[i].NorthHand;
                    hcp = northHand.Count(x => x == 'A') * 4 + northHand.Count(x => x == 'K') * 3 + northHand.Count(x => x == 'Q') * 2 + northHand.Count(x => x == 'J');
                }
                while
                    (!localshuffleRestrictions.Match(hands[i].SouthHand) || hcp < 16);
            }

            return hands;
        }

        private HandsNorthSouth ShuffleRandomHand()
        {
            var handsNorthSouth = new HandsNorthSouth();
            var cards = Shuffling.FisherYates(26).ToList();

            var orderedCardsNorth = cards.Take(13).OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
            handsNorthSouth.NorthHand = Common.GetDeckAsString(orderedCardsNorth);

            unOrderedCards = cards.Skip(13).Take(13).ToList();
            var orderedCardsSouth = unOrderedCards.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
            handsNorthSouth.SouthHand = Common.GetDeckAsString(orderedCardsSouth);

            return handsNorthSouth;
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

        private void ToolStripMenuItem11Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pinvoke.Setup(openFileDialog1.FileName);
            }
        }
    }
}