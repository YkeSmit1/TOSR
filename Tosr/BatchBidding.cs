using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using NLog;
using Common;
using Solver;
using System.Threading;
using System.ComponentModel;
using BiddingLogic;

namespace Tosr
{
    public class BatchBidding
    {
        public enum CorrectnessContractBreakdown
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

        public enum PullType
        {
            NoPull,
            PullNT,
            Pull4Di
        }

        public enum ExpectedContract
        {
            Game,
            SmallSlam,
            GrandSlam,
        }

        private class Statistics
        {
            public int handsBid;
            public int handsNotBidBecauseofFreakhand = 0;
            public int handsNotBidBecauseOfError = 0;
            public SortedDictionary<Bid, int> contracts = new();
            public SortedDictionary<ConstructedSouthhandOutcome, int> outcomes = new();
            public SortedDictionary<Player, int> dealers = new();
            public SortedDictionary<int, int> bidsNonShape = new();
            public SortedDictionary<(CorrectnessContractBreakdown, (ConstructedSouthhandOutcome, PullType)), int> ContractCorrectnessBreakdownOutcome = new();
            public SortedDictionary<CorrectnessContract, int> ContractCorrectness = new();
            public SortedDictionary<CorrectnessContractBreakdown, int> ContractCorrectnessBreakdown = new();

        }

        private readonly Statistics statistics = new();
        private readonly Dictionary<string, List<string>> handPerAuction = new();
        private readonly StringBuilder expectedSouthHands = new();
        private readonly StringBuilder inCorrectContracts = new();
        private readonly BidManager bidManager;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly bool useSingleDummySolver;
        private CorrectnessContractBreakdown correctnessContractBreakdown;
        private CorrectnessContract correctnessContract;
        private Dictionary<ExpectedContract, int> confidence;

        public BatchBidding(ReverseDictionaries reverseDictionaries, Dictionary<Fase, bool> fasesWithOffset, bool useSingleDummySolver)
        {
            bidManager = new BidManager(new BidGenerator(), fasesWithOffset, reverseDictionaries, useSingleDummySolver);
            this.useSingleDummySolver = useSingleDummySolver;
        }

        public Pbn Execute(IEnumerable<string[]> boards, IProgress<int> progress, CancellationToken token)
        {
            var pbn = new Pbn();
            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            if (boards == null || !boards.Any())
            {
                MessageBox.Show("Cannot do batchbidding. Shuffle first.", "Error");
                return pbn;
            }

            logger.Info($"Start batchbidding. Number of boards : {boards.Count()}");

            foreach (var board in boards)
            {
                try
                {
                    if (Util.IsFreakHand(board[(int)Player.South].Split(',').Select(x => x.Length)))
                    {
                        logger.Debug($"Hand {board[(int)Player.South]} is a freak hand. Will not be bid");
                        statistics.handsNotBidBecauseofFreakhand++;
                        continue;
                    }

                    statistics.handsBid++;
                    var auction = bidManager.GetAuction(board[(int)Player.North], board[(int)Player.South]);
                    AddHandAndAuction(board, auction, statistics.handsBid);
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
                        Description = $"{correctnessContract}:{correctnessContractBreakdown}{JsonConvert.SerializeObject(confidence)}"
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
                    logger.Warn(exception, $"Error:{exception.Message}. Board:{statistics.handsBid}. North hand:{board[(int)Player.North]}. South hand:{board[(int)Player.South]}. Controls:{Util.GetControlCount(board[(int)Player.South])}. " +
                        $"HCP: {Util.GetHcpCount(board[(int)Player.South])}. Projected AK as 4333: {Util.GetHandWithOnlyControlsAs4333(board[(int)Player.South], "AK")}");
                    stringbuilder.AppendLine($"{exception.Message}. Board:{statistics.handsBid}");
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
            stringbuilder.AppendLine(@$"Seconds elapsed: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
            stringbuilder.AppendLine(@"Duplicate auctions are written to ""HandPerAuction.txt""");
            stringbuilder.AppendLine(@"Statistics are written to ""Statistics.txt""");
            stringbuilder.AppendLine(@"Error info for hand-matching is written to ""ExpectedSouthHands.txt""");
            stringbuilder.AppendLine(@"Incorrect contract hands are written to ""IncorrectContract.txt""");
            SaveAuctions();

            logger.Info($"End batchbidding");

            MessageBox.Show(stringbuilder.ToString(), "Batch bidding done");
            return pbn;
        }

        private void AddHandAndAuction(string[] board, Auction auction, int boardNumber)
        {
            var suitLengthSouth = board[(int)Player.South].Split(',').Select(x => x.Length);
            var str = string.Join("", suitLengthSouth);

            var strAuction = auction.GetBidsAsString(Fase.Shape);

            AddHandPerAuction(str, strAuction);

            // Start calculating hand
            if (!auction.responderHasSignedOff)
                expectedSouthHands.AppendLine($"Board:{boardNumber} { bidManager.ConstructSouthHandSafe(board, auction)}");

            var longestSuit = Util.GetLongestSuit(board[(int)Player.North], board[(int)Player.South]);
            var dealer = auction.GetDeclarer(3 - longestSuit.Item1);
            statistics.dealers.AddOrUpdateDictionary(dealer);
            var contract = auction.currentContract > new Bid(7, Suit.NoTrump) ? new Bid(7, Suit.NoTrump) : auction.currentContract;
            statistics.contracts.AddOrUpdateDictionary(contract);
            if (!auction.responderHasSignedOff)
                statistics.bidsNonShape.AddOrUpdateDictionary(auction.GetBids(Player.South).Where(bid => bid.bidType == BidType.bid).Last() - auction.GetBids(Player.South, Fase.Shape).Last());
            statistics.outcomes.AddOrUpdateDictionary(bidManager.constructedSouthhandOutcome);
            correctnessContractBreakdown = CheckContract(contract, board, dealer == Player.UnKnown ? Player.North : dealer);
            var pullType = GetPullType(auction);
            statistics.ContractCorrectnessBreakdownOutcome.AddOrUpdateDictionary((correctnessContractBreakdown, (bidManager.constructedSouthhandOutcome, pullType)));
            correctnessContract = GetCorrectness(correctnessContractBreakdown);
            statistics.ContractCorrectnessBreakdown.AddOrUpdateDictionary(correctnessContractBreakdown);
            statistics.ContractCorrectness.AddOrUpdateDictionary(correctnessContract);
            if (correctnessContract == CorrectnessContract.InCorrect || correctnessContract == CorrectnessContract.NoFit)
                inCorrectContracts.AppendLine($"({correctnessContractBreakdown}, {bidManager.constructedSouthhandOutcome}) Board:{boardNumber} Contract:{auction.currentContract}" +
                    $" Auction:{auction.GetPrettyAuction("|")} Northhand: {board[(int)Player.North]} Southhand: {board[(int)Player.South]}");

        }

        private static PullType GetPullType(Auction auction)
        {
            var signOffBidsNT = auction.GetPullBids(Player.South, Util.signOffFasesFor3NT.ToArray());
            if (signOffBidsNT.Any())
                return PullType.PullNT;
            var signOffBids4Di = auction.GetPullBids(Player.South, Util.signOffFasesFor4Di.ToArray());
            if (signOffBids4Di.Any())
                return PullType.Pull4Di;
            return PullType.NoPull;

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

        private void SaveAuctions()
        {
            logger.Info("Save auctions");
            var multiHandPerAuction = handPerAuction.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllText("txt\\HandPerAuction.txt", JsonConvert.SerializeObject(multiHandPerAuction, Formatting.Indented));
            File.WriteAllText("txt\\Statistics.txt", JsonConvert.SerializeObject(statistics, Formatting.Indented));
            File.WriteAllText("txt\\ExpectedSouthHands.txt", expectedSouthHands.ToString());
            var list = new List<string>(inCorrectContracts.ToString().Split('\n'));
            list.Sort();
            File.WriteAllText("txt\\IncorrectContract.txt", string.Join('\n', list));
        }

        private CorrectnessContractBreakdown CheckContract(Bid contract, string[] board, Player declarer)
        {
            confidence = new Dictionary<ExpectedContract, int>();
            if (!useSingleDummySolver)
                return CorrectnessContractBreakdown.Unknown;
            var northHand = board[(int)Player.North];
            var southHand = board[(int)Player.South];
            if (contract.suit != Suit.NoTrump && Util.GetNumberOfTrumps(contract.suit, northHand, southHand) < 8)
                return CorrectnessContractBreakdown.NoFit;
            var tricks = SingleDummySolver.SolveSingleDummyExactHands(contract.suit, declarer, northHand, southHand);
            var expectedContract = GetExpectedContract(tricks);
            var expectedContractType = expectedContract.expectedContract;
            confidence = expectedContract.confidence;

            if (expectedContractType == ExpectedContract.GrandSlam && contract.rank == 7)
                return CorrectnessContractBreakdown.GrandSlamCorrect;
            if (expectedContractType == ExpectedContract.GrandSlam && contract.rank < 7)
                return confidence[ExpectedContract.GrandSlam] > 8 ? CorrectnessContractBreakdown.MissedLaydownGrandSlam : CorrectnessContractBreakdown.MissedGoodGrandSlam;

            if (expectedContractType == ExpectedContract.SmallSlam && contract.rank == 6)
                return CorrectnessContractBreakdown.SmallSlamCorrect;
            if (expectedContractType == ExpectedContract.SmallSlam && contract.rank < 6)
                return confidence[ExpectedContract.SmallSlam] + confidence[ExpectedContract.GrandSlam] > 8 ? CorrectnessContractBreakdown.MissedLaydownSmallSlam : CorrectnessContractBreakdown.MissedGoodSmallSlam;

            if (expectedContractType != ExpectedContract.GrandSlam && contract.rank == 7)
                return confidence[ExpectedContract.GrandSlam] < 2 ? CorrectnessContractBreakdown.GrandSlamHopeless : CorrectnessContractBreakdown.GrandSlamTooHigh;
            if (expectedContractType != ExpectedContract.SmallSlam && contract.rank == 6)
                return confidence[ExpectedContract.SmallSlam] + confidence[ExpectedContract.GrandSlam] < 2 ? CorrectnessContractBreakdown.SmallSlamHopeless : CorrectnessContractBreakdown.SmallSlamTooHigh;

            return CorrectnessContractBreakdown.GameCorrect;

            static (ExpectedContract expectedContract, Dictionary<ExpectedContract, int> confidence) GetExpectedContract(IEnumerable<int> scores)
            {
                ExpectedContract expectedContract;
                if (scores.Count(x => x == 13) / (double)scores.Count() > .6)
                    expectedContract = ExpectedContract.GrandSlam;
                else if (scores.Count(x => x == 12) / (double)scores.Count() > .6)
                    expectedContract = ExpectedContract.SmallSlam;
                else if (scores.Count(x => x == 12 || x == 13) / (double)scores.Count() > .6)
                    expectedContract = scores.Count(x => x == 12) >= scores.Count(x => x == 13) ? ExpectedContract.SmallSlam : ExpectedContract.GrandSlam;
                else expectedContract = ExpectedContract.Game;

                return (expectedContract, new Dictionary<ExpectedContract, int> {
                {ExpectedContract.GrandSlam, scores.Count(x => x == 13) },
                { ExpectedContract.SmallSlam, scores.Count(x => x == 12) },
                { ExpectedContract.Game, scores.Count(x => x < 12)}});
            }
        }
    }
}