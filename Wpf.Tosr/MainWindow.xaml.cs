using BiddingLogic;
using Common;
using Microsoft.Win32;
using MvvmHelpers.Commands;
using Newtonsoft.Json;
using NLog;
using Solver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.BidControls.ViewModels;
using Path = System.IO.Path;

namespace Wpf.Tosr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            BiddingBoxViewModel.DoBid = new Command(ClickBiddingBoxButton, ButtonCanExecute);
        }

        // ViewModels
        private BiddingBoxViewModel BiddingBoxViewModel => (BiddingBoxViewModel)BiddingBoxView.DataContext;
        private AuctionViewModel AuctionViewModel => (AuctionViewModel)AuctionView.DataContext;
        private HandViewModel HandViewModelNorth => (HandViewModel)panelNorth.DataContext;
        private HandViewModel HandViewModelSouth => (HandViewModel)panelSouth.DataContext;
        private Auction Auction = new();


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

        private async void Form1Load(object sender, EventArgs e)
        {
            logger.Info("Starting program");
            FillFilteredComboBox();

            // Need to set in code because of a .net core bug
            numericUpDown1.Maximum = 100_000;
            numericUpDown1.Value = 1000;
            if (Pinvoke.Setup("Tosr.db3") != 0)
            {
                MessageBox.Show("Cannot find file Tosr.db3", "Error");
                return;
            }
            logger.Info($"Initialized engine with database '{"Tosr.db3"}'");

            shufflingDeal.North = new North { Hcp = new MinMax(16, 37) };
            shufflingDeal.South = new South { Hcp = new MinMax(8, 37), Controls = new MinMax(2, 12) };

            // Load user settings
            toolStripMenuItemUseSolver.IsChecked = Settings.Default.useSolver;
            toolStripMenuItemAlternateSuits.IsChecked = Settings.Default.alternateSuits;
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

            toolStripStatusLabel1.Content = "Generating reverse dictionaries...";
            await Task.Run(() =>
            {
                var progress = new Progress<string>(report => Dispatcher.Invoke(() => toolStripStatusLabel1.Content = $"Generating dictionary {report}..."));
                reverseDictionaries = new ReverseDictionaries(fasesWithOffset, progress);
                bidManager = new BidManager(new BidGeneratorDescription(), fasesWithOffset, reverseDictionaries, true);
                Dispatcher.Invoke(() => bidManager.Init(Auction));
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
            toolStripComboBoxFilter.ItemsSource = Enum.GetNames(typeof(BatchBidding.CorrectnessContract)).Append("All");
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

        private void ClickBiddingBoxButton(object parameter)
        {
            var bid = (Bid)parameter;
            resetEvent.WaitOne();
            AuctionViewModel.UpdateAuction(Auction);
            bidManager.SouthBid(biddingState, Auction, deal[(int)Player.South]);
            BiddingBoxViewModel.DoBid.RaiseCanExecuteChanged();

            if (bid != biddingState.CurrentBid)
            {
                MessageBox.Show($"The correct bid is {biddingState.CurrentBid}. Description: {biddingState.CurrentBid.description}.", "Incorrect bid");
            }

            BidTillSouth(Auction, biddingState);
        }

        private bool ButtonCanExecute(object param)
        {
            var bid = (Bid)param;
            return Auction.BidIsPossible(bid);
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

            AuctionViewModel.UpdateAuction(auction);
            BiddingBoxViewModel.DoBid.RaiseCanExecuteChanged();

            if (auction.IsEndOfBidding())
            {
                BiddingBoxView.IsEnabled = false;
                panelNorth.Visibility = Visibility.Visible;
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
            bidManager.Init(Auction);
            resetEvent.WaitOne();
        }

        private void StartBidding()
        {
            biddingState.Init();

            Auction.Clear();
            Auction.AddBid(Bid.PassBid);
            Auction.AddBid(Bid.OneClub);
            Auction.AddBid(Bid.PassBid);
            BiddingBoxViewModel.DoBid.RaiseCanExecuteChanged();
            AuctionViewModel.UpdateAuction(Auction);
            BiddingBoxView.IsEnabled = true;
        }

        private void Shuffle()
        {
            do
            {
                var board = shufflingDeal.Execute().First();
                deal = Util.GetBoardsTosr(board);
            }
            while (Util.IsFreakHand(deal[(int)Player.South].Split(',').Select(x => x.Length)));
            panelNorth.Visibility = Visibility.Hidden;
            ShowBothHands();
        }

        private void ButtonGetAuctionClick(object sender, EventArgs e)
        {
            try
            {
                resetEvent.WaitOne();
                Auction = bidManager.GetAuction(deal[(int)Player.North], deal[(int)Player.South]);
                BiddingBoxViewModel.DoBid.RaiseCanExecuteChanged();
                AuctionViewModel.UpdateAuction(Auction);
                BiddingBoxView.IsEnabled = true;
                panelNorth.Visibility = Visibility.Visible;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error");
            }
        }

        private async void ButtonBatchBiddingClick(object sender, EventArgs e)
        {
            resetEvent.WaitOne();
            panelNorth.Visibility = Visibility.Hidden;
            var batchBidding = new BatchBidding(reverseDictionaries, fasesWithOffset, toolStripMenuItemUseSolver.IsChecked);
            toolStripStatusLabel1.Content = "Batch bidding hands...";
            cancelBatchbidding.Dispose();
            cancelBatchbidding = new CancellationTokenSource();
            string report = "";
            var oldCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                await Task.Run(() =>
                {
                    var progress = new Progress<int>(report => Dispatcher.Invoke(() => toolStripStatusLabel1.Content = $"Hands done: {report}"));
                    (pbn, report) = batchBidding.Execute(pbn.Boards.Select(x => x.Deal), progress, cancelBatchbidding.Token);
                });
            }
            finally
            {
                Cursor = oldCursor;
            }
            pbnFilepath = "";
            toolStripComboBoxFilter.SelectedItem = Settings.Default.filter;
            ApplyFilter();
            boardIndex = 0;
            LoadCurrentBoard();
            if (!string.IsNullOrWhiteSpace(report))
                MessageBox.Show(report, "Batch bidding done");

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
            var oldCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                GenerateBoards((int)numericUpDown1.Value);
            }
            finally
            {
                Cursor = oldCursor;
            }
        }

        private void ToolStripButton4Click(object sender, EventArgs e)
        {
            var localShufflingDeal = ObjectCloner.ObjectCloner.DeepClone(shufflingDeal);
            var shuffleRestrictionsForm = new ShuffleRestrictionsWindow(localShufflingDeal);
            if (shuffleRestrictionsForm.ShowDialog().Value)
                shufflingDeal = localShufflingDeal;
        }

        private void ToolStripMenuItem11Click(object sender, EventArgs e)
        {
            try
            {
                var openFileDialogDatabase = new OpenFileDialog
                {
                    InitialDirectory = Environment.CurrentDirectory,
                    DefaultExt = "pbn"
                };

                if (openFileDialogDatabase.ShowDialog().Value)
                {
                    if (Pinvoke.Setup("Tosr.db3") != 0)
                    {
                        MessageBox.Show($"Cannot find file {openFileDialogDatabase.FileName}", "Error");
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error opening database. {exception.Message}", "Error");
            }
        }

        private void ViewAuctionClick(object sender, EventArgs e)
        {
            MessageBox.Show(Auction.GetPrettyAuction("\n"));
        }

        private void ToolStripMenuItemSaveSetClick(object sender, EventArgs e)
        {
            try
            {
                var saveFileDialogPBN = new SaveFileDialog
                {
                    DefaultExt = "pbn"
                };

                if (saveFileDialogPBN.ShowDialog().Value)
                {
                    pbn.Save(saveFileDialogPBN.FileName);
                    pbnFilepath = saveFileDialogPBN.FileName;
                    Title = $"{Path.GetFileName(pbnFilepath)} Board: {boardIndex} from {pbn.Boards.Count}";
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
                var openFileDialogPBN = new OpenFileDialog()
                {
                    DefaultExt = "pbn"
                };
                if (openFileDialogPBN.ShowDialog().Value)
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
                Auction = localPbn.Item1.Boards.First().Auction ?? new Auction();
                AuctionViewModel.UpdateAuction(Auction);
                toolStripStatusLabel1.Content = localPbn.Item1.Boards.First().Description;
            }
        }

        private void ToolStripMenuItemBidAgainClick(object sender, EventArgs e)
        {
            StartBidding();
            bidManager.Init(Auction);
            panelNorth.Visibility = Visibility.Hidden;
            if (interactivePbn.Boards.Any())
                interactivePbn.Boards.RemoveAt(interactivePbn.Boards.Count - 1);
            toolStripStatusLabel1.Content = "";
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

            Title = $"{Path.GetFileName(pbnFilepath)} Number of boards in pbn: {pbn.Boards.Count}. Number of filtered boards: {filteredPbn.Boards.Count}";

            if (filteredPbn.Boards.Count > 0)
            {
                toolStripTextBoxBoard.Text = Convert.ToString(filteredPbn.Boards[boardIndex].BoardNumber);
                var board = filteredPbn.Boards[boardIndex];
                deal = board.Deal;
                panelNorth.Visibility = Visibility.Visible;
                ShowBothHands();
                Auction = board.Auction ?? new Auction();
                AuctionViewModel.UpdateAuction(Auction);
                toolStripStatusLabel1.Content = board.Description;
            }
        }

        private void ShowBothHands()
        {
            HandViewModelNorth.ShowHand(deal[(int)Player.North], toolStripMenuItemAlternateSuits.IsChecked, "default");
            HandViewModelSouth.ShowHand(deal[(int)Player.South], toolStripMenuItemAlternateSuits.IsChecked, "default");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.useSolver = toolStripMenuItemUseSolver.IsChecked;
            Settings.Default.alternateSuits = toolStripMenuItemAlternateSuits.IsChecked;
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
            var openFileDialogSystemParameters = new OpenFileDialog()
            {
                DefaultExt = "json"
            };
            if (openFileDialogSystemParameters.ShowDialog().Value)
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
            var openFileDialogOptimizationParameters = new OpenFileDialog()
            {
                DefaultExt = "json"
            };
            if (openFileDialogOptimizationParameters.ShowDialog().Value)
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
                var saveFileDialogPBN = new SaveFileDialog()
                {
                    DefaultExt = "pbn"
                };
                if (saveFileDialogPBN.ShowDialog().Value)
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
