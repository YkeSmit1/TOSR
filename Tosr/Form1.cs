using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Common;

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

        private HandsNorthSouth hand;
        private HandsNorthSouth[] hands;
        private readonly ShuffleRestrictions shuffleRestrictionsSouth = new ShuffleRestrictions();
        private readonly ShuffleRestrictions shuffleRestrictionsNorth = new ShuffleRestrictions();

        private readonly BidManager bidManager;

        ReverseDictionaries reverseDictionaries;

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
            Pinvoke.Setup("Tosr.db3");
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;

            reverseDictionaries = new ReverseDictionaries("txt\\AuctionsByShape.txt", "txt\\AuctionsByControls.txt", 
                "txt\\AuctionsByControlsOnly.txt", "txt\\AuctionsByControlsScanning.txt", fasesWithOffset);
            bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, true);
            bidManager.Init(auctionControl.auction);
            shuffleRestrictionsSouth.SetControls(2, 12);
            shuffleRestrictionsNorth.SetHcp(16, 37);

            Shuffle();
            BidTillSouth(auctionControl.auction, biddingState);
        }

        private void ShowBiddingBox()
        {
            void handler(object x, EventArgs y)
            {
                var biddingBoxButton = (BiddingBoxButton)x;
                bidManager.SouthBid(biddingState, auctionControl.auction, hand.SouthHand);
                if (biddingBoxButton.bid != biddingState.CurrentBid)
                {
                    MessageBox.Show($"The correct bid is {biddingState.CurrentBid}. Description: {biddingState.CurrentBid.description}.", "Incorrect bid");
                }

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
                Width = 220,
                Height = 200
            };
            auctionControl.Show();
        }

        private void BidTillSouth(Auction auction, BiddingState biddingState)
        {
            // West
            auction.AddBid(Bid.PassBid);

            // North
            bidManager.NorthBid(biddingState, auction, hand.NorthHand);
            auction.AddBid(biddingState.CurrentBid);

            // East
            auction.AddBid(Bid.PassBid);

            auctionControl.ReDraw();
            biddingBox.UpdateButtons(biddingState.CurrentBid, auctionControl.auction.currentPlayer);
            if (biddingState.EndOfBidding)
                panelNorth.Visible = true;
        }

        private void ButtonShuffleClick(object sender, EventArgs e)
        {
            Shuffle();
            biddingState.Init();
            bidManager.Init(auctionControl.auction);
            Clear();
            BidTillSouth(auctionControl.auction, biddingState);
            biddingBox.Enabled = true;
        }

        private void Shuffle()
        {
            IOrderedEnumerable<CardDto> cardsSouth;
            IOrderedEnumerable<CardDto> cardsNorth;

            do
            {
                (hand, cardsSouth, cardsNorth) = ShuffleRandomHand();
            }
            while
                (!shuffleRestrictionsSouth.Match(hand.SouthHand) || !shuffleRestrictionsNorth.Match(hand.NorthHand));

            ShowHand(cardsNorth, panelNorth);
            panelNorth.Visible = false;
            ShowHand(cardsSouth, panelSouth);
        }

        private void ShowHand(IOrderedEnumerable<CardDto> cards, Panel parent)
        {
            parent.Controls.OfType<Card>().ToList().ForEach((card) =>
            {
                parent.Controls.Remove(card);
                card.Dispose();
            });
            var left = 20 * 12;
            foreach (var cardDto in cards.Reverse())
            {
                var card = new Card(cardDto.Face, cardDto.Suit, Back.Crosshatch, false, false)
                {
                    Left = left,
                    Parent = parent
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
            try
            {
                Clear();
                auctionControl.auction = bidManager.GetAuction(hand.NorthHand, hand.SouthHand);
                auctionControl.ReDraw();
                biddingBox.Enabled = false;
                panelNorth.Visible = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error");
            }
        }

        private void ButtonBatchBiddingClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                panelNorth.Visible = false;
                BatchBidding batchBidding = new BatchBidding(reverseDictionaries, fasesWithOffset);
                batchBidding.Execute(hands);
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
            localshuffleRestrictions.SetControls(2, 12);

            for (int i = 0; i < batchSize; ++i)
            {
                do
                    (hands[i], _, _) = ShuffleRandomHand();
                while
                    (!localshuffleRestrictions.Match(hands[i].SouthHand) || Util.GetHcpCount(hands[i].NorthHand) < 16);
            }

            return hands;
        }

        private (HandsNorthSouth, IOrderedEnumerable<CardDto>, IOrderedEnumerable<CardDto>) ShuffleRandomHand()
        {
            var cards = Shuffling.FisherYates(26).ToList();
            var orderedCardsNorth = cards.Take(13).OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
            var orderedCardsSouth = cards.Skip(13).Take(13).ToList().OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());

            var handsNorthSouth = new HandsNorthSouth
            {
                NorthHand = Util.GetDeckAsString(orderedCardsNorth),
                SouthHand = Util.GetDeckAsString(orderedCardsSouth)
            };

            return (handsNorthSouth, orderedCardsSouth, orderedCardsNorth);
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
            using ShuffleRestrictionsForm shuffleRestrictionsForm = new ShuffleRestrictionsForm(shuffleRestrictionsSouth);
            shuffleRestrictionsForm.ShowDialog();
        }

        private void ToolStripMenuItem11Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Pinvoke.Setup(openFileDialog1.FileName);
            }
        }

        private void ViewAuctionClick(object sender, EventArgs e)
        {
            var stringBuilder = new StringBuilder();
            foreach (var bid in auctionControl.auction.GetBids(Player.South))
            {
                stringBuilder.AppendLine($"{bid} {bid.description} ");
            }
            MessageBox.Show(stringBuilder.ToString(), "Auction");
        }
    }
}