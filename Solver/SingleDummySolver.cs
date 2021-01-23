using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace Solver
{
    public class SingleDummySolver
    {
        public static List<int> SolveSingleDummyExactHands(Suit trumpSuit, Player declarer, string northHand, string southHand)
        {
            var handsForSolver = GetHandsForSolverExactHands(northHand, southHand).ToArray();
            return Api.SolveAllBoards(handsForSolver, Util.GetDDSSuit(trumpSuit), Util.GetDDSFirst(declarer)).ToList();
        }

        private static IEnumerable<string> GetHandsForSolverExactHands(string northHandStr, string southHandStr)
        {
            var shufflingDeal = new ShufflingDeal
            {
                North = new North { Hand = northHandStr.Split(',') },
                South = new South { Hand = southHandStr.Split(',') }
            };
            return shufflingDeal.Execute();
        }

        public static List<int> SolveSingleDummy(Suit trumpSuit, Player declarer, string northHand, string southHandShape, int minControls, int maxControls)
        {
            var handsForSolver = GetHandsForSolver(northHand, southHandShape, minControls, maxControls).ToArray();
            return Api.SolveAllBoards(handsForSolver, Util.GetDDSSuit(trumpSuit), Util.GetDDSFirst(declarer)).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="northHandStr">Whole northhand. Cannot contain x's</param>
        /// <param name="southHandShape">For example 5431</param>
        /// <param name="minControls">Number of controls in the southhand</param>
        /// <param name="nrOfHands"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetHandsForSolver(string northHandStr, string southHandShape, int minControls, int maxControls)
        {
            var shufflingDeal = new ShufflingDeal
            {
                North = new North { Hand = northHandStr.Split(',')},
                South = new South { Shape = southHandShape, Controls = new MinMax(minControls, maxControls) },
            };
            if (minControls == 2)
                shufflingDeal.South.Hcp = new MinMax(8, 37);

            return shufflingDeal.Execute().ToArray();
        }

        public static List<int> SolveSingleDummy(Suit trumpSuit, Player declarer, string northHand, string southHand, MinMax hcp, string queens)
        {
            var handsForSolver = GetHandsForSolver(northHand, southHand, hcp, queens).ToArray();
            return Api.SolveAllBoards(handsForSolver, Util.GetDDSSuit(trumpSuit), Util.GetDDSFirst(declarer)).ToList();
        }

        private static IEnumerable<string> GetHandsForSolver(string northHandStr, string southHandStr, MinMax hcp, string queens)
        {
            var southHand = southHandStr.Split(',');
            var controlsSpecific = southHand.Select(x => Regex.Match(x, "[AK]").ToString()).ToArray();
            var controls = Util.GetControlCount(southHandStr);

            var shufflingDeal = new ShufflingDeal
            {
                North = new North { Hand = northHandStr.Split(',') },
                South = new South
                {
                    Shape = string.Join("", southHand.Select(x => x.Length.ToString())),
                    Hcp = hcp,
                    Controls = new MinMax(controls, controls),
                    SpecificControls = controlsSpecific,
                    Queens = queens
                }
            };
            if (controls == 2 && shufflingDeal.South.Hcp == null)
                shufflingDeal.South.Hcp = new MinMax(8, 37);

            return shufflingDeal.Execute();
        }

    }
}
