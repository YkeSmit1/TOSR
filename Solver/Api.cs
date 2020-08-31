using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    public class Api
    {
        public static void SolveBoardsST(string hand)
        {
            Console.WriteLine($"Nr of tricks:{SolveBoardPBN(hand)}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hands">new[] { "N:T984.AK96.KQJ9.4 Q652.QJT53.T3.AT AKJ73.7.752.KJ62 .842.A864.Q98753" ,
        /// "N:KT98.AK96.J964.4 Q652.QJT53.T3.AT AJ743.7.752.KJ62 .842.AKQ8.Q98753"}</param>
        public static IEnumerable<int> SolveBoardsMT(IEnumerable<string> hands)
        {
            try
            {
                return hands.AsParallel().WithDegreeOfParallelism(2).Select(SolveBoardPBN);
            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"Exception: {ex.Flatten().Message}");
                throw;
            }
        }

        /// <summary>
        /// Solve a hand dd
        /// </summary>
        /// <param name="hand">Example"N:AT5.AJT.A632.KJ7 Q763.KQ9.KQJ94.T 942.87653..98653 KJ8.42.T875.AQ42"</param>
        /// <returns></returns>
        public static int SolveBoardPBN(string hand)
        {
            DealPbn deal = CreateDeal(hand);

            var target = -1; // max nr of tricks
            var solution = 1; // one solution;
            var mode = 0; // automatic. Fastest;

            var res = Pinvoke.SolveBoardPBN(deal, target, solution, mode, out var ddsResult, 0);
            if (res != 1)
            {
                var line = new StringBuilder(80);
                Pinvoke.ErrorMessage(res, line);
                throw new Exception(line.ToString());
            }
            return ddsResult.score[0];
        }

        /// <summary>
        /// <param name="hands">new[] { "N:T984.AK96.KQJ9.4 Q652.QJT53.T3.AT AKJ73.7.752.KJ62 .842.A864.Q98753" ,
        /// "N:KT98.AK96.J964.4 Q652.QJT53.T3.AT AJ743.7.752.KJ62 .842.AKQ8.Q98753"}</param>
        /// </summary>
        public static IEnumerable<int> SolveAllBoards(IEnumerable<string> hands)
        {
            var nrOfHands = hands.Count();

            var dealsPBN = new DealPbn[BoardsPBN.MAXNOOFBOARDS];
            for (var i = 0; i < nrOfHands; i++)
            {
                dealsPBN[i] = CreateDeal(hands.ElementAt(i));
            }

            var boardsBPN = new BoardsPBN
            {
                noOfBoards = 2,
                dealsPBN = dealsPBN,
                targets = Enumerable.Repeat(-1, BoardsPBN.MAXNOOFBOARDS).ToArray(),
                solutions = Enumerable.Repeat(1, BoardsPBN.MAXNOOFBOARDS).ToArray(),
                modes = Enumerable.Repeat(0, BoardsPBN.MAXNOOFBOARDS).ToArray()
            };

            IntPtr boardsBPNPtr = Marshal.AllocHGlobal(Marshal.SizeOf(boardsBPN));
            Marshal.StructureToPtr(boardsBPN, boardsBPNPtr, false);

            var res = Pinvoke.SolveAllBoards(boardsBPNPtr, out var solvedBoards);

            if (res != 1)
            {
                var line = new StringBuilder(80);
                Pinvoke.ErrorMessage(res, line);
                throw new Exception(line.ToString());
            }

            for (var i = 0; i < solvedBoards.noOfBoards; i++)
            {
                yield return solvedBoards.solvedBoards[i].score[0];
            }
        }
        private static DealPbn CreateDeal(string hand)
        {
            hand = hand.PadRight(80, '\0');
            var deal = new DealPbn
            {
                trump = 1,
                first = 2,
                currentTrickRank = new int[3],
                currentTrickSuit = new int[3],
                remainCards = hand.ToCharArray()
            };
            return deal;
        }
    }
}
