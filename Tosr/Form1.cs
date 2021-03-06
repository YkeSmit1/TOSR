﻿using System;
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
using Common.Controls;
using BiddingLogic;

namespace Tosr
{
    public partial class Form1 : Form
    {
        private BiddingBox biddingBox;
        private AuctionControl auctionControl;

        private string[] deal;
        private ShufflingDeal shufflingDeal = new() { NrOfHands = 1 };

        private BidManager bidManager;
        private ReverseDictionaries reverseDictionaries;

        private static readonly Dictionary<Fase, bool> fasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Fase, bool>>(File.ReadAllText("FasesWithOffset.json"));
        private readonly BiddingState biddingState = new(fasesWithOffset);
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ManualResetEvent resetEvent = new(false);
        private Pbn pbn = new();
        private readonly Pbn interactivePbn = new();
        private readonly Pbn filteredPbn = new();
        private int boardIndex;
        private string pbnFilepath;
        private CancellationTokenSource cancelBatchbidding = new();

        private readonly string defaultSystemParameters = "BiddingLogic.SystemParameters.json";
        private readonly string defaultOptimizationParameters = "BiddingLogic.OptimizationParameters.json";

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1LoadAsync(object sender, EventArgs e)
        {
            logger.Info("Starting program");
            FillFilteredComboBox();
            ShowBiddingBox();
            ShowAuction();

            // Need to set in code because of a .net core bug
            numericUpDown1.Maximum = 100_000;
            numericUpDown1.Value = 1000;
            _ = Pinvoke.Setup("Tosr.db3");
            logger.Info($"Initialized engine with database '{"Tosr.db3"}'");
            openFileDialogDatabase.InitialDirectory = Environment.CurrentDirectory;

            shufflingDeal.North = new North { Hcp = new MinMax(16, 37) };
            shufflingDeal.South = new South { Hcp = new MinMax(8, 37), Controls = new MinMax(2, 12) };

            // Load user settings
            toolStripMenuItemUseSolver.Checked = Settings.Default.useSolver;
            toolStripMenuItemAlternateSuits.Checked = Settings.Default.alternateSuits;
            numericUpDown1.Value = Settings.Default.numberOfHandsToBid;
            UseSavedSystemParameters();
            UseSavedOptimizationParameters();
            if (File.Exists("interactive.pbn"))
                interactivePbn.Load("interactive.pbn"); ;
            if (File.Exists(Settings.Default.pbnFilePath))
            {
                try
                {
                    pbnFilepath = Settings.Default.pbnFilePath;
                    pbn.Load(pbnFilepath);
                    toolStripComboBoxFilter.SelectedItem = Settings.Default.filter;
                    ApplyFilter();
                    boardIndex = Math.Min(Settings.Default.boardNumber, filteredPbn.Boards.Count - 1);
                    LoadCurrentBoard();
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Error loading PBN file. {exception.Message}", "Error");
                }
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

        private void ApplyFilter()
        {
            filteredPbn.Boards = (string)toolStripComboBoxFilter.SelectedItem == "All" ? pbn.Boards :
                pbn.Boards.Where(b => b.Description != null && b.Description.Split(':')[0] == (string)toolStripComboBoxFilter.SelectedItem).ToList();
        }

        private void FillFilteredComboBox()
        {
            toolStripComboBoxFilter.Items.AddRange(Enum.GetNames(typeof(BatchBidding.CorrectnessContract)));
            toolStripComboBoxFilter.Items.Add("All");
        }

        private void UseSavedSystemParameters()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.systemParametersPath))
            {
                try
                {
                    BidManager.SetSystemParameters(File.ReadAllText(Settings.Default.systemParametersPath));
                    return;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Could not load previous system parameters file. Using the default system parameters instead. ({e.Message})");
                    Settings.Default.systemParametersPath = "";
                }
            }
            BidManager.SetSystemParameters(Util.ReadResource(defaultSystemParameters));
        }

        private void UseSavedOptimizationParameters()
        {
            if (!string.IsNullOrWhiteSpace(Settings.Default.optimizationParametersPath))
            {
                try
                {
                    BidManager.SetOptimizationParameters(File.ReadAllText(Settings.Default.optimizationParametersPath));
                    return;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Could not load previous optimization parameters file. Using the default optimization parameters instead. ({e.Message})");
                    Settings.Default.optimizationParametersPath = "";
                }
            }
            BidManager.SetOptimizationParameters(Util.ReadResource(defaultOptimizationParameters));
        }

        private void ShowBiddingBox()
        {
            void handler(object x, EventArgs y)
            {
                resetEvent.WaitOne();
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
            biddingBox.UpdateButtons(biddingState.CurrentBid, auctionControl.auction.CurrentPlayer);
            if (auction.IsEndOfBidding())
            {
                biddingBox.Enabled = false;
                panelNorth.Visible = true;
                AddBoardToInteractivePBNFile(deal, auction);
            }
        }

        private void AddBoardToInteractivePBNFile(string[] deal, Auction auction)
        {
            interactivePbn.Boards.Add(new BoardDto
            {
                Deal = ObjectCloner.ObjectCloner.DeepClone(deal),
                Auction = ObjectCloner.ObjectCloner.DeepClone(auction)
            });
            interactivePbn.Save("interactive.pbn");
        }

        private void ButtonShuffleClick(object sender, EventArgs e)
        {
            Shuffle();
            StartBidding();
            bidManager.Init(auctionControl.auction);
            resetEvent.WaitOne();
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
            biddingBox.UpdateButtons(Bid.OneClub, auctionControl.auction.CurrentPlayer);
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
            panelNorth.Visible = false;
            ShowBothHands();
        }

        private void ShowHand(string hand, Panel parent)
        {
            parent.Controls.OfType<PictureBox>().ToList().ForEach((card) =>
            {
                parent.Controls.Remove(card);
                card.Dispose();
            });
            var left = 20 * 12;
            var suitOrder = toolStripMenuItemAlternateSuits.Checked ?
                new List<Suit> { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds } :
                new List<Suit> { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
            var suits = hand.Split(',').Select((x, index) => (x, (Suit)(3 - index))).OrderByDescending(x => suitOrder.IndexOf(x.Item2));

            foreach (var suit in suits)
            {
                foreach (var card in suit.x.Reverse())
                {
                    var pictureBox = new PictureBox
                    {
                        Image = CardControl.GetFaceImageForCard(suit.Item2, Util.GetFaceFromDescription(card)),
                        Left = left,
                        Parent = parent,
                        Height = 97,
                        Width = 73,
                        SizeMode = PictureBoxSizeMode.StretchImage,
                    };
                    pictureBox.Show();
                    left -= 20;
                }
            }
        }

        private void ButtonGetAuctionClick(object sender, EventArgs e)
        {
            try
            {
                resetEvent.WaitOne();
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
                resetEvent.WaitOne();
                Cursor.Current = Cursors.WaitCursor;
                panelNorth.Visible = false;
                var batchBidding = new BatchBidding(reverseDictionaries, fasesWithOffset, toolStripMenuItemUseSolver.Checked);
                toolStripStatusLabel1.Text = "Batch bidding hands...";
                cancelBatchbidding.Dispose();
                cancelBatchbidding = new CancellationTokenSource();
                await Task.Run(() =>
                {
                    var progress = new Progress<int>(report => toolStripStatusLabel1.Text = $"Hands done: {report}");
                    pbn = batchBidding.Execute(pbn.Boards.Select(x => x.Deal), progress, cancelBatchbidding.Token);
                });
                pbnFilepath = "";
                ApplyFilter();
                boardIndex = 0;
                LoadCurrentBoard();
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }

        private void GenerateBoards(int batchSize)
        {
            var shufflingDeal = new ShufflingDeal()
            {
                NrOfHands = batchSize,
                North = new North { Hcp = new MinMax(16, 37) },
                South = new South { Hcp = new MinMax(8, 37), Controls = new MinMax(2, 12) }
            };

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
            try
            {
                if (openFileDialogDatabase.ShowDialog() == DialogResult.OK)
                    _ = Pinvoke.Setup(openFileDialogDatabase.FileName);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error opening database. {exception.Message}", "Error");
            }
        }

        private void ViewAuctionClick(object sender, EventArgs e)
        {
            MessageBox.Show(auctionControl.auction.GetPrettyAuction("\n"));
        }

        private void ToolStripMenuItemSaveSetClick(object sender, EventArgs e)
        {
            try
            {
                if (saveFileDialogPBN.ShowDialog() == DialogResult.OK)
                {
                    pbn.Save(saveFileDialogPBN.FileName);
                    pbnFilepath = saveFileDialogPBN.FileName;
                    Text = $"{Path.GetFileName(pbnFilepath)} Board: {boardIndex} from {pbn.Boards.Count}";
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error saving PBN file. {exception.Message}", "Error");
            }
        }

        private void ToolStripMenuItemLoadSetClick(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialogPBN.ShowDialog() == DialogResult.OK)
                {
                    pbn.Load(openFileDialogPBN.FileName);
                    pbnFilepath = openFileDialogPBN.FileName;
                    ApplyFilter();
                    boardIndex = 0;
                    LoadCurrentBoard();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error loading PBN file. {exception.Message}", "Error");
            }
        }

        private void ToolStripMenuItemOneBoardClick(object sender, EventArgs e)
        {
            if (int.TryParse(toolStripTextBoxBoard.Text, out var board) && board <= pbn.Boards.Count)
            {
                boardIndex = filteredPbn.Boards.IndexOf(filteredPbn.Boards.Single(b => b.BoardNumber == board));
                LoadCurrentBoard();

                resetEvent.WaitOne();
                var batchBidding = new BatchBidding(reverseDictionaries, fasesWithOffset, true);
                var localPbn = batchBidding.Execute(new[] { pbn.Boards[boardIndex].Deal }, new Progress<int>(), CancellationToken.None);
                auctionControl.auction = localPbn.Boards.First().Auction ?? new Auction();
                auctionControl.ReDraw();
                toolStripStatusLabel1.Text = localPbn.Boards.First().Description;
            }
        }

        private void ToolStripMenuItemBidAgainClick(object sender, EventArgs e)
        {
            StartBidding();
            bidManager.Init(auctionControl.auction);
            panelNorth.Visible = false;
            if (interactivePbn.Boards.Any())
                interactivePbn.Boards.RemoveAt(interactivePbn.Boards.Count - 1);
            toolStripStatusLabel1.Text = "";
            resetEvent.WaitOne();
        }

        private void ToolStripButtonFirstClick(object sender, EventArgs e)
        {
            boardIndex = 0;
            LoadCurrentBoard();
        }

        private void ToolStripButtonLastClick(object sender, EventArgs e)
        {
            boardIndex = filteredPbn.Boards.Count - 1;
            LoadCurrentBoard();
        }

        private void ToolStripButtonNextClick(object sender, EventArgs e)
        {
            if (boardIndex < filteredPbn.Boards.Count - 1)
            {
                boardIndex++;
                LoadCurrentBoard();
            }
        }

        private void ToolStripButtonPreviousClick(object sender, EventArgs e)
        {
            if (boardIndex > 0)
            {
                boardIndex--;
                LoadCurrentBoard();
            }
        }

        private void ToolStripTextBoxBoardLeave(object sender, EventArgs e)
        {
            if (int.TryParse(toolStripTextBoxBoard.Text, out var board) && filteredPbn.Boards.Any(b => b.BoardNumber == board))
            {
                boardIndex = filteredPbn.Boards.IndexOf(filteredPbn.Boards.Single(b => b.BoardNumber == board));
                LoadCurrentBoard();
            }
        }

        private void LoadCurrentBoard()
        {
            if (pbn.Boards.Count == 0)
            {
                MessageBox.Show("No valid PBN file is loaded.", "Error");
                return;
            }

            Text = $"{Path.GetFileName(pbnFilepath)} Number of boards in pbn: {pbn.Boards.Count}. Number of filtered boards: {filteredPbn.Boards.Count}";

            if (filteredPbn.Boards.Count > 0)
            {
                toolStripTextBoxBoard.Text = Convert.ToString(filteredPbn.Boards[boardIndex].BoardNumber);
                var board = filteredPbn.Boards[boardIndex];
                deal = board.Deal;
                panelNorth.Visible = true;
                ShowBothHands();
                auctionControl.auction = board.Auction ?? new Auction();
                auctionControl.ReDraw();
                toolStripStatusLabel1.Text = board.Description;
            }
        }

        private void ShowBothHands()
        {
            ShowHand(deal[(int)Player.North], panelNorth);
            ShowHand(deal[(int)Player.South], panelSouth);
        }

        private void Form1Closed(object sender, FormClosedEventArgs e)
        {
            Settings.Default.useSolver = toolStripMenuItemUseSolver.Checked;
            Settings.Default.alternateSuits = toolStripMenuItemAlternateSuits.Checked;
            Settings.Default.boardNumber = boardIndex;
            Settings.Default.numberOfHandsToBid = (int)numericUpDown1.Value;
            Settings.Default.pbnFilePath = pbn.Boards.Count > 0 ? pbnFilepath : "";
            Settings.Default.filter = (string)toolStripComboBoxFilter.SelectedItem;
            Settings.Default.Save();
        }

        private void ToolStripMenuItemAbortClick(object sender, EventArgs e)
        {
            cancelBatchbidding.Cancel();
        }

        private void ToolStripMenuItemLoadSystemParametersClick(object sender, EventArgs e)
        {
            if (openFileDialogSystemParameters.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    BidManager.SetSystemParameters(File.ReadAllText(openFileDialogSystemParameters.FileName));
                    Settings.Default.systemParametersPath = openFileDialogSystemParameters.FileName;
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"No valid system parameters file is loaded. ({exception.Message})", "Error");
                }
            }
        }

        private void ToolStripMenuItemLoadOptimizationParametersClick(object sender, EventArgs e)
        {
            if (openFileDialogOptimizationParameters.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    BidManager.SetOptimizationParameters(File.ReadAllText(openFileDialogOptimizationParameters.FileName));
                    Settings.Default.optimizationParametersPath = openFileDialogOptimizationParameters.FileName;
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"No valid optimization parameters file is loaded. ({exception.Message})", "Error");
                }
            }
        }

        private void ToolStripMenuItemUseDefaultParametersClick(object sender, EventArgs e)
        {
            BidManager.SetSystemParameters(Util.ReadResource(defaultSystemParameters));
            BidManager.SetOptimizationParameters(Util.ReadResource(defaultOptimizationParameters));
        }

        private void ToolStripComboBoxFilterSelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilter();
            boardIndex = 0;
            LoadCurrentBoard();
        }

        private void ToolStripMenuItemSaveFilteredSetClick(object sender, EventArgs e)
        {
            try
            {
                if (saveFileDialogPBN.ShowDialog() == DialogResult.OK)
                    filteredPbn.Save(saveFileDialogPBN.FileName);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error saving filtered PBN file. {exception.Message}", "Error");
            }

        }

        private void ToolStripMenuItemAlternateSuitsClick(object sender, EventArgs e)
        {
            ShowBothHands();
        }
    }
}