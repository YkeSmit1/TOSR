using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Solver
{
    public static class Api
    {
        /// <summary>
        /// Solve a hand dd
        /// </summary>
        /// <param name="hand">Example"N:AT5.AJT.A632.KJ7 Q763.KQ9.KQJ94.T 942.87653..98653 KJ8.42.T875.AQ42"</param>
        /// <param name="trumpSuit"></param>
        /// <param name="first"></param>
        /// <returns></returns>
        public static int SolveBoardPbn(string hand, int trumpSuit, int first)
        {
            DealPbn deal = CreateDeal(hand, trumpSuit, first);

            var target = -1; // max nr of tricks
            var solution = 1; // one solution;
            var mode = 0; // automatic. Fastest;

            var res = Pinvoke.SolveBoardPbn(deal, target, solution, mode, out var ddsResult, 0);
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
        public static IEnumerable<int> SolveAllBoards(IEnumerable<string> hands, int trumpSuit, int first)
        {
            var handList = hands.ToList();
            var nrOfHands = handList.Count;

            var dealsPbn = new DealPbn[BoardsPbn.MaxNoOfBoards];
            for (var i = 0; i < nrOfHands; i++)
            {
                dealsPbn[i] = CreateDeal(handList.ElementAt(i), trumpSuit, first);
            }

            var boardsBpn = new BoardsPbn
            {
                noOfBoards = nrOfHands,
                dealsPbn = dealsPbn,
                targets = Enumerable.Repeat(-1, BoardsPbn.MaxNoOfBoards).ToArray(),
                solutions = Enumerable.Repeat(1, BoardsPbn.MaxNoOfBoards).ToArray(),
                modes = Enumerable.Repeat(0, BoardsPbn.MaxNoOfBoards).ToArray()
            };

            IntPtr boardsBpnPtr = Marshal.AllocHGlobal(Marshal.SizeOf(boardsBpn));
            Marshal.StructureToPtr(boardsBpn, boardsBpnPtr, false);

            var res = Pinvoke.SolveAllBoards(boardsBpnPtr, out var solvedBoards);

            if (res != 1)
            {
                var line = new StringBuilder(80);
                Pinvoke.ErrorMessage(res, line);
                throw new Exception(line.ToString());
            }

            for (var i = 0; i < solvedBoards.noOfBoards; i++)
            {
                yield return 13 - solvedBoards.solvedBoards[i].score[0];
            }
        }
        private static DealPbn CreateDeal(string hand, int trumpSuit, int first)
        {
            hand = hand.PadRight(80, '\0');
            var deal = new DealPbn
            {
                trump = trumpSuit,
                first = first,
                currentTrickRank = new int[3],
                currentTrickSuit = new int[3],
                remainCards = hand.ToCharArray()
            };
            return deal;
        }
    }
}
