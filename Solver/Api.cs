using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    class Api
    {
        private static void SolveBoardsST(string hand)
        {
            Console.WriteLine($"Nr of tricks:{SolveBoardPBN(hand)}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hands">new[] { "N:T984.AK96.KQJ9.4 Q652.QJT53.T3.AT AKJ73.7.752.KJ62 .842.A864.Q98753" ,
        /// "N:KT98.AK96.J964.4 Q652.QJT53.T3.AT AJ743.7.752.KJ62 .842.AKQ8.Q98753"}</param>
        private static IEnumerable<int> SolveBoardsMT(IEnumerable<string> hands)
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

            var ddsResult = new FutureTricks();

            var res = Pinvoke.SolveBoardPBN(deal, target, solution, mode, ref ddsResult, 0);
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
            var boardsBPN = new BoardsPBN
            {
                noOfBoards = 2,
                dealsPBN = hands.Select(CreateDeal).ToArray(),
                targets = Enumerable.Repeat(-1, nrOfHands).ToArray(),
                solutions = Enumerable.Repeat(1, nrOfHands).ToArray(),
                modes = Enumerable.Repeat(0, nrOfHands).ToArray()
            };

            var solvedBoards = new SolvedBoards
            {
                noOfBoards = nrOfHands
            };

            FutureTricks futureTricks1 = new FutureTricks();
            FutureTricks futureTricks2 = new FutureTricks();
            var ddsResults = new[] { futureTricks1, futureTricks2 };
            solvedBoards.solvedBoards = ddsResults;

            IntPtr boardsBPNPtr = Marshal.AllocHGlobal(Marshal.SizeOf(boardsBPN));
            Marshal.StructureToPtr(boardsBPN, boardsBPNPtr, false);

            int cb = Marshal.SizeOf(solvedBoards);
            IntPtr solvedBoardsPtr = Marshal.AllocHGlobal(cb);

            var res = Pinvoke.SolveAllBoards(boardsBPNPtr, solvedBoardsPtr);
            solvedBoards = Marshal.PtrToStructure<SolvedBoards>(solvedBoardsPtr);

            if (res != 1)
            {
                var line = new StringBuilder(80);
                Pinvoke.ErrorMessage(res, line);
                throw new Exception(line.ToString());
            }
            foreach (var solvedBoard in solvedBoards.solvedBoards)
            {
                yield return solvedBoard.score[0];
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
