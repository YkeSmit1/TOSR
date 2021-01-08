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

namespace Tosr
{
    public static class DictionaryExtension 
    {
        public static void AddOrUpdateDictionary<T>(this IDictionary<T, int> dictionary, T item)
        {
            if (!dictionary.ContainsKey(item))
                dictionary.Add(item, 1);
            else
                dictionary[item]++;
        }
    }
    public class BatchBidding
    {
        public enum CorrectnessContractBreakdown
        {
            WrongTrumpSuit,
            GameCorrect,
            MissedSmallSlam,
            SmallSlamCorrect,
            SmallSlamTooHigh,
            MissedGrandSlam,
            GrandSlamCorrect,
            GrandSlamTooHigh,
            NoFit,
            Unknonwn,
        }

        public enum CorrectnessContract
        {
            InCorrect,
            GameCorrect,
            SmallSlamCorrect,
            GrandSlamCorrect,
            NoFit,
        }


        class Statistics
        {
            public int handsBid;
            public int handsNotBidBecauseofFreakhand = 0;
            public int handsNotBidBecauseOfError = 0;
            public SortedDictionary<Bid, int> contracts = new SortedDictionary<Bid, int>();
            public SortedDictionary<BidManager.ConstructedSouthhandOutcome, int> outcomes = new SortedDictionary<BidManager.ConstructedSouthhandOutcome, int>();
            public SortedDictionary<Player, int> dealers = new SortedDictionary<Player, int>();
            public SortedDictionary<int, int> bidsNonShape = new SortedDictionary<int, int>();
            public SortedDictionary<(CorrectnessContractBreakdown, BidManager.ConstructedSouthhandOutcome), int> ContractCorrectnessBreakdownOutcome = 
                new SortedDictionary<(CorrectnessContractBreakdown, BidManager.ConstructedSouthhandOutcome), int>();
            public SortedDictionary<CorrectnessContract, int> ContractCorrectness = new SortedDictionary<CorrectnessContract, int>();
            public SortedDictionary<CorrectnessContractBreakdown, int> ContractCorrectnessBreakdown = new SortedDictionary<CorrectnessContractBreakdown, int>();

        }

        private readonly Statistics statistics = new Statistics();
        private readonly Dictionary<string, List<string>> handPerAuction = new Dictionary<string, List<string>>();
        private readonly StringBuilder expectedSouthHands = new StringBuilder();
        private readonly StringBuilder inCorrectContracts = new StringBuilder();
        private readonly BidManager bidManager;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly bool useSingleDummySolver;

        public BatchBidding(ReverseDictionaries reverseDictionaries, Dictionary<Fase, bool> fasesWithOffset, bool useSingleDummySolver)
        {
            bidManager = new BidManager(new BidGenerator(), fasesWithOffset, reverseDictionaries, useSingleDummySolver);
            this.useSingleDummySolver = useSingleDummySolver;
        }

        public Pbn Execute(IEnumerable<string[]> boards, IProgress<int> progress)
        {
            var pbn = new Pbn();
            handPerAuction.Clear();

            var stopwatch = Stopwatch.StartNew();
            var stringbuilder = new StringBuilder();

            if (boards == null || boards.Count() == 0)
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
                    pbn.Boards.Add(new BoardDto { 
                        Deal = board, 
                        Auction = auction, 
                        BoardNumber = statistics.handsBid, 
                        Event = "TOSR Batchbidding", 
                        Date = DateTime.Now, 
                        Declarer = auction.GetDeclarerOrNorth(auction.currentContract.suit),
                        Dealer = Player.West,
                        Vulnerable = "None"});

                    AddHandAndAuction(board, auction, statistics.handsBid);
                    if (statistics.handsBid % 100 == 0)
                        progress.Report(statistics.handsBid);
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
                expectedSouthHands.AppendLine(bidManager.ConstructSouthHandSafe(board, auction));

            var longestSuit = Util.GetLongestSuit(board[(int)Player.North], board[(int)Player.South]);
            var dealer = auction.GetDeclarer(3 - longestSuit.Item1);
            statistics.dealers.AddOrUpdateDictionary(dealer);
            var contract = auction.currentContract > new Bid(7, Suit.NoTrump) ? new Bid(7, Suit.NoTrump) : auction.currentContract;
            statistics.contracts.AddOrUpdateDictionary(contract);
            if (!auction.responderHasSignedOff)
                statistics.bidsNonShape.AddOrUpdateDictionary(auction.GetBids(Player.South).Where(bid => bid.bidType == BidType.bid).Last() - auction.GetBids(Player.South, Fase.Shape).Last());
            statistics.outcomes.AddOrUpdateDictionary(bidManager.constructedSouthhandOutcome);
            var correctnessContractBreakdown = CheckContract(contract, board, dealer == Player.UnKnown ? Player.North : dealer);
            statistics.ContractCorrectnessBreakdownOutcome.AddOrUpdateDictionary((correctnessContractBreakdown, bidManager.constructedSouthhandOutcome));
            var correctnessContract = GetCorrectness(correctnessContractBreakdown);
            statistics.ContractCorrectnessBreakdown.AddOrUpdateDictionary(correctnessContractBreakdown);
            statistics.ContractCorrectness.AddOrUpdateDictionary(correctnessContract);
            if (correctnessContract == CorrectnessContract.InCorrect || correctnessContract == CorrectnessContract.NoFit)
                inCorrectContracts.AppendLine($"({correctnessContractBreakdown}, {bidManager.constructedSouthhandOutcome}) Board:{boardNumber} Contract:{auction.currentContract}" +
                    $" Auction:{auction.GetPrettyAuction("|")} Northhand: {board[(int)Player.North]} Southhand: {board[(int)Player.South]}");

        }

        private CorrectnessContract GetCorrectness(CorrectnessContractBreakdown correctnessContractBreakdown)
        {
            switch (correctnessContractBreakdown)
            {
                case CorrectnessContractBreakdown.WrongTrumpSuit:
                case CorrectnessContractBreakdown.MissedSmallSlam:
                case CorrectnessContractBreakdown.SmallSlamTooHigh:
                case CorrectnessContractBreakdown.MissedGrandSlam:
                case CorrectnessContractBreakdown.GrandSlamTooHigh:
                case CorrectnessContractBreakdown.Unknonwn:
                    return CorrectnessContract.InCorrect;
                case CorrectnessContractBreakdown.GameCorrect:
                    return CorrectnessContract.GameCorrect;
                case CorrectnessContractBreakdown.SmallSlamCorrect:
                    return CorrectnessContract.SmallSlamCorrect;
                case CorrectnessContractBreakdown.GrandSlamCorrect:
                    return CorrectnessContract.GrandSlamCorrect;
                case CorrectnessContractBreakdown.NoFit:
                    return CorrectnessContract.NoFit;

                default:
                    throw new ArgumentException(nameof(correctnessContractBreakdown));
            }
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
            if (!useSingleDummySolver)
                return CorrectnessContractBreakdown.Unknonwn;
            var northHand = board[(int)Player.North];
            var southHand = board[(int)Player.South];
            if (contract.suit != Suit.NoTrump && Util.GetNumberOfTrumps(contract.suit, northHand, southHand) < 8)
                return CorrectnessContractBreakdown.NoFit;
            var tricks = SingleDummySolver.SolveSingleDummy(contract.suit, declarer, northHand, southHand);
            var expectedContractType = Util.GetExpectedContract(tricks);

            if (expectedContractType == ExpectedContract.GrandSlam && contract.rank == 7)
                return CorrectnessContractBreakdown.GrandSlamCorrect;
            if (expectedContractType == ExpectedContract.GrandSlam && contract.rank < 7)
                return CorrectnessContractBreakdown.MissedGrandSlam;

            if (expectedContractType == ExpectedContract.SmallSlam && contract.rank == 6)
                return CorrectnessContractBreakdown.SmallSlamCorrect;
            if (expectedContractType == ExpectedContract.SmallSlam && contract.rank < 6)
                return CorrectnessContractBreakdown.MissedSmallSlam;

            if (expectedContractType != ExpectedContract.GrandSlam && contract.rank == 7)
                return CorrectnessContractBreakdown.GrandSlamTooHigh;
            if (expectedContractType != ExpectedContract.SmallSlam && contract.rank == 6)
                return CorrectnessContractBreakdown.SmallSlamTooHigh;

            return CorrectnessContractBreakdown.GameCorrect;
        }
    }
}