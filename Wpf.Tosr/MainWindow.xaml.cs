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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Common.Tosr;
using Wpf.BidControls.ViewModels;
using Path = System.IO.Path;

namespace Wpf.Tosr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            BiddingBoxViewModel.DoBid = new Command(ClickBiddingBoxButton, ButtonCanExecute);
        }

        // ViewModels
        private BiddingBoxViewModel BiddingBoxViewModel => (BiddingBoxViewModel)BiddingBoxView.DataContext;
        private AuctionViewModel AuctionViewModel => (AuctionViewModel)AuctionView.DataContext;
        private HandViewModel HandViewModelNorth => (HandViewModel)PanelNorth.DataContext;
        private HandViewModel HandViewModelSouth => (HandViewModel)PanelSouth.DataContext;
        // ReSharper disable once InconsistentNaming
        private Auction Auction = new();


        private Dictionary<Player, string> deal;
        private ShufflingDeal shufflingDeal = new() { NrOfHands = 1 };

        private BidManager bidManager;
        private ReverseDictionaries reverseDictionaries;

        private static readonly Dictionary<Phase, bool> PhasesWithOffset = JsonConvert.DeserializeObject<Dictionary<Phase, bool>>(File.ReadAllText("FasesWithOffset.json"));
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ManualResetEvent resetEvent = new(false);
        private Pbn pbn = new();
        private readonly Pbn interactivePbn = new();
        private readonly Pbn filteredPbn = new();
        private int boardIndex;
        private string pbnFilepath;
        private CancellationTokenSource cancelBatchBidding = new();

        private const string DefaultSystemParameters = "BiddingLogic.SystemParameters.json";
        private const string DefaultOptimizationParameters = "BiddingLogic.OptimizationParameters.json";

        private async void Form1Load(object sender, EventArgs e)
        {
            Logger.Info("Starting program");
            FillFilteredComboBox();

            // Need to set in code because of a .net core bug
            NumericUpDown1.Maximum = 100_000;
            NumericUpDown1.Value = 1000;
            if (PInvoke.Setup("Tosr.db3") != 0)
            {
                MessageBox.Show("Cannot find file Tosr.db3", "Error");
                return;
            }
            Logger.Info($"Initialized engine with database '{"Tosr.db3"}'");

            shufflingDeal.North = new North { Hcp = new MinMax(16, 37) };
            shufflingDeal.South = new South { Hcp = new MinMax(8, 37), Controls = new MinMax(2, 12) };

            // Load user settings
            ToolStripMenuItemUseSolver.IsChecked = Settings.Default.useSolver;
            ToolStripMenuItemAlternateSuits.IsChecked = Settings.Default.alternateSuits;
            NumericUpDown1.Value = Settings.Default.numberOfHandsToBid;
            UseSavedSystemParameters();
            UseSavedOptimizationParameters();
            if (File.Exists("interactive.pbn"))
                await interactivePbn.LoadAsync("interactive.pbn");
            if (File.Exists(Settings.Default.pbnFilePath))
            {
                try
                {
                    pbnFilepath = Settings.Default.pbnFilePath;
                    await pbn.LoadAsync(pbnFilepath);
                    ToolStripComboBoxFilter.SelectedItem = Settings.Default.filter;
                    ApplyFilter();
                    boardIndex = Math.Min(Settings.Default.boardNumber, filteredPbn.Boards.Count - 1);
                    LoadCurrentBoard();
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Error loading Pbn file. {exception.Message}", "Error");
                }
            }
            if (pbn.Boards.Count == 0)
            {
                pbnFilepath = "";
                Shuffle();
                StartBidding();
            }

            ToolStripStatusLabel1.Content = "Generating reverse dictionaries...";
            await Task.Run(() =>
            {
                var progress = new Progress<string>(report => Dispatcher.Invoke(() => ToolStripStatusLabel1.Content = $"Generating dictionary {report}..."));
                reverseDictionaries = new ReverseDictionaries(PhasesWithOffset, progress);
                bidManager = new BidManager(new BidGeneratorDescription(), PhasesWithOffset, reverseDictionaries, true);
                Dispatcher.Invoke(() => bidManager.Init(Auction));
                resetEvent.Set();
            });
        }

        private void ApplyFilter()
        {
            filteredPbn.Boards = (string)ToolStripComboBoxFilter.SelectedItem == "All" ? pbn.Boards :
                pbn.Boards.Where(b => b.Description != null && b.Description.Split(':')[0] == (string)ToolStripComboBoxFilter.SelectedItem).ToList();
        }

        private void FillFilteredComboBox()
        {
            ToolStripComboBoxFilter.ItemsSource = Enum.GetNames(typeof(BatchBidding.CorrectnessContract)).Append("All");
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
            BidManager.SetSystemParameters(UtilTosr.ReadResource(DefaultSystemParameters));
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
            BidManager.SetOptimizationParameters(UtilTosr.ReadResource(DefaultOptimizationParameters));
        }

        private void ClickBiddingBoxButton(object parameter)
        {
            var bid = (Bid)parameter;
            resetEvent.WaitOne();
            AuctionViewModel.UpdateAuction(Auction);
            bidManager.SouthBid(Auction, deal[Player.South]);
            BiddingBoxViewModel.DoBid.RaiseCanExecuteChanged();

            if (bid != bidManager.BiddingState.CurrentBid)
            {
                MessageBox.Show($"The correct bid is {bidManager.BiddingState.CurrentBid}. Description: {bidManager.BiddingState.CurrentBid.description}.", "Incorrect bid");
            }

            BidTillSouth(Auction);
        }

        private bool ButtonCanExecute(object param)
        {
            var bid = (Bid)param;
            return Auction.BidIsPossible(bid);
        }

        private void BidTillSouth(Auction auction)
        {
            // West
            auction.AddBid(Bid.PassBid);

            // North
            bidManager.NorthBid(auction, deal[Player.North]);
            auction.AddBid(bidManager.BiddingState.CurrentBid);

            // East
            auction.AddBid(Bid.PassBid);

            AuctionViewModel.UpdateAuction(auction);
            BiddingBoxViewModel.DoBid.RaiseCanExecuteChanged();

            if (auction.IsEndOfBidding())
            {
                BiddingBoxView.IsEnabled = false;
                PanelNorth.Visibility = Visibility.Visible;
                AddBoardToInteractivePbnFile(deal, auction);
            }
        }

        // ReSharper disable once ParameterHidesMember
        private void AddBoardToInteractivePbnFile(Dictionary<Player, string> deal, Auction auction)
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
            Auction.Clear(Player.West);
            Auction.AddBid(Bid.PassBid);
            Auction.AddBid(new Bid(1, Suit.Clubs));
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
            while (UtilTosr.IsFreakHand(deal[Player.South].Split(',').Select(x => x.Length)));
            PanelNorth.Visibility = Visibility.Hidden;
            ShowBothHands();
        }

        private void ButtonGetAuctionClick(object sender, EventArgs e)
        {
            try
            {
                resetEvent.WaitOne();
                Auction = bidManager.GetAuction(deal[Player.North], deal[Player.South]);
                BiddingBoxViewModel.DoBid.RaiseCanExecuteChanged();
                AuctionViewModel.UpdateAuction(Auction);
                BiddingBoxView.IsEnabled = true;
                PanelNorth.Visibility = Visibility.Visible;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error");
            }
        }

        private async void ButtonBatchBiddingClick(object sender, EventArgs e)
        {
            resetEvent.WaitOne();
            PanelNorth.Visibility = Visibility.Hidden;
            var batchBidding = new BatchBidding(reverseDictionaries, PhasesWithOffset, ToolStripMenuItemUseSolver.IsChecked);
            ToolStripStatusLabel1.Content = "Batch bidding hands...";
            cancelBatchBidding.Dispose();
            cancelBatchBidding = new CancellationTokenSource();
            string report = "";
            var oldCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                await Task.Run(() =>
                {
                    var progress = new Progress<int>(message => Dispatcher.Invoke(() => ToolStripStatusLabel1.Content = $"Hands done: {message}"));
                    (pbn, report) = batchBidding.Execute(pbn.Boards.Select(x => x.Deal), progress, Path.GetFileNameWithoutExtension(pbnFilepath), cancelBatchBidding.Token);
                });
            }
            finally
            {
                Cursor = oldCursor;
            }
            ToolStripComboBoxFilter.SelectedItem = Settings.Default.filter;
            ApplyFilter();
            boardIndex = 0;
            LoadCurrentBoard();
            if (!string.IsNullOrWhiteSpace(report))
                MessageBox.Show(report, "Batch bidding done");

        }

        private void GenerateBoards(int batchSize)
        {
            var lShufflingDeal = new ShufflingDeal()
            {
                NrOfHands = batchSize,
                North = new North { Hcp = new MinMax(16, 37) },
                South = new South { Hcp = new MinMax(8, 37), Controls = new MinMax(2, 12) }
            };

            var boards = lShufflingDeal.Execute();
            pbn.Boards = boards.Select((board, index) => new BoardDto { Deal = Util.GetBoardsTosr(board), BoardNumber = index + 1 }).ToList();
            pbnFilepath = "";
        }

        private void ButtonGenerateHandsClick(object sender, EventArgs e)
        {
            var oldCursor = Cursor;
            try
            {
                Cursor = Cursors.Wait;
                if (NumericUpDown1.Value != null) GenerateBoards(NumericUpDown1.Value.Value);
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
            // ReSharper disable once PossibleInvalidOperationException
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

                // ReSharper disable once PossibleInvalidOperationException
                if (openFileDialogDatabase.ShowDialog().Value)
                {
                    if (PInvoke.Setup("Tosr.db3") != 0)
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
                var saveFileDialogPbn = new SaveFileDialog
                {
                    DefaultExt = "pbn"
                };

                if (saveFileDialogPbn.ShowDialog().GetValueOrDefault())
                {
                    pbn.Save(saveFileDialogPbn.FileName);
                    pbnFilepath = saveFileDialogPbn.FileName;
                    Title = $"{Path.GetFileName(pbnFilepath)} Board: {boardIndex} from {pbn.Boards.Count}";
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error saving Pbn file. {exception.Message}", "Error");
            }
        }

        private void ToolStripMenuItemLoadSetClick(object sender, EventArgs e)
        {
            try
            {
                var openFileDialogPbn = new OpenFileDialog()
                {
                    DefaultExt = "pbn"
                };
                // ReSharper disable once PossibleInvalidOperationException
                if (openFileDialogPbn.ShowDialog().Value)
                {
                    pbn.Load(openFileDialogPbn.FileName);
                    pbnFilepath = openFileDialogPbn.FileName;
                    ApplyFilter();
                    boardIndex = 0;
                    LoadCurrentBoard();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error loading Pbn file. {exception.Message}", "Error");
            }
        }

        private void ToolStripMenuItemOneBoardClick(object sender, EventArgs e)
        {
            if (int.TryParse(ToolStripTextBoxBoard.Text, out var board) && board <= pbn.Boards.Count)
            {
                boardIndex = filteredPbn.Boards.IndexOf(filteredPbn.Boards.Single(b => b.BoardNumber == board));
                LoadCurrentBoard();

                resetEvent.WaitOne();
                var batchBidding = new BatchBidding(reverseDictionaries, PhasesWithOffset, true);
                var localPbn = batchBidding.Execute(new[] { pbn.Boards[boardIndex].Deal }, new Progress<int>(), "", CancellationToken.None);
                Auction = localPbn.Item1.Boards.First().Auction ?? new Auction();
                AuctionViewModel.UpdateAuction(Auction);
                ToolStripStatusLabel1.Content = localPbn.Item1.Boards.First().Description;
            }
        }

        private void ToolStripMenuItemBidAgainClick(object sender, EventArgs e)
        {
            StartBidding();
            bidManager.Init(Auction);
            PanelNorth.Visibility = Visibility.Hidden;
            if (interactivePbn.Boards.Any())
                interactivePbn.Boards.RemoveAt(interactivePbn.Boards.Count - 1);
            ToolStripStatusLabel1.Content = "";
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

        //private void ToolStripTextBoxBoardLeave(object sender, EventArgs e)
        //{
        //    if (int.TryParse(toolStripTextBoxBoard.Text, out var board) && filteredPbn.Boards.Any(b => b.BoardNumber == board))
        //    {
        //        boardIndex = filteredPbn.Boards.IndexOf(filteredPbn.Boards.Single(b => b.BoardNumber == board));
        //        LoadCurrentBoard();
        //    }
        //}

        private void LoadCurrentBoard()
        {
            if (pbn.Boards.Count == 0)
            {
                MessageBox.Show("No valid Pbn file is loaded.", "Error");
                return;
            }

            Title = $"{Path.GetFileName(pbnFilepath)} Number of boards in pbn: {pbn.Boards.Count}. Number of filtered boards: {filteredPbn.Boards.Count}";

            if (filteredPbn.Boards.Count > 0)
            {
                ToolStripTextBoxBoard.Text = Convert.ToString(filteredPbn.Boards[boardIndex].BoardNumber);
                var board = filteredPbn.Boards[boardIndex];
                deal = board.Deal;
                PanelNorth.Visibility = Visibility.Visible;
                ShowBothHands();
                Auction = board.Auction ?? new Auction();
                AuctionViewModel.UpdateAuction(Auction);
                ToolStripStatusLabel1.Content = board.Description;
            }
        }

        private void ShowBothHands()
        {
            HandViewModelNorth.ShowHand(deal[Player.North], ToolStripMenuItemAlternateSuits.IsChecked, "default");
            HandViewModelSouth.ShowHand(deal[Player.South], ToolStripMenuItemAlternateSuits.IsChecked, "default");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.useSolver = ToolStripMenuItemUseSolver.IsChecked;
            Settings.Default.alternateSuits = ToolStripMenuItemAlternateSuits.IsChecked;
            Settings.Default.boardNumber = boardIndex;
            Settings.Default.numberOfHandsToBid = NumericUpDown1.Value.GetValueOrDefault();
            Settings.Default.pbnFilePath = pbn.Boards.Count > 0 ? pbnFilepath : "";
            Settings.Default.filter = (string)ToolStripComboBoxFilter.SelectedItem;
            Settings.Default.Save();
        }

        private void ToolStripMenuItemAbortClick(object sender, EventArgs e)
        {
            cancelBatchBidding.Cancel();
        }

        private void ToolStripMenuItemLoadSystemParametersClick(object sender, EventArgs e)
        {
            var openFileDialogSystemParameters = new OpenFileDialog()
            {
                DefaultExt = "json"
            };
            if (openFileDialogSystemParameters.ShowDialog().GetValueOrDefault())
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
            if (openFileDialogOptimizationParameters.ShowDialog().GetValueOrDefault())
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
            BidManager.SetSystemParameters(UtilTosr.ReadResource(DefaultSystemParameters));
            BidManager.SetOptimizationParameters(UtilTosr.ReadResource(DefaultOptimizationParameters));
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
                var saveFileDialogPbn = new SaveFileDialog()
                {
                    DefaultExt = "pbn"
                };
                if (saveFileDialogPbn.ShowDialog().GetValueOrDefault())
                    filteredPbn.Save(saveFileDialogPbn.FileName);
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Error saving filtered Pbn file. {exception.Message}", "Error");
            }
        }

        private void ToolStripMenuItemAlternateSuitsClick(object sender, EventArgs e)
        {
            ShowBothHands();
        }
    }
}
