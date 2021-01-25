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
using Solver;
using Tosr.Properties;

namespace Tosr
{
    public partial class Form1 : Form
    {
        private BiddingBox biddingBox;
        private AuctionControl auctionControl;

        private string[] deal;
        private ShufflingDeal shufflingDeal = new ShufflingDeal() { NrOfHands = 1};

        private BidManager bidManager;

        ReverseDictionaries reverseDictionaries;

        private readonly static Dictionary<Fase, bool> fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
        private readonly BiddingState biddingState = new BiddingState(fasesWithOffset);
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        private Pbn pbn = new Pbn();
        private int boardNumber;
        private string pbnFilepath;
        private CancellationTokenSource cancelBatchbidding = new CancellationTokenSource();

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

            shufflingDeal.North = new North { Hcp = new MinMax(16, 37) };
            shufflingDeal.South = new South { Hcp = new MinMax(8, 37), Controls = new MinMax(2, 12) };

            // Load user settings
            toolStripMenuItemUseSolver.Checked = Settings.Default.useSolver;
            numericUpDown1.Value = Settings.Default.numberOfHandsToBid;
            if (File.Exists(Settings.Default.pbnFilePath))
            {
                pbnFilepath = Settings.Default.pbnFilePath;
                pbn.Load(pbnFilepath);
                boardNumber = Math.Min(Settings.Default.boardNumber, pbn.Boards.Count());
                LoadCurrentBoard();
            }
            if (pbn.Boards.Count == 0)
            {
                pbnFilepath = "";
                Shuffle();
                StartBidding();
            }

            toolStripStatusLabel1.Text = "Generating reverse dictionaries...";
            await Task.Run(() =>
            {
                var progress = new Progress<string>(report => toolStripStatusLabel1.Text = $"Generating dictionary {report}...");
                reverseDictionaries = new ReverseDictionaries(fasesWithOffset, progress);
                bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, true);
                bidManager.Init(auctionControl.auction);
                resetEvent.Set();
            });
        }

        private void ShowBiddingBox()
        {
            void handler(object x, EventArgs y)
            {
                _ = resetEvent.WaitOne();
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
            StartBidding();
            _ = resetEvent.WaitOne();
            bidManager.Init(auctionControl.auction);
        }

        private void StartBidding()
        {
            biddingState.Init();

            auctionControl.auction.Clear();
            auctionControl.auction.AddBid(Bid.PassBid);
            auctionControl.auction.AddBid(Bid.OneClub);
            auctionControl.auction.AddBid(Bid.PassBid);
            auctionControl.ReDraw();

            biddingBox.Clear();
            biddingBox.UpdateButtons(Bid.OneClub, auctionControl.auction.currentPlayer);
            biddingBox.Enabled = true;
        }

        private void Shuffle()
        {
            do
            {
                var board = shufflingDeal.Execute().First();
                deal = Util.GetBoardsTosr(board);
            } 
            while (Util.IsFreakHand(deal[(int)Player.South].Split(',').Select(x => x.Length)));
            ShowHand(deal[(int)Player.North], panelNorth);
            panelNorth.Visible = false;
            ShowHand(deal[(int)Player.South], panelSouth);
        }

        private void ShowHand(string hand, Panel parent)
        {
            parent.Controls.OfType<PictureBox>().ToList().ForEach((card) =>
            {
                parent.Controls.Remove(card);
                card.Dispose();
            });
            var suits = hand.Split(',');
            var suit = Suit.Clubs;
            var left = 20 * 12;
            foreach (var suitStr in suits.Reverse())
            {
                foreach (var card in suitStr.Reverse())
                {
                    var pictureBox = new PictureBox
                    {
                        Image = CardControl.GetFaceImageForCard(suit, Util.GetFaceFromDescription(card)),
                        Left = left,
                        Parent = parent,
                        Height = 97,
                        Width = 73,
                        SizeMode = PictureBoxSizeMode.StretchImage,
                    };
                    pictureBox.Show();
                    left -= 20;
                }
                suit++;
            }
        }

        private void ButtonGetAuctionClick(object sender, EventArgs e)
        {
            try
            {
                _ = resetEvent.WaitOne();
                auctionControl.auction = bidManager.GetAuction(deal[(int)Player.North], deal[(int)Player.South]);
                auctionControl.ReDraw();
                biddingBox.Clear();
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
                cancelBatchbidding.Dispose();
                cancelBatchbidding = new CancellationTokenSource();
                await Task.Run(() =>
                {
                    var progress = new Progress<int>(report => toolStripStatusLabel1.Text = $"Hands done: {report}");
                    pbn = batchBidding.Execute(pbn.Boards.Select(x => x.Deal), progress, cancelBatchbidding.Token);
                });
                pbnFilepath = "";
                boardNumber = 1;
                LoadCurrentBoard();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void GenerateBoards(int batchSize)
        {
            var shufflingDeal = new ShufflingDeal() { NrOfHands = batchSize, 
                North = new North { Hcp = new MinMax(16, 37) }, 
                South = new South { Hcp = new MinMax(8, 37), Controls = new MinMax(2, 12) } };

            var boards = shufflingDeal.Execute();
            pbn.Boards = boards.Select(board => new BoardDto { Deal = Util.GetBoardsTosr(board) }).ToList();
        }

        private void ButtonGenerateHandsClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                GenerateBoards((int)numericUpDown1.Value);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void ToolStripButton4Click(object sender, EventArgs e)
        {
            var localShufflingDeal = ObjectCloner.ObjectCloner.DeepClone(shufflingDeal);
            using var shuffleRestrictionsForm = new ShuffleRestrictionsForm(localShufflingDeal);
            if (shuffleRestrictionsForm.ShowDialog() == DialogResult.OK)
                shufflingDeal = localShufflingDeal;
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
            {
                pbnFilepath = saveFileDialog1.FileName;
                pbn.Save(pbnFilepath);
                Text = $"{Path.GetFileName(pbnFilepath)} Board: {boardNumber} from {pbn.Boards.Count}";
            }
        }

        private void ToolStripMenuItemLoadSetClick(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                pbnFilepath = openFileDialog2.FileName;
                pbn.Load(pbnFilepath);
                boardNumber = 1;
                LoadCurrentBoard();
            }
        }

        private void ToolStripMenuItemOneBoardClick(object sender, EventArgs e)
        {
            if (int.TryParse(toolStripTextBoxBoard.Text, out var board) && board <= pbn.Boards.Count - 1)
            {
                boardNumber = board;
                LoadCurrentBoard();

                var batchBidding = new BatchBidding(reverseDictionaries, fasesWithOffset, true);
                var localPbn = batchBidding.Execute(new[] { pbn.Boards[boardNumber - 1].Deal}, new Progress<int>(), CancellationToken.None);
                auctionControl.auction = localPbn.Boards.First().Auction ?? new Auction();
                auctionControl.ReDraw();
                toolStripStatusLabel1.Text = localPbn.Boards.First().Description;
            }
        }

        private void ToolStripMenuItemBidAgainClick(object sender, EventArgs e)
        {
            StartBidding();
            _ = resetEvent.WaitOne();
            bidManager.Init(auctionControl.auction);
        }

        private void ToolStripButtonFirstClick(object sender, EventArgs e)
        {
            boardNumber = 1;
            LoadCurrentBoard();
        }

        private void ToolStripButtonLastClick(object sender, EventArgs e)
        {
            boardNumber = pbn.Boards.Count;
            LoadCurrentBoard();
        }

        private void ToolStripButtonNextClick(object sender, EventArgs e)
        {
            if (boardNumber < pbn.Boards.Count)
            {
                boardNumber++;
                LoadCurrentBoard();
            }
        }

        private void ToolStripButtonPreviousClick(object sender, EventArgs e)
        {
            if (boardNumber > 1)
            {
                boardNumber--;
                LoadCurrentBoard();
            }
        }

        private void ToolStripTextBoxBoardLeave(object sender, EventArgs e)
        {
            if (int.TryParse(toolStripTextBoxBoard.Text, out var board) && board <= pbn.Boards.Count - 1)
            {
                boardNumber = board;
                LoadCurrentBoard();
            }
        }

        private void LoadCurrentBoard()
        {
            if (pbn.Boards.Count() == 0)
            {
                MessageBox.Show("No valid PBN file is loaded.", "Error");
                return;
            }
            Text = $"{Path.GetFileName(pbnFilepath)} Board: {boardNumber} from {pbn.Boards.Count}";
            toolStripTextBoxBoard.Text = Convert.ToString(boardNumber);
            var board = pbn.Boards[boardNumber - 1];
            deal = board.Deal;
            ShowHand(board.Deal[(int)Player.North], panelNorth);
            panelNorth.Visible = true;
            ShowHand(board.Deal[(int)Player.South], panelSouth);
            auctionControl.auction = board.Auction ?? new Auction();
            auctionControl.ReDraw();
            toolStripStatusLabel1.Text = board.Description;
        }

        private void Form1Closed(object sender, FormClosedEventArgs e)
        {
            Settings.Default.useSolver = toolStripMenuItemUseSolver.Checked;
            Settings.Default.boardNumber = boardNumber;
            Settings.Default.numberOfHandsToBid = (int)numericUpDown1.Value;
            Settings.Default.pbnFilePath = pbn.Boards.Count() > 0 ? pbnFilepath : "";
            Settings.Default.Save();
        }

        private void ToolStripMenuItemAbortClick(object sender, EventArgs e)
        {
            cancelBatchbidding.Cancel();
        }
    }
}