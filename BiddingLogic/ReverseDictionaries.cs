using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using NLog;
using Newtonsoft.Json;
using System.Collections.Immutable;
using Solver;
using Common;
using Common.Tosr;

namespace BiddingLogic
{
    using ShapeDictionary = Dictionary<string, (List<string> pattern, bool zoom)>;
    using ControlsOnlyDictionary = Dictionary<string, List<int>>;
    using ControlScanningDictionary = Dictionary<string, (List<string> controlsScanning, bool zoom)>;
    using SignOffFasesDictionary = Dictionary<Fase, Dictionary<string, List<int>>>;
    using QueensDictionary = Dictionary<string, string>;

    public class ListComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y) => (x ?? Array.Empty<T>()).SequenceEqual(y ?? Array.Empty<T>());
        public int GetHashCode(IEnumerable<T> obj) => 0;
    }

    public class ReverseDictionaries
    {
        public ShapeDictionary ShapeAuctions { get; }
        public ControlsOnlyDictionary ControlsOnlyAuctions { get; }
        private ControlScanningDictionary ControlScanningAuctions0 { get; }
        private ControlScanningDictionary ControlScanningAuctions1 { get; }
        private ControlScanningDictionary ControlScanningAuctions2 { get; }
        public SignOffFasesDictionary SignOffFasesAuctions { get; }
        private QueensDictionary QueensAuction0 { get; }
        private QueensDictionary QueensAuction1 { get; }
        private QueensDictionary QueensAuction2 { get; }

        private readonly Dictionary<Fase, bool> fasesWithOffset;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly int[] SuitLengthNoSingleton = { 4, 3, 3, 3 };
        private static readonly int[] SuitLengthSingleton = { 5, 4, 3, 1 };
        private static readonly int[] SuitLength2Singletons = { 6, 5, 1, 1 };

        public ReverseDictionaries(ShapeDictionary shapeAuctions, ControlsOnlyDictionary controlsOnlyAuctions,
            ControlScanningDictionary controlScanningAuctions, SignOffFasesDictionary signOffFasesAuctions)
        {
            ShapeAuctions = shapeAuctions;
            ControlsOnlyAuctions = controlsOnlyAuctions;
            ControlScanningAuctions0 = controlScanningAuctions;
            SignOffFasesAuctions = signOffFasesAuctions;
        }

        public ReverseDictionaries(Dictionary<Fase, bool> fasesWithOffset, IProgress<string> progress)
        {
            this.fasesWithOffset = fasesWithOffset;

            progress.Report(nameof(ShapeAuctions));
            ShapeAuctions = LoadAuctions("txt\\AuctionsByShape.txt", GenerateAuctionsForShape, 0);
            progress.Report(nameof(ControlsOnlyAuctions));
            ControlsOnlyAuctions = LoadAuctions("txt\\AuctionsByControlsOnly.txt", GenerateAuctionsForControlsOnly, 0);

            progress.Report(nameof(ControlScanningAuctions0));
            ControlScanningAuctions0 = LoadAuctions("txt\\AuctionsByControlsScanning0.txt", GenerateAuctionsForControlsScanning, 0);
            progress.Report(nameof(ControlScanningAuctions1));
            ControlScanningAuctions1 = LoadAuctions("txt\\AuctionsByControlsScanning1.txt", GenerateAuctionsForControlsScanning, 1);
            progress.Report(nameof(ControlScanningAuctions2));
            ControlScanningAuctions2 = LoadAuctions("txt\\AuctionsByControlsScanning2.txt", GenerateAuctionsForControlsScanning, 2);

            progress.Report(nameof(SignOffFasesAuctions));
            SignOffFasesAuctions = LoadAuctions("txt\\AuctionsBySignOffFases.txt", GenerateAuctionsForSignOffFases, 0);

            progress.Report(nameof(QueensAuction0));
            QueensAuction0 = LoadAuctions("txt\\AuctionsByQueen0.txt", GenerateQueensDictionary, 0);
            progress.Report(nameof(QueensAuction1));
            QueensAuction1 = LoadAuctions("txt\\AuctionsByQueen1.txt", GenerateQueensDictionary, 1);
            progress.Report(nameof(QueensAuction2));
            QueensAuction2 = LoadAuctions("txt\\AuctionsByQueen2.txt", GenerateQueensDictionary, 2);
            progress.Report("done");
        }

        private static Dictionary<T, TU> LoadAuctions<T, TU>(string fileName, Func<int, Dictionary<T, TU>> generateAuctions, int nrOfShortage)
        {
            var logger = LogManager.GetCurrentClassLogger();

            Dictionary<T, TU> auctions;
            // Generate only if file does not exist or is older then one day
            if (File.Exists(fileName) && File.GetLastWriteTime(fileName) > DateTime.Now - TimeSpan.FromDays(1))
            {
                auctions = JsonConvert.DeserializeObject<Dictionary<T, TU>>(File.ReadAllText(fileName));
            }
            else
            {
                logger.Info($"File {fileName} is too old or does not exist. File will be generated");
                auctions = generateAuctions(nrOfShortage);
                var sortedAuctions = auctions.ToImmutableSortedDictionary();
                var path = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrWhiteSpace(path))
                    Directory.CreateDirectory(path);
                File.WriteAllText(fileName, JsonConvert.SerializeObject(sortedAuctions, Formatting.Indented));
            }
            return auctions;
        }

        private ShapeDictionary GenerateAuctionsForShape(int nrOfShortages)
        {
            Logger.Info("Generating dictionaries for shape");
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ShapeDictionary();
            var regex = new Regex("x");

            for (int spades = 0; spades < 8; spades++)
                for (int hearts = 0; hearts < 8; hearts++)
                    for (int diamonds = 0; diamonds < 8; diamonds++)
                        for (int clubs = 0; clubs < 8; clubs++)
                            if (spades + hearts + diamonds + clubs == 13)
                            {
                                var hand = new string('x', spades) + "," + new string('x', hearts) + "," + new string('x', diamonds) + "," + new string('x', clubs);
                                // We need a hand with two controls. Otherwise engine cannot find a bid
                                hand = regex.Replace(hand, "A", 1);
                                var suitLengthSouth = hand.Split(',').Select(x => x.Length).ToList();
                                var str = string.Join("", suitLengthSouth);

                                if (!UtilTosr.IsFreakHand(suitLengthSouth))
                                {
                                    _ = bidManager.GetAuction(string.Empty, hand); // No north hand. Just for generating reverse dictionaries
                                    var isZoom = bidManager.BiddingState.IsZoomShape;
                                    var key = bidManager.BiddingState.GetBidsAsString(Fase.Shape);
                                    if (auctions.ContainsKey(key))
                                        auctions[key].pattern.Add(str);
                                    else
                                        auctions.Add(key, (new List<string> { str }, isZoom));
                                }
                            }
            return auctions;
        }

        private ControlsOnlyDictionary GenerateAuctionsForControlsOnly(int nrOfShortages)
        {
            Logger.Info("Generating dictionaries for controls only");
            var auctions = new ControlsOnlyDictionary();
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);

            var shufflingDeal = new ShufflingDeal
            {
                NrOfHands = 1,
                South = new South { Shape = "4333" }
            };

            foreach (var control in Enumerable.Range(2, 9))
            {
                if (control == 4)
                {
                    shufflingDeal.South.Hcp = new MinMax(0, 11);
                    BidAndStoreHand(control);
                    shufflingDeal.South.Hcp = new MinMax(12, 37);
                    BidAndStoreHand(control);
                    shufflingDeal.South.Hcp = null;
                }
                else
                    BidAndStoreHand(control);
            }
            // Generate entries for one ask control(s)
            var oneAskAuction = new ControlsOnlyDictionary();
            foreach (var auction in auctions.Keys)
            {
                if (auction.Length == 4)
                {
                    var key = auction.Substring(0, 2);
                    if (!oneAskAuction.ContainsKey(key))
                        oneAskAuction.Add(key, new List<int>());
                    oneAskAuction[key].Add(auctions[auction].First());
                }
            }
            return auctions.Concat(oneAskAuction).ToDictionary(pair => pair.Key, pair => pair.Value);

            void BidAndStoreHand(int control)
            {
                shufflingDeal.South.Controls = new MinMax(control, control);
                var board = Util.GetBoardsTosr(shufflingDeal.Execute().First());

                _ = bidManager.GetAuction(string.Empty, board[Player.South]); // No north hand. Just for generating reverse dictionaries
                auctions.Add(bidManager.BiddingState.GetBidsAsString(Fase.Controls), new List<int> { control });
            }
        }

        private ControlScanningDictionary GenerateAuctionsForControlsScanning(int nrOfShortages)
        {
            Logger.Info($"Generating dictionaries for controlsScanning. Shortages:{nrOfShortages}");
            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new ControlScanningDictionary();
            var controls = new[] { "", "A", "K", "AK" };

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                            BidAndStoreHandsByHcp(GetSuitLength(nrOfShortages), spades, hearts, diamonds, clubs);
            return auctions;

            void BidAndStoreHandsByHcp(int[] suitLength, params string[] suits)
            {
                var lSuits = ObjectCloner.ObjectCloner.DeepClone(suits);
                var hand = ConstructHand(SuitLengthNoSingleton, lSuits);
                if (Util.GetControlCount(hand) > 1)
                {
                    foreach (var hcp in GetHcpGeneratorGeneral().Invoke(hand))
                    {
                        if (UtilTosr.TryAddQuacksTillHcp(hcp, ref lSuits, suitLength))
                            BidAndStoreHand(ConstructHand(suitLength, lSuits), hand);
                    }
                }
            }

            void BidAndStoreHand(string hand, string handToStore)
            {
                if (hand.Length != 16)
                    return;
                _ = bidManager.GetAuction(string.Empty, hand);// No north hand. Just for generating reverse dictionaries
                var key = string.Join("", bidManager.BiddingState.GetBids(Fase.Controls, Fase.ScanningControls).
                    Select(bid => bid - (bidManager.BiddingState.GetBids(Fase.Shape).Last() - Bids.ThreeDiamondBid)));
                if (!auctions.ContainsKey(key))
                    auctions.Add(key, (new List<string>() { handToStore }, bidManager.BiddingState.IsZoomControlScanning));
                else if (!auctions[key].controlsScanning.Contains(handToStore))
                    auctions[key].controlsScanning.Add(handToStore);
            }
        }

        private SignOffFasesDictionary GenerateAuctionsForSignOffFases(int nrOfShortages)
        {
            Logger.Info("Generating dictionaries for sign-off fases");
            var signOffFasesAuctions = new SignOffFasesDictionary();
            var shuffleRestriction = new ShufflingDeal() { South = new South { Controls = new MinMax(2, 12) } };
            foreach (var fase in BiddingState.SignOffFases)
            {
                var dictionaryForFase = new ControlsOnlyDictionary();
                foreach (var hcp in Enumerable.Range(8, 15))
                {
                    shuffleRestriction.South.Hcp = new MinMax(hcp, hcp);
                    var board = Util.GetBoardsTosr(shuffleRestriction.Execute().First());
                    var bidFromRule = PInvoke.GetBidFromRule(fase, Fase.Controls, board[Player.South], 0, out _, out _);
                    if (bidFromRule != 0)
                    {
                        var bidStr = BiddingState.GetSignOffBid(fase, Bids.ThreeNTBid + bidFromRule).ToString();
                        if (!dictionaryForFase.ContainsKey(bidStr))
                            dictionaryForFase.Add(bidStr, new List<int>());
                        if (!dictionaryForFase[bidStr].Contains(hcp))
                            dictionaryForFase[bidStr].Add(hcp);
                    }
                }
                signOffFasesAuctions.Add(fase, dictionaryForFase);
            }
            return signOffFasesAuctions;
        }

        private QueensDictionary GenerateQueensDictionary(int nrOfShortages)
        {
            Logger.Info("Generating dictionaries for queens");

            var bidManager = new BidManager(new BidGenerator(), fasesWithOffset);
            var auctions = new Dictionary<IEnumerable<Bid>, string>(new ListComparer<Bid>());
            var partialAuctions = new List<(IEnumerable<Bid> auction, string queens)>();
            var controls = new[] { "", "Q" };

            foreach (var spades in controls)
                foreach (var hearts in controls)
                    foreach (var diamonds in controls)
                        foreach (var clubs in controls)
                        {
                            var hand = ConstructHand(GetSuitLength(nrOfShortages), "A" + spades, hearts, diamonds, clubs);
                            BidAndStoreHand(hand, spades, hearts, diamonds, clubs);
                        }

            // Add partial auctions
            ExtractPartialAuctions();
            AddPartialAuctions();

            return auctions.ToDictionary(key => string.Join("", key.Key), value => value.Value);

            void BidAndStoreHand(string hand, params string[] suits)
            {
                _ = bidManager.GetAuction(string.Empty, hand);// No north hand. Just for generating reverse dictionaries
                var key = bidManager.BiddingState.GetBids(Fase.ScanningOther).ToList();
                if (auctions.TryGetValue(key, out var value))
                {
                    var queenStr = string.Empty;
                    foreach (var i in Enumerable.Range(0, 4))
                        queenStr += value[i] == (suits[i] == "Q" ? 'Y' : 'N') ? value[i] : 'X';
                    auctions[key] = queenStr;

                }
                else
                    auctions.Add(key, suits.Aggregate(string.Empty, (sum, queen) => sum + (queen == "Q" ? 'Y' : 'N')));
            }

            void ExtractPartialAuctions()
            {
                foreach (var auction in auctions)
                {
                    var counter = 1;
                    while (auction.Key.SkipLast(counter).Any())
                    {
                        partialAuctions.Add((auction.Key.SkipLast(counter), auction.Value));
                        counter++;
                    }
                }
            }

            void AddPartialAuctions()
            {
                foreach (var group in partialAuctions.GroupBy(key => key.auction, new ListComparer<Bid>()))
                {
                    var values = group.ToList().Select(x => x.queens).ToList();
                    var queenStr = Enumerable.Range(0, 4).Aggregate(string.Empty, (current, i) => current + (values.All(x => x[i] == values.First()[i]) ? values.First()[i] : 'X'));
                    auctions.Add(group.Key, queenStr);
                }
            }
        }


        private static int[] GetSuitLength(int nrOfShortages)
        {
            return nrOfShortages switch
            {
                0 => SuitLengthNoSingleton,
                1 => SuitLengthSingleton,
                2 => SuitLength2Singletons,
                _ => throw new ArgumentOutOfRangeException(nameof(nrOfShortages), "nrOfShortages should be 0, 1 or 2")
            };
        }

        private static Func<string, int[]> GetHcpGeneratorGeneral()
        {
            return (hand) => Util.GetControlCount(hand) == 4 && Util.GetHcpCount(hand) < 12 ? (new[] { 0, 12 }) : (new[] { 0 });
        }

        private static string ConstructHand(int[] suitLength, params string[] suits)
        {
            return string.Join(',', suitLength.Zip(suits, (x, y) => y.PadRight(x, 'x')));
        }

        public QueensDictionary GetQueensDictionary(string handShape)
        {
            var n = UtilTosr.NrOfShortages(handShape);
            var queensAuctions = n switch
            {
                0 => QueensAuction0,
                1 => QueensAuction1,
                2 => QueensAuction2,
                _ => throw new ArgumentException("nrOfShortages should be 0, 1 or 2", nameof(handShape)),
            };
            return queensAuctions;
        }

        public ControlScanningDictionary GetControlScanningDictionary(string handShape)
        {
            return UtilTosr.NrOfShortages(handShape) switch
            {
                0 => ControlScanningAuctions0,
                1 => ControlScanningAuctions1,
                2 => ControlScanningAuctions2,
                _ => throw new ArgumentException("nrOfShortages should be 0, 1 or 2", nameof(handShape)),
            };
        }

        public static Bid GetOffsetBidForQueens(string shapeStr)
        {
            // TODO this should be determined when creating queens dictionaries
            return UtilTosr.NrOfShortages(shapeStr) switch
            {
                0 => Bids.FiveHeartsBid,
                1 => Bids.FiveClubBid,
                2 => Bids.FiveDiamondBid,
                _ => throw new ArgumentException("Unsupported number of shortages", nameof(shapeStr)),
            };
        }
    }
}