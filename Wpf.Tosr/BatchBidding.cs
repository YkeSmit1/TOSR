using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NLog;
using Common;
using Solver;
using System.Threading;
using System.ComponentModel;
using BiddingLogic;
using System.Windows;
using Common.Tosr;

namespace Wpf.Tosr
{
    public class BatchBidding
    {
        private enum CorrectnessContractBreakdown
        {
            GameCorrect,
            MissedGoodSmallSlam,
            MissedLaydownSmallSlam,
            SmallSlamCorrect,
            SmallSlamTooHigh,
            SmallSlamHopeless,
            MissedGoodGrandSlam,
            MissedLaydownGrandSlam,
            GrandSlamCorrect,
            GrandSlamTooHigh,
            GrandSlamHopeless,
            NoFit,
            Unknown,
        }

        public enum CorrectnessContract
        {
            InCorrect,
            Poor,
            GameCorrect,
            SmallSlamCorrect,
            GrandSlamCorrect,
            NoFit,
        }

        private enum PullType
        {
            NoPull,
            HasPulled,
        }

        private enum ExpectedContract
        {
            Game,
            SmallSlam,
            GrandSlam,
        }

        private class Statistics
        {
            public int handsBid;
            // ReSharper disable once NotAccessedField.Local
            public int handsNotBidBecauseOfFreakhand;
            // ReSharper disable once NotAccessedField.Local
            public int handsNotBidBecauseOfError;
            public readonly SortedDictionary<Bid, int> contracts = new();
            public readonly SortedDictionary<ConstructedSouthHandOutcome, int> outcomes = new();
            public readonly SortedDictionary<Player, int> dealers = new();
            public readonly SortedDictionary<int, int> bidsNonShape = new();
            public readonly SortedDictionary<(CorrectnessContractBreakdown, (ConstructedSouthHandOutcome, PullType)), int> contractCorrectnessBreakdownOutcome = new();
            public readonly SortedDictionary<CorrectnessContract, int> contractCorrectness = new();
            public readonly SortedDictionary<CorrectnessContractBreakdown, int> contractCorrectnessBreakdown = new();

        }

        private readonly Statistics statistics = new();
        private readonly Dictionary<string, List<string>> handPerAuction = new();
        private readonly StringBuilder expectedSouthHands = new();
        private readonly StringBuilder inCorrectContracts = new();
        private readonly BidManager bidManager;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly bool useSingleDummySolver;
        private CorrectnessContractBreakdown correctnessContractBreakdown;
        private CorrectnessContract correctnessContract;
        private Dictionary<ExpectedContract, int> confidence;
        private readonly List<Bid> endContracts = new();

        public BatchBidding(ReverseDictionaries reverseDictionaries, Dictionary<Fase, bool> fasesWithOffset, bool useSingleDummySolver)
        {
            bidManager = new BidManager(new BidGenerator(), fasesWithOffset, reverseDictionaries, useSingleDummySolver);
            this.useSingleDummySolver = useSingleDummySolver;
        }

        public (Pbn, string) Execute(IEnumerable<Dictionary<Player, string>> boards, IProgress<int> progress, string batchName, CancellationToken token)
        {
            var pbn = new Pbn();
            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringBuilder = new StringBuilder();

            var boardsList = boards.ToList();
            if (!boardsList.Any())
            {
                MessageBox.Show("Cannot do batchbidding. Generate hands first.", "Error");
                return (pbn, "");
            }

            Logger.Info($"Start batchbidding. Number of boards : {boardsList.Count}");

            foreach (var board in boardsList)
            {
                try
                {
                    if (UtilTosr.IsFreakHand(board[Player.South].Split(',').Select(x => x.Length)))
                    {
                        Logger.Debug($"Hand {board[Player.South]} is a freak hand. Will not be bid");
                        statistics.handsNotBidBecauseOfFreakhand++;
                        continue;
                    }

                    statistics.handsBid++;
                    var auction = bidManager.GetAuction(board[Player.North], board[Player.South]);
                    AddHandAndAuction(board, auction, statistics.handsBid, bidManager.BiddingState);
                    pbn.Boards.Add(new BoardDto
                    {
                        Deal = board,
                        Auction = auction,
                        BoardNumber = statistics.handsBid,
                        Event = "TOSR Batchbidding",
                        Date = DateTime.Now,
                        Declarer = auction.GetDeclarerOrNorth(auction.currentContract.suit),
                        Dealer = Player.West,
                        Vulnerable = "None",
                        Description = $"{correctnessContract}: {correctnessContractBreakdown}{JsonConvert.SerializeObject(confidence)}"
                    });
                    if (statistics.handsBid % 100 == 0)
                        progress.Report(statistics.handsBid);
                    if (token.IsCancellationRequested)
                    {
                        MessageBox.Show($"Batch bidding canceled after board {statistics.handsBid}", "Batch bidding");
                        break;
                    }
                }
                catch (Exception exception)
                {
                    statistics.handsNotBidBecauseOfError++;
                    Logger.Warn(exception, $"Error:{exception.Message}. Board:{statistics.handsBid}. North hand:{board[Player.North]}. South hand:{board[Player.South]}. Controls:{Util.GetControlCount(board[Player.South])}. " +
                        $"HCP: {Util.GetHcpCount(board[Player.South])}. Projected AK as 4333: {UtilTosr.GetHandWithOnlyControlsAs4333(board[Player.South], "AK")}");
                    stringBuilder.AppendLine($"{exception.Message}. Board:{statistics.handsBid}");
                    pbn.Boards.Add(new BoardDto
                    {
                        Deal = board,
                        BoardNumber = statistics.handsBid,
                        Event = "TOSR Batchbidding with error",
                        Date = DateTime.Now,
                        Dealer = Player.West,
                        Vulnerable = "None"
                    });
                }
            }
            progress.Report(statistics.handsBid);
            stringBuilder.AppendLine(@$"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
            stringBuilder.AppendLine(@"Duplicate auctions are written to ""HandPerAuction.txt""");
            stringBuilder.AppendLine(@"Statistics are written to ""Statistics.txt""");
            stringBuilder.AppendLine(@"Error info for hand-matching is written to ""ExpectedSouthHands.txt""");
            stringBuilder.AppendLine(@"Incorrect contract hands are written to ""IncorrectContract.txt""");
            SaveAuctions(batchName);

            Logger.Info("End batchbidding");
            return (pbn, stringBuilder.ToString());
        }

        private void AddHandAndAuction(Dictionary<Player, string> board, Auction auction, int boardNumber, BiddingState biddingState)
        {
            var suitLengthSouth = board[Player.South].Split(',').Select(x => x.Length);
            var str = string.Join("", suitLengthSouth);

            var strAuction = biddingState.GetBidsAsString(Fase.Shape);

            AddHandPerAuction(str, strAuction);

            // Start calculating hand
            if (!auction.responderHasSignedOff)
                expectedSouthHands.AppendLine($"Board:{boardNumber} { bidManager.ConstructSouthHandSafe(board, auction)}");

            var longestSuit = Util.GetLongestSuit(board[Player.North], board[Player.South]);
            var dealer = auction.GetDeclarer(3 - longestSuit.Item1);
            statistics.dealers.AddOrUpdateDictionary(dealer);
            var contract = auction.currentContract > new Bid(7, Suit.NoTrump) ? new Bid(7, Suit.NoTrump) : auction.currentContract;
            statistics.contracts.AddOrUpdateDictionary(contract);
            if (!auction.responderHasSignedOff)
                statistics.bidsNonShape.AddOrUpdateDictionary(auction.GetBids(Player.South).Last(bid => bid.bidType == BidType.bid) - biddingState.GetBids(Fase.Shape).Last());
            statistics.outcomes.AddOrUpdateDictionary(bidManager.constructedSouthHandOutcome);
            correctnessContractBreakdown = CheckContract(contract, board, dealer == Player.UnKnown ? Player.North : dealer);
            var pullType = biddingState.GetPullBid() != default ? PullType.HasPulled : PullType.NoPull;
            statistics.contractCorrectnessBreakdownOutcome.AddOrUpdateDictionary((correctnessContractBreakdown, (bidManager.constructedSouthHandOutcome, pullType)));
            correctnessContract = GetCorrectness(correctnessContractBreakdown);
            statistics.contractCorrectnessBreakdown.AddOrUpdateDictionary(correctnessContractBreakdown);
            statistics.contractCorrectness.AddOrUpdateDictionary(correctnessContract);
            if (correctnessContract is CorrectnessContract.InCorrect or CorrectnessContract.NoFit)
                inCorrectContracts.AppendLine($"({correctnessContractBreakdown}, {bidManager.constructedSouthHandOutcome}) Board:{boardNumber} Contract:{auction.currentContract}" +
                    $" Auction:{auction.GetPrettyAuction("|")} NorthHand: {board[Player.North]} SouthHand: {board[Player.South]}");
            endContracts.Add(auction.currentContract);
        }

        private static CorrectnessContract GetCorrectness(CorrectnessContractBreakdown correctnessContractBreakdown)
        {
            return correctnessContractBreakdown switch
            {
                CorrectnessContractBreakdown.MissedGoodSmallSlam or CorrectnessContractBreakdown.SmallSlamTooHigh or CorrectnessContractBreakdown.MissedGoodGrandSlam or CorrectnessContractBreakdown.GrandSlamTooHigh => CorrectnessContract.Poor,
                CorrectnessContractBreakdown.MissedLaydownSmallSlam or CorrectnessContractBreakdown.SmallSlamHopeless or CorrectnessContractBreakdown.MissedLaydownGrandSlam or CorrectnessContractBreakdown.GrandSlamHopeless or CorrectnessContractBreakdown.Unknown => CorrectnessContract.InCorrect,
                CorrectnessContractBreakdown.GameCorrect => CorrectnessContract.GameCorrect,
                CorrectnessContractBreakdown.SmallSlamCorrect => CorrectnessContract.SmallSlamCorrect,
                CorrectnessContractBreakdown.GrandSlamCorrect => CorrectnessContract.GrandSlamCorrect,
                CorrectnessContractBreakdown.NoFit => CorrectnessContract.NoFit,
                _ => throw new InvalidEnumArgumentException(nameof(correctnessContractBreakdown)),
            };
        }

        private void AddHandPerAuction(string str, string strAuction)
        {
            if (!handPerAuction.ContainsKey(strAuction))
                handPerAuction[strAuction] = new List<string>();
            if (!handPerAuction[strAuction].Contains(str))
                handPerAuction[strAuction].Add(str);
        }

        private void SaveAuctions(string batchName)
        {
            Logger.Info("Save auctions");
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("txt\\HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("txt\\Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
            File.WriteAllText("txt\\ExpectedSouthHands.txt", expectedSouthHands.ToString());
            var list = new List<string>(inCorrectContracts.ToString().Split('\n'));
            list.Sort();
            File.WriteAllText("txt\\IncorrectContract.txt", string.Join('\n', list));
            if (!string.IsNullOrWhiteSpace(batchName))
            {
                var pathEndContracts = $"txt\\endcontracts_{batchName}.csv";
                if (!File.Exists(pathEndContracts))
                    File.WriteAllText(pathEndContracts, string.Join(',', Enumerable.Range(1, endContracts.Count)));
                File.AppendAllText(pathEndContracts, $"\n{string.Join(',', endContracts)}");
            }
        }

        private CorrectnessContractBreakdown CheckContract(Bid contract, Dictionary<Player, string> board, Player declarer)
        {
            confidence = new Dictionary<ExpectedContract, int>();
            if (!useSingleDummySolver)
                return CorrectnessContractBreakdown.Unknown;
            var northHand = board[Player.North];
            var southHand = board[Player.South];
            if (contract.suit != Suit.NoTrump && Util.GetNumberOfTrumps(contract.suit, northHand, southHand) < 8)
                return CorrectnessContractBreakdown.NoFit;
            var tricks = SingleDummySolver.SolveSingleDummyExactHands(contract.suit, declarer, northHand, southHand);
            var expectedContract = GetExpectedContract(tricks);
            var expectedContractType = expectedContract.expectedContract;
            confidence = expectedContract.confidence;

            return expectedContractType == ExpectedContract.GrandSlam && contract.rank == 7
                ? CorrectnessContractBreakdown.GrandSlamCorrect
                : expectedContractType == ExpectedContract.GrandSlam && contract.rank < 7
                ? confidence[ExpectedContract.GrandSlam] > 8 ? CorrectnessContractBreakdown.MissedLaydownGrandSlam : CorrectnessContractBreakdown.MissedGoodGrandSlam
                : expectedContractType == ExpectedContract.SmallSlam && contract.rank == 6
                ? CorrectnessContractBreakdown.SmallSlamCorrect
                : expectedContractType == ExpectedContract.SmallSlam && contract.rank < 6
                ? confidence[ExpectedContract.SmallSlam] + confidence[ExpectedContract.GrandSlam] > 8 ? CorrectnessContractBreakdown.MissedLaydownSmallSlam : CorrectnessContractBreakdown.MissedGoodSmallSlam
                : expectedContractType != ExpectedContract.GrandSlam && contract.rank == 7
                ? confidence[ExpectedContract.GrandSlam] < 2 ? CorrectnessContractBreakdown.GrandSlamHopeless : CorrectnessContractBreakdown.GrandSlamTooHigh
                : expectedContractType != ExpectedContract.SmallSlam && contract.rank == 6
                ? confidence[ExpectedContract.SmallSlam] + confidence[ExpectedContract.GrandSlam] < 2 ? CorrectnessContractBreakdown.SmallSlamHopeless : CorrectnessContractBreakdown.SmallSlamTooHigh
                : CorrectnessContractBreakdown.GameCorrect;

            static (ExpectedContract expectedContract, Dictionary<ExpectedContract, int> confidence) GetExpectedContract(IEnumerable<int> scores)
            {
                ExpectedContract expectedContract;
                var scoresList = scores.ToList();
                if (scoresList.Count(x => x == 13) / (double)scoresList.Count > .6)
                    expectedContract = ExpectedContract.GrandSlam;
                else if (scoresList.Count(x => x == 12) / (double)scoresList.Count > .6)
                    expectedContract = ExpectedContract.SmallSlam;
                else if (scoresList.Count(x => x == 12 || x == 13) / (double)scoresList.Count > .6)
                    expectedContract = scoresList.Count(x => x == 12) >= scoresList.Count(x => x == 13) ? ExpectedContract.SmallSlam : ExpectedContract.GrandSlam;
                else expectedContract = ExpectedContract.Game;

                return (expectedContract, new Dictionary<ExpectedContract, int> {
                {ExpectedContract.GrandSlam, scoresList.Count(x => x == 13) },
                { ExpectedContract.SmallSlam, scoresList.Count(x => x == 12) },
                { ExpectedContract.Game, scoresList.Count(x => x < 12)}});
            }
        }
    }
}