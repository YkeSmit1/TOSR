using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using Common;

namespace Tosr
{
    public partial class Form1 : Form
    {
        private BiddingBox biddingBox;
        private AuctionControl auctionControl;

        private string[] deal;
        private readonly ShuffleRestrictions shuffleRestrictionsSouth = new ShuffleRestrictions();
        private readonly ShuffleRestrictions shuffleRestrictionsNorth = new ShuffleRestrictions();

        private BidManager bidManager;

        ReverseDictionaries reverseDictionaries;

        private readonly static Dictionary<Fase, bool> fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
        private readonly BiddingState biddingState = new BiddingState(fasesWithOffset);
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        private Pbn pbn = new Pbn();

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1LoadAsync(object sender, EventArgs e)
        {
            logger.Info("Starting program");
            ShowBiddingBox();
            ShowAuction();

            // Need to set in code because of a .net core bug
            numericUpDown1.Maximum = 100_000;
            numericUpDown1.Value = 1000;
            Pinvoke.Setup("Tosr.db3");
            logger.Info($"Initialized engine with database '{"Tosr.db3"}'");
            openFileDialog1.InitialDirectory = Environment.CurrentDirectory;

            toolStripStatusLabel1.Text = "Generating reverse dictionaries...";
            await Task.Run(() =>
            {
                var progress = new Progress<string>(report => toolStripStatusLabel1.Text = $"Generating dictionary {report}...");
                reverseDictionaries = new ReverseDictionaries(fasesWithOffset, progress);
            });
            toolStripStatusLabel1.Text = "Generating reverse dictionaries done";

            bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, true);
            bidManager.Init(auctionControl.auction);
            shuffleRestrictionsSouth.SetControls(2, 12);
            shuffleRestrictionsNorth.SetHcp(16, 37);

            Shuffle();
            BidTillSouth(auctionControl.auction, biddingState);
            resetEvent.Set();
        }

        private void ShowBiddingBox()
        {
            void handler(object x, EventArgs y)
            {
                var biddingBoxButton = (BiddingBoxButton)x;
                // TODO consider to use the solution proposed in
                // https://stackoverflow.com/questions/981776/using-an-enum-as-an-array-index-in-c-sharp Ian Goldby solution
                bidManager.SouthBid(biddingState, auctionControl.auction, deal[(int)Player.South]);
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
            bidManager.NorthBid(biddingState, auction, deal[(int)Player.North]);
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
            Dictionary<Player, IOrderedEnumerable<CardDto>> cards;

            do
            {
                (deal, cards) = ShuffleRandomHand();
            }
            while
                (!shuffleRestrictionsSouth.Match(deal[(int)Player.South]) || !shuffleRestrictionsNorth.Match(deal[(int)Player.North]));

            ShowHand(cards[Player.North], panelNorth);
            panelNorth.Visible = false;
            ShowHand(cards[Player.South], panelSouth);
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
                _ = resetEvent.WaitOne();
                Clear();
                auctionControl.auction = bidManager.GetAuction(deal[(int)Player.North], deal[(int)Player.South]);
                auctionControl.ReDraw();
                biddingBox.Enabled = false;
                panelNorth.Visible = true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error");
            }
        }

        private async void ButtonBatchBiddingClickAsync(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                _ = resetEvent.WaitOne();
                Cursor.Current = Cursors.WaitCursor;
                panelNorth.Visible = false;
                BatchBidding batchBidding = new BatchBidding(reverseDictionaries, fasesWithOffset, toolStripMenuItemUseSolver.Checked);
                toolStripStatusLabel1.Text = "Batch bidding hands...";
                await Task.Run(() =>
                {
                    var progress = new Progress<int>(report => toolStripStatusLabel1.Text = $"Hands done: {report}");
                    pbn = batchBidding.Execute(pbn.Boards.Select(x => x.Deal), progress);
                });
                toolStripStatusLabel1.Text = "Batch bidding done";
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void GenerateHandStrings(int batchSize)
        {
            var localshuffleRestrictions = new ShuffleRestrictions();
            localshuffleRestrictions.SetControls(2, 12);
            pbn.Boards.Clear();

            for (int i = 0; i < batchSize; ++i)
            {
                string[] deal;
                do
                    (deal, _) = ShuffleRandomHand();
                while
                    (!localshuffleRestrictions.Match(deal[(int)Player.South]) || Util.GetHcpCount(deal[(int)Player.North]) < 16);
                pbn.Boards.Add(new BoardDto { Deal = deal });
            }
        }

        private (string[], Dictionary<Player, IOrderedEnumerable<CardDto>>) ShuffleRandomHand()
        {
            var cards = Shuffling.FisherYates(52).ToList();
            var hands = new string [4];
            var orderedCards = new Dictionary<Player, IOrderedEnumerable<CardDto>>();
            var offset = 0;
            foreach (var player in new[] { Player.West, Player.North, Player.East, Player.South})
            {
                var orderedCardForPlayer = cards.Skip(offset).Take(13).OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
                orderedCards.Add(player, orderedCardForPlayer);
                hands[(int)player] = Util.GetDeckAsString(orderedCardForPlayer);
                offset += 13;
            }
            return (hands, orderedCards);
        }

        private void ButtonGenerateHandsClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                GenerateHandStrings((int)numericUpDown1.Value);
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

        private void ToolStripMenuItemSaveSetClick(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                pbn.Save(saveFileDialog1.FileName);
        }

        private void ToolStripMenuItemLoadSetClick(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
                pbn.Load(openFileDialog2.FileName);
        }

        private void ToolStripMenuItemGoToBoardClick(object sender, EventArgs e)
        {
            if (pbn.Boards.Count() == 0)
            {
                MessageBox.Show("No PBN file is loaded.", "Error");
                return;
            }
            var goToBoardForm = new GoToBoardForm(pbn.Boards.Count());
            if (goToBoardForm.ShowDialog() == DialogResult.OK)
            {
                var board = pbn.Boards[goToBoardForm.BoardNumber - 1];
                deal = board.Deal;
                ShowHand(Player.North, panelNorth);
                panelNorth.Visible = true;
                ShowHand(Player.South, panelSouth);
                auctionControl.auction = board.Auction;
                auctionControl.ReDraw();
            }
        }

        private void ShowHand(Player player, Panel panel)
        {
            var cardDtos = Util.GetOrderdCards(deal[(int)player]);
            var orderedCards = cardDtos.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
            ShowHand(orderedCards, panel);
        }
    }
}