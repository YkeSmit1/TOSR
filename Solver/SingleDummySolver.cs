using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Common;

namespace Solver
{
    public class SingleDummySolver
    {
        public static List<int> SolveSingleDummy(int trumpSuit, int declarer, string northHand, string southHand)
        {
            var handsForSolver = GetHandsForSolver(northHand, southHand, 10).ToArray();
            var scores = Api.SolveAllBoards(handsForSolver, trumpSuit, declarer).ToList();
            for (int i = 0; i < scores.Count; i++)
            {
                Console.WriteLine($"Deal: {handsForSolver[i]} Nr of tricks:{scores[i]}");
            }
            return scores;
        }

        private static IEnumerable<string> GetHandsForSolver(string northHandStr, string southHandStr, int nrOfHands)
        {
            var northHand = northHandStr.Split(',');
            var southHand = southHandStr.Split(',');
            var northHandCards = GetCardDtosFromString(northHand);

            for (int i = 0; i < nrOfHands; i++)
            {
                // Also randomize partners hand
                var southHandCards = GetCardDtosFromStringWithx(southHand, northHand);
                // Shuffle
                var deal = Shuffling.FisherYates(northHandCards, southHandCards).ToList();
                var handStrs = GetDealAsString(deal);
                yield return handStrs.Aggregate("W:", (current, hand) => current + hand.handStr.Replace(',', '.') + " ");
            }
        }

        public static List<int> SolveSingleDummy2(int trumpSuit, int declarer)
        {
            var handsForSolver = GetHandsForSolver2(10);
            //var scores = Api.SolveAllBoards(handsForSolver, trumpSuit, declarer).ToList();
            //for (int i = 0; i < scores.Count; i++)
            //{
            //    Console.WriteLine($"Deal: {handsForSolver[i]} Nr of tricks:{scores[i]}");
            //}
            return new List<int>();
        }

        private static string[] GetHandsForSolver2(int nrOfHands)
        {
            var directory = "C:/Users/Administrator/Downloads/andrews-deal-master/andrews-deal-master";
            //Environment.CurrentDirectory = directory;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = directory;
            startInfo.FileName = Path.Combine(directory, "Dealer.exe");
            startInfo.UseShellExecute = false;
            startInfo.Arguments = "-i format/pbn -i ex/custom.tcl " + nrOfHands.ToString() + $" >> {directory}/deal.txt";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            Process process = new Process { StartInfo = startInfo };

            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception($"Dealer has incorrect exit code: {process.ExitCode}");
            }
            return null;
            //return process.StandardOutput.ReadToEnd().Split("\n").Where(x => x.StartsWith("[")).Select(x => x.Substring(6, 71)).ToArray();
        }

        private static IEnumerable<(Player player, string handStr)> GetDealAsString(IEnumerable<CardDto> deal)
        {
            foreach (Player player in Enum.GetValues(typeof(Player)))
            {
                if (player == Player.UnKnown)
                    continue;
                var cardsPlayer = player switch
                {
                    Player.West => deal.Skip(26).Take(13),
                    Player.North => deal.Take(13),
                    Player.East => deal.Skip(39).Take(13),
                    Player.South => deal.Skip(13).Take(13),
                    _ => throw new ArgumentException(nameof(player)),
                };
                var orderedCards = cardsPlayer.OrderByDescending(x => x.Suit).ThenByDescending(c => c.Face, new FaceComparer());
                var dealStr = Util.GetDeckAsString(orderedCards);
                yield return (player, dealStr);
            }
        }

        private static IEnumerable<CardDto> GetCardDtosFromString(string[] hand)
        {
            for (var suit = 0; suit < hand.Count(); ++suit)
                foreach (var card in hand[suit])
                {
                    yield return new CardDto() { Suit = (Suit)(3 - suit), Face = Util.GetFaceFromDescription(card) };
                }
        }

        private static IEnumerable<CardDto> GetCardDtosFromStringWithx(string[] hand, string[] partnersHand)
        {
            var random = new Random();

            for (var suit = 0; suit < hand.Count(); ++suit)
            {
                var remainingCards = Enumerable.Range(1, 10).Where(x => !partnersHand[suit].Contains(Util.GetFaceDescription((Face)x))).Select(x => (Face)x).ToList();
                foreach (var card in hand[suit])
                {
                    Face face;
                    if (card != 'x')
                    {
                        face = Util.GetFaceFromDescription(card);
                    }
                    else
                    {
                        var c = random.Next(0, remainingCards.Count());
                        face = remainingCards.ElementAt(c);
                        remainingCards.RemoveAt(c);
                    }
                    yield return new CardDto() { Suit = (Suit)(3 - suit), Face = face };
                }
            }
        }
    }
}
